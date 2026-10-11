using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The laptop's own Wi-Fi in the café: network list (opened from the taskbar Wi-Fi icon),
// the real password-protected network, and two open evil twins whose captive portal asks for
// the player's school login. Added at runtime by LaptopDesktop outside the house.
public class LaptopWifi : MonoBehaviour
{
    public const string FreeA = "CafeWifi67_Free", FreeB = "Cafe WiFi 67";
    static readonly string[] Networks = { CafeCounter.WifiName, FreeA, FreeB };

    static readonly Color Flyout = new Color(0.12f, 0.13f, 0.16f, 0.98f);
    static readonly Color Row = new Color(0.19f, 0.2f, 0.24f);
    static readonly Color Dim = new Color(0.65f, 0.68f, 0.74f);
    static readonly Color Blue = new Color(0.2f, 0.47f, 0.93f);
    static readonly Color Grey = new Color(0.55f, 0.57f, 0.62f);
    static readonly Color Ink = new Color(0.12f, 0.13f, 0.16f);
    static readonly Color Error = new Color(0.9f, 0.3f, 0.3f);

    public string Network { get; private set; } = "";
    public bool HasInternet => Network != "";

    RectTransform list, portal;
    bool askPassword;
    string portalNetwork;
    TMP_InputField passwordField, emailField, portalPasswordField;
    TextMeshProUGUI passwordError, portalError;

    void Update()
    {
        // Café Wi-Fi only reaches inside the café.
        if (HasInternet && !CafeZone.PlayerInCafe) Network = "";
    }

    public void ToggleList()
    {
        if (list != null) CloseList();
        else BuildList();
    }

    void CloseList()
    {
        if (list != null) Destroy(list.gameObject);
        list = null;
        askPassword = false;
    }

    void BuildList()
    {
        list = UIBuild.NewRect("WifiList", transform);
        list.anchorMin = list.anchorMax = list.pivot = new Vector2(1, 0);
        list.anchoredPosition = new Vector2(-110, 60);
        list.sizeDelta = new Vector2(560, 0);
        list.gameObject.AddComponent<Image>().color = Flyout;
        UIBuild.Column(list, 22, 10);
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        UIBuild.Label(list, "Wi-Fi", 30, Color.white, FontStyles.Bold);
        if (!CafeZone.PlayerInCafe)
        {
            UIBuild.Label(list, "No networks found", 26, Dim, FontStyles.Normal);
            return;
        }
        foreach (var name in Networks) AddRow(name);
    }

    void AddRow(string name)
    {
        bool locked = name == CafeCounter.WifiName, connected = name == Network;
        var row = UIBuild.NewRect(name, list);
        var image = row.gameObject.AddComponent<Image>();
        image.color = connected ? new Color(0.17f, 0.3f, 0.52f) : Row;
        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => Join(name));
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        var label = UIBuild.Label(row, name, 28, Color.white, FontStyles.Normal);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        UIBuild.Label(row, connected ? "Connected" : locked ? "Secured" : "Open", 22, Dim, FontStyles.Normal)
            .alignment = TextAlignmentOptions.Right;

