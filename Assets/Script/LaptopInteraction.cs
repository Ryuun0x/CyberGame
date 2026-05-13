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

    [Header("Player")]
    public MonoBehaviour firstPersonController;
    public GameObject playerCameraRoot;

    private bool _isUsingLaptop = false;
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
        if (_isUsingLaptop)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.Escape))
#endif
            {
                CloseLaptop();
            }
        }
    }

    public void Interact()
    {
        if (!_isUsingLaptop)
            StartCoroutine(OpenLaptop());
    }

    IEnumerator OpenLaptop()
    {
        _isUsingLaptop = true;

        // Disable player control
        if (firstPersonController != null)
            firstPersonController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Save current camera position
        _originalCamPos = _camera.transform.position;
        _originalCamRot = _camera.transform.rotation;

        // Smoothly move camera to laptop view point
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

        // Show laptop UI
        if (laptopCanvas != null)
            laptopCanvas.SetActive(true);
    }

    void CloseLaptop()
    {
        StartCoroutine(ExitLaptop());
    }

    IEnumerator ExitLaptop()
    {
        // Hide UI
        if (laptopCanvas != null)
            laptopCanvas.SetActive(false);

        // Smoothly move camera back
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

        // Re-enable player control
        if (firstPersonController != null)
            firstPersonController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _isUsingLaptop = false;
    }
}