using UnityEngine;

// First-person body: the character model follows the player capsule and animates with its speed.
// The head is hidden so it never covers the camera.
public class PlayerBody : MonoBehaviour
{
    public GameObject character;
    public RuntimeAnimatorController animator;
    public Vector3 offset = Vector3.zero;
    [Tooltip("Size of the body model. Keep its eyes near the camera (PlayerCameraRoot height).")]
    public float bodyScale = 1f;

    CharacterController controller;
    Animator anim;
    Transform model, head;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (character == null || controller == null) return;
        var body = Instantiate(character, transform);
        body.name = "PlayerBody";
        body.transform.localPosition = offset;
        body.transform.localRotation = Quaternion.identity;
        body.transform.localScale = Vector3.one * bodyScale;
        model = body.transform;
        foreach (var t in body.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
        // Long hair hangs from the spine, not the head, so it would swing through the view.
        foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>())
            if (r.name.ToLowerInvariant().Contains("hair")) r.enabled = false;
        var capsule = transform.Find("Capsule")?.GetComponent<MeshRenderer>();
        if (capsule != null) capsule.enabled = false;
        anim = body.GetComponentInChildren<Animator>();
        anim.runtimeAnimatorController = animator;
        anim.applyRootMotion = false;
        body.AddComponent<PinHips>();
        head = anim.GetBoneTransform(HumanBodyBones.Head);
        // First person: the arms stay relaxed (FirstPersonArms layer) instead of swinging through the view;
        // NPCs share the controller with that layer off.
        int arms = anim.GetLayerIndex("FirstPersonArms");
        if (arms >= 0) anim.SetLayerWeight(arms, 1f);
        // Ground speed of the walk/run clips for this body (the clips' own speed × the character's size).
        float size = anim.humanScale * body.transform.lossyScale.y;
        walkStride = walkClipSpeed * size;
        runStride = runClipSpeed * size;
    }

    [Tooltip("How fast the walk and run clips move (m/s at human scale 1): Alice walk.anim and Meshy Run_02 " +
             "(both in place: measured from how fast the planted foot slides back).")]
    public float walkClipSpeed = 1.15f, runClipSpeed = 3.9f;
    float walkStride, runStride;

    [Tooltip("Most the body turns toward a sideways/diagonal move (degrees).")]
    public float maxTurn = 30f;
    [Tooltip("How far the body slides back when looking straight down (m), so the legs show in front of the view.")]
    public float lookDownShift = 0.3f;

    // The eye camera sits inside the head: collapse it (face and hair skin to it) so it never blocks the view.
    // The Animator rewrites bone scale every frame, so this runs after it.
    void LateUpdate()
    {
        if (head != null) head.localScale = Vector3.one * 0.001f;
    }

    void Update()
    {
        if (anim == null) return;
        Vector3 v = controller.velocity;
        float speed = new Vector2(v.x, v.z).magnitude;
        // Legs lean toward the way you move, but only a little (maxTurn): turned further, they stick out
        // sideways under the view. Backing up keeps facing forward and plays the walk in reverse.
        Vector3 local = transform.InverseTransformDirection(new Vector3(v.x, 0, v.z));
        float yaw = 0, direction = 1;
        if (speed > 0.2f)
        {
            yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            if (Mathf.Abs(yaw) > 100) { yaw -= 180 * Mathf.Sign(yaw); direction = -1; }
            yaw = Mathf.Clamp(yaw, -maxTurn, maxTurn);
        }
        model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(0, yaw, 0), 10 * Time.deltaTime);
        // Looking down, the camera is right over the chest and the legs show foreshortened from above.
        // Sliding the body back a little puts the legs ahead of the view instead.
        var cam = Camera.main;
        float down = cam != null ? Mathf.InverseLerp(30f, 85f, -Mathf.Asin(Mathf.Clamp(cam.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg) : 0f;
        model.localPosition = offset + Vector3.back * (lookDownShift * down);
        anim.SetFloat("Direction", direction);
        // Blend tree: 0 idle, 0.5 walk, 1 run, picked by the real ground speed so the feet don't slide;
        // faster than the run clip (sprinting) plays it faster instead.
        float blend = speed <= walkStride ? 0.5f * speed / walkStride : 0.5f + 0.5f * Mathf.InverseLerp(walkStride, runStride, speed);
        anim.SetFloat("Speed", blend, 0.1f, Time.deltaTime);
        anim.speed = speed > runStride ? speed / runStride : 1f;
    }
}
