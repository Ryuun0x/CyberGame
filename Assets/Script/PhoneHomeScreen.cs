using UnityEngine;

/// <summary>
/// Manages phone navigation — opening app screens and returning to home.
/// Attach this to the HomeScreen object.
/// Each app icon button calls the matching Open method.
/// Each app's back button calls GoHome().
/// </summary>
public class PhoneHomeScreen : MonoBehaviour
{
    [Header("Screens")]
    public GameObject homeScreen;
    public GameObject wifiScreen;
    public GameObject authenticatorScreen;
    // Add more screens here as you build them:
    // public GameObject messagesScreen;
    // public GameObject contactsScreen;
    // public GameObject mapScreen;
    // public GameObject browserScreen;
    // public GameObject mailScreen;

    void Start()
    {
        // Make sure only home screen is visible on start
        GoHome();
    }

    // ── Open App Methods (wire to app icon buttons) ────────

    public void OpenWifi()
    {
        HideAll();
        if (wifiScreen != null)
            wifiScreen.SetActive(true);
    }

    public void OpenAuthenticator()
    {
        HideAll();
        if (authenticatorScreen != null)
            authenticatorScreen.SetActive(true);
    }

    // Add more as you build them:
    // public void OpenMessages() { HideAll(); messagesScreen.SetActive(true); }
    // public void OpenContacts() { HideAll(); contactsScreen.SetActive(true); }
    // public void OpenMap()      { HideAll(); mapScreen.SetActive(true); }
    // public void OpenBrowser()  { HideAll(); browserScreen.SetActive(true); }
    // public void OpenMail()     { HideAll(); mailScreen.SetActive(true); }

    // ── Back to Home (wire to all back buttons) ────────────

    public void GoHome()
    {
        HideAll();
        if (homeScreen != null)
            homeScreen.SetActive(true);
    }

    // ── Helper ─────────────────────────────────────────────

    private void HideAll()
    {
        if (homeScreen != null) homeScreen.SetActive(false);
        if (wifiScreen != null) wifiScreen.SetActive(false);
        if (authenticatorScreen != null) authenticatorScreen.SetActive(false);
        // Hide future screens here too
    }
}
