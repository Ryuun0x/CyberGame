using StarterAssets;
using UnityEngine;

// First-person body: the character model follows the player capsule and animates with its speed.
// The head is hidden so it never covers the camera.
public class PlayerBody : MonoBehaviour
{
    public GameObject character;
    public RuntimeAnimatorController animator;
    public Vector3 offset = Vector3.zero;

    CharacterController controller;
    FirstPersonController fps;
    Animator anim;
    Transform model, head;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        fps = GetComponent<FirstPersonController>();
        if (character == null || controller == null) return;
        var body = Instantiate(character, transform);
        body.name = "PlayerBody";
        body.transform.localPosition = offset;
        body.transform.localRotation = Quaternion.identity;
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
    }

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
        // Legs face the way you move; backing up keeps facing forward and plays the walk in reverse.
        Vector3 local = transform.InverseTransformDirection(new Vector3(v.x, 0, v.z));
        float yaw = 0, direction = 1;
        if (speed > 0.2f)
        {
            yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            if (Mathf.Abs(yaw) > 100) { yaw -= 180 * Mathf.Sign(yaw); direction = -1; }
        }
        model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(0, yaw, 0), 10 * Time.deltaTime);
        anim.SetFloat("Direction", direction);
        float walk = fps != null ? fps.MoveSpeed : 4, sprint = fps != null ? fps.SprintSpeed : 6;
        // Blend tree: 0 idle, 0.5 walk, 1 run.
        float blend = speed <= walk ? 0.5f * speed / walk : 0.5f + 0.5f * Mathf.InverseLerp(walk, sprint, speed);
        anim.SetFloat("Speed", blend, 0.1f, Time.deltaTime);
    }
}
