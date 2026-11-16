using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The rundown at the end of each chapter. One page per threat and habit the player met: a picture of the risky
/// move next to the safer one (art in Sprites/Rundown, made by ArtSource/RundownArt/make_samples.py), one line under
/// each, one rule, and the player's result as a badge. "Details and source" opens the full explanation.
/// After the last page come the results (score counted up, rating) with the Main Menu / Play Again / Continue
/// buttons, so the lessons are seen before the score.
/// It opens on a map of where each threat happened (page 1 goes back to it).
/// It is the DebriefScreen prefab, placed in both game scenes. Buttons call the public methods from their OnClick.
/// </summary>
public class DebriefScreen : MonoBehaviour
{
    public const string RestartScene = "Interior";
    public const string MenuScene = "StartMenu";

    static readonly Color SafeColor = new Color(0.36f, 0.85f, 0.52f, 1f);
    static readonly Color RecoveredColor = new Color(0.98f, 0.76f, 0.3f, 1f);
    static readonly Color CompromisedColor = new Color(0.94f, 0.35f, 0.3f, 1f);

    [Serializable]
    public struct Art
    {
        public string threatId;
        public Sprite risky, safer;
        [Tooltip("\"How the trick works\" picture for Details (ArtSource/RundownArt/make_details.py). Empty: the text.")]
        public Sprite howItWorks;
    }

    [Header("Screen (hidden until a chapter ends)")]
    public GameObject screen;
    public CanvasGroup screenGroup;

    [Header("Threat pages")]
    public GameObject pagesView;
    public TextMeshProUGUI pageTitle;
    public Image badgeFrame;
    public TextMeshProUGUI badgeText;
    public Image riskyArt, saferArt;
    [Tooltip("Shown where a threat has no picture yet.")]
    public GameObject riskyPlaceholder, saferPlaceholder;
    public TextMeshProUGUI riskyCaption, saferCaption, ruleText;
    public GameObject previousButton;
    public TextMeshProUGUI nextLabel;
    public GameObject detailsPanel;
    [Tooltip("Inside Details: the text box (threats without a picture) and the full-screen picture view.")]
    public GameObject detailsBox, detailsFlow;
    public TextMeshProUGUI detailsText;
    public Image flowArt;
    public TextMeshProUGUI flowTitle, flowSubtitle, flowSource;
    [Tooltip("The two pictures of each threat (the ThreatCatalog id).")]
    public Art[] art;

    [Header("Map page (first): where each threat happened")]
    public GameObject mapView;
    [Tooltip("The map picture (Sprites/Rundown/map.png, made by ArtSource/RundownArt/make_map.py); pins go inside it.")]
    public RectTransform mapImage;
    [Tooltip("Inactive template: a dot with a card (Image) holding a TMP text. One copy per place.")]
    public RectTransform mapPinTemplate;

    // The world area the map picture shows (x, z): keep in step with MAP_MIN / MAP_MAX in make_map.py.
    static readonly Vector2 MapMin = new Vector2(-180f, 35f), MapMax = new Vector2(15f, 112f);

    [Header("Results (after the last page)")]
    public GameObject resultsView;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI summaryText;
    public TextMeshProUGUI scoreText;
    public GameObject mainMenuButton;
    public GameObject continueButton;

    [Header("Next chapter title card")]
    public GameObject titleCard;
    public CanvasGroup titleCardGroup;
    public TextMeshProUGUI titleCardText;

    public static bool IsOpen { get; private set; }

    static DebriefScreen _instance;
    string nextChapter;
    readonly List<ThreatLog.Entry> pages = new List<ThreatLog.Entry>();
    int page; // -1: the map page
    int chapter; // the rundown shows only this chapter's threats and score
    readonly List<GameObject> pins = new List<GameObject>();
    readonly List<TextMeshProUGUI> pinTexts = new List<TextMeshProUGUI>();

    void Awake() => _instance = this;

    void Start()
    {
        screen.SetActive(false);
        titleCard.SetActive(false);
        IsOpen = false;
    }

