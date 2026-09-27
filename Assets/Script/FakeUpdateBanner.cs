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

    RectTransform banner;
    bool decided;

    void Awake()
    {
        var desktop = GetComponent<LaptopDesktop>();
        var window = desktop != null ? desktop.thesisWindow : null;
        banner = UIBuild.NewRect("FakeUpdateBanner", window != null ? window.transform : transform);
        banner.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; // in case the window lays out its children
        banner.anchorMin = new Vector2(0, 1);
        banner.anchorMax = Vector2.one;
        banner.pivot = new Vector2(0.5f, 1);
        // Docked under the window's 50-unit title bar, like a browser info bar (keeps the close button reachable).
        banner.offsetMin = new Vector2(0, -50 - 84);
        banner.offsetMax = new Vector2(0, -50);
        banner.gameObject.AddComponent<Image>().color = Amber;
        Row(banner);

        var text = UIBuild.Label(banner, "Your browser is out of date. Download update to continue.", 26, Ink, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        UIBuild.MakeButton(banner, "Download", Blue, Download, 200, 56, 24);
        UIBuild.MakeButton(banner, "X", Grey, Dismiss, 56, 56, 26);
    }

    static void Row(RectTransform rect)
    {
        var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(24, 14, 14, 14);
        row.spacing = 14;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
    }

    // X, or submitting the thesis without ever touching it.
    public void Dismiss()
    {
        if (!Decide(true)) return;
        Destroy(banner.gameObject);
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
        foreach (Transform child in banner) Destroy(child.gameObject);
        var text = UIBuild.Label(banner, "Installing browser update...", 24, Ink, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.gameObject.AddComponent<LayoutElement>().preferredWidth = 360;
        var track = UIBuild.NewRect("Progress", banner);
        track.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        var size = track.gameObject.AddComponent<LayoutElement>();
        size.flexibleWidth = 1;
        size.preferredHeight = 18;
        var fill = UIBuild.Stretch(UIBuild.NewRect("Fill", track));
        fill.gameObject.AddComponent<Image>().color = Blue;

        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 2.5f)
        {
            fill.anchorMax = new Vector2(t, 1);
            yield return null;
        }
        Destroy(banner.gameObject);
        ConsequenceToast.Show();
    }
}
