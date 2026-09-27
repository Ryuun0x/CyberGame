using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Draws the phone map as flat illustrated artwork straight from scene geometry (no camera):
// every triangle is filled top-down with its unlit atlas colour, then outlines, drop shadows
// and cartoon tree canopies are painted on top.
public static class CityMapArtworkBaker
{
    const int Width = 4096;
    const byte Ground = 0, Road = 1, Building = 2, Prop = 3;
    static readonly string[] Skip = { "_lod1", "_lod2", "_lod3", "light", "lamp", "powerline", "cable", "wire", "pole", "traffic", "sign", "interior", "doghouse" };
    static readonly string[] Foliage = { "tree", "plant", "bush" };
    static readonly Color Backdrop = new Color32(142, 190, 104, 255);

    struct Canopy { public Vector2 center; public float radius; public float hue; }

    public static void Bake(Scene scene, CityMapLayout layout)
    {
        Vector2 size = layout.max - layout.min;
        int width = Width, height = Mathf.RoundToInt(Width * size.y / size.x);
        float scale = width / size.x;
        var color = new Color[width * height];
        var depth = new float[width * height];
        var kind = new byte[width * height];
        var owner = new int[width * height];
        for (int i = 0; i < depth.Length; i++) { depth[i] = float.MinValue; color[i] = Backdrop; }
        var atlases = new Dictionary<Texture, (Color32[] pixels, int w, int h)>();
        var canopies = new List<Canopy>();

        foreach (var root in scene.GetRootGameObjects())
        {
            // Cars and the player are not part of the permanent map.
            if (root.name == "Vehicles" || root.name == "Car" || root.name == "Player" || root.layer == 5) continue;
            byte rootKind = root.name == "Buildings" ? Building : root.name == "Road and Ground" ? Ground : Prop;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                string name = renderer.name.ToLowerInvariant();
                if (Contains(name, Skip) || renderer.GetComponentInParent<Animator>() != null) continue;
                var bounds = renderer.bounds;
                if (Contains(name, Foliage))
                {
                    float radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.85f, 0.6f, 5f);
                    canopies.Add(new Canopy { center = new Vector2(bounds.center.x, bounds.center.z), radius = radius,
                        hue = Mathf.Abs(name.GetHashCode() % 7) / 7f });
                    continue;
                }
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                byte k = rootKind == Ground && name.StartsWith("road") ? Road : rootKind;
                Rasterize(renderer, mesh, k, layout.min, scale, width, height, color, depth, kind, owner, atlases);
            }
        }

        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            texture.SetPixels(Stylize(color, depth, kind, owner, canopies, layout.min, scale, width, height));
            texture.Apply();
            const string path = "Assets/Resources/CityMapArtwork.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.isReadable = false;
            importer.SaveAndReimport();
            Debug.Log($"[CityMapArtwork] Drew illustrated city map {width} x {height}, {canopies.Count} canopies.");
        }
        finally { Object.DestroyImmediate(texture); }
    }

    static bool Contains(string name, string[] parts)
    {
        foreach (var part in parts) if (name.Contains(part)) return true;
        return false;
    }

    static void Rasterize(MeshRenderer renderer, Mesh mesh, byte k, Vector2 min, float scale, int width, int height,
        Color[] color, float[] depth, byte[] kind, int[] owner, Dictionary<Texture, (Color32[] pixels, int w, int h)> atlases)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        try { mesh.GetVertices(vertices); mesh.GetUVs(0, uvs); }
        catch { return; }
        var matrix = renderer.localToWorldMatrix;
        for (int v = 0; v < vertices.Count; v++) vertices[v] = matrix.MultiplyPoint3x4(vertices[v]);
        var materials = renderer.sharedMaterials;
        int id = renderer.GetInstanceID();
        for (int sub = 0; sub < mesh.subMeshCount && sub < materials.Length; sub++)
        {
            var material = materials[sub];
            if (material == null || material.name.Contains("Glass") || material.name.Contains("Light")) continue;
            Color tint = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
            var atlas = Atlas(material.mainTexture, atlases);
            var triangles = mesh.GetTriangles(sub);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = vertices[triangles[t]], b = vertices[triangles[t + 1]], c = vertices[triangles[t + 2]];
                Vector2 pa = new Vector2((a.x - min.x) * scale, (a.z - min.y) * scale);
                Vector2 pb = new Vector2((b.x - min.x) * scale, (b.z - min.y) * scale);
                Vector2 pc = new Vector2((c.x - min.x) * scale, (c.z - min.y) * scale);
                float area = (pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x);
                if (Mathf.Abs(area) < 0.01f) continue; // walls are edge-on from above
                int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.x, pb.x, pc.x)));
                int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(pa.x, pb.x, pc.x)));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.y, pb.y, pc.y)));
                int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(pa.y, pb.y, pc.y)));
                bool hasUV = uvs.Count == vertices.Count;
                Vector2 ua = hasUV ? uvs[triangles[t]] : Vector2.zero, ub = hasUV ? uvs[triangles[t + 1]] : Vector2.zero, uc = hasUV ? uvs[triangles[t + 2]] : Vector2.zero;
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float w0 = ((pb.x - px) * (pc.y - py) - (pb.y - py) * (pc.x - px)) / area;
                    float w1 = ((pc.x - px) * (pa.y - py) - (pc.y - py) * (pa.x - px)) / area;
                    float w2 = 1 - w0 - w1;
                    if (w0 < 0 || w1 < 0 || w2 < 0) continue;
                    float z = a.y * w0 + b.y * w1 + c.y * w2;
                    int i = y * width + x;
                    if (z <= depth[i]) continue;
                    depth[i] = z;
                    kind[i] = k;
                    owner[i] = id;
                    Color sample = tint;
                    if (atlas.pixels != null)
                    {
                        Vector2 uv = ua * w0 + ub * w1 + uc * w2;
                        int u = Mathf.Clamp((int)(Mathf.Repeat(uv.x, 1) * atlas.w), 0, atlas.w - 1);
                        int vv = Mathf.Clamp((int)(Mathf.Repeat(uv.y, 1) * atlas.h), 0, atlas.h - 1);
                        sample *= atlas.pixels[vv * atlas.w + u];
                    }
                    color[i] = sample;
                }
            }
        }
    }

    // Reads any texture regardless of import settings by copying it through a render texture.
    static (Color32[] pixels, int w, int h) Atlas(Texture source, Dictionary<Texture, (Color32[], int, int)> cache)
    {
        if (source == null) return (null, 0, 0);
        if (cache.TryGetValue(source, out var hit)) return hit;
        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = RenderTexture.active;
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        var result = (copy.GetPixels32(), source.width, source.height);
        Object.DestroyImmediate(copy);
        return cache[source] = result;
    }

    static Color[] Stylize(Color[] color, float[] depth, byte[] kind, int[] owner, List<Canopy> canopies,
        Vector2 min, float scale, int width, int height)
    {
        var output = new Color[color.Length];
        int shadow = Mathf.RoundToInt(1.4f * scale);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            Color c = color[i];
            Color.RGBToHSV(c, out float h, out float s, out float v);
            // Flat poster colours: gentle saturation lift, values snapped to a few bands.
            s = Mathf.Clamp01(s * 1.15f);
            v = Mathf.Round(Mathf.Lerp(0.3f, 1f, v) * 12) / 12;
            if (kind[i] == Building) v = Mathf.Min(1, v * 1.08f);
            // Darker asphalt so streets read clearly against the pale sidewalks.
            else if (kind[i] == Road && s < 0.12f && v < 0.62f) v *= 0.68f;
            c = Color.HSVToRGB(h, s, v);
            if (kind[i] == Building)
            {
                if (Edge(owner, x, y, width, height, 2)) c = Color.Lerp(c, new Color(0.13f, 0.12f, 0.16f), 0.85f);
            }
            else
            {
                // Soft drop shadow cast down-right by anything that stands taller than this pixel.
                for (int d = 3; d <= shadow; d += 3)
                {
                    int sx = x - d, sy = y + d;
                    if (sx < 0 || sy >= height) break;
                    int j = sy * width + sx;
                    if (kind[j] == Building && depth[j] > depth[i] + 1.5f) { c *= 0.72f; break; }
                }
                if (kind[i] == Road && Edge(kind, x, y, width, height, 1)) c *= 0.8f;
                else if (kind[i] == Prop && Edge(owner, x, y, width, height, 1)) c *= 0.7f;
            }
            c.a = 1;
            output[i] = c;
        }
        foreach (var tree in canopies) DrawCanopy(output, tree, min, scale, width, height, true);
        foreach (var tree in canopies) DrawCanopy(output, tree, min, scale, width, height, false);
        return output;
    }

    static bool Edge<T>(T[] field, int x, int y, int width, int height, int r) where T : System.IEquatable<T>
    {
        T self = field[y * width + x];
        return x < r || y < r || x >= width - r || y >= height - r ||
            !field[y * width + x - r].Equals(self) || !field[y * width + x + r].Equals(self) ||
            !field[(y - r) * width + x].Equals(self) || !field[(y + r) * width + x].Equals(self);
    }

    static void DrawCanopy(Color[] output, Canopy tree, Vector2 min, float scale, int width, int height, bool shadowPass)
    {
        Vector2 center = (tree.center - min) * scale;
        float r = tree.radius * scale, outline = Mathf.Max(2, scale * 0.18f);
        Vector2 shadowCenter = center + new Vector2(r * 0.35f, -r * 0.35f);
        Color leaf = Color.HSVToRGB(Mathf.Lerp(0.26f, 0.36f, tree.hue), 0.62f, 0.62f);
        Color light = Color.Lerp(leaf, new Color(0.85f, 0.95f, 0.55f), 0.35f);
        Color rim = new Color(0.11f, 0.24f, 0.15f);
        int x0 = Mathf.Max(0, (int)(center.x - r * 1.5f)), x1 = Mathf.Min(width - 1, (int)(center.x + r * 1.5f));
        int y0 = Mathf.Max(0, (int)(center.y - r * 1.5f)), y1 = Mathf.Min(height - 1, (int)(center.y + r * 1.5f));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            int i = y * width + x;
            float d = Vector2.Distance(p, center);
            if (shadowPass)
            {
                if (d > r && Vector2.Distance(p, shadowCenter) < r) output[i] *= 0.75f;
                continue;
            }
            if (d > r) continue;
            // Lit from the upper-left: a lighter crown inside a darker rim.
            bool crown = Vector2.Distance(p, center + new Vector2(-r, r) * 0.25f) < r * 0.55f;
            output[i] = d > r - outline ? rim : crown ? light : leaf;
        }
    }
}