    // nextChapter: if set, a Continue button leads to that chapter's title card.
    // summary: the line under the title; leave it out to use the Chapter 1 thesis line.
    public static void Show(string chapterTitle, string nextChapter = null, string summary = null)
    {
        if (IsOpen || _instance == null) return;
        IsOpen = true;
        _instance.Open(chapterTitle, nextChapter, summary);
    }

    void Open(string chapterTitle, string next, string summary)
    {
        nextChapter = next;
        FreezeGameplay();

        var log = ThreatLog.Instance;
        titleText.text = chapterTitle;
        chapter = GameProgressManager.Instance != null ? GameProgressManager.Instance.chapter : 1;
        summaryText.text = summary ?? (log != null && log.AnyCompromised(chapter)
            ? "Your thesis was submitted, but an attacker stole your Goggle login."
            : "Your thesis was submitted safely.");
        string later = PendingTitles(log);
        if (later != "") summaryText.text += $"\n<size=80%><color=#8A93A3>Still to come: {later}</color></size>";

        // This chapter's threats in the order they happened, then its habits.
        pages.Clear();
        if (log != null)
            foreach (var kind in new[] { ThreatKind.Threat, ThreatKind.Habit })
                foreach (var e in log.Entries)
                {
                    var info = ThreatCatalog.Get(e.id);
                    if (info != null && info.Kind == kind && e.chapter == chapter && !log.IsPending(e.id)) pages.Add(e);
                }

        mainMenuButton.SetActive(Application.CanStreamedLevelBeLoaded(MenuScene));
        continueButton.SetActive(!string.IsNullOrEmpty(nextChapter));
        screen.SetActive(true);
        detailsPanel.SetActive(false);
        if (pages.Count > 0) ShowMap();
        else StartCoroutine(ShowResults());
        StartCoroutine(FadeIn());
    }

    static string PendingTitles(ThreatLog log)
    {
        if (log == null) return "";
        var titles = new List<string>();
        foreach (var id in log.Pending)
        {
            var info = ThreatCatalog.Get(id);
            if (info != null) titles.Add(info.Title);
        }
        return string.Join(", ", titles);
    }

    void Update()
    {
        if (!IsOpen) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Arrow keys turn the pages too.
        var k = Keyboard.current;
        if (mapView.activeSelf) ClickMapLine();
        if (k == null || !(pagesView.activeSelf || mapView.activeSelf) || detailsPanel.activeSelf) return;
        if (k.rightArrowKey.wasPressedThisFrame) Next();
        else if (k.leftArrowKey.wasPressedThisFrame) Previous();
    }

    // ── Pages ──

    void ShowPage(int index)
    {
        page = index;
        var entry = pages[index];
        var info = ThreatCatalog.Get(entry.id);
        pagesView.SetActive(true);
        mapView.SetActive(false);
        resultsView.SetActive(false);

        pageTitle.text = $"{info.Title}   <size=60%><color=#8A93A3>{index + 1} / {pages.Count}</color></size>";
        Color accent = Accent(entry.outcome);
        badgeText.text = info.Kind == ThreatKind.Habit
            ? (entry.outcome == ThreatOutcome.Compromised ? "SKIPPED" : "DONE")
            : entry.outcome.ToString().ToUpperInvariant();
        badgeText.color = badgeFrame.color = accent;

        Art pictures = Array.Find(art, a => a.threatId == entry.id);
        SetArt(riskyArt, riskyPlaceholder, pictures.risky);
        SetArt(saferArt, saferPlaceholder, pictures.safer);
        riskyCaption.text = $"<color=#F0584C>Risky:</color> {info.Risky}";
        saferCaption.text = $"<color=#5CD985>Safer:</color> {info.Safer}";
        ruleText.text = info.Rule;

        previousButton.SetActive(true); // page 1 goes back to the map
        nextLabel.text = index == pages.Count - 1 ? "RESULTS  >" : "NEXT  >";
    }

    static void SetArt(Image image, GameObject placeholder, Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        placeholder.SetActive(sprite == null);
    }

