using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public GameObject messagesScreen;

    [Header("Messages screen parts")]
    public TextMeshProUGUI chatTitle;
    public Transform chatThread;
    [Tooltip("A hidden message row; AddChat copies it for every message.")]
    public GameObject bubbleTemplate;

    [HideInInspector] public CityMapController mapController;

    void Awake()
    {
        // Make sure only home screen is visible on start
        GoHome();
    }

    // ── Open App Methods (wire to app icon buttons) ────────

    public void OpenMap()
    {
        if (mapController == null) return;
        mapController.Open();
    }

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

    // ── Messages: one chat thread. Each message copies the hidden bubble template. ──

    public void ShowMessage(string sender, string text)
    {
        OpenChat(sender);
        AddChat(text, false);
    }

    // Opens the Messages screen with an empty thread for this contact.
    public void OpenChat(string contact)
    {
        chatTitle.text = contact;
        foreach (Transform bubble in chatThread)
            if (bubble != bubbleTemplate.transform) Destroy(bubble.gameObject);
        OpenMessages();
    }

    // mine = true: my message (blue, on the right). false: their message (grey, on the left).
    public void AddChat(string text, bool mine)
    {
        var row = Instantiate(bubbleTemplate, chatThread);
        row.SetActive(true);
        row.GetComponent<HorizontalLayoutGroup>().childAlignment = mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        row.GetComponentInChildren<Image>().color = mine ? new Color(0.2f, 0.47f, 0.93f) : new Color(0.23f, 0.23f, 0.25f);
        row.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }

    public void OpenMessages()
    {
        HideAll();
        messagesScreen.SetActive(true);
    }

    // ── Back to Home (wire to all back buttons) ────────────

    public void GoHome()
    {
        HideAll();
        if (homeScreen != null)
            homeScreen.SetActive(true);
    }

    // ── Helper ─────────────────────────────────────────────

    public void HideAll()
    {
        if (mapController != null) mapController.Close();
        if (homeScreen != null) homeScreen.SetActive(false);
        if (wifiScreen != null) wifiScreen.SetActive(false);
        if (authenticatorScreen != null) authenticatorScreen.SetActive(false);
        if (messagesScreen != null) messagesScreen.SetActive(false);
    }
}
