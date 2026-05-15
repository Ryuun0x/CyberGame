using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Displays objectives with sub-tasks, completion animations, and auto-advancement.
/// Uses a template text object that gets duplicated for each sub-task.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("UI References")]
    public GameObject objectivePanel;
    public TextMeshProUGUI objectiveTitle;
    public Image iconImage;

    [Header("Sub-Task Template")]
    [Tooltip("A pre-made TMP text in the editor. Will be duplicated for each sub-task. Disable it in the hierarchy.")]
    public TextMeshProUGUI subTaskTemplate;

    [Header("Icons")]
    public Sprite starIcon;
    public Sprite checkIcon;

    [Header("Animation")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public float completedDelay = 1.5f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color completedColor = new Color(0.4f, 0.9f, 0.4f, 1f);

    [Header("Timing")]
    public float initialDelay = 0f;

    // ── Objective Data ──────────────────────────────────────
    [System.Serializable]
    public struct SubTask
    {
        [TextArea] public string description;
        [HideInInspector] public bool completed;
    }

    [System.Serializable]
    public struct Objective
    {
        public string title;
        public SubTask[] subTasks;
    }

    [HideInInspector]
    public Objective[] objectives;

    private int _currentIndex = -1;
    private bool _isTransitioning = false;
    private List<TextMeshProUGUI> _activeSubTasks = new List<TextMeshProUGUI>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Hardcode objectives here so the Unity Inspector doesn't override them
        objectives = new Objective[]
        {
            new Objective
            {
                title = "Morning Routine",
                subTasks = new SubTask[] { new SubTask { description = "Check your laptop and submit your thesis." } }
            },
            new Objective
            {
                title = "No Connection",
                subTasks = new SubTask[] { new SubTask { description = "Find a flash drive to backup your thesis." } }
            },
            new Objective
            {
                title = "Secure Your Work",
                subTasks = new SubTask[]
                {
                    new SubTask { description = "Backup your thesis using the flash drive." },
                    new SubTask { description = "Enable 2FA on your phone." }
                }
            },
            new Objective
            {
                title = "Time to Go",
                subTasks = new SubTask[] { new SubTask { description = "Grab your things, leave the house, and head over to the local café." } }
            },
            new Objective
            {
                title = "Get Connected",
                subTasks = new SubTask[] { new SubTask { description = "Check your phone's Wi-Fi. Identify and connect to the real café network (avoid the fake ones!)." } }
            },
            new Objective
            {
                title = "Submit Thesis",
                subTasks = new SubTask[] { new SubTask { description = "Open your laptop and finally submit your thesis." } }
            }
        };

        // Hide the template — it's only used for cloning
        if (subTaskTemplate != null)
            subTaskTemplate.gameObject.SetActive(false);
    }

    void Start()
    {
        if (objectivePanel != null)
            objectivePanel.SetActive(false);

        // Subscribe to game progress events
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnTriedSubmit       += HandleTriedSubmit;
            GameProgressManager.Instance.OnFlashDrivePickedUp += HandleFlashDrivePickedUp;
            GameProgressManager.Instance.OnThesisBackedUp     += HandleThesisBackedUp;
            GameProgressManager.Instance.On2FAEnabled          += Handle2FAEnabled;
            GameProgressManager.Instance.OnArrivedAtCafe       += HandleArrivedAtCafe;
            GameProgressManager.Instance.OnConnectedToCafeWiFi += HandleConnectedToCafeWiFi;
        }

        // Determine which objective to show based on current progress
        int startIndex = GetCurrentObjectiveFromProgress();
        Debug.Log($"[Objective] Starting at objective index: {startIndex}");

        if (initialDelay > 0f && startIndex == 0)
            StartCoroutine(ShowFirstObjectiveAfterDelay());
        else
            SetObjective(startIndex);
    }

    /// <summary>
    /// Reads GameProgressManager flags to figure out which objective to show.
    /// This lets the prefab work in any scene without losing progress.
    /// </summary>
    int GetCurrentObjectiveFromProgress()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("[Objective] GameProgressManager.Instance is NULL! Defaulting to objective 0.");
            return 0;
        }

        var gp = GameProgressManager.Instance;
        Debug.Log($"[Objective] Flags → triedSubmit:{gp.triedSubmitWithoutInternet} hasFlash:{gp.hasFlashDrive} backedUp:{gp.thesisBackedUp} 2FA:{gp.is2FAEnabled} arrivedCafe:{gp.arrivedAtCafe} connectedCafe:{gp.connectedToCafeWiFi}");

        // If connected to cafe WiFi -> show "Submit Thesis"
        if (gp.connectedToCafeWiFi) return 5;

        // If arrived at cafe -> show "Get Connected"
        if (gp.arrivedAtCafe) return 4;

        // If 2FA is done → show "Time to Go"
        if (gp.is2FAEnabled && gp.thesisBackedUp) return 3;

        // If flash drive picked up → show "Secure Your Work"
        if (gp.hasFlashDrive) return 2;

        // If tried to submit → show "No Connection"
        if (gp.triedSubmitWithoutInternet) return 1;

        // Default → first objective
        return 0;
    }

    IEnumerator ShowFirstObjectiveAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        SetObjective(0);
    }

    // ── Public API ──────────────────────────────────────────

    public void SetObjective(int index)
    {
        if (index < 0 || index >= objectives.Length) return;
        if (index <= _currentIndex) return;
        if (_isTransitioning) return;

        _currentIndex = index;
        Objective obj = objectives[index];

        // Reset icon to star
        if (iconImage != null && starIcon != null)
            iconImage.sprite = starIcon;

        // Set title
        if (objectiveTitle != null)
        {
            objectiveTitle.text = obj.title;
            objectiveTitle.color = normalColor;
        }

        // Clear old sub-tasks
        ClearSubTasks();

        // Create sub-tasks by duplicating the template
        for (int i = 0; i < obj.subTasks.Length; i++)
        {
            obj.subTasks[i].completed = false;
            CreateSubTask(obj.subTasks[i].description);
        }

        // Show panel
        if (objectivePanel != null)
            objectivePanel.SetActive(true);

        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }

    public void CompleteSubTask(int objectiveIndex, int subTaskIndex)
    {
        if (objectiveIndex != _currentIndex) return;
        if (subTaskIndex < 0 || subTaskIndex >= objectives[objectiveIndex].subTasks.Length) return;
        if (objectives[objectiveIndex].subTasks[subTaskIndex].completed) return;

        // Mark completed
        objectives[objectiveIndex].subTasks[subTaskIndex].completed = true;

        // Turn text green
        if (subTaskIndex < _activeSubTasks.Count)
            _activeSubTasks[subTaskIndex].color = completedColor;

        // Check if ALL sub-tasks are done
        bool allDone = true;
        for (int i = 0; i < objectives[objectiveIndex].subTasks.Length; i++)
        {
            if (!objectives[objectiveIndex].subTasks[i].completed)
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
            StartCoroutine(CompleteObjective());
    }

    public void CompleteAndAdvance(int objectiveIndex, int subTaskIndex)
    {
        if (objectiveIndex != _currentIndex) return;
        CompleteSubTask(objectiveIndex, subTaskIndex);
    }

    // ── Event Handlers ──────────────────────────────────────
    private void HandleTriedSubmit() { CompleteAndAdvance(0, 0); }
    private void HandleFlashDrivePickedUp() { CompleteAndAdvance(1, 0); }
    private void HandleThesisBackedUp() { CompleteSubTask(2, 0); }
    private void Handle2FAEnabled() { CompleteSubTask(2, 1); }
    private void HandleArrivedAtCafe() { CompleteAndAdvance(3, 0); }
    private void HandleConnectedToCafeWiFi() { CompleteAndAdvance(4, 0); }

    void OnDestroy()
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnTriedSubmit       -= HandleTriedSubmit;
            GameProgressManager.Instance.OnFlashDrivePickedUp -= HandleFlashDrivePickedUp;
            GameProgressManager.Instance.OnThesisBackedUp     -= HandleThesisBackedUp;
            GameProgressManager.Instance.On2FAEnabled          -= Handle2FAEnabled;
            GameProgressManager.Instance.OnArrivedAtCafe       -= HandleArrivedAtCafe;
            GameProgressManager.Instance.OnConnectedToCafeWiFi -= HandleConnectedToCafeWiFi;
        }
    }

    // ── Internal ────────────────────────────────────────────

    void CreateSubTask(string text)
    {
        if (subTaskTemplate == null) return;

        // Duplicate the template
        GameObject clone = Instantiate(subTaskTemplate.gameObject, subTaskTemplate.transform.parent);
        clone.SetActive(true);

        TextMeshProUGUI tmp = clone.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = normalColor;

        _activeSubTasks.Add(tmp);
    }

    void ClearSubTasks()
    {
        foreach (var t in _activeSubTasks)
        {
            if (t != null) Destroy(t.gameObject);
        }
        _activeSubTasks.Clear();
    }

    IEnumerator CompleteObjective()
    {
        _isTransitioning = true;

        // Change icon to checkmark with pop-up animation
        if (iconImage != null && checkIcon != null)
        {
            iconImage.sprite = checkIcon;
            yield return StartCoroutine(PopUpIcon(iconImage.transform));
        }

        // Turn title green
        if (objectiveTitle != null)
            objectiveTitle.color = completedColor;

        yield return new WaitForSeconds(completedDelay);

        // Fade out
        if (canvasGroup != null)
            yield return StartCoroutine(FadeOut());

        // Reset title color
        if (objectiveTitle != null)
            objectiveTitle.color = normalColor;

        _isTransitioning = false;

        // Advance to next objective
        int nextIndex = _currentIndex + 1;
        _currentIndex = nextIndex - 1;
        if (nextIndex < objectives.Length)
        {
            yield return new WaitForSeconds(0.5f);
            _currentIndex = nextIndex - 1;
            SetObjective(nextIndex);
        }
        else
        {
            if (objectivePanel != null)
                objectivePanel.SetActive(false);
        }
    }

    IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    IEnumerator PopUpIcon(Transform icon)
    {
        float duration = 0.3f;
        float overshoot = 1.3f; // scale up past 1 for bounce

        // Start from zero
        icon.localScale = Vector3.zero;

        // Scale up to overshoot
        float elapsed = 0f;
        float halfDuration = duration * 0.6f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(0f, overshoot, t);
            icon.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        // Settle back to 1
        elapsed = 0f;
        float settleDuration = duration * 0.4f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / settleDuration;
            float scale = Mathf.Lerp(overshoot, 1f, t);
            icon.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        icon.localScale = Vector3.one;
    }
}
