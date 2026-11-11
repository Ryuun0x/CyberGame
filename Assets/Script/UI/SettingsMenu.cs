using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// The Settings screen. It sits in the same left-side column as the menu it belongs to (Start Menu or Pause Menu):
/// Open() hides that menu and shows the settings in its place, Back() swaps them back.
/// Three tabs (Graphics / Audio / Controls) are placed pages; the tab buttons call ShowTab(index).
/// Every slider, toggle and arrow button calls one method here from the Inspector, which changes GameSettings
/// right away (no "Apply" button). Everything is saved when the screen closes.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    static readonly Color TabOn = Color.white;
    static readonly Color TabOff = new Color(1f, 1f, 1f, 0.45f);

    [Tooltip("The menu column this screen replaces while open (MenuContainer).")]
    public GameObject menu;
    [Tooltip("This screen's column (the Settings object itself). Keep it hidden in the scene; Open() shows it.")]
    public GameObject screen;

    [Header("Start Menu only (the pause menu already blurs the game)")]
    [Tooltip("Dark overlay behind the settings column.")]
    public GameObject overlay;
    [Tooltip("Blur Volume (PauseBlur profile), shown while Settings is open.")]
    public GameObject blur;

    [Header("Tabs (same order: Graphics, Audio, Controls)")]
    public GameObject[] pages;
    public TextMeshProUGUI[] tabLabels;
    public GameObject[] tabUnderlines;

    [Header("Graphics")]
    public Toggle fullscreen;
    public TextMeshProUGUI resolutionText;
    public Toggle vSync;
    public TextMeshProUGUI qualityText;

    [Header("Audio")]
    public Slider master;
    public Slider music;
    public Slider sfx;

    [Header("Controls")]
    public Slider sensitivity;
    [Tooltip("Assist: arrows on the ground to the next goal.")]
    public Toggle pathGuide;

    public bool IsOpen => screen.activeSelf;

    UniversalAdditionalCameraData cameraData;
    bool cameraHadPostProcessing;

    // Called by the Settings button.
    public void Open()
    {
        // Show the saved values without triggering the change methods.
        fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen);
        vSync.SetIsOnWithoutNotify(GameSettings.VSync);
        master.SetValueWithoutNotify(GameSettings.Master);
        music.SetValueWithoutNotify(GameSettings.Music);
        sfx.SetValueWithoutNotify(GameSettings.Sfx);
        sensitivity.SetValueWithoutNotify(GameSettings.Sensitivity);
        pathGuide.SetIsOnWithoutNotify(GameSettings.PathGuide);
        RefreshTexts();

        ShowBackground(true);
        menu.SetActive(false);
        screen.SetActive(true);
        ShowTab(0);
    }

    // Called by the Back button (and by Esc in the pause menu).
    public void Back()
    {
        GameSettings.Save();
        screen.SetActive(false);
        menu.SetActive(true);
        ShowBackground(false);
    }

    // Start Menu: darken and blur the room behind Settings. A blur Volume only works while the camera's
    // post-processing is on, so it is switched on only while Settings is open (same as the pause menu).
    void ShowBackground(bool show)
    {
        if (overlay != null) overlay.SetActive(show);
        if (blur == null) return;
        if (show)
        {
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>(); // the Start Menu camera isn't tagged MainCamera
            cameraData = cam != null ? cam.GetComponent<UniversalAdditionalCameraData>() : null;
            if (cameraData != null) { cameraHadPostProcessing = cameraData.renderPostProcessing; cameraData.renderPostProcessing = true; }
        }
        else if (cameraData != null) cameraData.renderPostProcessing = cameraHadPostProcessing;
        blur.SetActive(show);
    }

    // Called by the tab buttons: 0 = Graphics, 1 = Audio, 2 = Controls.
    public void ShowTab(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
            tabLabels[i].color = i == index ? TabOn : TabOff;
            tabUnderlines[i].SetActive(i == index);
        }
    }

    // ── Graphics ──

    public void SetFullscreen(bool on) { GameSettings.Fullscreen = on; GameSettings.ApplyGraphics(); }
    public void SetVSync(bool on) { GameSettings.VSync = on; GameSettings.ApplyGraphics(); }

    // The < and > buttons next to Resolution: step is -1 or +1.
    // Steps through 720p / 900p / 1080p / 1440p / 4K, up to what the monitor can show.
    public void ChangeResolution(int step)
    {
        GameSettings.Resolution = Mathf.Clamp(GameSettings.Resolution + step, 0, GameSettings.LargestResolution());
        GameSettings.ApplyGraphics();
        RefreshTexts();
    }

    // The < and > buttons next to Quality.
    public void ChangeQuality(int step)
    {
        GameSettings.Quality = Mathf.Clamp(GameSettings.Quality + step, 0, QualitySettings.names.Length - 1);
        GameSettings.ApplyGraphics();
        RefreshTexts();
    }

    void RefreshTexts()
    {
        resolutionText.text = GameSettings.ResolutionNames[GameSettings.Resolution];
        qualityText.text = GameSettings.Quality == 0 ? "Low" : "High";
    }

    // ── Audio ──

    public void SetMaster(float value) { GameSettings.Master = value; AudioListener.volume = value; }
    public void SetMusic(float value) { GameSettings.Music = value; }
    public void SetSfx(float value) { GameSettings.Sfx = value; }

    // ── Controls ──

    public void SetSensitivity(float value) { GameSettings.Sensitivity = value; }
    public void SetPathGuide(bool on) { GameSettings.PathGuide = on; }
}
