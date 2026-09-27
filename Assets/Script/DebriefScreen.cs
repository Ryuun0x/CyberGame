using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class DebriefScreen : MonoBehaviour
{
    public const string RestartScene = "Interior";
    public const string MenuScene = "StartMenu";

    static readonly Color BackdropColor = new Color(0.03f, 0.04f, 0.07f, 0.94f);
    static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.14f, 1f);
    static readonly Color CardColor = new Color(0.12f, 0.14f, 0.19f, 1f);
    static readonly Color TextMain = new Color(0.93f, 0.95f, 0.98f, 1f);
    static readonly Color TextDim = new Color(0.62f, 0.67f, 0.75f, 1f);
    static readonly Color SafeColor = new Color(0.36f, 0.85f, 0.52f, 1f);
    static readonly Color RecoveredColor = new Color(0.98f, 0.76f, 0.3f, 1f);
    static readonly Color CompromisedColor = new Color(0.96f, 0.38f, 0.38f, 1f);
    static readonly Color PrimaryButton = new Color(0.25f, 0.52f, 0.96f, 1f);
    static readonly Color SecondaryButton = new Color(0.18f, 0.21f, 0.28f, 1f);

    public static bool IsOpen { get; private set; }

    CanvasGroup _group;

    string _nextChapter;

    // nextChapter: if set, a Continue button leads to that chapter's title card.
    public static void Show(string chapterTitle, string nextChapter = null)
    {
        if (IsOpen) return;
        IsOpen = true;
        var screen = new GameObject("DebriefScreen").AddComponent<DebriefScreen>();
        screen._nextChapter = nextChapter;
        screen.Build(chapterTitle);
    }

    void Build(string chapterTitle)
    {
        FreezeGameplay();
        EnsureEventSystem();

        UIBuild.Overlay(gameObject, 500);
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;

        var backdrop = UIBuild.Stretch(UIBuild.NewRect("Backdrop", transform));
        backdrop.gameObject.AddComponent<Image>().color = BackdropColor;

        var panel = UIBuild.NewRect("Panel", transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1180, 920);
        panel.gameObject.AddComponent<Image>().color = PanelColor;
        var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(56, 56, 48, 48);
        panelLayout.spacing = 14;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        var log = ThreatLog.Instance;
        bool compromised = log != null && log.AnyCompromised();
        int score = log != null ? log.ScorePercent() : 100;
        Color scoreColor = score >= 90 ? SafeColor : score >= 60 ? RecoveredColor : CompromisedColor;

        var eyebrow = UIBuild.Label(panel, "CHAPTER COMPLETE", 20, TextDim, FontStyles.Bold);
        eyebrow.characterSpacing = 8;
        UIBuild.Label(panel, chapterTitle, 46, TextMain, FontStyles.Bold);
        UIBuild.Label(panel, compromised
            ? "Your thesis was submitted, but an attacker stole your school login."
            : "Your thesis was submitted safely.", 24, TextMain, FontStyles.Normal);
        UIBuild.Label(panel, $"Security score  <b>{score}%</b>   ·   {ThreatLog.RatingFor(score)}", 28, scoreColor, FontStyles.Normal);

        var content = ScrollArea(panel);
        if (log != null)
        {
            AddSection(content, log, ThreatKind.Threat, "THREATS YOU FACED");
            AddSection(content, log, ThreatKind.Habit, "HABITS THAT PROTECTED YOU");
        }

        var buttons = UIBuild.NewRect("Buttons", panel);
        var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 16;
        row.childAlignment = TextAnchor.MiddleRight;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        if (Application.CanStreamedLevelBeLoaded(MenuScene))
            UIBuild.MakeButton(buttons, "Main Menu", SecondaryButton, () => LoadFresh(MenuScene));
        bool next = !string.IsNullOrEmpty(_nextChapter);
        UIBuild.MakeButton(buttons, "Play Again", next ? SecondaryButton : PrimaryButton, () => LoadFresh(RestartScene));
        if (next) UIBuild.MakeButton(buttons, "Continue", PrimaryButton, () => StartCoroutine(TitleCard()));

        StartCoroutine(FadeIn());
    }

    void Update()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDestroy()
    {
        IsOpen = false;
    }

    static void FreezeGameplay()
    {
        Time.timeScale = 0f;
        foreach (var c in FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (var c in FindObjectsByType<PhoneManager>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (var c in FindObjectsByType<LaptopInteraction>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (var c in FindObjectsByType<RaycastCrosshair>(FindObjectsSortMode.None)) c.enabled = false;
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    static void LoadFresh(string sceneName)
    {
        Time.timeScale = 1f;
        IsOpen = false;
        if (ThreatLog.Instance != null) ThreatLog.Instance.ResetLog();
        if (GameProgressManager.Instance != null)
        {
            Destroy(GameProgressManager.Instance.gameObject);
            GameProgressManager.Instance = null;
        }
        CafeZone.PlayerInCafe = false;
        SceneManager.LoadScene(sceneName);
    }

    // Next chapter's title card over the debrief. Nothing is built past it yet, so it ends at the menu.
    IEnumerator TitleCard()
    {
        var card = UIBuild.Stretch(UIBuild.NewRect("TitleCard", transform));
        card.gameObject.AddComponent<Image>().color = Color.black;
        var group = card.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        UIBuild.Column(card, 0, 24).childAlignment = TextAnchor.MiddleCenter;
        var title = UIBuild.Label(card, _nextChapter, 72, TextMain, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;
        var soon = UIBuild.Label(card, "To be continued", 26, TextDim, FontStyles.Italic);
        soon.alignment = TextAlignmentOptions.Center;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 1.2f)
        {
            group.alpha = t;
            yield return null;
        }
        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.5f);
        var buttons = UIBuild.NewRect("Buttons", card);
        var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        UIBuild.MakeButton(buttons, Application.CanStreamedLevelBeLoaded(MenuScene) ? "Main Menu" : "Play Again", SecondaryButton,
            () => LoadFresh(Application.CanStreamedLevelBeLoaded(MenuScene) ? MenuScene : RestartScene));
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.6f;
            _group.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        _group.alpha = 1f;
    }

    static void AddSection(RectTransform content, ThreatLog log, ThreatKind kind, string heading)
    {
        bool any = false;
        foreach (var e in log.Entries)
        {
            var info = ThreatCatalog.Get(e.id);
            if (info == null || info.Kind != kind) continue;
            if (!any)
            {
                var h = UIBuild.Label(content, heading, 18, TextDim, FontStyles.Bold);
                h.characterSpacing = 6;
                h.margin = new Vector4(0, 10, 0, 0);
                any = true;
            }
            AddCard(content, info, e.outcome);
        }
    }

    static void AddCard(RectTransform parent, ThreatInfo info, ThreatOutcome outcome)
    {
        Color accent = outcome == ThreatOutcome.Safe ? SafeColor
            : outcome == ThreatOutcome.Recovered ? RecoveredColor
            : CompromisedColor;
        string badge = info.Kind == ThreatKind.Habit
            ? (outcome == ThreatOutcome.Compromised ? "SKIPPED" : "DONE")
            : outcome.ToString().ToUpperInvariant();
        string result = outcome == ThreatOutcome.Safe ? info.SafeResult
            : outcome == ThreatOutcome.Recovered ? info.RecoveredResult ?? info.SafeResult
            : info.CompromisedResult;

        var card = UIBuild.NewRect("Card", parent);
        card.gameObject.AddComponent<Image>().color = CardColor;
        var cardRow = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        cardRow.childControlWidth = true;
        cardRow.childControlHeight = true;
        cardRow.childForceExpandWidth = false;
        cardRow.childForceExpandHeight = true;

        var stripe = UIBuild.NewRect("Accent", card);
        stripe.gameObject.AddComponent<Image>().color = accent;
        stripe.gameObject.AddComponent<LayoutElement>().preferredWidth = 6;

        var body = UIBuild.NewRect("Body", card);
        body.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(24, 24, 18, 18);
        bodyLayout.spacing = 6;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        var header = UIBuild.NewRect("Header", body);
        var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerRow.childControlWidth = true;
        headerRow.childControlHeight = true;
        headerRow.childForceExpandWidth = false;
        headerRow.childForceExpandHeight = false;
        var title = UIBuild.Label(header, $"{info.Title}  <color=#8A93A3><size=70%>{info.Category.ToUpperInvariant()}</size></color>", 26, TextMain, FontStyles.Bold);
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var badgeLabel = UIBuild.Label(header, badge, 18, accent, FontStyles.Bold);
        badgeLabel.alignment = TextAlignmentOptions.Right;
        badgeLabel.characterSpacing = 4;

        UIBuild.Label(body, result, 21, TextMain, FontStyles.Normal);
        UIBuild.Label(body, $"<b>What to do:</b> {info.Lesson}", 20, TextDim, FontStyles.Normal);
        UIBuild.Label(body, info.Source, 15, TextDim, FontStyles.Italic);
    }

    static RectTransform ScrollArea(RectTransform parent)
    {
        var area = UIBuild.NewRect("Scroll", parent);
        area.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        var scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        var viewport = UIBuild.Stretch(UIBuild.NewRect("Viewport", area));
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = UIBuild.NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        return content;
    }
}
