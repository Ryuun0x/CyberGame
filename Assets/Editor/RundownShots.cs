using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The scenes behind the rundown's risky/safer pictures (one per page, see ArtSource/RundownArt/make_samples.py):
/// Alice at a desk, on a bed or a sofa, or standing, with a laptop or a phone. Each shot writes
/// Temp/Shots/rd_&lt;name&gt;.png, rd_&lt;name&gt;_id.png and rd_&lt;name&gt;_anchors.json through RundownStudio.
/// Run from MCP: RundownShots.RenderAll() (or Render("desk_front")).
/// </summary>
public static class RundownShots
{
    const string Home = "Assets/VNB - Home Set/Prefabs/";
    const string Table = Home + "Gaming Room/Table (Modern).prefab";
    const string Chair = Home + "Gaming Room/Gamer Chair.prefab";
    const string Laptop = Home + "Gaming Room/Laptop.prefab";
    const string Phone = Home + "Gaming Room/Smartphone 1.prefab";
    const string Bed = Home + "Bedroom/1P Bed.prefab";
    const string Sofa = Home + "Backyard/Outside Sofa.prefab";
    const string Usb = "Assets/Prefabs/usb.fbx";

    const string SitLegs = "Upper Leg Front-Back=-0.8; Lower Leg Stretch=-0.1; ";
    const string Typing = "Head Nod Down-Up=-0.25; Arm Down-Up=-0.45; Arm Front-Back=0.3; Forearm Stretch=-0.1";
    const string Standing = "Upper Leg Front-Back=0.6; Lower Leg Stretch=1; ";

    public static readonly string[] Names = { "desk_front", "desk_side", "desk_side_plain", "desk_back", "bed_laptop", "bed_phone", "sofa_phone", "stand_phone" };

    public static void RenderAll()
    {
        foreach (var n in Names) Render(n);
    }

    public static void Render(string name)
    {
        var stage = RundownStudio.NewStage();
        try
        {
            var points = new Dictionary<string, Vector3>();
            GameObject alice, prop;
            Vector3 eye, look;
            float fov = 38;
            switch (name)
            {
                case "desk_front":
                case "desk_side":
                case "desk_side_plain":
                case "desk_back":
                    (alice, prop) = Desk(stage, points, name == "desk_side");
                    if (name == "desk_front") { eye = new Vector3(1.7f, 1.55f, 1.4f); look = new Vector3(0, 1.02f, -0.3f); }
                    else if (name.StartsWith("desk_side")) { eye = new Vector3(2.3f, 1.45f, -0.2f); look = new Vector3(0, 0.98f, -0.25f); }
                    else { eye = new Vector3(-1.65f, 1.75f, -0.8f); look = new Vector3(0.05f, 1.02f, -0.05f); fov = 40; }
                    break;
                case "bed_laptop":
                    (alice, prop) = BedLaptop(stage, points);
                    eye = new Vector3(2.7f, 1.35f, 1.3f); look = new Vector3(0.45f, 0.9f, 0); fov = 40;
                    break;
                case "bed_phone":
                    (alice, prop) = SeatedPhone(stage, points, Bed, new Vector3(0.45f, 0, 0), 90);
                    eye = new Vector3(2.8f, 1.3f, -1.2f); look = new Vector3(0.5f, 0.9f, 0); fov = 38;
                    break;
                case "sofa_phone":
                    (alice, prop) = SeatedPhone(stage, points, Sofa, new Vector3(-0.35f, 0, 0), 0);
                    eye = new Vector3(0.9f, 1.3f, 2.6f); look = new Vector3(-0.1f, 0.75f, 0); fov = 36;
                    break;
                default: // stand_phone
                    alice = RundownStudio.Place(stage, RundownStudio.Alice, Vector3.zero, 0);
                    HandsAtChest(alice, Standing, 0);
                    prop = HoldPhone(stage, alice, points);
                    eye = new Vector3(-1.6f, 1.35f, 2.2f); look = new Vector3(0, 0.95f, 0); fov = 36;
                    break;
            }
            var anim = alice.GetComponentInChildren<Animator>();
            points["face"] = anim.GetBoneTransform(HumanBodyBones.Head).position + Vector3.up * 0.08f;
            string dir = "Temp/Shots/";
            System.IO.Directory.CreateDirectory(dir);
            RundownStudio.Render(stage, eye, look, fov, dir + $"rd_{name}.png");
            RundownStudio.RenderIds(stage, eye, look, fov, dir + $"rd_{name}_id.png", 900, 700,
                (alice, new Color(1, 0, 0)), (prop, new Color(1, 1, 0)));
            RundownStudio.SaveAnchors(dir + $"rd_{name}_anchors.json", eye, look, fov, 900, 700, points);
        }
        finally { RundownStudio.Close(stage); }
    }

