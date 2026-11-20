using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only photo studio for the chapter rundown art: stages characters and props in a hidden preview scene,
/// poses them with humanoid muscles and renders them on a green background (keyed out and turned into
/// silhouettes by ArtSource/RundownArt/make_silhouettes.py). Used from MCP execute_code; nothing touches the open scene.
/// </summary>
public static class RundownStudio
{
    // The student as she is in the game (Prologue/Student in Interior): humanoid rig with her own materials.
    public const string Alice = "Assets/Character/Alice/Alice.fbx";
    public const string Hoodie = "Assets/SyntyStudios/PolygonCity/Prefabs/Characters/Character_Male_Hoodie_01.prefab";
    public static readonly Color Key = new Color(0, 1, 0, 1);

    public static Scene NewStage()
    {
        var stage = EditorSceneManager.NewPreviewScene();
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.6f;
        sun.transform.rotation = Quaternion.Euler(35, -40, 0);
        SceneManager.MoveGameObjectToScene(sun.gameObject, stage);
        var fill = new GameObject("Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.6f;
        fill.transform.rotation = Quaternion.Euler(20, 150, 0);
        SceneManager.MoveGameObjectToScene(fill.gameObject, stage);
        return stage;
    }

    public static GameObject Place(Scene stage, string prefabPath, Vector3 position, float yaw, float scale = 1)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        go.transform.localScale *= scale;
        foreach (var a in go.GetComponentsInChildren<Animator>()) a.enabled = false;
        foreach (var b in go.GetComponentsInChildren<MonoBehaviour>()) b.enabled = false;
        return go;
    }

    static readonly Dictionary<int, Vector3> restBody = new Dictionary<int, Vector3>();

    /// Muscles by name ("Left Arm Down-Up", or "Arm Down-Up" for both sides), everything else 0.
    /// bodyDrop lowers the hips (metres) for sitting.
    public static void Pose(GameObject character, Dictionary<string, float> muscles, float bodyDrop = 0)
    {
        var anim = character.GetComponentInChildren<Animator>();
        // GetHumanPose reads world space, SetHumanPose writes root space: pose at the origin, then put it back.
        Vector3 at = anim.transform.position;
        Quaternion turn = anim.transform.rotation;
        anim.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        using (var handler = new HumanPoseHandler(anim.avatar, anim.transform))
        {
            var pose = new HumanPose();
            handler.GetHumanPose(ref pose);
            // Hips height of the rest pose, kept so a sitting drop doesn't add up over several poses.
            int id = character.GetInstanceID();
            if (!restBody.ContainsKey(id)) restBody[id] = pose.bodyPosition;
            pose.bodyPosition = restBody[id];
            for (int i = 0; i < pose.muscles.Length; i++) pose.muscles[i] = 0;
            foreach (var kv in muscles)
            {
                int i = System.Array.IndexOf(HumanTrait.MuscleName, kv.Key);
                if (i >= 0) { pose.muscles[i] = kv.Value; continue; }
                foreach (var side in new[] { "Left ", "Right " })
                {
                    i = System.Array.IndexOf(HumanTrait.MuscleName, side + kv.Key);
                    if (i >= 0) pose.muscles[i] = kv.Value;
                }
            }
            pose.bodyPosition += Vector3.down * bodyDrop;
            pose.bodyRotation = Quaternion.identity;
            handler.SetHumanPose(ref pose);
        }
        anim.transform.SetPositionAndRotation(at, turn);
    }

    /// Same, written as "Arm Down-Up=-0.6; Head Nod Down-Up=-0.3".
    public static void Pose(GameObject character, string muscles, float bodyDrop = 0)
    {
        var values = new Dictionary<string, float>();
        foreach (var part in muscles.Split(';'))
        {
            var kv = part.Split('=');
            if (kv.Length == 2) values[kv[0].Trim()] = float.Parse(kv[1], System.Globalization.CultureInfo.InvariantCulture);
        }
        Pose(character, values, bodyDrop);
    }

