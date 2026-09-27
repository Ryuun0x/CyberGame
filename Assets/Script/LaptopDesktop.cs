using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LaptopDesktop : MonoBehaviour
{
    [Header("Windows")]
    public GameObject thesisWindow;
    public GameObject noInternetPopup; // Build later, leave empty for now

    [Header("Backup")]
    [Tooltip("The backup button inside the thesis window — hidden until flash drive is found")]
    public GameObject backupButton;
    [Tooltip("Optional: popup/message shown after successful backup")]
    public GameObject backupSuccessPopup;

    [Header("Taskbar")]
    public TextMeshProUGUI timeText;
    public GameObject pdfIcon;
    public GameObject noInternetIcon;

    [Header("WiFi Tooltip")]
    public GameObject wifiTooltip;
    public TextMeshProUGUI tooltipStatus;
    private bool _tooltipVisible = false;


    [Header("Settings")]
    public float internetLostDelay = 2f;

    private bool _hasInternet = true;
    private bool _thesisOpen = false;
    private bool _chapterEnding = false;
    private LaptopWifi _wifi; // café only: the laptop picks its own network
    private FakeUpdateBanner _updateBanner;

    public string chapterTitle = "Chapter 1: The Deadline";

    void Start()
    {
        // Hide everything on boot
        if (thesisWindow != null)
            thesisWindow.SetActive(false);
        if (noInternetPopup != null)
            noInternetPopup.SetActive(false);
        if (pdfIcon != null)
            pdfIcon.SetActive(false);
        if (noInternetIcon != null)
            noInternetIcon.SetActive(false);

        // Hide backup button until flash drive is found
        if (backupButton != null)
            backupButton.SetActive(false);
        if (backupSuccessPopup != null)
            backupSuccessPopup.SetActive(false);

        _hasInternet = true;

        if (SceneManager.GetActiveScene().name != "Interior")
            _wifi = gameObject.AddComponent<LaptopWifi>();
    }

    void Update()
    {
        // Update taskbar clock
        if (timeText != null)
            timeText.text = System.DateTime.Now.ToString("h:mm tt");

        // Café: the 'No Internet' X follows the laptop's own connection
        if (_wifi != null && noInternetIcon != null)
            noInternetIcon.SetActive(!_wifi.HasInternet);

        // Café: once online (any network) with the thesis open, the page pushes a fake update. Once only.
        if (_thesisOpen && _wifi != null && _wifi.HasInternet && _updateBanner == null)
            _updateBanner = gameObject.AddComponent<FakeUpdateBanner>();
    }

    // Called when WifiIcon is clicked
    public void ToggleWifiTooltip()
    {
        // Café: the Wi-Fi icon opens the laptop's network list instead of the status tooltip
        if (_wifi != null)
        {
            _wifi.ToggleList();
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
        if (_thesisOpen) return;
        _thesisOpen = true;

        // Show thesis window
        if (thesisWindow != null)
            thesisWindow.SetActive(true);

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

    // Called by Submit button in BottomBar
    public void SubmitThesis()
    {
        bool isInterior = SceneManager.GetActiveScene().name == "Interior"; // Adjust if your interior scene is named differently

        if (!isInterior)
        {
            if (_chapterEnding) return;

            // ── CAFE LOGIC: any working connection uploads the same way ──
            // The evil twin was already recorded when the player chose a network; nothing here gives it away.
            if (_wifi != null && _wifi.HasInternet)
            {
                Debug.Log("Thesis submitted via " + _wifi.Network);
                _chapterEnding = true;
                if (_updateBanner != null) _updateBanner.Dismiss(); // ignored it: that's the safe choice
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

        // ── INTERIOR LOGIC (Old Behavior) ──
        if (_hasInternet)
        {
            // Internet still up (unlikely with 2s delay)
            Debug.Log("Thesis submitted successfully!");
            CloseThesis();
        }
        else
        {
            // No internet — show error popup
            Debug.Log("No internet connection!");

            // Track that the player tried to submit
            if (GameProgressManager.Instance != null)
                GameProgressManager.Instance.MarkTriedSubmit();

            // Frustration & Hint Narration (Sequential)
            StartCoroutine(PlayFrustrationNarration());

            if (noInternetPopup != null)
            {
                noInternetPopup.SetActive(true);
            }
            else
            {
                // Popup not built yet — just log for now
                Debug.LogWarning("No Internet Popup UI not assigned yet.");
            }
        }
    }

    // ── NEW: Backup Logic ──────────────────────────────────

    /// <summary>
    /// Shows the backup button only if the player has the flash drive
    /// and the thesis hasn't been backed up yet.
    /// </summary>
    public void RefreshBackupButton()
    {
        if (backupButton == null) return;

        bool showBackup = GameProgressManager.Instance != null
                          && GameProgressManager.Instance.hasFlashDrive
                          && !GameProgressManager.Instance.thesisBackedUp;

        backupButton.SetActive(showBackup);
    }

    /// <summary>
    /// Called by the Backup button in the thesis window.
    /// Wire this to the Button's OnClick event.
    /// </summary>
    public void BackupThesis()
    {
        if (GameProgressManager.Instance == null) return;

        if (!GameProgressManager.Instance.hasFlashDrive)
        {
            Debug.LogWarning("No flash drive — cannot backup!");
            return;
        }

        // Register backup in game progress
        GameProgressManager.Instance.BackupThesis();

        // Hide backup button (already backed up)
        if (backupButton != null)
            backupButton.SetActive(false);

        // ── NEW: 2FA / Cafe Prompt Narration ───────────────────────
        StartCoroutine(PlayBackupNarration());

        Debug.Log("Thesis backed up to flash drive!");
    }

    // Called by OK/Close button on the no internet popup (wire up later)
    public void CloseNoInternetPopup()
    {
        if (noInternetPopup != null)
            noInternetPopup.SetActive(false);
    }

    // Called by Close button on the backup success popup
    public void CloseBackupSuccessPopup()
    {
        if (backupSuccessPopup != null)
            backupSuccessPopup.SetActive(false);
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

    IEnumerator HidePopupAfterDelay(GameObject popup, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (popup != null)
            popup.SetActive(false);
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
        NarrationManager.Instance.Show("Looks like I'm finally done! Time to relax.", 4f);
        yield return new WaitForSecondsRealtime(4.5f);
        EndChapter();
    }

    void EndChapter()
    {
        var gp = GameProgressManager.Instance;
        if (ThreatLog.Instance != null && gp != null)
        {
            ThreatLog.Instance.Record("backup", gp.thesisBackedUp);
            ThreatLog.Instance.Record("mfa", gp.is2FAEnabled);
        }
        DebriefScreen.Show(chapterTitle);
    }

    IEnumerator PlayCafeNoWifiNarration()
    {
        if (NarrationManager.Instance == null) yield break;
        NarrationManager.Instance.Show("I'm not connected to the internet.", 3f);
        yield return new WaitForSecondsRealtime(3.5f);
        NarrationManager.Instance.Show("I need to get this laptop on the café Wi-Fi first.", 4f);
    }
}
