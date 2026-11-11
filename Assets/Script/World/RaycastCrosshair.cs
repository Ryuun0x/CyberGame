using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RaycastCrosshair : MonoBehaviour
{
    public Image crosshairDot;

    [Tooltip("Max distance (in units) the player can interact with objects")]
    public float raycastDistance = 3f;
    private const float MAX_INTERACT_DISTANCE = 3f;

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
    private StarterAssets.FirstPersonController _walking;
    private IHoldInteractable _holding; // E went down on this, waiting to see if it's a tap or a hold
    private float _holdTime;
    private const float HOLD_SECONDS = 0.5f;

    void Start()
    {
        _camera = Camera.main;
        if (promptPanel != null)
        {
            promptPanel.gameObject.SetActive(false);
            _promptCanvas = promptPanel.GetComponentInParent<Canvas>(true); // true: still found if the canvas starts hidden
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

        // No prompts (and no E) while the player can't walk: using the laptop, the phone, a cutscene.
        // Otherwise the laptop camera would keep aiming at the laptop and show "Use Laptop".
        if (_walking == null) _walking = GetComponentInParent<StarterAssets.FirstPersonController>();
        if (_walking != null && !_walking.enabled)
        {
            HidePrompt();
            return;
        }

        // Use the hardcoded constant so Inspector serialization can't break it
        float maxDist = MAX_INTERACT_DISTANCE;

        Ray ray = _camera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2, 0));

        _isHitting = Physics.Raycast(ray, out _hit, maxDist,
            ~0, QueryTriggerInteraction.Ignore);

        if (_isHitting && _hit.distance <= maxDist)
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

                var hold = targetMB != null ? targetMB.GetComponent<IHoldInteractable>() : null;
                bool canHold = hold != null && hold.CanHold && !isLocked;
                ShowPrompt(interactable, gate, isLocked, canHold ? hold.HoldPrompt : null);

                if (canHold)
                {
                    HandleHold(interactable, hold);
                }
                else
                {
                    _holding = null;
                }

                #if ENABLE_INPUT_SYSTEM
                if (!canHold && Keyboard.current.eKey.wasPressedThisFrame)
                #else
                if (!canHold && Input.GetKeyDown(KeyCode.E))
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

    // Hold E on something with a second action (IHoldInteractable, e.g. pick up the laptop):
    // released before HOLD_SECONDS = a normal tap (Interact), held that long = HoldInteract.
    void HandleHold(IInteractable interactable, IHoldInteractable hold)
    {
        var e = Keyboard.current.eKey;
        if (e.wasPressedThisFrame) { _holding = hold; _holdTime = 0f; }
        if (_holding != hold) return;

        if (e.isPressed)
        {
            _holdTime += Time.deltaTime;
            if (_holdTime >= HOLD_SECONDS) { _holding = null; hold.HoldInteract(); }
        }
        else
        {
            _holding = null;
            interactable.Interact();
        }
    }

    void ShowPrompt(IInteractable interactable,
                    GatekeepRequirement gate, bool isLocked, string holdPrompt = null)
    {
        // A missing prompt must never stop E from working, so bail out instead of throwing.
        if (promptPanel == null || _promptCanvas == null) return;

        MonoBehaviour targetMB = interactable as MonoBehaviour;
        if (targetMB == null) return;

        // Get custom label or fall back to default
        InteractableLabel label = targetMB.GetComponent<InteractableLabel>();
        string actionText = label != null ? label.promptText : "Interact";
        Vector3 offset = label != null ? label.promptOffset : new Vector3(0f, 0.5f, 0f);

        // Override prompt text if locked and override is set
        if (isLocked && gate != null && !string.IsNullOrEmpty(gate.lockedPromptOverride))
            actionText = gate.lockedPromptOverride;
        if (holdPrompt != null)
            actionText += $"  <alpha=#99>·  Hold: {holdPrompt}";

        // Update text and color
        if (promptActionText != null)
        {
            promptActionText.text = actionText;
            promptActionText.color = isLocked ? lockedPromptColor : Color.white;
        }

        // Anchor to the center of the WHOLE object (all its colliders), not just the part the ray hit:
        // the laptop's screen and keyboard are separate colliders and the prompt jumped between them.
        // (Not the pivot either: a door's pivot is on its hinge.)
        Vector3 center = targetMB.transform.position;
        bool any = false;
        Bounds whole = default;
        foreach (var c in targetMB.GetComponentsInChildren<Collider>())
        {
            if (!any) { whole = c.bounds; any = true; } else whole.Encapsulate(c.bounds);
        }
        if (any) center = whole.center;
        Vector3 worldPos = center + offset;
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
        if (!isLocked) Hints.Show("interact", "hand", "Interact", $"Press {Hints.Cap("E")} to use what you're looking at", UnityEngine.InputSystem.Key.E);
    }

    void HidePrompt()
    {
        _holding = null; // looked away mid-hold: no tap or pick-up later
        if (promptPanel != null)
            promptPanel.gameObject.SetActive(false);
    }
}

public interface IInteractable
{
    void Interact();
}

// A second action for holding E instead of tapping it (next to the object's IInteractable).
public interface IHoldInteractable
{
    bool CanHold { get; }
    string HoldPrompt { get; }
    void HoldInteract();
}
