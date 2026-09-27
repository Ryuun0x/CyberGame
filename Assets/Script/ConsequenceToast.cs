using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Life is Strange style "This action will have consequences..." notice, placed under the objective panel.
// A butterfly flutters in, lands next to the hand-written line, then flies off.
// Show it after every meaningful choice (good or bad) so it never gives away which one was wrong.
public class ConsequenceToast : MonoBehaviour
{
    const float ButterflySize = 58, TextHeight = 46, Gap = 12;
    static ConsequenceToast _current;

    RectTransform _root, _butterfly;
    CanvasGroup _butterflyAlpha, _textAlpha;

    public static void Show()
    {
        if (_current != null) Destroy(_current.gameObject);
        _current = new GameObject("ConsequenceToast").AddComponent<ConsequenceToast>();
        _current.Build();
    }

    void Build()
    {
        UIBuild.Overlay(gameObject, 450);
        GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; // same scale as the HUD
        GetComponent<GraphicRaycaster>().enabled = false; // never blocks clicks

        _root = UIBuild.NewRect("Notice", transform);
        _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0, 1);
        _root.anchoredPosition = new Vector2(48, -48); // until the objective panel is visible

        var textSprite = Resources.Load<Sprite>("ConsequenceText");
        var text = UIBuild.NewRect("Text", _root);
        text.anchorMin = text.anchorMax = text.pivot = new Vector2(0, 1);
        text.anchoredPosition = new Vector2(ButterflySize + Gap, -(ButterflySize - TextHeight) / 2);
        text.sizeDelta = new Vector2(TextHeight * textSprite.rect.width / textSprite.rect.height, TextHeight);
        text.gameObject.AddComponent<Image>().sprite = textSprite;
        _textAlpha = text.gameObject.AddComponent<CanvasGroup>();
        _textAlpha.alpha = 0;

        _butterfly = UIBuild.NewRect("Butterfly", _root);
        _butterfly.anchorMin = _butterfly.anchorMax = new Vector2(0, 1);
        _butterfly.sizeDelta = Vector2.one * ButterflySize;
        _butterfly.gameObject.AddComponent<Image>().sprite = Resources.Load<Sprite>("ConsequenceButterfly");
        _butterflyAlpha = _butterfly.gameObject.AddComponent<CanvasGroup>();

        LateUpdate();
        StartCoroutine(Play());
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
        _root.localPosition = point + new Vector2(4, -14);
    }

    IEnumerator Play()
    {
        Vector2 perch = new Vector2(ButterflySize / 2, -ButterflySize / 2);
        Vector2 from = perch + new Vector2(-150, -70), away = perch + new Vector2(260, 120);

        // Flutter in on a small arc.
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.9f)
        {
            float e = 1 - (1 - t) * (1 - t);
            Fly(Vector2.Lerp(from, perch, e) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 35, 22, 0.2f);
            _butterflyAlpha.alpha = Mathf.Clamp01(t * 3);
            yield return null;
        }

        // Land, show the line, rest with slow wing beats.
        float rest = 0;
        while (rest < 3.6f)
        {
            rest += Time.unscaledDeltaTime;
            _textAlpha.alpha = Mathf.Clamp01(rest / 0.5f);
            Fly(perch, 5, 0.55f);
            yield return null;
        }

        // Fly away while the line fades.
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.9f)
        {
            Fly(Vector2.Lerp(perch, away, t * t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 20, 24, 0.2f);
            _butterflyAlpha.alpha = 1 - t;
            _textAlpha.alpha = 1 - Mathf.Clamp01(t * 2);
            yield return null;
        }
        Destroy(gameObject);
    }

    // Wing beat = squashing the drawing horizontally around its body.
    void Fly(Vector2 position, float beatSpeed, float closedWidth)
    {
        _butterfly.anchoredPosition = position;
        float beat = (Mathf.Sin(Time.unscaledTime * beatSpeed) + 1) / 2;
        _butterfly.localScale = new Vector3(Mathf.Lerp(closedWidth, 1, beat), 1, 1);
        _butterfly.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 2.3f) * 8);
    }
}