    static Color Accent(ThreatOutcome outcome) =>
        outcome == ThreatOutcome.Safe ? SafeColor : outcome == ThreatOutcome.Recovered ? RecoveredColor : CompromisedColor;

    // ── Map page ──

    // The city between home and the café, a pin at each place with the threats that happened there, numbered like
    // the pages and coloured by result. Clicking a line opens its page.
    void ShowMap()
    {
        page = -1;
        mapView.SetActive(true);
        pagesView.SetActive(false);
        resultsView.SetActive(false);
        foreach (var p in pins) Destroy(p);
        pins.Clear();
        pinTexts.Clear();

        var places = new[] { ("Home", false), ("Café", true) };
        foreach (var (name, atCafe) in places)
        {
            var lines = new List<string>();
            for (int i = 0; i < pages.Count; i++)
            {
                var info = ThreatCatalog.Get(pages[i].id);
                if (info.AtCafe != atCafe) continue;
                string hex = ColorUtility.ToHtmlStringRGB(Accent(pages[i].outcome));
                lines.Add($"<link=\"{i}\"><color=#{hex}>{i + 1}   {info.Title}</color></link>");
            }
            if (lines.Count == 0) continue;
            Vector2 at = PlacePosition(name);
            var pin = Instantiate(mapPinTemplate, mapPinTemplate.parent);
            pin.gameObject.SetActive(true);
            Vector2 size = mapImage.rect.size;
            Vector2 uv = new Vector2(Mathf.InverseLerp(MapMin.x, MapMax.x, at.x), Mathf.InverseLerp(MapMin.y, MapMax.y, at.y));
            pin.anchoredPosition = (uv - new Vector2(0.5f, 0.5f)) * size;
            var text = pin.GetComponentInChildren<TextMeshProUGUI>();
            text.text = $"<b><color=#E8E8E8>{name}</color></b>\n" + string.Join("\n", lines);
            // The card sits to the right of its pin, or to the left near the right edge.
            var card = (RectTransform)text.transform.parent;
            bool left = uv.x > 0.62f;
            card.pivot = new Vector2(left ? 1 : 0, 0.5f);
            card.anchoredPosition = new Vector2(left ? -24 : 24, 0);
            pins.Add(pin.gameObject);
            pinTexts.Add(text);
        }
    }

    // Where a place of the city map is (x, z), from the layout the minimap uses.
    static Vector2 PlacePosition(string name)
    {
        var asset = Resources.Load<TextAsset>("CityMapLayout");
        var layout = asset != null ? JsonUtility.FromJson<CityMapLayout>(asset.text) : null;
        if (layout != null)
            foreach (var p in layout.places)
                if (p.name == name) return p.position;
        return name == "Home" ? new Vector2(-160.6f, 62f) : new Vector2(-2.6f, 81.1f);
    }

