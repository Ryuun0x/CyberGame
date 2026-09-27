using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Life is Strange style "This action will have consequences." notice.
// Show it after every meaningful choice (good or bad) so it never gives away which one was wrong.
public class ConsequenceToast : MonoBehaviour
{
    static ConsequenceToast _current;
    static Sprite _butterfly;

    public static void Show(string message = "This action will have consequences.")
    {
        if (_current != null) Destroy(_current.gameObject);
        _current = new GameObject("ConsequenceToast").AddComponent<ConsequenceToast>();
        _current.Build(message);
    }

    void Build(string message)
    {
        UIBuild.Overlay(gameObject, 450);
        GetComponent<GraphicRaycaster>().enabled = false; // never blocks clicks
        var group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;

        var panel = UIBuild.NewRect("Toast", transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
        panel.anchoredPosition = new Vector2(48, -48);
        panel.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.72f);
        var row = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(18, 26, 12, 12);
        row.spacing = 14;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (_butterfly == null) _butterfly = UIBuild.Icon(64, UIBuild.Butterfly, new Color(0.45f, 0.75f, 1f));
        var icon = UIBuild.NewRect("Butterfly", panel);
        icon.gameObject.AddComponent<Image>().sprite = _butterfly;
        var size = icon.gameObject.AddComponent<LayoutElement>();
        size.preferredWidth = size.preferredHeight = 40;

        var text = UIBuild.Label(panel, message, 28, Color.white, FontStyles.Italic);
        text.textWrappingMode = TextWrappingModes.NoWrap;

        StartCoroutine(Play(group));
    }

    IEnumerator Play(CanvasGroup group)
    {
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.4f) { group.alpha = t; yield return null; }
        group.alpha = 1;
        yield return new WaitForSecondsRealtime(3f);
        for (float t = 1; t > 0; t -= Time.unscaledDeltaTime / 0.6f) { group.alpha = t; yield return null; }
        Destroy(gameObject);
    }
}
