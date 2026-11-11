using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Life is Strange style "This action will have consequences..." notice, placed under the objective panel.
// A butterfly flutters in, lands next to the hand-written line, then flies off.
// Show it after every meaningful choice (good or bad) so it never gives away which one was wrong.
// The notice is the ConsequenceToast prefab, placed in both game scenes; ConsequenceToast.Show() plays it.
public class ConsequenceToast : MonoBehaviour
{
    const float ButterflySize = 80;

    [Tooltip("The notice (butterfly + text). Hidden until shown.")]
    public RectTransform notice;
    public RectTransform butterfly;
    public CanvasGroup butterflyAlpha;
    public CanvasGroup textAlpha;

    static ConsequenceToast _instance;

    void Awake() => _instance = this;

    void Start()
    {
        notice.gameObject.SetActive(false);
        // Same pixel scale as the HUD it sits in (the HUD canvas uses a fixed scale factor).
        var panel = ObjectiveManager.Instance != null ? ObjectiveManager.Instance.objectivePanel : null;
        var hud = panel != null ? panel.GetComponentInParent<Canvas>(true) : null;
        var hudScaler = hud != null ? hud.rootCanvas.GetComponent<CanvasScaler>() : null;
        if (hudScaler != null) GetComponent<CanvasScaler>().scaleFactor = hudScaler.scaleFactor;
    }

    public static void Show()
    {
        if (_instance == null) return;
        _instance.StopAllCoroutines();
        _instance.StartCoroutine(_instance.Play());
    }

    void LateUpdate()
    {
        // Sit under the objective panel's bottom-left corner (it resizes with each objective).
        // While it's hidden between objectives, stay where it was last shown.
        var panel = ObjectiveManager.Instance != null ? ObjectiveManager.Instance.objectivePanel : null;
        if (panel == null || !panel.activeInHierarchy) return;
        var corners = new Vector3[4];
        ((RectTransform)panel.transform).GetWorldCorners(corners); // overlay canvas: world space is screen pixels
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, corners[0], null, out var point);
        notice.localPosition = point + new Vector2(4, -14);
    }

    IEnumerator Play()
    {
        notice.gameObject.SetActive(true);
        textAlpha.alpha = 0;
        Vector2 perch = new Vector2(ButterflySize / 2, -ButterflySize / 2);
        Vector2 from = perch + new Vector2(-150, -70), away = perch + new Vector2(260, 120);

        // Flutter in on a small arc.
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.9f)
        {
            float e = 1 - (1 - t) * (1 - t);
            Fly(Vector2.Lerp(from, perch, e) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 35, 22, 0.2f);
            butterflyAlpha.alpha = Mathf.Clamp01(t * 3);
            yield return null;
        }

        // Land, show the line, rest with slow wing beats.
        for (float rest = 0; rest < 3.6f; rest += Time.unscaledDeltaTime)
        {
            textAlpha.alpha = Mathf.Clamp01(rest / 0.5f);
            Fly(perch, 5, 0.55f);
            yield return null;
        }

        // Fly away while the line fades.
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.9f)
        {
            Fly(Vector2.Lerp(perch, away, t * t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 20, 24, 0.2f);
            butterflyAlpha.alpha = 1 - t;
            textAlpha.alpha = 1 - Mathf.Clamp01(t * 2);
            yield return null;
        }
        notice.gameObject.SetActive(false);
    }

    // Wing beat = squashing the drawing horizontally around its body.
    void Fly(Vector2 position, float beatSpeed, float closedWidth)
    {
        butterfly.anchoredPosition = position;
        float beat = (Mathf.Sin(Time.unscaledTime * beatSpeed) + 1) / 2;
        butterfly.localScale = new Vector3(Mathf.Lerp(closedWidth, 1, beat), 1, 1);
        butterfly.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 2.3f) * 8);
    }
}
