using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static StoryTools;

/// <summary>
/// Prologue, "The night before" (11:47 PM): a short cinematic before the Chapter 1 wake-up, first time only.
/// The student sits on the bed in the dark, texting (Animator "StudentPhone": Sitting Idle on the body,
/// Texting on the arms and head through the UpperBodyPhone mask). The camera moves through five placed shots.
/// Text is placed like a film: letterbox bars, dialogue and texts as subtitles inside the bottom bar (never over
/// the character), and a small time card in the lower-left corner.
///   Shot 1  wide, slow push-in   time card "11:47 PM / The night before the deadline"
///   Shot 2  side close-up        Kai's texts, one subtitle at a time
///   Shot 3  face close-up        her reply, "Kai is offline."
///   Shot 4  the living-room TV   late-night news about the scam wave and Ghostline (TVNews screen + ticker)
///   Shot 5  she lies down        (Alice's lying clip at LieSpot, phone on the bed) "Ghostline..."
/// Then PlayWakeUp: time card "9:04 AM", shot 6: she wakes up and sits up in bed (getting-up clip), then black
/// and WakeUpSequence switches to first person. Any key skips; Esc/P pause.
///
/// Everything it uses is placed in the Interior scene and hidden until the prologue:
///   Story & Interactables > Prologue  (student, phone, TVNews, shots: each shot camera glides to its child "End")
///   UI > PrologueOverlay              (Letterbox, TimeCard, Subtitle)
/// On the WakeUpSequence object; WakeUpSequence.prologue points to it.
/// </summary>
public class Prologue : MonoBehaviour
{
    // Who says what. Edit the lines here.
    static readonly string[] KaiTexts =
    {
        "Are you still awake?",
        "I got hacked. Someone got into my account and emptied my e-wallet.",
        "Don't trust any message from me, okay? It might not be me.",
        "And turn on 2FA. Seriously."
    };
    const string Reply = "Wait, what?? Are you okay?";
    static readonly string[] NewsLines =
    {
        "...a wave of online scams is targeting students across the city tonight.",
        "Victims report fake \"free Wi-Fi\", urgent texts, and messages from hacked friends' accounts.",
        "Police link the attacks to a group calling itself Ghostline."
    };
    const string KaiColor = "#7ED670", YouColor = "#8DB8FF", NewsColor = "#FF8A80";

    [Header("Scene (Story & Interactables > Prologue)")]
    [Tooltip("The Prologue group: shown only during the prologue.")]
    public GameObject scene;
    [Tooltip("The six shots in order (the sixth is the morning wake-up). The camera starts at each shot and moves to its child \"End\".")]
    public Transform[] shots;
    [Tooltip("Shot lengths in seconds, same order.")]
    public float[] shotSeconds = { 6f, 12f, 7f, 12f, 6f, 14f };
    [Tooltip("The student's Animator (StudentPhone): sitting + texting, then the \"Lying\" state at the end.")]
    public Animator student;
    [Tooltip("Where she lies down on the bed (move it in the Scene view). Her position jumps here during the cut.")]
    public Transform lieSpot;
    [Tooltip("Lights left on at night: the phone's glow, the face light, the TV's glow.")]
    public Light[] nightLights;
    [Tooltip("The faint blue light filling the room at night.")]
    public Color nightAmbient = new Color(0.03f, 0.035f, 0.06f);
    [Tooltip("The scrolling news ticker on the TV screen.")]
    public RectTransform newsTicker;
    public float tickerSpeed = 160f;

    [Header("Overlay (UI > PrologueOverlay)")]
    public GameObject overlay;
    [Tooltip("The black bars at the top and bottom.")]
    public CanvasGroup letterbox;
    [Tooltip("The small time card in the lower-left corner.")]
    public CanvasGroup timeCard;
    public TextMeshProUGUI timeCardText;
    [Tooltip("One line of dialogue, inside the bottom bar.")]
    public CanvasGroup subtitle;
    public TextMeshProUGUI subtitleText;

    bool skipped;

    // The Prologue group and overlay stay visible in the editor (so they can be placed by hand) and are hidden
    // as soon as the game starts. Awake runs before WakeUpSequence starts the prologue.
    void Awake()
    {
        scene.SetActive(false);
        overlay.SetActive(false);
    }

