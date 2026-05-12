using UnityEngine;
using TMPro;
using System.Collections;
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
    public float bottomOffset = 60f;
    public float maxWidth = 500f;

    void Awake()
    {
        Instance = this;
        _boxRect = narrationBox.GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        narrationBox.SetActive(false);
    }

    void Start()
    {
        NarrationManager.Instance.Show("Ugh... Thesis is due at noon. I better check if the file synced.");
    }

    public void Show(string message, float duration = 4f)
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message, duration));
    }

    IEnumerator ShowRoutine(string message, float duration)
    {
        // Set text first while box is still hidden
        narrationText.text = message;

        // Force TMP to calculate size immediately
        narrationText.ForceMeshUpdate();
        Vector2 textSize = narrationText.GetPreferredValues(message, maxWidth, 0);

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
        _boxRect.anchoredPosition = new Vector2(0, bottomOffset);

        // NOW show it — already correct size
        narrationBox.SetActive(true);

        yield return new WaitForSeconds(duration);
        narrationBox.SetActive(false);
    }
}