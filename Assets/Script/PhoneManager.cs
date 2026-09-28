using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PhoneManager : MonoBehaviour
{
    public GameObject phoneCanvas;
    public MonoBehaviour firstPersonController;
    [Header("Gatekeep")]
    public bool requireFlashDrive = true;
    [TextArea]
    public string blockedNarration = "I don't need my phone right now. Let me focus on my thesis.";
    public bool IsOpen { get; private set; }
    public CityMapController Map { get; private set; }
    float previousTimeScale;
    CursorLockMode previousCursorLock;
    bool previousCursorVisible, previousControllerEnabled;
    RaycastCrosshair interaction;
    bool previousInteractionEnabled;

    void Start()
    {
        if (phoneCanvas == null) return;
        phoneCanvas.SetActive(false);
        interaction = GetComponentInChildren<RaycastCrosshair>(true);
        Map = GetComponent<CityMapController>();
        if (Map == null) Map = gameObject.AddComponent<CityMapController>();
        Map.Initialize(this);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return;
        bool tab = Keyboard.current.tabKey.wasPressedThisFrame;
        bool mapKey = Keyboard.current.mKey.wasPressedThisFrame;
        bool back = Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        bool tab = Input.GetKeyDown(KeyCode.Tab);
        bool mapKey = Input.GetKeyDown(KeyCode.M);
        bool back = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (tab) { TogglePhone(); return; }
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool typing = selected != null && selected.GetComponent<TMP_InputField>() != null;
        if (mapKey && !typing)
        {
            if (Map != null && Map.IsExpanded) ClosePhone();
            else OpenMap();
        }
        else if (back && IsOpen) ClosePhone();
    }

    public void TogglePhone()
    {
        if (IsOpen) ClosePhone();
        else TryOpenPhone();
    }

    public bool TryOpenPhone()
    {
        if (IsOpen) return true;
        if (phoneCanvas == null || Time.timeScale == 0 || (firstPersonController != null && !firstPersonController.enabled)) return false;
        if (requireFlashDrive && (GameProgressManager.Instance == null || !GameProgressManager.Instance.hasFlashDrive))
        {
            if (NarrationManager.Instance != null) NarrationManager.Instance.Show(blockedNarration);
            return false;
        }
        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        previousControllerEnabled = firstPersonController != null && firstPersonController.enabled;
        previousInteractionEnabled = interaction != null && interaction.enabled;
        IsOpen = true;
        phoneCanvas.SetActive(true);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (firstPersonController != null) firstPersonController.enabled = false;
        if (interaction != null) interaction.enabled = false;
        return true;
    }

    public void OpenMap()
    {
        if (Map == null || Map.IsIndoor || !TryOpenPhone()) return;
        phoneCanvas.GetComponentInChildren<PhoneHomeScreen>(true)?.OpenMap();
    }

    public void ClosePhone()
    {
        if (!IsOpen) return;
        if (Map != null && Map.IsExpanded)
            phoneCanvas.GetComponentInChildren<PhoneHomeScreen>(true)?.GoHome();
        IsOpen = false;
        if (phoneCanvas != null) phoneCanvas.SetActive(false);
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        if (firstPersonController != null) firstPersonController.enabled = previousControllerEnabled;
        if (interaction != null) interaction.enabled = previousInteractionEnabled;
    }

    void OnDisable() => ClosePhone();
}
