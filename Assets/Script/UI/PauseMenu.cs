using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pause menu: press Esc (or P) while walking around to pause the game.
/// The menu is the "PauseMenu" prefab, placed in both the Interior and MainScene(city) scenes,
/// so editing the prefab changes both. It uses the same left-side layout as the Start Menu.
/// Its buttons call Resume / MainMenu / QuitGame from their OnClick.
///
/// While paused:
///  - time stops (Time.timeScale = 0) and the player can't move or turn the camera;
///  - the "PauseBlur" Volume blurs and darkens the game behind the menu. A Volume only works when the camera's
///    post-processing is on, so it is switched on while paused and back off on resume (the normal game look
///    never changes).
///
/// Esc only pauses when the player is walking around. When the phone or laptop is open, Esc closes
/// that instead (their own scripts handle it), and nothing pauses during cutscenes or the results screen.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Tooltip("The pause screen (dark overlay + left-side menu). Hidden until the game is paused.")]
    public GameObject pauseScreen;

    [Tooltip("The PauseBlur Volume (blur + darken). Hidden until the game is paused.")]
    public GameObject blurVolume;

    [Tooltip("The Settings screen inside this menu.")]
    public SettingsMenu settings;

    public static bool IsPaused { get; private set; }

    bool couldPauseLastFrame;
    FirstPersonController player;
    StarterAssetsInputs playerInput;
    RaycastCrosshair crosshair;
    UniversalAdditionalCameraData cameraData;
    bool cameraHadPostProcessing;

    void Start()
    {
        pauseScreen.SetActive(false);
        blurVolume.SetActive(false);
        IsPaused = false;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool pressed = Keyboard.current != null &&
                       (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame);
#else
        bool pressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
        if (!pressed) return;
        if (IsPaused && settings.IsOpen) settings.Back(); // Esc in Settings goes back to the pause menu
        else if (IsPaused) Resume();
        else if (couldPauseLastFrame) Pause();
    }

    // Checked at the end of each frame, so the Esc that just closed the phone or laptop
    // doesn't also open the pause menu in the same frame.
    void LateUpdate()
    {
        couldPauseLastFrame = CanPause();
        KeepCursorLocked();
    }

    // While walking around, the mouse must be locked and hidden (it steers the camera). Closing the laptop or
    // phone with Esc can leave it free: in the Unity Editor, Esc always releases the mouse, so the player had to
    // click the game window again. Lock it again here whenever it got released while walking.
    void KeepCursorLocked()
    {
        if (IsPaused || DebriefScreen.IsOpen || CafeReceipt.IsOpen || Time.timeScale == 0) return;
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        if (player == null || !player.enabled) return; // laptop, phone or a cutscene uses the mouse
        if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;
        if (Cursor.visible) Cursor.visible = false;
    }

    // Pausing is allowed only while the player is walking around freely.
    bool CanPause()
    {
        if (IsPaused || Time.timeScale == 0 || DebriefScreen.IsOpen || CafeReceipt.IsOpen) return false;
        if (WakeUpSequence.Playing) return true; // the opening cutscenes (prologue + wake-up) can be paused too
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        return player != null && player.enabled;
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0;

        // Stop the player and the camera. The look input is cleared too, so the camera
        // doesn't jump when the game resumes.
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            player.enabled = false;
            playerInput = player.GetComponent<StarterAssetsInputs>();
        }
        if (playerInput != null) playerInput.look = Vector2.zero;
        if (crosshair == null) crosshair = FindFirstObjectByType<RaycastCrosshair>();
        ShowCrosshair(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Blur the game behind the menu.
        var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>(); // cutscene cameras aren't tagged MainCamera
        cameraData = cam != null ? cam.GetComponent<UniversalAdditionalCameraData>() : null;
        if (cameraData != null)
        {
            cameraHadPostProcessing = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = true;
        }
        blurVolume.SetActive(true);
        pauseScreen.SetActive(true);
    }

    // Called by the Resume button, and by pressing Esc again.
    public void Resume()
    {
        pauseScreen.SetActive(false);
        blurVolume.SetActive(false);
        if (cameraData != null) cameraData.renderPostProcessing = cameraHadPostProcessing;

        IsPaused = false;
        Time.timeScale = 1;
        if (playerInput != null) playerInput.look = Vector2.zero;
        if (player != null) player.enabled = true;
        if (!WakeUpSequence.Playing) ShowCrosshair(true); // stays hidden if we're back in a cutscene
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // The crosshair dot and the "Press E" prompt are hidden while paused.
    void ShowCrosshair(bool show)
    {
        if (crosshair == null) return;
        crosshair.enabled = show;
        if (crosshair.crosshairDot != null) crosshair.crosshairDot.gameObject.SetActive(show);
        if (!show && crosshair.promptPanel != null) crosshair.promptPanel.gameObject.SetActive(false);
    }

    // Called by the Main Menu button: starts the game over from the Start Menu.
    public void MainMenu()
    {
        IsPaused = false;
        DebriefScreen.LoadFresh(DebriefScreen.MenuScene);
    }

    // Called by the Quit button.
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