    public static Transform Bone(GameObject character, string mixamoName)
    {
        foreach (var t in character.GetComponentsInChildren<Transform>()) if (t.name == "mixamorig:" + mixamoName || t.name == mixamoName) return t;
        return null;
    }

    public static string Render(Scene stage, Vector3 eye, Vector3 lookAt, float fov, string path, int width = 900, int height = 700)
    {
        var go = new GameObject("StudioCam");
        SceneManager.MoveGameObjectToScene(go, stage);
        var cam = go.AddComponent<Camera>();
        cam.scene = stage;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Key;
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.05f;
        cam.transform.position = eye;
        cam.transform.LookAt(lookAt);
        // Skinned meshes don't re-skin in a preview scene without a frame: bake the current pose into plain meshes.
        var baked = new List<GameObject>();
        var skinned = new List<SkinnedMeshRenderer>();
        foreach (var root in stage.GetRootGameObjects())
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                smr.BakeMesh(mesh, false); // false: world size, whatever scale the FBX nodes carry
                var copy = new GameObject(smr.name + "_Baked");
                SceneManager.MoveGameObjectToScene(copy, stage);
                copy.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                copy.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
                baked.Add(copy);
                smr.enabled = false;
                skinned.Add(smr);
            }
        var rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            foreach (var b in baked) { Object.DestroyImmediate(b.GetComponent<MeshFilter>().sharedMesh); Object.DestroyImmediate(b); }
            foreach (var smr in skinned) smr.enabled = true;
        }
        return path;
    }

    /// Same framing, every object a flat colour: the listed characters/props in their colour, everything else grey.
    /// Lets the silhouette script tell who is who (and what is in front).
    public static string RenderIds(Scene stage, Vector3 eye, Vector3 lookAt, float fov, string path, int width, int height, params (GameObject go, Color color)[] ids)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var saved = new Dictionary<Renderer, Material[]>();
        var made = new List<Material>();
        Material Flat(Color c) { var m = new Material(shader); m.SetColor("_BaseColor", c); made.Add(m); return m; }
        var other = Flat(new Color(0.5f, 0.5f, 0.5f));
        foreach (var root in stage.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                saved[r] = r.sharedMaterials;
                Material m = other;
                foreach (var id in ids) if (r.transform.IsChildOf(id.go.transform)) m = Flat(id.color);
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        try { return Render(stage, eye, lookAt, fov, path, width, height); }
        finally
        {
            foreach (var kv in saved) kv.Key.sharedMaterials = kv.Value;
            foreach (var m in made) Object.DestroyImmediate(m);
        }
    }

    /// Where a world point lands in a Render() image with the same framing (pixels, top-left origin).
    public static Vector2 Project(Vector3 eye, Vector3 lookAt, float fov, int width, int height, Vector3 point)
    {
        var view = Matrix4x4.TRS(eye, Quaternion.LookRotation(lookAt - eye), new Vector3(1, 1, -1)).inverse;
        var clip = Matrix4x4.Perspective(fov, (float)width / height, 0.05f, 100f) * view * new Vector4(point.x, point.y, point.z, 1);
        return new Vector2((clip.x / clip.w * 0.5f + 0.5f) * width, (0.5f - clip.y / clip.w * 0.5f) * height);
    }

    /// Writes named anchor points (see Project) as JSON next to a render, for the overlays.
    public static void SaveAnchors(string path, Vector3 eye, Vector3 lookAt, float fov, int width, int height, Dictionary<string, Vector3> points)
    {
        var parts = new List<string>();
        foreach (var kv in points)
        {
            var p = Project(eye, lookAt, fov, width, height, kv.Value);
            parts.Add($"\"{kv.Key}\": [{p.x.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}, {p.y.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}]");
        }
        File.WriteAllText(path, "{" + string.Join(", ", parts) + "}");
    }

    public static void Close(Scene stage) => EditorSceneManager.ClosePreviewScene(stage);
}
