using System.Collections;
using TMPro;
using UnityEngine;

// Café laptop, once online with the thesis open: the web page pushes a fake "browser update" popup
// (FakeUpdatePopup, placed inside the browser window, hidden until shown). Reviewer #12: the player gets a real
// chance to avoid it.
//   Question:  "Update required"  X = Safe (nothing downloaded), Download → the file downloads, then
//   RunPrompt: a security warning "Do you want to run this file?" (Browser_Update.exe, Unknown Publisher):
//                Run        → installs it: Compromised (GameProgressManager.installedFakeUpdate; Chapter 2 locks the files)
//                Don't run  → Recovered (downloaded, never ran)
//                Scan with antivirus → the scan finds ransomware and removes the file: Recovered
//   ScanResult: what the antivirus found, OK closes.
// "Working" shows the progress bar (downloading / scanning / installing) with its text.
public class FakeUpdateBanner : MonoBehaviour
{
    [Tooltip("The dimmed popup over the browser.")]
    public GameObject popup;
    [Tooltip("Title, message, X and Download buttons.")]
    public GameObject question;
    [Tooltip("The security warning after the download: Scan with antivirus / Run / Don't run.")]
    public GameObject runPrompt;
    [Tooltip("The antivirus result: threat found and removed, OK.")]
    public GameObject scanResult;
    [Tooltip("Progress bar with a line of text (downloading / scanning / installing).")]
    public GameObject installing;
    public TextMeshProUGUI workingText;
    public RectTransform progressFill;

    public bool Shown { get; private set; }

    bool decided, downloaded;

    void Start() => popup.SetActive(false);

    // Called by LaptopDesktop once the laptop is online with the thesis open. Only once.
    public void Show()
    {
        if (Shown) return;
        Shown = true;
        ShowStep(question);
        popup.SetActive(true);
    }

    // Called by the first popup's X, or when the thesis is sent without touching the popup.
    public void Dismiss()
    {
        if (downloaded) { DontRun(); return; } // closed at the security warning = didn't run it
        if (!Decide(true, false)) return;
        popup.SetActive(false);
        ConsequenceToast.Show();
    }

    // Called by the Download button: only downloads. Running it is a separate choice.
    public void Download()
    {
        if (decided || downloaded) return;
        downloaded = true;
        StartCoroutine(Work("Downloading Browser_Update.exe...", 1.2f, runPrompt));
    }

    // Called by the security warning's Run button.
    public void Run()
    {
        if (!downloaded || !Decide(false, false)) return;
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.installedFakeUpdate = true;
        StartCoroutine(Work("Installing browser update...", 2.5f, null)); // looks harmless; nothing visible happens
    }

    // Called by the security warning's Don't run button (and its X).
    public void DontRun()
    {
        if (!downloaded || !Decide(true, true)) return;
        popup.SetActive(false);
        ConsequenceToast.Show();
    }

    // Called by the security warning's Scan with antivirus button.
    public void Scan()
    {
        if (!downloaded || !Decide(true, true)) return;
        StartCoroutine(Work("Scanning Browser_Update.exe with antivirus...", 2f, scanResult));
    }

    // Called by the scan result's OK (and its X).
    public void CloseScanResult()
    {
        popup.SetActive(false);
        ConsequenceToast.Show();
    }

    // exposed = took the bait (downloaded) but backed out → Recovered instead of Safe.
    bool Decide(bool safe, bool exposed)
    {
        if (decided) return false;
        decided = true;
        if (ThreatLog.Instance != null)
        {
            if (exposed) ThreatLog.Instance.MarkExposed("fake_update");
            ThreatLog.Instance.Record("fake_update", safe);
        }
        return true;
    }

    void ShowStep(GameObject step)
    {
        question.SetActive(step == question);
        runPrompt.SetActive(step == runPrompt);
        scanResult.SetActive(step == scanResult);
        installing.SetActive(step == installing);
    }

    // The progress bar for a moment, then the next step (null = close, as after installing).
    IEnumerator Work(string text, float seconds, GameObject next)
    {
        workingText.text = text;
        ShowStep(installing);
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / seconds)
        {
            progressFill.anchorMax = new Vector2(t, 1);
            yield return null;
        }
        if (next != null) { ShowStep(next); yield break; }
        popup.SetActive(false);
        ConsequenceToast.Show();
    }

    // Editor checks only: back to never shown.
    public void ResetForChecks()
    {
        StopAllCoroutines();
        Shown = decided = downloaded = false;
        popup.SetActive(false);
    }
}
