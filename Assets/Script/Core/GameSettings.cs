using UnityEngine;

// The player's settings, saved on this computer (PlayerPrefs) so they stay after the game is closed.
// Loaded once when the game starts. The Settings screen (SettingsMenu) changes them; Save() runs when it closes.
public static class GameSettings
{
    // The common screen sizes the Resolution setting steps through (only the ones that fit the monitor).
    public static readonly Vector2Int[] Resolutions =
        { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3840, 2160) };
    public static readonly string[] ResolutionNames = { "720p", "900p", "1080p", "1440p", "4K" };

    // Graphics
    public static bool Fullscreen;
    public static int Resolution; // index in Resolutions
    public static bool VSync;
    public static int Quality; // the project's quality levels: 0 = Low (Mobile renderer), 1 = High (PC renderer)

    // Audio, 0 to 1. Music and SFX are for the sound effects to come: their AudioSources multiply by these.
    public static float Master, Music, Sfx;

    // Controls
    public static float Sensitivity; // multiplies the camera turn speed (FirstPersonController)
    public static bool PathGuide;    // assist: arrows on the ground to the next goal (World/PathGuide)

    // Read the saved values the first time anything uses GameSettings. Doing it here (not only at startup) matters:
    // if Unity reloads the scripts while playing, every value would otherwise drop to 0 and then be saved as 0.
    static GameSettings() => Load();

    // At startup, put the saved screen size, quality and volume into effect.
    [RuntimeInitializeOnLoadMethod]
    static void ApplyOnStart()
    {
        ApplyGraphics();
        AudioListener.volume = Master;
    }

    static void Load()
    {
        Fullscreen = PlayerPrefs.GetInt("Settings.Fullscreen", 1) == 1;
        // First time: the biggest common size that fits this monitor (e.g. 1080p on a 1080p screen).
        Resolution = Mathf.Min(PlayerPrefs.GetInt("Settings.Resolution", LargestResolution()), LargestResolution());
        VSync = PlayerPrefs.GetInt("Settings.VSync", 1) == 1;
        Quality = PlayerPrefs.GetInt("Settings.Quality", QualitySettings.names.Length - 1);
        Master = PlayerPrefs.GetFloat("Settings.Master", 1f);
        Music = PlayerPrefs.GetFloat("Settings.Music", 0.8f);
        Sfx = PlayerPrefs.GetFloat("Settings.Sfx", 1f);
        Sensitivity = PlayerPrefs.GetFloat("Settings.Sensitivity", 1f);
        PathGuide = PlayerPrefs.GetInt("Settings.PathGuide", 0) == 1;
    }

    public static void Save()
    {
        PlayerPrefs.SetInt("Settings.Fullscreen", Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("Settings.Resolution", Resolution);
        PlayerPrefs.SetInt("Settings.VSync", VSync ? 1 : 0);
        PlayerPrefs.SetInt("Settings.Quality", Quality);
        PlayerPrefs.SetFloat("Settings.Master", Master);
        PlayerPrefs.SetFloat("Settings.Music", Music);
        PlayerPrefs.SetFloat("Settings.Sfx", Sfx);
        PlayerPrefs.SetFloat("Settings.Sensitivity", Sensitivity);
        PlayerPrefs.SetInt("Settings.PathGuide", PathGuide ? 1 : 0);
        PlayerPrefs.Save();
    }

    // The index of the biggest common size that fits the monitor.
    public static int LargestResolution()
    {
        int best = 0;
        for (int i = 0; i < Resolutions.Length; i++)
            if (Resolutions[i].x <= Screen.currentResolution.width && Resolutions[i].y <= Screen.currentResolution.height)
                best = i;
        return best;
    }

    // Screen size, quality and V-Sync. Only called when one of them changes (it can flicker the screen).
    public static void ApplyGraphics()
    {
        var size = Resolutions[Resolution];
        Screen.SetResolution(size.x, size.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        QualitySettings.SetQualityLevel(Quality, true);
        QualitySettings.vSyncCount = VSync ? 1 : 0; // after the quality level, which sets its own V-Sync
    }
}