    public IEnumerator Play(WakeUpSequence wake, ScreenFader fader)
    {
        Camera cam = wake.cinematicCamera;
        Vector3 camPosition = cam.transform.position;
        Quaternion camRotation = cam.transform.rotation;
        // The day lighting, to put back in the morning.
        var dayMode = RenderSettings.ambientMode;
        var dayAmbient = RenderSettings.ambientLight;
        float dayReflections = RenderSettings.reflectionIntensity;

        // Night: every room light off (the sun and the ceiling spots), a faint flat blue fill and almost no sky
        // reflections. Only the night lights (phone, face, TV) stay on.
        fader.SetBlack();
        var roomLights = new System.Collections.Generic.List<Light>();
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (System.Array.IndexOf(nightLights, light) < 0 && light.enabled) { light.enabled = false; roomLights.Add(light); }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = nightAmbient;
        RenderSettings.reflectionIntensity = 0.1f;
        DynamicGI.UpdateEnvironment(); // the sky light is calculated once; this applies the change now
        scene.SetActive(true);
        overlay.SetActive(true);
        letterbox.alpha = timeCard.alpha = subtitle.alpha = 0;
        StartCoroutine(WatchForSkip());

        // Shot 1: wide, slow push-in, with the time card.
        StartShot(cam, 0);
        StartCoroutine(Fade(letterbox, 1f, 1f));
        yield return fader.FadeTo(0f, 1.5f);
        yield return Card("11:47 PM", "THE NIGHT BEFORE THE DEADLINE", shotSeconds[0] - 2.5f);

        // Shot 2: side close-up, Kai's texts.
        StartShot(cam, 1);
        yield return Hold(0.6f);
        foreach (var text in KaiTexts) yield return Line("Kai", KaiColor, text, 1.9f);

        // Shot 3: face close-up, the reply.
        StartShot(cam, 2);
        yield return Hold(1f);
        yield return Line("You", YouColor, Reply, 2f);
        yield return Line(null, null, "<i>Kai is offline.</i>", 2f);

        // Shot 4: the TV in the living room, the late-night news.
        StartShot(cam, 3);
        var ticker = StartCoroutine(ScrollTicker());
        yield return Hold(1f);
        foreach (var line in NewsLines) yield return Line("News", NewsColor, line, 2.6f);
        StopCoroutine(ticker);

        // Shot 5: she has put the phone down and lies in the dark. The cut hides the jump to the bed.
        StartShot(cam, 4);
        student.transform.SetPositionAndRotation(lieSpot.position, lieSpot.rotation);
        student.SetLayerWeight(1, 0f); // no more texting arms
        student.GetComponent<HeldPhone>().Held = false; // the phone goes on the bed next to her
        student.Play("Lying", 0);
        yield return Hold(1.2f);
        yield return Line("You", YouColor, "Ghostline... I hope Kai's okay.", 2.4f);

        // Morning: black, then the time card on its own.
        StartCoroutine(Fade(letterbox, 0f, 1.5f));
        yield return fader.FadeTo(1f, 1.5f);
        StopAllCoroutines(); // the shot movement and the skip watcher
        foreach (var light in roomLights) light.enabled = true;
        RenderSettings.ambientMode = dayMode;
        RenderSettings.ambientLight = dayAmbient;
        RenderSettings.reflectionIntensity = dayReflections;
        DynamicGI.UpdateEnvironment();
        newsTicker.GetComponentInParent<Canvas>().gameObject.SetActive(false); // the TV is off in the morning
        foreach (var light in nightLights) light.enabled = false;
        cam.transform.SetPositionAndRotation(camPosition, camRotation);
        letterbox.alpha = subtitle.alpha = 0;
    }

