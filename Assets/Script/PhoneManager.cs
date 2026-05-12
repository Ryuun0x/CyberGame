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

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.tabKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Tab))
#endif
        {
            TogglePhone();
        }
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