    void ClickMapLine()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        foreach (var text in pinTexts)
        {
            int link = TMP_TextUtilities.FindIntersectingLink(text, mouse.position.ReadValue(), null);
            if (link >= 0)
            {
                ShowPage(int.Parse(text.textInfo.linkInfo[link].GetLinkID()));
                return;
            }
        }
    }

    public void Next()
    {
        if (page < pages.Count - 1) ShowPage(page + 1);
        else StartCoroutine(ShowResults());
    }

    public void Previous()
    {
        if (page > 0) ShowPage(page - 1);
        else if (page == 0) ShowMap();
    }

    // How the trick works as a picture when there is one, otherwise the full text: what happened, the better choice,
    // what to do, the source.
    public void Details()
    {
        var entry = pages[page];
        var info = ThreatCatalog.Get(entry.id);
        Sprite flow = Array.Find(art, a => a.threatId == entry.id).howItWorks;
        detailsFlow.SetActive(flow != null);
        detailsBox.SetActive(flow == null);
        if (flow != null)
        {
            flowArt.sprite = flow;
            flowTitle.text = info.Title;
            flowSubtitle.text = info.Kind == ThreatKind.Habit ? "Why it matters" : "How the trick works";
            flowSource.text = info.Source;
            detailsPanel.SetActive(true);
            return;
        }
        string result = entry.outcome == ThreatOutcome.Safe ? info.SafeResult
            : entry.outcome == ThreatOutcome.Recovered ? info.RecoveredResult ?? info.SafeResult
            : info.CompromisedResult;
        string better = entry.outcome != ThreatOutcome.Safe && info.Better != null
            ? $"\n\n<color=#5CD985><b>Better choice:</b> {info.Better}</color>" : "";
        detailsText.text = $"<size=120%><b>{info.Title}</b></size>\n\n{result}{better}\n\n<b>What to do:</b> {info.Lesson}" +
                           $"\n\n<size=80%><color=#8A93A3>{info.Source}</color></size>";
        detailsPanel.SetActive(true);
    }

    public void CloseDetails() => detailsPanel.SetActive(false);

    // ── Results ──

    IEnumerator ShowResults()
    {
        pagesView.SetActive(false);
        mapView.SetActive(false);
        resultsView.SetActive(true);
        var log = ThreatLog.Instance;
        int score = log != null ? log.ScorePercent(chapter) : 100;
        // After the last chapter, the whole game's score too (the one the ending is based on).
        string total = log != null && chapter > 1 && string.IsNullOrEmpty(nextChapter)
            ? $"\n<size=70%><color=#8A93A3>Whole game  {log.ScorePercent()}%</color></size>" : "";
        Color colour = score >= 90 ? SafeColor : score >= 60 ? RecoveredColor : CompromisedColor;
        // The score counts up, then the rating appears.
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 1.2f)
        {
            scoreText.text = $"Security score  <b>{Mathf.RoundToInt(score * Mathf.SmoothStep(0, 1, t))}%</b>";
            scoreText.color = Color.Lerp(Color.white, colour, t);
            yield return null;
        }
        scoreText.text = $"Security score  <b>{score}%</b>   ·   {ThreatLog.RatingFor(score)}{total}";
        scoreText.color = colour;
    }

    static void FreezeGameplay()
    {
        Time.timeScale = 0f;
        foreach (var c in FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (var c in FindObjectsByType<PhoneManager>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (var c in FindObjectsByType<LaptopInteraction>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (var c in FindObjectsByType<RaycastCrosshair>(FindObjectsSortMode.None)) c.enabled = false;
    }

    // ── Buttons (OnClick in the Inspector) ──

    public void MainMenu() => LoadFresh(MenuScene);

    public void PlayAgain() => LoadFresh(RestartScene);

    public void Continue() => StartCoroutine(TitleCard());

    // Starts over: clears the progress flags and the threat log, then loads the scene. Also used by the pause menu.
    public static void LoadFresh(string sceneName)
    {
        Time.timeScale = 1f;
        IsOpen = false;
        if (ThreatLog.Instance != null) ThreatLog.Instance.ResetLog();
        if (GameProgressManager.Instance != null)
        {
            Destroy(GameProgressManager.Instance.gameObject); // also removes ThreatLog (same object)
            GameProgressManager.Instance = null;
            ThreatLog.Instance = null;
        }
        CafeZone.PlayerInCafe = false;
        Hints.ResetAll(); // a new game teaches the controls again
        SceneManager.LoadScene(sceneName);
    }

    // Next chapter's title card over the results, then that chapter starts at home.
    IEnumerator TitleCard()
    {
        titleCardText.text = nextChapter;
        titleCardGroup.alpha = 0f;
        titleCard.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 1.2f)
        {
            titleCardGroup.alpha = t;
            yield return null;
        }
        titleCardGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.5f);

        Time.timeScale = 1f;
        IsOpen = false;
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.chapter++;
        CafeZone.PlayerInCafe = false;
        SceneManager.LoadScene(RestartScene);
    }

    IEnumerator FadeIn()
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
        {
            screenGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        screenGroup.alpha = 1f;
    }
}