    // Alice typing at a desk; side: a flash drive plugged in beside the laptop.
    static (GameObject, GameObject) Desk(Scene stage, Dictionary<string, Vector3> points, bool usb)
    {
        // The chair at 0.9: seat 0.62 m, armrests clear of her hips; placed so her hips sit near the backrest.
        var chair = RundownStudio.Place(stage, Chair, new Vector3(0, 0, -0.32f), 0, 0.9f);
        var alice = RundownStudio.Place(stage, RundownStudio.Alice, new Vector3(0, 0, -0.25f), 0);
        Sit(stage, alice, chair, new Vector3(0, 0, -0.1f), SitLegs + Typing);
        // The desk comes up to her hands, so they rest on the keyboard (the VNB table is coffee-table height).
        var anim = alice.GetComponentInChildren<Animator>();
        float hands = (anim.GetBoneTransform(HumanBodyBones.LeftHand).position.y + anim.GetBoneTransform(HumanBodyBones.RightHand).position.y) / 2;
        float top = hands - 0.04f;
        var table = RundownStudio.Place(stage, Table, new Vector3(0, 0, 0.15f), 0);
        table.transform.localScale = new Vector3(1.0f, top / 0.51f, 1.3f);
        var laptop = RundownStudio.Place(stage, Laptop, new Vector3(0, top, -0.02f), 180);
        points["screen"] = new Vector3(0, top + 0.22f, 0.12f);
        points["laptop"] = new Vector3(0, top + 0.04f, -0.05f);
        if (usb)
        {
            var drive = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Usb));
            SceneManager.MoveGameObjectToScene(drive, stage);
            Fit(drive, 0.09f);
            drive.transform.position = new Vector3(0.3f, top + 0.01f, -0.08f);
            drive.transform.SetParent(laptop.transform, true);
            points["usb"] = drive.transform.position + Vector3.up * 0.02f;
        }
        return (alice, laptop);
    }

    // Alice on the side of a bed with the laptop on her knees.
    static (GameObject, GameObject) BedLaptop(Scene stage, Dictionary<string, Vector3> points)
    {
        var bed = RundownStudio.Place(stage, Bed, Vector3.zero, 0);
        var alice = RundownStudio.Place(stage, RundownStudio.Alice, Vector3.zero, 90);
        Sit(stage, alice, bed, new Vector3(0.42f, 0, 0), SitLegs + Typing);
        var anim = alice.GetComponentInChildren<Animator>();
        Vector3 knees = (anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position + anim.GetBoneTransform(HumanBodyBones.RightLowerLeg).position) / 2;
        Vector3 hips = anim.GetBoneTransform(HumanBodyBones.Hips).position;
        Vector3 lap = Vector3.Lerp(hips, knees, 0.6f) + Vector3.up * 0.02f;
        var laptop = RundownStudio.Place(stage, Laptop, lap, 90 + 180, 0.85f);
        points["screen"] = lap + new Vector3(0.1f, 0.2f, 0);
        points["laptop"] = lap;
        return (alice, laptop);
    }

    // Alice sitting on a bed or sofa (seat: where her hips go, local to the furniture), looking at her phone.
    static (GameObject, GameObject) SeatedPhone(Scene stage, Dictionary<string, Vector3> points, string furniture, Vector3 seat, float yaw)
    {
        var seatObj = RundownStudio.Place(stage, furniture, Vector3.zero, 0);
        var alice = RundownStudio.Place(stage, RundownStudio.Alice, Vector3.zero, yaw);
        Sit(stage, alice, seatObj, seat, SitLegs);
        HandsAtChest(alice, SitLegs, 0.2f);
        return (alice, HoldPhone(stage, alice, points));
    }

    // Poses Alice sitting, then moves her so her hips rest on the surface under `seat`.
    static void Sit(Scene stage, GameObject alice, GameObject furniture, Vector3 seat, string muscles)
    {
        RundownStudio.Pose(alice, muscles, 0.2f);
        foreach (var mf in furniture.GetComponentsInChildren<MeshFilter>())
            if (!mf.GetComponent<Collider>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        Physics.SyncTransforms();
        Vector3 top = furniture.transform.TransformPoint(seat) + Vector3.up * 3;
        float y = stage.GetPhysicsScene().Raycast(top, Vector3.down, out var hit, 5) ? hit.point.y : 0.5f;
        var hips = alice.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Hips);
        Vector3 target = new Vector3(top.x, y + 0.1f, top.z);
        alice.transform.position += target - hips.position;
    }

    // Both hands together in front of her chest, head bowed: searches the arm muscles (same on both sides) so the
    // hands meet there. `body` is the rest of the pose (legs), bodyDrop as in RundownStudio.Pose.
    static string HandsAtChest(GameObject alice, string body, float bodyDrop)
    {
        var anim = alice.GetComponentInChildren<Animator>();
        string head = "Head Nod Down-Up=-0.7; Neck Nod Down-Up=-0.4; ";
        RundownStudio.Pose(alice, body + head, bodyDrop);
        Transform l = anim.GetBoneTransform(HumanBodyBones.LeftHand), r = anim.GetBoneTransform(HumanBodyBones.RightHand);
        Transform c = anim.GetBoneTransform(HumanBodyBones.UpperChest);
        Vector3 chest = (c != null ? c : anim.GetBoneTransform(HumanBodyBones.Chest)).position;
        Vector3 fwd = alice.transform.forward, right = alice.transform.right;
        Vector3 headPos = anim.GetBoneTransform(HumanBodyBones.Head).position;
        Vector3 aim = new Vector3(chest.x, headPos.y - 0.4f, chest.z) + fwd * 0.3f; // phone up in front of her, below her eyes
        Vector3 tl = aim - right * 0.05f, tr = aim + right * 0.05f;
        float best = float.MaxValue; string bestPose = body + head;
        foreach (float du in new[] { -0.9f, -0.7f, -0.5f, -0.3f, -0.1f, 0.1f })
        foreach (float fb in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
        foreach (float fs in new[] { -1f, -0.8f, -0.6f, -0.4f, -0.2f, 0f })
        foreach (float tw in new[] { -0.6f, 0f, 0.6f })
        {
            string s = body + head + $"Arm Down-Up={du}; Arm Front-Back={fb}; Forearm Stretch={fs}; Arm Twist In-Out={tw}";
            RundownStudio.Pose(alice, s, bodyDrop);
            float e = (l.position - tl).magnitude + (r.position - tr).magnitude;
            if (e < best) { best = e; bestPose = s; }
        }
        RundownStudio.Pose(alice, bestPose, bodyDrop);
        return bestPose;
    }

    static GameObject HoldPhone(Scene stage, GameObject alice, Dictionary<string, Vector3> points)
    {
        var anim = alice.GetComponentInChildren<Animator>();
        Vector3 l = anim.GetBoneTransform(HumanBodyBones.LeftHand).position, r = anim.GetBoneTransform(HumanBodyBones.RightHand).position;
        Vector3 head = anim.GetBoneTransform(HumanBodyBones.Head).position;
        Vector3 at = (l + r) / 2 + alice.transform.forward * 0.04f + Vector3.up * 0.03f;
        var phone = RundownStudio.Place(stage, Phone, at, 0);
        // Screen up towards her face.
        phone.transform.rotation = Quaternion.LookRotation(alice.transform.forward, (head - at).normalized) * Quaternion.Euler(0, 0, 0);
        phone.transform.rotation = Quaternion.LookRotation(Vector3.Cross(alice.transform.right, (head - at).normalized), (head - at).normalized);
        points["phone"] = at;
        return phone;
    }

    static void Fit(GameObject go, float size)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.localScale *= size / Mathf.Max(b.size.x, b.size.y, b.size.z);
    }
}
