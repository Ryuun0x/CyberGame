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

        if (_isUsingLaptop && !_isAnimating)
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
        _isUsingLaptop = true;
        _isAnimating = true;

        if (firstPersonController != null)
            firstPersonController.enabled = false;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _isUsingLaptop = false;
        _isAnimating = false;
    }
}