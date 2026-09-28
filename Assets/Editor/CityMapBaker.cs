using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityMapBaker
{
    const string ScenePath = "Assets/Scenes/MainScene(city).unity";

    [MenuItem("CADSNET/Map/Rebuild Illustrated City Layout")]
    public static void Bake()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding the map.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var shapes = new List<CityMapLayout.Shape>();
            var places = new List<CityMapLayout.Place>();
            var counts = new Dictionary<string, int>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var door in root.GetComponentsInChildren<DoorEntrance>(true))
                    if (door.sceneToLoad == "Interior") AddPlace(places, "Home", door.transform.position);
                foreach (var cafe in root.GetComponentsInChildren<CafeZone>(true))
                    AddPlace(places, "Café", cafe.GetComponent<Collider>() != null ? cafe.GetComponent<Collider>().bounds.center : cafe.transform.position);
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    string name = renderer.name.ToLowerInvariant();
                    if (name.Contains("_lod1") || name.Contains("_lod2") || name.Contains("_lod3") || name.Contains("doghouse")) continue;
                    int kind = Kind(name);
                    if (kind < 0) continue;
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    var bounds = renderer.bounds;
                    if (bounds.size.x > 350 || bounds.size.z > 350 || bounds.size.x < 1 || bounds.size.z < 1) continue;
                    Vector3[] vertices;
                    try { vertices = filter.sharedMesh.vertices; }
                    catch { continue; }
                    var points = vertices.Select(v => renderer.transform.TransformPoint(v)).Select(v => new Vector2(v.x, v.z));
                    var hull = Hull(points);
                    if (hull.Length < 3) continue;
                    shapes.Add(new CityMapLayout.Shape { kind = kind, points = hull });
                    counts[name] = counts.TryGetValue(name, out int count) ? count + 1 : 1;
                }
            }
            if (!places.Any(p => p.name == "Home") || !places.Any(p => p.name == "Café"))
                throw new InvalidOperationException("Home or Café could not be located. No map was written.");
            var core = shapes.Where(s => s.kind == 2 || s.kind == 3).SelectMany(s => s.points).ToArray();
            if (core.Length == 0) throw new InvalidOperationException("No roads/buildings found. No map was written.");
            var data = new CityMapLayout {
                shapes = shapes.OrderBy(s => s.kind).ToArray(), places = places.ToArray(), home = places.First(p => p.name == "Home").position,
                min = new Vector2(core.Min(p => p.x) - 12, core.Min(p => p.y) - 12),
                max = new Vector2(core.Max(p => p.x) + 12, core.Max(p => p.y) + 12)
            };
            Directory.CreateDirectory("Assets/Resources");
            CityMapArtworkBaker.Bake(scene, data);
            File.WriteAllText("Assets/Resources/CityMapLayout.json", JsonUtility.ToJson(data, true));
            AssetDatabase.ImportAsset("Assets/Resources/CityMapLayout.json");
            Debug.Log("[CityMapBaker] Baked " + shapes.Count + " flat shapes, " + places.Count + " verified destinations. Bounds " + data.min + " to " + data.max + ". " + string.Join(", ", counts.Select(p => p.Key + ":" + p.Value)));
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    static void AddPlace(List<CityMapLayout.Place> places, string name, Vector3 position)
    {
        if (!places.Any(p => p.name == name)) places.Add(new CityMapLayout.Place { name = name, position = new Vector2(position.x, position.z) });
    }

    static int Kind(string name)
    {
        if (name.Contains("road") && !name.Contains("sign") && !name.Contains("barrier")) return 2;
        if (name.Contains("sidewalk") || name.Contains("pavement")) return 1;
        if (name.Contains("grass") || name.Contains("parkground")) return 0;
        if (name.Contains("water") && !name.Contains("tower")) return 4;
        if ((name.Contains("house") || name.Contains("building") || name.Contains("shop") || name.Contains("store") || name.Contains("bld")) &&
            !name.Contains("door") && !name.Contains("window") && !name.Contains("interior") && !name.Contains("fence")) return 3;
        return -1;
    }

    static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    static Vector2[] Hull(IEnumerable<Vector2> source)
    {
        var points = source.Select(p => new Vector2(Mathf.Round(p.x * 10) / 10, Mathf.Round(p.y * 10) / 10)).Distinct().OrderBy(p => p.x).ThenBy(p => p.y).ToArray();
        if (points.Length < 3) return points;
        var hull = new List<Vector2>();
        foreach (var p in points) { while (hull.Count > 1 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p); }
        int lower = hull.Count;
        for (int i = points.Length - 2; i >= 0; i--) { var p = points[i]; while (hull.Count > lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p); }
        hull.RemoveAt(hull.Count - 1);
        return hull.ToArray();
    }
}
