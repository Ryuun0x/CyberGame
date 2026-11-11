using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Submitting the thesis: the thesis window's Submit button (or the Browser icon on the desktop) opens a browser window
/// (Goggle Mail). It loads for a moment, then:
///   - no internet (home: the Wi-Fi is down; café: not connected) → an offline page, like a browser shows
///     when a site won't load ("No internet ... ERR_INTERNET_DISCONNECTED");
///   - online (café) → a mail compose page with Thesis_Final.pdf attached. Send → "Sending..." → "Message sent".
/// At the café the web page also pushes the fake browser update when it loads (FakeUpdateBanner).
///
/// On the LaptopCanvas, next to LaptopDesktop. Everything is placed under LaptopCanvas > BrowserWindow, hidden
/// until opened. Buttons call Open / Send / Close from their OnClick.
/// </summary>
public class ThesisSubmit : MonoBehaviour
{
    public LaptopDesktop desktop;
    [Tooltip("The Browser icon on the desktop (Chapter 1). At night Chapter 2 shows its own browser icon instead.")]
    public GameObject desktopIcon;

    [Header("Browser window (LaptopCanvas > BrowserWindow)")]
    public GameObject window;
    public TextMeshProUGUI tabText;
    public TextMeshProUGUI addressText;

    [Header("Pages (only one shows at a time)")]
    public GameObject loadingPage;
    public GameObject offlinePage;
    public GameObject mailPage;
    [Tooltip("Goggle Drive upload page (Save to Drive).")]
    public GameObject drivePage;
    public RectTransform driveFill;
    public TextMeshProUGUI driveStatus;

    [Header("Mail page")]
    [Tooltip("Compose header text: shows \"From:\" the email typed into the fake Wi-Fi sign-in, if any.")]
    public TextMeshProUGUI composeHeader;
    [Tooltip("The Thesis_Final.pdf chip: only when the browser was opened with the thesis (Submit).")]
    public GameObject attachment;
    public GameObject sendButton;
    public GameObject sendingRow;
    public RectTransform sendingFill;
    public GameObject sentBanner;

    const string MailAddress = "https://mail.goggle.com/compose";
    const string DriveAddress = "https://drive.goggle.com/my-drive";

    bool sent;
    bool withThesis; // opened from the thesis window's Submit (vs. just browsing from the desktop icon)
    bool toDrive;    // opened from Save to Drive

    void Start()
    {
        window.SetActive(false);
        bool night = GameProgressManager.Instance != null && GameProgressManager.Instance.chapter >= 2;
        if (desktopIcon != null) desktopIcon.SetActive(!night);
    }

    // Called by the thesis window's Submit button: the mail opens with the thesis attached.
    public void Open() { toDrive = false; OpenBrowser(true); }

    // Called by the thesis window's Save to Drive button: uploads a copy to the school Drive (needs internet).
    public void SaveToDrive() { toDrive = true; OpenBrowser(false); }

    // Called by the desktop Browser icon: just the browser. Nothing is sent, and at home the offline page
    // doesn't count as trying to submit.
    public void Browse() { toDrive = false; OpenBrowser(false); }

    void OpenBrowser(bool thesis)
    {
        withThesis = thesis;
        StopAllCoroutines();
        window.SetActive(true);
        window.transform.SetAsLastSibling(); // on top of the thesis window
        StartCoroutine(Load());
    }

    // Called by the browser's X.
    public void Close()
    {
        StopAllCoroutines();
        window.SetActive(false);
    }

    IEnumerator Load()
    {
        ShowPage(loadingPage, "Loading...");
        yield return new WaitForSecondsRealtime(1.2f);

        if (!desktop.HasInternet)
        {
            ShowPage(offlinePage, "No internet");
            if (withThesis) desktop.TriedToSendOffline();
            if (toDrive) desktop.TriedDriveOffline();
            yield break;
        }

        if (toDrive)
        {
            yield return UploadToDrive();
            yield break;
        }

        ShowPage(mailPage, "Goggle Mail - New message");
        // Signed in to the fake Wi-Fi page? Then the thesis goes out from the very account the attacker now has.
        string from = GameProgressManager.Instance != null ? GameProgressManager.Instance.schoolEmail : "";
        if (composeHeader != null)
            composeHeader.text = string.IsNullOrEmpty(from) ? "New message" : $"New message   <color=#9AA0A6>From: {from}</color>";
        attachment.SetActive(withThesis || sent);
        sendButton.SetActive(withThesis && !sent);
        sendingRow.SetActive(false);
        sentBanner.SetActive(sent);
        desktop.MailPageLoaded(); // the café's web page pushes the fake update here
    }

    void ShowPage(GameObject page, string tab)
    {
        loadingPage.SetActive(page == loadingPage);
        offlinePage.SetActive(page == offlinePage);
        mailPage.SetActive(page == mailPage);
        drivePage.SetActive(page == drivePage);
        tabText.text = tab;
        addressText.text = toDrive ? DriveAddress : MailAddress;
    }

    // "Uploading Thesis_Final.pdf to My Drive" with a progress bar, then "Saved to My Drive".
    IEnumerator UploadToDrive()
    {
        ShowPage(drivePage, "Goggle Drive - My Drive");
        bool already = GameProgressManager.Instance != null && GameProgressManager.Instance.thesisInCloud;
        if (!already)
        {
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 2.8f)
            {
                driveFill.anchorMax = new Vector2(t, 1);
                driveStatus.text = $"Uploading Thesis_Final.pdf...  {Mathf.RoundToInt(t * 100)}%";
                yield return null;
            }
            desktop.DriveSaved();
        }
        driveFill.anchorMax = Vector2.one;
        driveStatus.text = "<color=#81C995>Saved to My Drive</color>   Thesis_Final.pdf · 2.4 MB";
    }

    // Called by the Send button.
    public void Send()
    {
        if (sent || !withThesis || !desktop.HasInternet) return;
        sent = true;
        StartCoroutine(Sending());
    }

    IEnumerator Sending()
    {
        sendButton.SetActive(false);
        sendingRow.SetActive(true);
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 2.2f)
        {
            sendingFill.anchorMax = new Vector2(t, 1);
            yield return null;
        }
        sendingRow.SetActive(false);
        sentBanner.SetActive(true);
        desktop.SubmitThesis(); // records the submission, unlocks the café exit, narration
        yield return new WaitForSecondsRealtime(2.5f);
        window.SetActive(false);
    }
}
