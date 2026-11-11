using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The results screen at the end of each chapter: the score, then one card per threat and habit
/// (what you did, what to do, the source). It is the DebriefScreen prefab, placed in both game scenes.
/// The cards are copies of hidden templates inside the screen (like ObjectiveManager's sub-task template).
/// Its buttons call MainMenu / PlayAgain / Continue from their OnClick.
/// </summary>
public class DebriefScreen : MonoBehaviour
{
    public const string RestartScene = "Interior";
    public const string MenuScene = "StartMenu";

    static readonly Color SafeColor = new Color(0.36f, 0.85f, 0.52f, 1f);
    static readonly Color RecoveredColor = new Color(0.98f, 0.76f, 0.3f, 1f);
    static readonly Color CompromisedColor = new Color(0.96f, 0.38f, 0.38f, 1f);

    [Header("Screen (hidden until a chapter ends)")]
    public GameObject screen;
    public CanvasGroup screenGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI summaryText;
    public TextMeshProUGUI scoreText;

    [Header("Cards (copied from the hidden templates)")]
    public Transform cardList;
    public GameObject headingTemplate;
    public GameObject cardTemplate;
    public GameObject pendingTemplate;

    [Header("Buttons")]
    public GameObject mainMenuButton;
    public GameObject continueButton;

    [Header("Next chapter title card")]
    public GameObject titleCard;
    public CanvasGroup titleCardGroup;
    public TextMeshProUGUI titleCardText;

    public static bool IsOpen { get; private set; }

    static DebriefScreen _instance;
    string nextChapter;

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
        bool compromised = log != null && log.AnyCompromised();
        int score = log != null ? log.ScorePercent() : 100;

        titleText.text = chapterTitle;
        summaryText.text = summary ?? (compromised
            ? "Your thesis was submitted, but an attacker stole your Goggle login."
            : "Your thesis was submitted safely.");
        scoreText.text = $"Security score  <b>{score}%</b>   ·   {ThreatLog.RatingFor(score)}";
        scoreText.color = score >= 90 ? SafeColor : score >= 60 ? RecoveredColor : CompromisedColor;

        foreach (Transform child in cardList)
            if (child.gameObject != headingTemplate && child.gameObject != cardTemplate && child.gameObject != pendingTemplate)
                Destroy(child.gameObject);
        if (log != null)
        {
            AddSection(log, ThreatKind.Threat, "THREATS YOU FACED");
            AddSection(log, ThreatKind.Habit, "HABITS THAT PROTECTED YOU");
        }

        mainMenuButton.SetActive(Application.CanStreamedLevelBeLoaded(MenuScene));
        continueButton.SetActive(!string.IsNullOrEmpty(nextChapter));
        screen.SetActive(true);
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        if (!IsOpen) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

    // ── Cards ──

    void AddSection(ThreatLog log, ThreatKind kind, string heading)
    {
        bool any = false;
        foreach (var e in log.Entries)
        {
            var info = ThreatCatalog.Get(e.id);
            if (info == null || info.Kind != kind || log.IsPending(e.id)) continue;
            if (!any) any = AddHeading(heading);
            AddCard(info, e.outcome);
        }
        foreach (var id in log.Pending)
        {
            var info = ThreatCatalog.Get(id);
            if (info == null || info.Kind != kind) continue;
            if (!any) any = AddHeading(heading);
            var card = Copy(pendingTemplate);
            SetText(card, "Body/Header/Title", $"{info.Title}  <color=#8A93A3><size=70%>{info.Category.ToUpperInvariant()}</size></color>");
        }
    }

    bool AddHeading(string heading)
    {
        SetText(Copy(headingTemplate), "", heading);
        return true;
    }

    void AddCard(ThreatInfo info, ThreatOutcome outcome)
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

        var card = Copy(cardTemplate);
        card.transform.Find("Accent").GetComponent<Image>().color = accent;
        SetText(card, "Body/Header/Title", $"{info.Title}  <color=#8A93A3><size=70%>{info.Category.ToUpperInvariant()}</size></color>");
        SetText(card, "Body/Header/Badge", badge).color = accent;
        SetText(card, "Body/Result", result);
        // Wrong (or late) choices also show the right action and what it would have done.
        string better = outcome != ThreatOutcome.Safe && info.Better != null
            ? $"<color=#5CD985><b>Better choice:</b> {info.Better}</color>\n" : "";
        SetText(card, "Body/Lesson", $"{better}<b>What to do:</b> {info.Lesson}");
        SetText(card, "Body/Source", info.Source);
    }

    GameObject Copy(GameObject template)
    {
        var copy = Instantiate(template, cardList);
        copy.SetActive(true);
        return copy;
    }

    static TextMeshProUGUI SetText(GameObject card, string path, string text)
    {
        var target = path == "" ? card.transform : card.transform.Find(path);
        var tmp = target.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        return tmp;
    }
}
