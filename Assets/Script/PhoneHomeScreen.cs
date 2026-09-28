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
    [HideInInspector] public CityMapController mapController;
    GameObject messagesScreen; // built on the first message (see ShowMessage)
    // Add more screens here as you build them:
    // public GameObject messagesScreen;
    // public GameObject contactsScreen;
    // public GameObject mapScreen;
    // public GameObject browserScreen;
    // public GameObject mailScreen;

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

    // Add more as you build them:
    // public void OpenMessages() { HideAll(); messagesScreen.SetActive(true); }
    // public void OpenContacts() { HideAll(); contactsScreen.SetActive(true); }
    // public void OpenMap()      { HideAll(); mapScreen.SetActive(true); }
    // public void OpenBrowser()  { HideAll(); browserScreen.SetActive(true); }
    // public void OpenMail()     { HideAll(); mailScreen.SetActive(true); }

    // Messages: one view-only conversation, built from the Wi-Fi screen's parts so it matches the phone.
    public void ShowMessage(string sender, string text)
    {
        if (messagesScreen == null) BuildMessages();
        messagesScreen.transform.Find("TopBar").GetComponentInChildren<TMP_Text>().text = sender;
        messagesScreen.transform.Find("Bubble").GetComponentInChildren<TMP_Text>().text = text;
        OpenMessages();
    }

    public void OpenMessages()
    {
        if (messagesScreen == null) return;
        HideAll();
        messagesScreen.SetActive(true);
    }

    void BuildMessages()
    {
        var screen = UIBuild.Stretch(UIBuild.NewRect("MessagesScreen", wifiScreen.transform.parent));
        screen.gameObject.AddComponent<Image>().color = wifiScreen.GetComponent<Image>().color;
        messagesScreen = screen.gameObject;

        var topBar = Instantiate(wifiScreen.transform.Find("TopBar"), screen, false);
        topBar.name = "TopBar";
        var back = topBar.GetComponentInChildren<Button>();
        back.onClick = new Button.ButtonClickedEvent(); // drop the Wi-Fi screen's wiring
        back.onClick.AddListener(GoHome);

        var rowImage = wifiScreen.transform.Find("ToggleRow").GetComponent<Image>(); // the phone's rounded sprite
        var bubble = UIBuild.NewRect("Bubble", screen);
        bubble.anchorMin = bubble.anchorMax = bubble.pivot = new Vector2(0, 1);
        bubble.anchoredPosition = new Vector2(12, -64);
        bubble.sizeDelta = new Vector2(180, 0);
        var image = bubble.gameObject.AddComponent<Image>();
        image.sprite = rowImage.sprite;
        image.type = rowImage.type;
        image.pixelsPerUnitMultiplier = rowImage.pixelsPerUnitMultiplier;
        image.color = new Color(0.23f, 0.23f, 0.25f);
        UIBuild.Column(bubble, 10, 0);
        bubble.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UIBuild.Label(bubble, "", 14, Color.white, FontStyles.Normal);

        var when = UIBuild.Label(screen, "Now", 10, new Color(0.6f, 0.6f, 0.63f), FontStyles.Normal);
        when.alignment = TextAlignmentOptions.Center;
        when.rectTransform.anchorMin = when.rectTransform.anchorMax = new Vector2(0.5f, 1);
        when.rectTransform.sizeDelta = new Vector2(200, 16);
        when.rectTransform.anchoredPosition = new Vector2(0, -50);

        // From now on the Messages icon opens the conversation.
        var app = homeScreen.transform.Find("MessageApp");
        if (app != null) app.GetComponent<Button>().onClick.AddListener(OpenMessages);
    }

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
        if (mapController != null) mapController.Close();
        if (homeScreen != null) homeScreen.SetActive(false);
        if (wifiScreen != null) wifiScreen.SetActive(false);
        if (authenticatorScreen != null) authenticatorScreen.SetActive(false);
        if (messagesScreen != null) messagesScreen.SetActive(false);
        // Hide future screens here too
    }
}