    // The morning (called right after Play, on a black screen): time card, then the student wakes up in bed and
    // sits up (Alice's getting-up clip, which starts in the same pose as the lying one), then black again.
    // WakeUpSequence then switches to first person.
    public IEnumerator PlayWakeUp(WakeUpSequence wake, ScreenFader fader)
    {
        Camera cam = wake.cinematicCamera;
        Vector3 camPosition = cam.transform.position;
        Quaternion camRotation = cam.transform.rotation;
        skipped = false;
        StartCoroutine(WatchForSkip());

        yield return Card("9:04 AM", "THE DAY OF THE DEADLINE", 1.5f);

        // Shot 6: morning light, she wakes up and sits up.
        student.Play("GettingUp", 0, 0f);
        StartShot(cam, 5);
        StartCoroutine(Fade(letterbox, 1f, 1f));
        yield return fader.FadeTo(0f, 1.5f);
        yield return Hold(1f);
        yield return Line("You", YouColor, "Ugh... my head.", 1.8f);
        yield return Hold(1.2f);
        yield return Line("You", YouColor, "Kai... I hope they're okay.", 2.2f);
        yield return Hold(1.5f);
        yield return Line("You", YouColor, "Wait, it's already nine?! The thesis is due at noon.", 2.6f);

        // Black, then hand over to first person.
        StartCoroutine(Fade(letterbox, 0f, 1.2f));
        yield return fader.FadeTo(1f, 1.2f);
        StopAllCoroutines();
        scene.SetActive(false);
        overlay.SetActive(false);
        cam.transform.SetPositionAndRotation(camPosition, camRotation);
    }

    // ── Shots ──

    Coroutine shotMove;

    void StartShot(Camera cam, int index)
    {
        if (shotMove != null) StopCoroutine(shotMove);
        shotMove = StartCoroutine(MoveShot(cam, shots[index], shotSeconds[index]));
    }

    // Cut to the shot, then glide toward its "End" child for the length of the shot.
    static IEnumerator MoveShot(Camera cam, Transform shot, float seconds)
    {
        Transform end = shot.Find("End");
        for (float t = 0; t < 1; t += Delta / seconds)
        {
            float s = Mathf.SmoothStep(0, 1, t);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(shot.position, end.position, s),
                Quaternion.Slerp(shot.rotation, end.rotation, s));
            yield return null;
        }
    }

    // ── Overlay ──

    // A time card on its own, without the letterbox (Chapter 2 uses it for "back home").
    public IEnumerator ShowTimeCard(string time, string label, float hold)
    {
        overlay.SetActive(true);
        letterbox.alpha = subtitle.alpha = timeCard.alpha = 0;
        skipped = false;
        yield return Card(time, label, hold);
        overlay.SetActive(false);
    }

    // The time card in the lower-left: fade in, hold, fade out.
    IEnumerator Card(string time, string label, float hold)
    {
        timeCardText.text = $"<size=150%><b>{time}</b></size>\n<alpha=#B0>{label}";
        yield return Fade(timeCard, 1f, 0.8f);
        yield return Hold(hold);
        yield return Fade(timeCard, 0f, 0.8f);
    }

    // One subtitle line in the bottom bar: "Speaker   text" (speaker in its colour), or just the text.
    IEnumerator Line(string speaker, string color, string text, float hold)
    {
        if (skipped) yield break;
        subtitleText.text = speaker == null ? text : $"<color={color}><b>{speaker}</b></color>   {text}";
        yield return Fade(subtitle, 1f, 0.25f);
        yield return Hold(hold);
        yield return Fade(subtitle, 0f, 0.25f);
    }

    // The news ticker on the TV slides left and starts again from the right.
    IEnumerator ScrollTicker()
    {
        float width = newsTicker.rect.width, start = newsTicker.anchoredPosition.x;
        while (true)
        {
            var p = newsTicker.anchoredPosition;
            p.x -= tickerSpeed * Delta;
            if (p.x < start - width) p.x = start;
            newsTicker.anchoredPosition = p;
            yield return null;
        }
    }

    static IEnumerator Fade(CanvasGroup group, float to, float seconds)
    {
        float from = group.alpha;
        for (float t = 0; t < 1; t += Delta / seconds)
        {
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        group.alpha = to;
    }

    // ── Skip ──

    // A wait that ends early once the player skips.
    IEnumerator Hold(float seconds)
    {
        for (float t = 0; t < seconds && !skipped; t += Delta) yield return null;
    }

    // Real time that stands still while the pause menu is open.
    static float Delta => PauseMenu.IsPaused ? 0f : Time.unscaledDeltaTime;

    // Any key skips, except Esc and P (they open the pause menu). Keys pressed while paused don't count.
    IEnumerator WatchForSkip()
    {
        yield return Wait(1f); // ignore a key still held from the menu
        var keys = Keyboard.current;
        while (PauseMenu.IsPaused || !keys.anyKey.wasPressedThisFrame
               || keys.escapeKey.wasPressedThisFrame || keys.pKey.wasPressedThisFrame)
            yield return null;
        skipped = true;
    }
}
