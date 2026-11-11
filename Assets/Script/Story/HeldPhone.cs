using UnityEngine;

// The phone in the prologue. On the Student object (with the Animator).
// While held: every frame, after the animation, it spreads the arms a little (the texting clip puts the hands
// on top of each other) and places the phone between the hands, screen toward the eyes, top pointing away.
// When put down (Held = false): it lies flat, screen up, at restSpot on the bed.
// The phone's position is set here every frame: adjust it with Toward Face / Height Offset, not by moving it.
public class HeldPhone : MonoBehaviour
{
    [Tooltip("The phone object (not a child of the hand; this script moves it).")]
    public Transform phone;
    [Tooltip("Where the phone lies on the bed after she puts it down.")]
    public Transform restSpot;
    [Tooltip("Degrees each upper arm is moved outward, so the hands don't overlap.")]
    [Range(0, 30)] public float armSpread = 10f;
    [Tooltip("How far the phone sits in front of the hands, toward the face (metres).")]
    public float towardFace = 0.03f;
    [Tooltip("Moves the held phone up (+) or down (-), in metres. The phone is placed by this script every frame, " +
             "so change this (or Toward Face) instead of moving the phone in the Scene view.")]
    public float heightOffset = -0.04f;

    public bool Held { get; set; } = true;

    Animator anim;

    void Awake() => anim = GetComponent<Animator>();

    void LateUpdate()
    {
        if (!Held)
        {
            phone.SetPositionAndRotation(restSpot.position, restSpot.rotation);
            return;
        }

        Spread(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftHand, -transform.right);
        Spread(HumanBodyBones.RightUpperArm, HumanBodyBones.RightHand, transform.right);

        Vector3 left = anim.GetBoneTransform(HumanBodyBones.LeftHand).position;
        Vector3 right = anim.GetBoneTransform(HumanBodyBones.RightHand).position;
        Vector3 eyes = anim.GetBoneTransform(HumanBodyBones.Head).position + Vector3.up * 0.06f;
        Vector3 between = (left + right) / 2;
        Vector3 position = between + (eyes - between).normalized * towardFace + Vector3.up * heightOffset;
        // The screen (a quad) faces the phone's -Z, so +Z points away from the eyes. Top = away from her body.
        phone.SetPositionAndRotation(position, Quaternion.LookRotation(position - eyes, transform.forward));
    }

    // Swing the upper arm so its hand moves toward `outward` by armSpread degrees.
    void Spread(HumanBodyBones upperArm, HumanBodyBones hand, Vector3 outward)
    {
        Transform arm = anim.GetBoneTransform(upperArm);
        Vector3 toHand = anim.GetBoneTransform(hand).position - arm.position;
        Vector3 axis = Vector3.Cross(toHand, outward);
        if (axis.sqrMagnitude > 0.0001f) arm.Rotate(axis.normalized, armSpread, Space.World);
    }
}
