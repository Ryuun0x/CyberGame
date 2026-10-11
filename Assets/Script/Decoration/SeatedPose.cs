using UnityEngine;

// Seated NPC without a sitting clip (the project has none): the Animator is off and the pose is set
// through humanoid muscles. Runs in the editor too, so café NPCs are placed and tuned in the Scene view:
// drag the character next to a chair and use the component menu "Sit on nearest chair", then move/rotate
// it or adjust the values below; it updates live.
// ponytail: procedural pose; a Mixamo "Sitting Idle" clip in the NPC controller would look better if one is added.
[ExecuteAlways]
public class SeatedPose : MonoBehaviour
{
    [Header("Pose (humanoid muscles, -1..1)")]
    [Tooltip("Upper Leg Front-Back. Negative lifts the thighs forward.")]
    [Range(-1, 1)] public float thighs = -0.55f;
    [Tooltip("Lower Leg Stretch. Negative bends the knees more.")]
    [Range(-1, 1)] public float knees = 0f;
    [Tooltip("Upper Leg In-Out. Positive spreads the knees.")]
    [Range(-1, 1)] public float kneesApart = 0.05f;
    [Range(-1, 1)] public float feet = 0.2f;
    [Tooltip("Arm Down-Up. Negative lowers the arms.")]
    [Range(-1, 1)] public float arms = -0.55f;
    [Tooltip("Arm Front-Back. Positive brings the hands forward.")]
    [Range(-1, 1)] public float armsForward = 0.35f;
    [Tooltip("Forearm Stretch. Negative bends the elbows.")]
    [Range(-1, 1)] public float elbows = 0.1f;
    [Tooltip("Spine Front-Back. Positive leans forward.")]
    [Range(-1, 1)] public float lean = 0.05f;
    [Tooltip("Breathing and slow head movement while playing.")]
    public bool idleMotion = true;

    const float SeatHeight = 0.47f, HipsAboveSeat = 0.07f;

    // Where the body sits relative to this root. Stored once and saved with the scene: re-reading it from
    // the skeleton on every script reload / Play mode switch drifted the body away from the root.
    [SerializeField, HideInInspector] Vector3 bodyOffset;
    [SerializeField, HideInInspector] Quaternion bodyTurn = Quaternion.identity;
    [SerializeField, HideInInspector] bool placed;

    Animator anim;
    HumanPoseHandler handler;
    HumanPose pose;
    float phase;

    void OnEnable()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null || anim.avatar == null || !anim.avatar.isHuman) return;
        anim.enabled = false;
        handler = new HumanPoseHandler(anim.avatar, anim.transform);
        handler.GetHumanPose(ref pose); // muscle array; the body placement comes from the stored offset
        if (!placed)
        {
            // First time only: GetHumanPose is world space, SetHumanPose reads root space.
            bodyOffset = anim.transform.InverseTransformPoint(pose.bodyPosition);
            bodyTurn = Quaternion.Inverse(anim.transform.rotation) * pose.bodyRotation;
            placed = true;
        }
        phase = Random.value * 10f;
        Apply();
    }

    void OnDisable()
    {
        handler?.Dispose();
        handler = null;
    }

    void OnValidate()
    {
        if (handler != null) Apply();
    }

    void LateUpdate()
    {
        if (handler != null) Apply();
    }

    void Apply()
    {
        pose.bodyPosition = bodyOffset;
        pose.bodyRotation = bodyTurn;
        Set("Upper Leg Front-Back", thighs);
        Set("Lower Leg Stretch", knees);
        Set("Upper Leg In-Out", kneesApart);
        Set("Foot Up-Down", feet);
        Set("Arm Down-Up", arms);
        Set("Arm Front-Back", armsForward);
        Set("Forearm Stretch", elbows);
        bool moving = Application.isPlaying && idleMotion;
        float t = Time.time + phase;
        SetOne("Spine Front-Back", lean + (moving ? Mathf.Sin(t * 1.3f) * 0.02f : 0));
        SetOne("Head Turn Left-Right", moving ? Mathf.Sin(t * 0.23f) * 0.25f : 0);
        SetOne("Head Nod Down-Up", -0.1f + (moving ? Mathf.Sin(t * 0.37f) * 0.05f : 0));
        handler.SetHumanPose(ref pose);
    }

    void Set(string muscle, float value)
    {
        SetOne("Left " + muscle, value);
        SetOne("Right " + muscle, value);
    }

    void SetOne(string muscle, float value)
    {
        int i = System.Array.IndexOf(HumanTrait.MuscleName, muscle);
        if (i >= 0) pose.muscles[i] = value;
    }

    // Seat on a chair facing lookAt: the root goes on the floor under the seat and the stored body offset
    // is solved so the hips land on it (SetHumanPose's exact mapping isn't exposed, so correct a few times).
    public void SeatOn(Transform chair, Vector3 lookAt)
    {
        if (handler == null) return;
        var model = chair.GetComponentInChildren<Renderer>();
        var bounds = model != null ? model.bounds : new Bounds(chair.position, Vector3.zero);
        Vector3 seat = new Vector3(bounds.center.x, bounds.min.y + SeatHeight + HipsAboveSeat, bounds.center.z);
        Vector3 face = lookAt - seat;
        face.y = 0;
        transform.SetPositionAndRotation(new Vector3(seat.x, bounds.min.y, seat.z),
            face.sqrMagnitude > 0.001f ? Quaternion.LookRotation(face) : transform.rotation);
        bodyTurn = Quaternion.identity; // body faces the root's forward
        for (int i = 0; i < 4; i++)
        {
            Apply();
            bodyOffset += transform.InverseTransformVector(seat - anim.GetBoneTransform(HumanBodyBones.Hips).position);
        }
        Apply();
        placed = true;
    }

#if UNITY_EDITOR
    [ContextMenu("Sit on nearest chair")]
    void SitOnNearestChair()
    {
        var hips = anim != null ? anim.GetBoneTransform(HumanBodyBones.Hips).position : transform.position;
        Transform chair = Nearest("Chair", hips, 2f);
        if (chair == null) { Debug.LogWarning("[SeatedPose] No chair within 2 m.", this); return; }
        Transform table = Nearest("Table", chair.position, 2f);
        UnityEditor.Undo.RecordObject(transform, "Sit on nearest chair");
        UnityEditor.Undo.RecordObject(this, "Sit on nearest chair");
        SeatOn(chair, table != null ? table.GetComponentInChildren<Renderer>().bounds.center : chair.position + chair.forward);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    static Transform Nearest(string prefix, Vector3 point, float within)
    {
        Transform best = null;
        float bestDistance = within;
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.name.StartsWith(prefix)) continue;
            Vector3 d = r.bounds.center - point;
            d.y = 0;
            if (d.magnitude < bestDistance) { bestDistance = d.magnitude; best = r.transform; }
        }
        return best;
    }
#endif

    // Get up: step out to their right (their front is the table), on the floor, animated again.
    public void Stand()
    {
        var hips = anim.GetBoneTransform(HumanBodyBones.Hips).position;
        Vector3 spot = hips + transform.right * 0.55f;
        spot.y = CityLife.GroundY(spot);
        enabled = false;
        anim.enabled = true;
        transform.position = spot;
        gameObject.AddComponent<PinHips>();
        Destroy(this);
    }
}
