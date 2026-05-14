using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays the current objective in the top-left corner (Schedule I style).
/// Listens to GameProgressManager events to auto-advance objectives.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("UI References")]
    public GameObject objectivePanel;
    public TextMeshProUGUI objectiveTitle;
    public TextMeshProUGUI objectiveDescription;

    [Header("Animation")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 0.5f;

    [Header("Timing")]
    [Tooltip("Delay before showing the first objective (wait for wake-up sequence)")]
    public float initialDelay = 0f;

    // ── Objective Data ──────────────────────────────────────
    [System.Serializable]
    public struct Objective
    {
        public string title;
        [TextArea] public string description;
    }

    [Header("Objectives (in order)")]
    public Objective[] objectives = new Objective[]
    {
        new Objective { title = "Morning Routine",  description = "Check your laptop and submit your thesis" },
        new Objective { title = "No Connection",    description = "Find a flash drive to backup your thesis" },
        new Objective { title = "Backup Plan",      description = "Backup your thesis using the flash drive" },
        new Objective { title = "Stay Secure",      description = "Enable 2FA on your phone" },
        new Objective { title = "Time to Go",       description = "Head to the café" },
    };

    private int _currentIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (objectivePanel != null)
            objectivePanel.SetActive(false);

        // Subscribe to game progress events
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnTriedSubmit       += () => SetObjective(1);
            GameProgressManager.Instance.OnFlashDrivePickedUp += () => SetObjective(2);
            GameProgressManager.Instance.OnThesisBackedUp     += () => SetObjective(3);
            GameProgressManager.Instance.On2FAEnabled          += () => SetObjective(4);
        }

        // Show first objective after optional delay
        if (initialDelay > 0f)
            StartCoroutine(ShowFirstObjectiveAfterDelay());
        else
            SetObjective(0);
    }

    IEnumerator ShowFirstObjectiveAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        SetObjective(0);
    }

    /// <summary>
    /// Advance to a specific objective index. Won't go backwards.
    /// </summary>
    public void SetObjective(int index)
    {
        if (index < 0 || index >= objectives.Length) return;
        if (index <= _currentIndex) return; // never go backwards

        _currentIndex = index;

        if (objectiveTitle != null)
            objectiveTitle.text = objectives[index].title;

        if (objectiveDescription != null)
            objectiveDescription.text = objectives[index].description;

        if (objectivePanel != null)
            objectivePanel.SetActive(true);

        if (canvasGroup != null)
            StartCoroutine(FadeIn());
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
}
