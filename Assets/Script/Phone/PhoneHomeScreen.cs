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
    [Tooltip("The Mail app: one email from the school asking students to turn on 2FA (a hint; never required).")]
    public GameObject mailScreen;
    [Tooltip("The News app: a short warning about the attacks (no advice).")]
    public GameObject newsScreen;
    [Tooltip("The red \"1\" on the Mail and News icons while there is something unread (GameProgressManager.mailUnread / newsUnread).")]
    public GameObject mailBadge, newsBadge;

    [Header("Messages screen parts")]
    public TextMeshProUGUI chatTitle;
    public Transform chatThread;
    [Tooltip("A hidden message row; AddChat copies it for every message.")]
    public GameObject bubbleTemplate;

    [HideInInspector] public CityMapController mapController;

    static GameProgressManager Progress => GameProgressManager.Instance;

    // The phone stops game time, and the home screen is only on while the phone is open: cheap enough per frame.
    void Update()
    {
        mailBadge.SetActive(Progress != null && Progress.mailUnread);
        newsBadge.SetActive(Progress != null && Progress.newsUnread);
    }

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

    public void OpenMail()
    {
        HideAll();
        mailScreen.SetActive(true);
        if (Progress == null || !Progress.mailUnread) return;
        Progress.mailUnread = false;
        // Reading the school's email brings the next thing to read: the news (a warning only).
        Progress.newsUnread = true;
        PhoneNotifications.Show("NEWS 24", "Cyber attacks hit students across the city");
    }

    public void OpenNews()
    {
        HideAll();
        newsScreen.SetActive(true);
        if (Progress != null) Progress.newsUnread = false;
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
        if (mailScreen != null) mailScreen.SetActive(false);
        if (newsScreen != null) newsScreen.SetActive(false);
    }
}