        if (!locked || connected || !askPassword) return;
        passwordField = UIBuild.Input(list, "Enter network security key", true, 56, 26);
        passwordField.onSubmit.AddListener(_ => SubmitPassword());
        passwordError = UIBuild.Label(list, "", 22, Error, FontStyles.Normal);
        UIBuild.MakeButton(list, "Connect", Blue, SubmitPassword, 560, 56, 26);
        passwordField.ActivateInputField();
    }

    public void Join(string name)
    {
        if (name == Network) return;
        if (name == CafeCounter.WifiName)
        {
            CloseList();
            askPassword = true; // rebuild with the security-key row open
            BuildList();
            return;
        }
        Network = ""; // joining a new network drops the old one
        CloseList();
        OpenPortal(name);
    }

    void SubmitPassword() => TryPassword(passwordField != null ? passwordField.text : "");

    public bool TryPassword(string password)
    {
        if (password != CafeCounter.WifiPassword)
        {
            if (passwordError != null) passwordError.text = "The network security key isn't correct.";
            return false;
        }
        Network = CafeCounter.WifiName;
        CloseList();
        if (ThreatLog.Instance != null) ThreatLog.Instance.Record("evil_twin", true);
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.ConnectToCafeWiFi();
        ConsequenceToast.Show();
        return true;
    }

    void OpenPortal(string name)
    {
        portalNetwork = name;
        portal = UIBuild.NewRect("CaptivePortal", transform);
        portal.anchorMin = portal.anchorMax = new Vector2(0.5f, 0.5f);
        portal.sizeDelta = new Vector2(1000, 0);
        portal.gameObject.AddComponent<Image>().color = Color.white;
        UIBuild.Column(portal, 0, 0);
        portal.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bar = UIBuild.NewRect("AddressBar", portal);
        bar.gameObject.AddComponent<Image>().color = new Color(0.9f, 0.91f, 0.93f);
        UIBuild.Column(bar, 16, 0);
        UIBuild.Label(bar, "http://login.freewifi-portal.net/cafe", 22, Grey, FontStyles.Normal);

        var page = UIBuild.NewRect("Page", portal);
        UIBuild.Column(page, 48, 16);
        UIBuild.Label(page, name, 24, Grey, FontStyles.Bold);
        UIBuild.Label(page, "Sign in with your school email to use free Wi-Fi", 33, Ink, FontStyles.Bold);
        emailField = UIBuild.Input(page, "School email", false, 64, 28);
        portalPasswordField = UIBuild.Input(page, "Password", true, 64, 28);
        foreach (var field in new[] { emailField, portalPasswordField })
            field.GetComponent<Image>().color = new Color(0.94f, 0.95f, 0.97f);
        portalPasswordField.onSubmit.AddListener(_ => SignIn(emailField.text, portalPasswordField.text));
        portalError = UIBuild.Label(page, "", 22, Error, FontStyles.Normal);

        var buttons = UIBuild.NewRect("Buttons", page);
        var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 16;
        row.childAlignment = TextAnchor.MiddleRight;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = false;
        UIBuild.MakeButton(buttons, "Cancel", Grey, CancelPortal, 200, 60, 26);
        UIBuild.MakeButton(buttons, "Sign in", Blue, () => SignIn(emailField.text, portalPasswordField.text), 200, 60, 26);
        emailField.ActivateInputField();
    }

    // The trap: it looks exactly like a normal café login and everything "works".
    public bool SignIn(string email, string password)
    {
        if (portal == null) return false;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            portalError.text = "Enter your school email and password.";
            return false;
        }
        Network = portalNetwork;
        var gp = GameProgressManager.Instance;
        if (gp != null)
        {
            gp.credentialsStolen = true;
            gp.ConnectToCafeWiFi();
        }
        if (ThreatLog.Instance != null) ThreatLog.Instance.Record("evil_twin", false);
        ShowConnected();
        ConsequenceToast.Show();
        return true;
    }

    public void CancelPortal()
    {
        if (portal == null) return;
        ClosePortal();
        if (ThreatLog.Instance != null)
        {
            ThreatLog.Instance.MarkExposed("evil_twin");
            ThreatLog.Instance.Record("evil_twin", true);
        }
        ConsequenceToast.Show();
    }

    void ClosePortal()
    {
        if (portal != null) Destroy(portal.gameObject);
        portal = null;
    }

    void ShowConnected()
    {
        var page = (RectTransform)portal.Find("Page");
        foreach (Transform child in page) Destroy(child.gameObject);
        var done = UIBuild.NewRect("Connected", page);
        var row = done.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 18;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = false;
        var icon = UIBuild.NewRect("Check", done);
        icon.gameObject.AddComponent<Image>().sprite = UIBuild.Icon(64, UIBuild.Check, new Color(0.2f, 0.7f, 0.4f));
        var size = icon.gameObject.AddComponent<LayoutElement>();
        size.preferredWidth = size.preferredHeight = 56;
        UIBuild.Label(done, "Connected! Enjoy free Wi-Fi", 40, Ink, FontStyles.Bold);
        StartCoroutine(CloseAfter(2.5f));
    }

    IEnumerator CloseAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        ClosePortal();
    }
}
