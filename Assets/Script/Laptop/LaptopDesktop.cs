using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LaptopDesktop : MonoBehaviour
{
    [Header("Windows")]
    public GameObject thesisWindow;

    [Header("Backup")]
    [Tooltip("The backup button inside the thesis window — hidden until flash drive is found")]
    public GameObject backupButton;

    [Header("Taskbar")]
    public TextMeshProUGUI timeText;
    public GameObject pdfIcon;
    public GameObject noInternetIcon;

    [Header("Backups")]
    [Tooltip("\"Copying...\" dialog shown while the thesis is copied to the flash drive.")]
    public GameObject copyDialog;
    public RectTransform copyFill;
    [Tooltip("The Save to Drive button in the thesis window.")]
    public GameObject driveButton;

    [Header("WiFi Tooltip")]
    public GameObject wifiTooltip;
    public TextMeshProUGUI tooltipStatus;
    private bool _tooltipVisible = false;


    [Header("Café only (leave empty on the house laptop)")]
    [Tooltip("The café laptop's own Wi-Fi list and fake sign-in page.")]
    public LaptopWifi wifi;
    [Tooltip("The fake browser update popup.")]
    public FakeUpdateBanner updateBanner;

    [Header("Settings")]
    public float internetLostDelay = 2f;

    private bool _hasInternet = true;
    private bool _thesisOpen = false;
    private bool _chapterEnding = false;

    public string chapterTitle = "Chapter 1: The Deadline";
    public string nextChapterTitle = "Chapter 2"; // title card shown after the debrief

    void Start()
    {
        // Hide everything on boot
        if (thesisWindow != null)
            thesisWindow.SetActive(false);
        if (pdfIcon != null)
            pdfIcon.SetActive(false);
        if (noInternetIcon != null)
            noInternetIcon.SetActive(false);

        // Hide backup button until flash drive is found
        if (backupButton != null)
            backupButton.SetActive(false);

        _hasInternet = true;

    }

    void Update()
    {
        // Update taskbar clock
        if (timeText != null)
            timeText.text = System.DateTime.Now.ToString("h:mm tt");

        // Café: the 'No Internet' X follows the laptop's own connection
        if (wifi != null && noInternetIcon != null)
            noInternetIcon.SetActive(!wifi.HasInternet);

    }

    // Can the laptop reach the internet? At home the Wi-Fi is down all morning; at the café it depends on
    // which network the laptop joined.
    public bool HasInternet => wifi != null && wifi.HasInternet;

    // ThesisSubmit: the browser loaded with no internet (the offline page is showing).
    bool _triedOffline;
    public void TriedToSendOffline()
    {
        if (wifi != null)
        {
            StartCoroutine(PlayCafeNoWifiNarration());
            return;
        }
        if (_triedOffline) return; // the frustration lines play once
        _triedOffline = true;
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.MarkTriedSubmit();
        StartCoroutine(PlayFrustrationNarration());
    }

    // ThesisSubmit: the mail page loaded online. At the café that web page pushes a fake browser update.
    public void MailPageLoaded()
    {
        if (updateBanner != null) updateBanner.Show();
    }

    // Called when WifiIcon is clicked
    public void ToggleWifiTooltip()
    {
        // Café: the Wi-Fi icon opens the laptop's network list instead of the status tooltip
        if (wifi != null)
        {
            wifi.ToggleList();
            return;
        }

        Debug.Log("Has Internet: " + _hasInternet);

        _tooltipVisible = !_tooltipVisible;
        
        if (wifiTooltip != null)
            wifiTooltip.SetActive(_tooltipVisible);
        
        // Update status text based on internet state
        if (tooltipStatus != null && _tooltipVisible)
        {
            tooltipStatus.text = _hasInternet ? "Internet access" : "No Internet access";
            tooltipStatus.color = _hasInternet 
                ? new Color(0.67f, 0.67f, 0.67f)
                : new Color(1f, 0.4f, 0.4f);
        }
    }

    // Called when ThesisIcon is clicked
    public void OpenThesis()
    {
        if (_thesisOpen)
        {
            thesisWindow.transform.SetAsLastSibling(); // already open (maybe behind the browser): bring it to the front
            return;
        }
        // Chapter 2: the thesis was already submitted; the Chapter 1 thesis / no-internet flow must not replay.
        if (GameProgressManager.Instance != null && GameProgressManager.Instance.chapter >= 2)
        {
            if (NarrationManager.Instance != null) NarrationManager.Instance.Show("I already submitted my thesis.", 3f);
            return;
        }
        _thesisOpen = true;

        // Show thesis window, in front of any other window (e.g. the browser)
        if (thesisWindow != null)
        {
            thesisWindow.SetActive(true);
            thesisWindow.transform.SetAsLastSibling();
        }

        // Show PDF icon in taskbar (app is now "open")
        if (pdfIcon != null)
            pdfIcon.SetActive(true);

        // Show or hide backup button based on flash drive possession
        RefreshBackupButton();

        // Start internet loss countdown
        StartCoroutine(LoseInternetAfterDelay());
    }

    // Called by Close button in TitleBar
    public void CloseThesis()
    {
        _thesisOpen = false;

        if (thesisWindow != null)
            thesisWindow.SetActive(false);

        // Hide PDF icon from taskbar (app closed)
        if (pdfIcon != null)
            pdfIcon.SetActive(false);
    }

    // Called by ThesisSubmit once the mail with the thesis is sent (only possible online, i.e. at the café).
    public void SubmitThesis()
    {
        bool isInterior = SceneManager.GetActiveScene().name == "Interior"; // Adjust if your interior scene is named differently

        if (!isInterior)
        {
            if (_chapterEnding) return;

            // ── CAFE LOGIC: any working connection uploads the same way ──
            // The evil twin was already recorded when the player chose a network; nothing here gives it away.
            if (wifi != null && wifi.HasInternet)
            {
                Debug.Log("Thesis submitted via " + wifi.Network);
                _chapterEnding = true;
                if (updateBanner != null && updateBanner.Shown) updateBanner.Dismiss(); // ignored it: that's the safe choice
                if (GameProgressManager.Instance != null) GameProgressManager.Instance.SubmitThesis(); // unlocks the café exit
                CafeStranger.LeaveIfPresent(); // the stranger packs up right away, well before she does
                StartCoroutine(PlayCafeSuccessNarration());
                CloseThesis();
            }
            else
            {
                Debug.Log("Not connected to WiFi in cafe.");
                StartCoroutine(PlayCafeNoWifiNarration());
            }
            return;
        }

        // Home: there is no internet, so sending can't happen here.
        TriedToSendOffline();
    }

    // ── NEW: Backup Logic ──────────────────────────────────

    /// <summary>
    /// Shows the backup button only while the flash drive is plugged in (LaptopUsbPort)
    /// and the thesis hasn't been backed up yet.
    /// </summary>
    public void RefreshBackupButton()
    {
        if (backupButton == null) return;

        bool showBackup = GameProgressManager.Instance != null
                          && GameProgressManager.Instance.flashDriveInserted
                          && !GameProgressManager.Instance.thesisBackedUp;

        backupButton.SetActive(showBackup);
        if (driveButton != null)
            driveButton.SetActive(GameProgressManager.Instance == null || !GameProgressManager.Instance.thesisInCloud);
    }

    /// <summary>
    /// Called by the Backup button in the thesis window.
    /// Wire this to the Button's OnClick event.
    /// </summary>
    public void BackupThesis()
    {
        if (GameProgressManager.Instance == null) return;

        if (!GameProgressManager.Instance.flashDriveInserted)
        {
            Debug.LogWarning("Flash drive not plugged in — cannot backup!");
            return;
        }

        if (_copying) return;
        StartCoroutine(CopyToUsb());
    }

    // "Copying Thesis_Final.pdf to USB Drive (E:)" with a progress bar, then the backup counts.
    bool _copying;
    public bool Copying => _copying; // UsbTray won't eject mid-copy
    IEnumerator CopyToUsb()
    {
        _copying = true;
        if (backupButton != null) backupButton.SetActive(false);
        if (copyDialog != null)
        {
            copyDialog.SetActive(true);
            copyDialog.transform.SetAsLastSibling();
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 2.5f)
            {
                copyFill.anchorMax = new Vector2(t, 1);
                yield return null;
            }
            copyFill.anchorMax = Vector2.one;
            yield return new WaitForSecondsRealtime(0.4f);
            copyDialog.SetActive(false);
        }
        _copying = false;

        GameProgressManager.Instance.BackupThesis();
        // A hint, never required: the school's email in the phone's Mail app asks students to turn on 2FA.
        GameProgressManager.Instance.mailUnread = true;
        PhoneNotifications.Show("Mail", "School IT Office: Protect your Goggle account");
        StartCoroutine(PlayBackupNarration());
        Debug.Log("Thesis backed up to flash drive!");
    }

    // ThesisSubmit: Save to Drive was clicked with no internet.
    bool _driveOfflineSaid;
    public void TriedDriveOffline()
    {
        if (_driveOfflineSaid || NarrationManager.Instance == null) return;
        _driveOfflineSaid = true;
        NarrationManager.Instance.Show("No internet, so it can't upload. I'll save it to my Drive once I'm online.", 4f);
    }

    // ThesisSubmit: the thesis finished uploading to Drive.
    public void DriveSaved()
    {
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.thesisInCloud = true;
        if (driveButton != null) driveButton.SetActive(false);
        bool usb = GameProgressManager.Instance != null && GameProgressManager.Instance.thesisBackedUp;
        if (NarrationManager.Instance != null)
            NarrationManager.Instance.Show(usb ? "Saved to my Drive too. Two backups, in two places." : "Saved to my Drive. At least there's a copy now.", 3.5f);
    }

    IEnumerator LoseInternetAfterDelay()
    {
        bool isInterior = SceneManager.GetActiveScene().name == "Interior";
        if (!isInterior)
        {
            // In the cafe, internet isn't lost automatically. It depends on the phone.
            yield break;
        }

        Debug.Log("Coroutine STARTED - _hasInternet = " + _hasInternet);
        yield return new WaitForSecondsRealtime(internetLostDelay);
        
        _hasInternet = false;
        Debug.Log("Coroutine FINISHED - _hasInternet = " + _hasInternet);
        
        if (noInternetIcon != null)
            noInternetIcon.SetActive(true);
        
        if (wifiTooltip != null && wifiTooltip.activeSelf && tooltipStatus != null)
        {
            tooltipStatus.text = "No Internet access";
            tooltipStatus.color = new Color(1f, 0.4f, 0.4f);
        }
    }

    IEnumerator PlayFrustrationNarration()
    {
        if (NarrationManager.Instance == null) yield break;

        NarrationManager.Instance.Show("What?! No internet? You've got to be kidding me!", 3.5f);
        yield return new WaitForSecondsRealtime(4f);

        NarrationManager.Instance.Show("I need to find a flash drive to back this up...", 3.5f);
        yield return new WaitForSecondsRealtime(4f);

        NarrationManager.Instance.Show("I think I left one in the living room.", 3.5f);
    }

    IEnumerator PlayBackupNarration()
    {
        if (NarrationManager.Instance == null) yield break;

        if (!GameProgressManager.Instance.is2FAEnabled)
        {
            NarrationManager.Instance.Show("Phew, thesis is backed up.", 3f);
            yield return new WaitForSecondsRealtime(3.5f);
            NarrationManager.Instance.Show("I could turn on 2FA on my phone too, before heading out.", 4f);
            // The phone hint comes with this line, when the phone first matters.
            yield return new WaitForSecondsRealtime(1f);
            Hints.Show("phone", "phone", "Phone", $"Press {Hints.Cap("TAB")} to open your phone", UnityEngine.InputSystem.Key.Tab);
        }
        else
        {
            NarrationManager.Instance.Show("Thesis is backed up.", 2.5f);
            yield return new WaitForSecondsRealtime(3f);
            NarrationManager.Instance.Show("Okay, everything's secure.", 2.5f);
            yield return new WaitForSecondsRealtime(3f);
            NarrationManager.Instance.Show("Now I just need to head out to the cafe to find some wifi and send this off.", 4.5f);
        }
    }

    // ── CAFE NARRATION OUTCOMES ──

    IEnumerator PlayCafeSuccessNarration()
    {
        if (NarrationManager.Instance == null) yield break;
        NarrationManager.Instance.Show("Uploading... Done!", 2.5f);
        yield return new WaitForSecondsRealtime(3f);
        NarrationManager.Instance.Show("Perfect. The thesis is submitted securely.", 3.5f);
        yield return new WaitForSecondsRealtime(4f);
        NarrationManager.Instance.Show("Looks like I'm finally done! Time to head out.", 4f);
    }

    // Called by CafeEnding once the player has walked out of the café.
    public void EndChapter()
    {
        var gp = GameProgressManager.Instance;
        if (ThreatLog.Instance != null && gp != null)
        {
            // Both copies (USB + Drive) = Safe; only one = Recovered (partly done); none = Compromised.
            bool usb = gp.thesisBackedUp, cloud = gp.thesisInCloud;
            if (usb != cloud) ThreatLog.Instance.MarkExposed("backup");
            ThreatLog.Instance.Record("backup", usb || cloud);
            ThreatLog.Instance.Record("mfa", gp.is2FAEnabled);
            // Paid off in Chapter 2: the stolen login gets used; the "update" locks the files.
            if (gp.credentialsStolen) ThreatLog.Instance.MarkPending("login_alert");
            if (gp.installedFakeUpdate) ThreatLog.Instance.MarkPending("fake_update");
        }
        DebriefScreen.Show(chapterTitle, nextChapterTitle);
    }

    IEnumerator PlayCafeNoWifiNarration()
    {
        if (NarrationManager.Instance == null) yield break;
        NarrationManager.Instance.Show("I'm not connected to the internet.", 3f);
        yield return new WaitForSecondsRealtime(3.5f);
        NarrationManager.Instance.Show("I need to get this laptop on the café Wi-Fi first.", 4f);
    }
}
