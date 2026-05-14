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

    [Header("World-Space Prompt")]
    [Tooltip("Root GameObject of the world-space prompt (will be moved to target)")]
    public GameObject worldPrompt;
    [Tooltip("The action text label inside the prompt (e.g. 'Use Laptop')")]
    public TextMeshProUGUI promptActionText;
    [Tooltip("Scale multiplier so the prompt stays readable at any distance")]
    public float baseScale = 0.008f;

    private Camera _camera;
    private RaycastHit _hit;
    private bool _isHitting;

    void Start()
    {
        _camera = Camera.main;
        if (worldPrompt != null)
            worldPrompt.SetActive(false);
    }

    void Update()
    {
        // Retry finding camera if null
        if (_camera == null)
        {
            _camera = Camera.main;
            return;
        }

        Ray ray = _camera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2, 0));

        _isHitting = Physics.Raycast(ray, out _hit, raycastDistance,
            ~0, QueryTriggerInteraction.Collide);

        if (_isHitting)
        {
            IInteractable interactable =
                _hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                ShowWorldPrompt(interactable);

                #if ENABLE_INPUT_SYSTEM
                if (Keyboard.current.eKey.wasPressedThisFrame)
                #else
                if (Input.GetKeyDown(KeyCode.E))
                #endif
                {
                    interactable.Interact();
                }
            }
            else
            {
                HideWorldPrompt();
            }
        }
        else
        {
            HideWorldPrompt();
        }
    }

    void ShowWorldPrompt(IInteractable interactable)
    {
        if (worldPrompt == null) return;

        MonoBehaviour targetMB = interactable as MonoBehaviour;
        if (targetMB == null) return;

        // Get custom label or fall back to default
        InteractableLabel label = targetMB.GetComponent<InteractableLabel>();
        string actionText = label != null ? label.promptText : "Interact";
        Vector3 offset = label != null ? label.promptOffset : new Vector3(0f, 0.5f, 0f);

        // Update text
        if (promptActionText != null)
            promptActionText.text = actionText;

        // Position at object center + offset
        worldPrompt.transform.position = targetMB.transform.position + offset;

        // Billboard — face the same direction as the camera so text is readable
        worldPrompt.transform.forward = _camera.transform.forward;

        worldPrompt.SetActive(true);
    }

    void HideWorldPrompt()
    {
        if (worldPrompt != null)
            worldPrompt.SetActive(false);
    }
}

public interface IInteractable
{
    void Interact();
}
