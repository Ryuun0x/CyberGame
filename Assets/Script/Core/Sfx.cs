using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The game's sound effects (Resources/Sfx/*.wav, made by ArtSource/Sounds/make_sounds.py), played at the SFX
/// volume from Settings. Starts by itself and survives scene loads; nothing to place in a scene.
///   - Every click on a UI button plays "click", every toggle "toggle_on"/"toggle_off" (found under the mouse,
///     so new buttons get it automatically).
///   - Other moments call Sfx.Play("name"): pause/resume, phone_open/phone_close, notify, buzz, objective.
/// </summary>
public class Sfx : MonoBehaviour
{
    static Sfx instance;
    AudioSource source;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly List<RaycastResult> hits = new List<RaycastResult>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("Sfx");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Sfx>();
        instance.source = go.AddComponent<AudioSource>();
        instance.source.playOnAwake = false;
        instance.source.ignoreListenerPause = true;
        foreach (var clip in Resources.LoadAll<AudioClip>("Sfx")) instance.clips[clip.name] = clip;
    }

    public static void Play(string name, float volume = 1f)
    {
        if (instance == null || !instance.clips.TryGetValue(name, out var clip)) return;
        instance.source.PlayOneShot(clip, volume * GameSettings.Sfx);
    }

    // UI clicks: whatever is under the mouse when the button goes down.
    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || EventSystem.current == null) return;
        var pointer = new PointerEventData(EventSystem.current) { position = mouse.position.ReadValue() };
        hits.Clear();
        EventSystem.current.RaycastAll(pointer, hits);
        if (hits.Count == 0) return;
        var target = hits[0].gameObject.GetComponentInParent<Selectable>();
        if (target == null || !target.IsInteractable()) return;
        if (target is Toggle toggle) Play(toggle.isOn ? "toggle_off" : "toggle_on");
        else if (target is Button || target is Dropdown || target is TMPro.TMP_Dropdown) Play("click");
    }
}
