using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance;

    public GameObject narrationBox;
    public TextMeshProUGUI narrationText;

    private RectTransform _boxRect;
    private Canvas _canvas;

    [Header("Padding")]
    public float paddingX = 20f;
    public float paddingY = 20f;

    [Header("Position")]
    [Tooltip("Gap between the subtitle and the top of the hotbar (PhoneNotifications prefab).")]
    public float bottomOffset = 12f;
    [Tooltip("Height of the hotbar with its \"Tab\" label, as a part of the screen height (it scales with the screen).")]
    public float hotbarHeight = 0.16f;
    public float maxWidth = 500f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _boxRect = narrationBox.GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        narrationBox.SetActive(false);
    }

    IEnumerator Start()
    {
        // Only show the opening narration if the player hasn't progressed yet,
        // and only once the opening cutscenes are over (it is voiced now, so it would be heard over them).
        if (GameProgressManager.Instance != null && GameProgressManager.Instance.triedSubmitWithoutInternet) yield break;
        yield return null; // the cutscenes start in their own Start
        while (WakeUpSequence.Playing) yield return null;
        Show("Ugh... Thesis is due at noon. I better check if the file synced.");
    }

    // Lines wait for the voice of the line before to finish (a new line used to replace the old one at once,
    // which would now cut the voice off mid-sentence). Without a voice playing, a new line still shows at once.
    readonly Queue<(string message, float duration)> queue = new Queue<(string, float)>();
    Coroutine runner, display;
    string current;

    public void Show(string message, float duration = 4f)
    {
        if (message == current && Voice.Remaining > 0) return; // the same line again, e.g. pressing E twice
        queue.Enqueue((message, duration));
        if (runner == null) runner = StartCoroutine(RunQueue());
    }

    IEnumerator RunQueue()
    {
        while (queue.Count > 0)
        {
            while (Voice.Remaining > 0.05f) yield return null;
            var (message, duration) = queue.Dequeue();
            if (display != null) StopCoroutine(display);
            current = message;
            duration = Mathf.Max(duration, Voice.Play(message) + 0.4f); // stays up while the voice speaks
            display = StartCoroutine(ShowRoutine(message, duration));
        }
        runner = null;
    }

    IEnumerator ShowRoutine(string message, float duration)
    {
        // Set text first while box is still hidden
        narrationText.text = message;

        // Force TMP to calculate size immediately
        narrationText.ForceMeshUpdate();
        // Measured at the width the text really gets inside the box (without the padding), so a long line
        // wraps the same way here as on screen and the box is tall enough for every line.
        Vector2 textSize = narrationText.GetPreferredValues(message, maxWidth - paddingX * 2, 0);

        // Resize box BEFORE showing it
        float boxWidth = Mathf.Min(textSize.x + paddingX * 2, maxWidth);
        float boxHeight = textSize.y + paddingY * 2;
        _boxRect.sizeDelta = new Vector2(boxWidth, boxHeight);

        narrationText.rectTransform.sizeDelta = new Vector2(
            boxWidth - paddingX * 2,
            boxHeight - paddingY * 2
        );

        // Set position
        _boxRect.anchorMin = new Vector2(0.5f, 0f);
        _boxRect.anchorMax = new Vector2(0.5f, 0f);
        _boxRect.pivot = new Vector2(0.5f, 0f);
        _boxRect.anchoredPosition = new Vector2(0, Screen.height * hotbarHeight / _canvas.scaleFactor + bottomOffset);

        // NOW show it - already correct size
        narrationBox.SetActive(true);
        // In front of the phone: the box has its own Canvas (order 30). Unity forgets "override sorting" on a canvas
        // that starts hidden, so it is switched on here, once the box is showing.
        var ownCanvas = narrationBox.GetComponent<Canvas>();
        if (ownCanvas != null) ownCanvas.overrideSorting = true;

        // Real time (the phone stops game time), standing still while the pause menu is open.
        for (float t = 0; t < duration; t += PauseMenu.IsPaused ? 0 : Time.unscaledDeltaTime) yield return null;
        narrationBox.SetActive(false);
        current = null;
    }
}
