using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PhoneManager : MonoBehaviour
{
    public GameObject phoneCanvas;
    private bool _isPhoneOpen = false;

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
            Time.timeScale = 0f; // pause game
            Cursor.lockState = CursorLockMode.None; // unlock cursor
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f; // resume game
            Cursor.lockState = CursorLockMode.Locked; // lock cursor back
            Cursor.visible = false;
        }
    }
}