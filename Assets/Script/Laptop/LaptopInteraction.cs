using UnityEngine;
using System.Collections;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LaptopInteraction : MonoBehaviour, IInteractable
{
    [Header("Camera")]
    public Transform laptopViewPoint;
    public float zoomSpeed = 2f;

    [Header("UI")]
    public GameObject laptopCanvas;

    [Header("UI to Hide")]
    public GameObject crosshairDot;
    public GameObject interactPrompt;


    [Header("Player")]
    public MonoBehaviour firstPersonController;
    public GameObject playerFollowCamera; // drag PlayerFollowCamera here

    private bool _isUsingLaptop = false;
    private bool _isAnimating = false;
    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;
    private Camera _camera;

    void Start()
    {
        _camera = Camera.main;
        if (laptopCanvas != null)
            laptopCanvas.SetActive(false);
    }

    void Update()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            return;
        }

        // (Not during the flash drive close-up: the camera is away from the screen.)
        var port = GetComponent<LaptopUsbPort>();
        if (_isUsingLaptop && !_isAnimating && !(port != null && port.Busy))
        {
            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            #else
            if (Input.GetKeyDown(KeyCode.Escape))
            #endif
            {
                StartCoroutine(CloseLaptop());
            }
        }
    }

    public void Interact()
    {
        if (!_isUsingLaptop && !_isAnimating)
            StartCoroutine(OpenLaptop());
    }

    IEnumerator OpenLaptop()
    {
        // Laptop hints go to the front of the line: first the mouse, then how to close it.
        // (Added in reverse: each "now" hint goes in front of the one before.)
        Hints.ShowUntil("laptop-close", "laptop", "Close laptop", $"Press {Hints.Cap("Esc")} to close the laptop",
            () => UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame || !_isUsingLaptop, now: true);
        Hints.ShowUntil("laptop-click", "mouse", "Laptop", "Click with the mouse to use apps and buttons", Hints.MouseClicked, now: true);
        _isUsingLaptop = true;
        _isAnimating = true;

        if (firstPersonController != null)
            firstPersonController.enabled = false;
        
        // Hide crosshair and prompt
        if (crosshairDot != null) crosshairDot.SetActive(false);
        if (interactPrompt != null) interactPrompt.SetActive(false);

        // Disable follow camera so Cinemachine stops controlling MainCamera
        if (playerFollowCamera != null)
            playerFollowCamera.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _originalCamPos = _camera.transform.position;
        _originalCamRot = _camera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            float t = Mathf.SmoothStep(0f, 1f, elapsed);
            _camera.transform.position = Vector3.Lerp(
                _originalCamPos, laptopViewPoint.position, t);
            _camera.transform.rotation = Quaternion.Slerp(
                _originalCamRot, laptopViewPoint.rotation, t);
            yield return null;
        }

        if (laptopCanvas != null)
            laptopCanvas.SetActive(true);

        _isAnimating = false;
    }

    IEnumerator CloseLaptop()
    {
        _isAnimating = true;

        if (laptopCanvas != null)
            laptopCanvas.SetActive(false);

        float elapsed = 0f;
        Vector3 currentPos = _camera.transform.position;
        Quaternion currentRot = _camera.transform.rotation;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            float t = Mathf.SmoothStep(0f, 1f, elapsed);
            _camera.transform.position = Vector3.Lerp(
                currentPos, _originalCamPos, t);
            _camera.transform.rotation = Quaternion.Slerp(
                currentRot, _originalCamRot, t);
            yield return null;
        }

        // Re-enable follow camera
        if (playerFollowCamera != null)
            playerFollowCamera.SetActive(true);

        if (firstPersonController != null)
            firstPersonController.enabled = true;
        
        // Show crosshair and prompt again
        if (crosshairDot != null) crosshairDot.SetActive(true);
        if (interactPrompt != null) interactPrompt.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _isUsingLaptop = false;
        _isAnimating = false;

    }
}