using UnityEngine;

// Keeps a character's hips horizontally over its feet so a walk clip can never carry the body
// away and snap it back at the loop, whatever the rig or clip import settings.
public class PinHips : MonoBehaviour
{
    Transform hips;
    Vector3 rest;

    void Start()
    {
        hips = GetComponentInChildren<Animator>()?.GetBoneTransform(HumanBodyBones.Hips);
        if (hips != null) rest = transform.InverseTransformPoint(hips.position);
    }

    void LateUpdate()
    {
        if (hips == null) return;
        Vector3 local = transform.InverseTransformPoint(hips.position);
        local.x = rest.x;
        local.z = rest.z;
        hips.position = transform.TransformPoint(local);
    }
}
