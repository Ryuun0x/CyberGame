using UnityEngine;

// Seated NPC without a sitting clip (the project has none): the Animator is off and the pose is set
// through humanoid muscles. Runs in the editor too, so café NPCs are placed and tuned in the Scene view:
// move/rotate the character to seat it, adjust the values below and it updates live.
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
        handler.GetHumanPose(ref pose);
        // GetHumanPose reports the body in world space but SetHumanPose reads it relative to the root:
        // convert, so the body stays exactly where it is and follows the root when it's moved.
        pose.bodyPosition = anim.transform.InverseTransformPoint(pose.bodyPosition);
        pose.bodyRotation = Quaternion.Inverse(anim.transform.rotation) * pose.bodyRotation;
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
