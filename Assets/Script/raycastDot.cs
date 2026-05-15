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

    [Header("Gatekeep Visual")]
    [Tooltip("Prompt text color when interaction is locked")]
    public Color lockedPromptColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Screen-Space Prompt (put inside UICanvas)")]
    [Tooltip("The prompt panel RectTransform (parent of KeyBadge + ActionText)")]
    public RectTransform promptPanel;
    [Tooltip("The action text label (e.g. 'Use Laptop')")]
    public TextMeshProUGUI promptActionText;

    private Camera _camera;
    private Canvas _promptCanvas;
    private RaycastHit _hit;
    private bool _isHitting;

    void Start()
    {
        _camera = Camera.main;
        if (promptPanel != null)
        {
            promptPanel.gameObject.SetActive(false);
            _promptCanvas = promptPanel.GetComponentInParent<Canvas>();
        }
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
                // ── Gatekeep Check ──────────────────────────────
                MonoBehaviour targetMB = interactable as MonoBehaviour;
                GatekeepRequirement gate = targetMB != null
                    ? targetMB.GetComponent<GatekeepRequirement>()
                    : null;
                bool isLocked = gate != null && !gate.IsAllowed();

                ShowPrompt(interactable, gate, isLocked);

                #if ENABLE_INPUT_SYSTEM
                if (Keyboard.current.eKey.wasPressedThisFrame)
                #else
                if (Input.GetKeyDown(KeyCode.E))
                #endif
                {
                    if (isLocked)
                    {
                        // Blocked — show narration hint instead
                        if (NarrationManager.Instance != null)
                            NarrationManager.Instance.Show(gate.blockedNarration);
                    }
                    else
                    {
                        interactable.Interact();
                    }
                }
            }
            else
            {
                HidePrompt();
            }
        }
        else
        {
            HidePrompt();
        }
    }

    void ShowPrompt(IInteractable interactable,
                    GatekeepRequirement gate, bool isLocked)
    {
        if (promptPanel == null) return;

        MonoBehaviour targetMB = interactable as MonoBehaviour;
        if (targetMB == null) return;

        // Get custom label or fall back to default
        InteractableLabel label = targetMB.GetComponent<InteractableLabel>();
        string actionText = label != null ? label.promptText : "Interact";
        Vector3 offset = label != null ? label.promptOffset : new Vector3(0f, 0.5f, 0f);

        // Override prompt text if locked and override is set
        if (isLocked && gate != null && !string.IsNullOrEmpty(gate.lockedPromptOverride))
            actionText = gate.lockedPromptOverride;

        // Update text and color
        if (promptActionText != null)
        {
            promptActionText.text = actionText;
            promptActionText.color = isLocked ? lockedPromptColor : Color.white;
        }

        // Convert world position to screen position
        Vector3 worldPos = targetMB.transform.position + offset;
        Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);

        // Only show if object is in front of camera
        if (screenPos.z <= 0f)
        {
            HidePrompt();
            return;
        }

        // Convert screen point to canvas local position
        RectTransform canvasRect = _promptCanvas.transform as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out localPoint);

        promptPanel.anchoredPosition = localPoint;
        promptPanel.gameObject.SetActive(true);
    }

    void HidePrompt()
    {
        if (promptPanel != null)
            promptPanel.gameObject.SetActive(false);
    }
}

public interface IInteractable
{
    void Interact();
}
