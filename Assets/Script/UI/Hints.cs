using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tutorial hints, in the style of The Last of Us: a small notice on the left side of the screen
/// (a dark band fading to the right, an icon, a bold title and a line with the key drawn as a key cap).
/// Each hint appears once, the first time the player needs that control, and disappears when the player
/// presses the key (or after a few seconds). Only one shows at a time: the next waits until the current one
/// is done, so the player learns one thing at a time.
///
/// Placed in each game scene: UI > Hints (this component) > Stack (left side, a bit below the middle),
/// with a hidden "HintTemplate" (children: Band, Icon, Title, Body) that is copied for every hint.
///
/// Use from any script, e.g. when the flash drive is picked up:
///     Hints.Show("phone", "phone", "Phone", $"Press {Hints.Cap("TAB")} to open", Key.Tab);
/// </summary>
public class Hints : MonoBehaviour
{
    [System.Serializable]
    public struct NamedIcon { public string name; public Sprite sprite; }

    [Tooltip("Where the hint appears (left side, a bit below the middle).")]
    public Transform stack;
    [Tooltip("Hidden hint copied for each new hint. Children: Icon (Image), Title and Body (TextMeshPro).")]
    public GameObject template;
    [Tooltip("Icons a hint can use, by name (e.g. \"phone\", \"keyboard\", \"mouse\").")]
    public NamedIcon[] icons;
    [Tooltip("Seconds a hint stays if the player never presses its key.")]
    public float defaultSeconds = 9f;
    [Tooltip("City scene only: shows the Run and Map hints a moment after arriving outside.")]
    public bool cityHints;

    static Hints instance;
    // Hints already shown in this playthrough (cleared when a new game starts).
    static readonly HashSet<string> shown = new HashSet<string>();
    // Hints waiting for their turn: what to show and when it's done.
    readonly List<(string icon, string title, string body, System.Func<bool> done)> waiting = new List<(string, string, string, System.Func<bool>)>();
    bool showing;
    bool interrupt; // a "now" hint arrived: the one on screen steps aside

    void Awake()
    {
        instance = this;
        template.SetActive(false);
    }

    void Start()
    {
        if (!cityHints) return;
        ShowAfter(2.5f, "run", "keys", "Run", $"Hold {Cap("Shift")} while walking", Key.LeftShift, Key.RightShift);
        ShowAfter(3.5f, "map", "map", "Map", $"Press {Cap("M")} to open the map", Key.M);
    }

    // Hidden while the pause menu or Settings is open (they sit on the same left side); it comes back on resume.
    void Update()
    {
        if (stack.gameObject.activeSelf == PauseMenu.IsPaused) stack.gameObject.SetActive(!PauseMenu.IsPaused);
    }

    // A key drawn as a key cap inside a line of text, e.g. $"Press {Hints.Cap("E")} to interact".
    public static string Cap(string key) => $"<mark=#FFFFFF30 padding=\"10,10,4,4\"><b> {key} </b></mark>";

    // Shows a hint once per playthrough. It goes away when one of doneKeys is pressed (or after the time runs out).
    public static void Show(string id, string icon, string title, string body, params Key[] doneKeys)
    {
        ShowUntil(id, icon, title, body, () => Pressed(doneKeys));
    }

    // The same, but done when `done` becomes true (e.g. a mouse click). now = true puts it at the front of the line,
    // for hints about what the player is doing right now (the laptop that just opened).
    public static void ShowUntil(string id, string icon, string title, string body, System.Func<bool> done, bool now = false)
    {
        if (instance == null || shown.Contains(id)) return;
        shown.Add(id);
        var hint = (icon, title, body, done);
        if (now) { instance.waiting.Insert(0, hint); instance.interrupt = instance.showing; }
        else instance.waiting.Add(hint);
        if (!instance.showing) instance.StartCoroutine(instance.ShowInTurn());
    }

    public static bool MouseClicked() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

    // One hint at a time, in the order they were asked for.
    IEnumerator ShowInTurn()
    {
        showing = true;
        while (waiting.Count > 0)
        {
            var next = waiting[0];
            waiting.RemoveAt(0);
            yield return Run(next.icon, next.title, next.body, next.done);
            yield return new WaitForSecondsRealtime(0.4f); // a short gap before the next one
        }
        showing = false;
    }

    // The same, after a delay (game time that stops while paused).
    public static void ShowAfter(float seconds, string id, string icon, string title, string body, params Key[] doneKeys)
    {
        if (instance != null) instance.StartCoroutine(instance.Later(seconds, id, icon, title, body, doneKeys));
    }

    IEnumerator Later(float seconds, string id, string icon, string title, string body, Key[] doneKeys)
    {
        for (float t = 0; t < seconds; t += PauseMenu.IsPaused ? 0 : Time.unscaledDeltaTime) yield return null;
        while (!StoryTools.PlayerCanMove) yield return null; // these are walking-around hints
        Show(id, icon, title, body, doneKeys);
    }

    public static bool WasShown(string id) => shown.Contains(id);

    // Called when a new game starts, so the hints teach again.
    public static void ResetAll() => shown.Clear();

    IEnumerator Run(string icon, string title, string body, System.Func<bool> done)
    {
        interrupt = false; // a "now" hint that arrived between hints is this one: nothing to step aside for
        var hint = Instantiate(template, stack);
        hint.SetActive(true);
        hint.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = title;
        hint.transform.Find("Body").GetComponent<TextMeshProUGUI>().text = body;
        var iconImage = hint.transform.Find("Icon").GetComponent<Image>();
        iconImage.sprite = IconFor(icon);
        iconImage.enabled = iconImage.sprite != null;

        var group = hint.GetComponent<CanvasGroup>();
        yield return Fade(group, 0f, 1f, 0.4f);

        // Stay until a matching key is pressed (ignoring the first moment) or the time runs out.
        float time = 0f;
        while (time < defaultSeconds)
        {
            if (!PauseMenu.IsPaused) time += Time.unscaledDeltaTime;
            if (time > 0.6f && !PauseMenu.IsPaused && done()) break; // keys pressed in the menus don't count
            if (interrupt) break;
            yield return null;
        }

        bool quick = interrupt;
        interrupt = false;
        yield return Fade(group, 1f, 0f, quick ? 0.15f : 0.5f);
        Destroy(hint);
    }

    Sprite IconFor(string name)
    {
        foreach (var icon in icons) if (icon.name == name) return icon.sprite;
        return null;
    }

    // Done when one of the keys is pressed. A hint with no keys (e.g. "Look around") is done when the mouse moves.
    static bool Pressed(Key[] keys)
    {
        if (keys.Length == 0) return Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 40f;
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;
        foreach (var key in keys) if (keyboard[key].wasPressedThisFrame) return true;
        return false;
    }

    static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
    {
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / seconds)
        {
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        group.alpha = to;
    }
}
