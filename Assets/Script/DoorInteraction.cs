using UnityEngine;
using System.Collections;

public class CafeDoor : MonoBehaviour, IInteractable
{
    public Transform hinge;         // drag DoorHinge here
    public float openAngle = 90f;
    public float openSpeed = 2f;

    private bool _isOpen = false;
    private bool _isAnimating = false;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;

    void Start()
    {
        _closedRotation = hinge.rotation;
        _openRotation = Quaternion.Euler(
            hinge.eulerAngles.x,
            hinge.eulerAngles.y + openAngle,
            hinge.eulerAngles.z
        );
    }

    public void Interact()
    {
        if (_isAnimating) return;
        StartCoroutine(ToggleDoor());
    }

    IEnumerator ToggleDoor()
    {
        _isAnimating = true;
        _isOpen = !_isOpen;

        Quaternion target = _isOpen ? _openRotation : _closedRotation;
        Quaternion start = hinge.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            hinge.rotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        hinge.rotation = target;
        _isAnimating = false;

        // Update prompt text based on door state
        InteractableLabel label = GetComponent<InteractableLabel>();
        if (label != null)
            label.promptText = _isOpen ? "Close Door" : "Open Door";
    }
}
