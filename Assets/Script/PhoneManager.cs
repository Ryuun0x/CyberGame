using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PhoneManager : MonoBehaviour
{
    public GameObject phoneCanvas;
    private bool _isPhoneOpen = false;

    // Add this reference
    public MonoBehaviour firstPersonController;

    [Header("Gatekeep")]
    [Tooltip("Require flash drive before allowing phone access")]
    public bool requireFlashDrive = true;

    [TextArea]
    [Tooltip("Narration shown when player tries to open phone before it's allowed")]
    public string blockedNarration = "I don't need my phone right now. Let me focus on my thesis.";

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.tabKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Tab))
#endif
        {
            // If phone is already open, always allow closing
            if (_isPhoneOpen)
            {
                TogglePhone();
                return;
            }

            // ── Gatekeep Check ──────────────────────────────
            if (requireFlashDrive && !IsPhoneAllowed())
            {
                if (NarrationManager.Instance != null)
                    NarrationManager.Instance.Show(blockedNarration);
                return;
            }

            TogglePhone();
        }
    }

    /// <summary>
    /// Phone is allowed once the player has the flash drive (entering "Secure Your Work" phase).
    /// This means the player has already checked the laptop and found the flash drive.
    /// </summary>
    bool IsPhoneAllowed()
    {
        if (GameProgressManager.Instance == null) return false;
        return GameProgressManager.Instance.hasFlashDrive;
    }

    void TogglePhone()
    {
        _isPhoneOpen = !_isPhoneOpen;
        phoneCanvas.SetActive(_isPhoneOpen);

        if (_isPhoneOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Disable camera/player control
            firstPersonController.enabled = false;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // Re-enable camera/player control
            firstPersonController.enabled = true;
        }
    }
}