using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Authenticator screen on the phone.
/// Two states: Insecure (2FA off) and Secure (2FA on).
/// Attach this to AuthenticatorScreen.
/// </summary>
public class Phone2FA : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The AuthenticatorScreen panel itself")]
    public GameObject authenticatorScreen;
    [Tooltip("The phone home screen to show when going back")]
    public GameObject homeScreen;

    [Header("Logos")]
    [Tooltip("Red shield with X — shown when INSECURE")]
    public GameObject vulnerableLogo;
    [Tooltip("Green shield with checkmark — shown when SECURE")]
    public GameObject protectedLogo;

    [Header("Text Elements")]
    [Tooltip("The '2FA Status: Insecure/Secure' label")]
    public TextMeshProUGUI statusLabel;
    [Tooltip("The description text below the status")]
    public TextMeshProUGUI descriptionText;

    [Header("Enable Button")]
    [Tooltip("The ENABLE button — hidden after 2FA is activated")]
    public GameObject enableButton;

    // Hardcoded state text
    private string insecureStatus = "2FA Status: Insecure";
    private string insecureDescription = "Protect your account from unauthorized access by enabling 2FA";
    private string secureStatus = "2FA Status: Secure";
    private string secureDescription = "Your account is protected with two-factor authentication";

    void OnEnable()
    {
        // Refresh UI every time the screen is opened
        RefreshUI();
    }

    /// <summary>
    /// Called from the Authenticator app icon on the home screen.
    /// Wire to the app icon Button → OnClick.
    /// </summary>
    public void OpenAuthenticator()
    {
        if (authenticatorScreen != null)
            authenticatorScreen.SetActive(true);
        if (homeScreen != null)
            homeScreen.SetActive(false);

        RefreshUI();
    }

    /// <summary>
    /// Called from the Back button in the TopBar.
    /// Wire to BackButton → OnClick.
    /// </summary>
    public void CloseAuthenticator()
    {
        if (authenticatorScreen != null)
            authenticatorScreen.SetActive(false);
        if (homeScreen != null)
            homeScreen.SetActive(true);
    }

    /// <summary>
    /// Called from the ENABLE button.
    /// Wire to 2faEnableButton → Button → OnClick.
    /// </summary>
    public void OnEnableClicked()
    {
        if (GameProgressManager.Instance == null) return;
        if (GameProgressManager.Instance.is2FAEnabled) return;

        GameProgressManager.Instance.Enable2FA();
        RefreshUI();

        StartCoroutine(Play2FANarration());
    }

    /// <summary>
    /// Switches all UI elements between Insecure and Secure states.
    /// </summary>
    private void RefreshUI()
    {
        bool isSecure = GameProgressManager.Instance != null
                        && GameProgressManager.Instance.is2FAEnabled;

        // ── Logos: show one, hide the other ──
        if (vulnerableLogo != null)
            vulnerableLogo.SetActive(!isSecure);
        if (protectedLogo != null)
            protectedLogo.SetActive(isSecure);

        // ── Status label ──
        if (statusLabel != null)
            statusLabel.text = isSecure ? secureStatus : insecureStatus;

        // ── Description text ──
        if (descriptionText != null)
            descriptionText.text = isSecure ? secureDescription : insecureDescription;

        // ── Enable button: hide when already secure ──
        if (enableButton != null)
            enableButton.SetActive(!isSecure);


    }

    System.Collections.IEnumerator Play2FANarration()
    {
        if (NarrationManager.Instance == null) yield break;

        if (GameProgressManager.Instance.thesisBackedUp)
        {
            NarrationManager.Instance.Show("2FA is on.", 2.5f);
            yield return new WaitForSecondsRealtime(3f);
            NarrationManager.Instance.Show("Okay, everything's secure.", 2.5f);
            yield return new WaitForSecondsRealtime(3f);
            NarrationManager.Instance.Show("Now I just need to head out to the cafe to find some wifi and send this off.", 4.5f);
        }
        else
        {
            NarrationManager.Instance.Show("2FA is on.", 2.5f);
            yield return new WaitForSecondsRealtime(3f);
            NarrationManager.Instance.Show("I still need to back up my thesis on the laptop before I go anywhere.", 4f);
        }
    }
}
