using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RaycastCrosshair : MonoBehaviour
{
    public Image crosshairDot;
    public float raycastDistance = 3f;
    public TextMeshProUGUI interactPrompt;

    private Camera _camera;
    private RaycastHit _hit;
    private bool _isHitting;

    void Start()
    {
        _camera = Camera.main;
        if (interactPrompt != null)
            interactPrompt.text = "";
    }

    void Update()
    {
        Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        _isHitting = Physics.Raycast(ray, out _hit, raycastDistance, ~0, QueryTriggerInteraction.Collide);

        if (_isHitting && _hit.collider.GetComponent<IInteractable>() != null)
        {
            if (interactPrompt != null)
                interactPrompt.text = "Press E to interact";

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.eKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.E))
#endif
            {
                _hit.collider.GetComponent<IInteractable>().Interact();
            }
        }
        else
        {
            if (interactPrompt != null)
                interactPrompt.text = "";
        }
    }
}

public interface IInteractable
{
    void Interact();
}