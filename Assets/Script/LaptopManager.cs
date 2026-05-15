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

    [Header("Cafe Outcomes")]
    [Tooltip("Assign your Canvas/Popup here that will show if the player gets hacked by the evil twin.")]
    public GameObject compromisedPopup;
    public GameObject successPopup;

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
    }

    void Update()
    {
        // Update taskbar clock
        if (timeText != null)
            timeText.text = System.DateTime.Now.ToString("h:mm tt");

        // Sync laptop Wi-Fi icon with Phone's Wi-Fi connection (Cafe scene only)
        bool isInterior = SceneManager.GetActiveScene().name == "Interior" || SceneManager.GetActiveScene().name == "MainScene";
        if (!isInterior)
        {
            WiFiManager wifiManager = FindObjectOfType<WiFiManager>(true);
            if (wifiManager != null)
            {
                string network = wifiManager.GetConnectedNetwork();
                bool hasConnection = (network == "CafeReal" || network == "Evil1" || network == "Evil2");
                
                // Hide or show the 'No Internet' X over the wifi icon
                if (noInternetIcon != null)
                {
                    noInternetIcon.SetActive(!hasConnection); 
                }

                // Constantly update the tooltip if it's open
                if (wifiTooltip != null && wifiTooltip.activeSelf && tooltipStatus != null)
                {
                    if (hasConnection)
                    {
                        tooltipStatus.text = "Connected: " + network;
                        tooltipStatus.color = new Color(0.67f, 0.67f, 0.67f); // Normal grey
                    }
                    else
                    {
                        tooltipStatus.text = "No Internet access";
                        tooltipStatus.color = new Color(1f, 0.4f, 0.4f); // Red error color
                    }
                }
            }
        }
    }

    // Called when WifiIcon is clicked
    public void ToggleWifiTooltip()
    {

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
        bool isInterior = SceneManager.GetActiveScene().name == "Interior" || SceneManager.GetActiveScene().name == "MainScene"; // Adjust if your interior scene is named differently

        if (!isInterior)
        {
            // ── CAFE LOGIC: Check the phone's WiFi connection ──
            // We pass 'true' to find the WiFiManager even if the phone screen is currently closed/hidden
            WiFiManager wifiManager = FindObjectOfType<WiFiManager>(true);
            if (wifiManager != null)
            {
                string network = wifiManager.GetConnectedNetwork();
                
                if (network == "CafeReal")
                {
                    Debug.Log("Thesis submitted safely via CafeWifi67!");
                    StartCoroutine(PlayCafeSuccessNarration());
                    CloseThesis();
                }
                else if (network == "Evil1" || network == "Evil2")
                {
                    Debug.Log("Submitted via Evil Twin! Data stolen!");
                    StartCoroutine(PlayCafeHackedNarration());
                    
                    if (compromisedPopup != null) compromisedPopup.SetActive(true);

                    CloseThesis(); // Hide the thesis so the player sees the popup
                }
                else
                {
                    Debug.Log("Not connected to WiFi in cafe.");
                    StartCoroutine(PlayCafeNoWifiNarration());
                }
            }
            else
            {
                Debug.LogWarning("WiFiManager not found! The Phone must be in the scene.");
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

    // Called by a Close/OK button on the Compromised Popup
    public void CloseCompromisedPopup()
    {
        if (compromisedPopup != null)
            compromisedPopup.SetActive(false);
    }

    IEnumerator LoseInternetAfterDelay()
    {
        bool isInterior = SceneManager.GetActiveScene().name == "Interior" || SceneManager.GetActiveScene().name == "MainScene";
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
            NarrationManager.Instance.Show("I still need to turn on 2FA on my phone before heading out.", 4f);
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
        // TODO: Trigger Game Over / Win Screen here
    }

    IEnumerator PlayCafeHackedNarration()
    {
        if (NarrationManager.Instance == null) yield break;
        NarrationManager.Instance.Show("Uploading...", 2f);
        yield return new WaitForSecondsRealtime(2.5f);
        NarrationManager.Instance.Show("Wait... why is the connection unencrypted?", 3.5f);
        yield return new WaitForSecondsRealtime(4f);
        NarrationManager.Instance.Show("Oh no. Someone is intercepting my data!", 3.5f);
        // TODO: Trigger Game Over / Lose Screen here
    }

    IEnumerator PlayCafeNoWifiNarration()
    {
        if (NarrationManager.Instance == null) yield break;
        NarrationManager.Instance.Show("I'm not connected to the internet.", 3f);
        yield return new WaitForSecondsRealtime(3.5f);
        NarrationManager.Instance.Show("I should use my phone to connect to the Cafe Wi-Fi first.", 4f);
    }
}
