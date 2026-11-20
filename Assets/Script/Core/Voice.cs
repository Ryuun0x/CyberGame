using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// The voice-over, at the Voice volume from Settings. Starts by itself and survives scene loads; nothing to place.
/// Each subtitle line has its own file, Resources/Voice/&lt;key&gt;.ogg, where the key is a hash of the line's text
/// (made by ArtSource/Voice/make_voice.py from the list in ArtSource/Voice/lines.py). NarrationManager and the
/// opening cutscenes call Play with the text they show, so a line is voiced wherever it appears.
/// One line at a time: a new line cuts off the one before. It pauses with the game (AudioListener.pause).
/// </summary>
public class Voice : MonoBehaviour
{
    static Voice instance;
    AudioSource source;
    readonly HashSet<string> missing = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        var go = new GameObject("Voice");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Voice>();
        instance.source = go.AddComponent<AudioSource>();
        instance.source.playOnAwake = false;
    }

    /// <summary>Plays the voice for this subtitle text, if it has one. Returns its length in seconds (0 if none).</summary>
    public static float Play(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (instance == null) Create();
        string key = Key(text);
        var clip = Resources.Load<AudioClip>("Voice/" + key);
        if (clip == null)
        {
            // So new lines get noticed and added to ArtSource/Voice/lines.py.
            if (Application.isEditor && instance.missing.Add(key)) Debug.Log($"[Voice] no line for {key}: {text}");
            return 0;
        }
        instance.source.Stop();
        instance.source.clip = clip;
        instance.source.volume = GameSettings.Voice;
        instance.source.Play();
        return clip.length;
    }

    /// <summary>Seconds left of the line playing now (0 if none). Counts while the game is paused, too.</summary>
    public static float Remaining
    {
        get
        {
            if (instance == null || instance.source.clip == null) return 0;
            var s = instance.source;
            bool on = s.isPlaying || (AudioListener.pause && s.time > 0);
            return on ? Mathf.Max(0, s.clip.length - s.time) : 0;
        }
    }

    public static void Stop()
    {
        if (instance != null) instance.source.Stop();
    }

    void Update() => source.volume = GameSettings.Voice; // the slider can move while a line plays

    /// <summary>FNV-1a (32 bit) of the text without rich-text tags. Must match key() in make_voice.py.</summary>
    public static string Key(string text)
    {
        uint h = 0x811C9DC5;
        foreach (byte b in Encoding.UTF8.GetBytes(Regex.Replace(text, "<[^>]*>", "").Trim()))
            h = (h ^ b) * 0x01000193;
        return h.ToString("x8");
    }
}
