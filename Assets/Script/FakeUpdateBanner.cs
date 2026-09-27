using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Café laptop, once online with the thesis open: the web page pushes a fake "browser update".
// X (or never touching it) is safe; Download "installs" it. The payoff (files locked, backup saves you)
// comes in Chapter 2 through GameProgressManager.installedFakeUpdate.
public class FakeUpdateBanner : MonoBehaviour
{
    static readonly Color Amber = new Color(1f, 0.87f, 0.45f);
    static readonly Color Ink = new Color(0.12f, 0.13f, 0.16f);
    static readonly Color Blue = new Color(0.2f, 0.47f, 0.93f);
    static readonly Color Grey = new Color(0.55f, 0.57f, 0.62f);

    RectTransform popup, card;
    bool decided;

    void Awake()
    {
        var desktop = GetComponent<LaptopDesktop>();
        var window = desktop != null ? desktop.thesisWindow : null;
        // Dimmed backdrop over the thesis window; it blocks the page until the player picks X or Download.
        popup = UIBuild.Stretch(UIBuild.NewRect("FakeUpdatePopup", window != null ? window.transform : transform));
        popup.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; // in case the window lays out its children
        popup.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);

        card = UIBuild.NewRect("Card", popup);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(620, 0);
        card.gameObject.AddComponent<Image>().color = Color.white;
        UIBuild.Column(card, 32, 18);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var header = Row(UIBuild.NewRect("Header", card), TextAnchor.MiddleLeft);
        var title = UIBuild.Label(header, "Update required", 32, Ink, FontStyles.Bold);
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        UIBuild.MakeButton(header, "X", Grey, Dismiss, 48, 48, 24);

        var stripe = UIBuild.NewRect("Stripe", card);
        stripe.gameObject.AddComponent<Image>().color = Amber;
        stripe.gameObject.AddComponent<LayoutElement>().preferredHeight = 6;

        UIBuild.Label(card, "Your browser is out of date. Download update to continue.", 26, Ink, FontStyles.Normal);

        var footer = Row(UIBuild.NewRect("Footer", card), TextAnchor.MiddleRight);
        UIBuild.MakeButton(footer, "Download", Blue, Download, 220, 60, 26);
    }

    static RectTransform Row(RectTransform rect, TextAnchor alignment)
    {
        var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 14;
        row.childAlignment = alignment;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        return rect;
    }

    // X, or submitting the thesis without ever touching it.
    public void Dismiss()
    {
        if (!Decide(true)) return;
        Destroy(popup.gameObject);
        ConsequenceToast.Show();
    }

    public void Download()
    {
        if (!Decide(false)) return;
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.installedFakeUpdate = true;
        StartCoroutine(Install());
    }

    bool Decide(bool safe)
    {
        if (decided) return false;
        decided = true;
        if (ThreatLog.Instance != null) ThreatLog.Instance.Record("fake_update", safe);
        return true;
    }

    // Looks like a harmless update, then nothing visible happens.
    IEnumerator Install()
    {
        foreach (Transform child in card) Destroy(child.gameObject);
        UIBuild.Label(card, "Installing browser update...", 28, Ink, FontStyles.Bold);
        var track = UIBuild.NewRect("Progress", card);
        track.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.12f);
        track.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;
        var fill = UIBuild.Stretch(UIBuild.NewRect("Fill", track));
        fill.gameObject.AddComponent<Image>().color = Blue;

        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 2.5f)
        {
            fill.anchorMax = new Vector2(t, 1);
            yield return null;
        }
        Destroy(popup.gameObject);
        ConsequenceToast.Show();
    }
}
