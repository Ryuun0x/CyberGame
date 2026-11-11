using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The café laptop's own Wi-Fi (scenario: evil twin). On the café LaptopCanvas:
//   - WifiList: the network list, opened from the taskbar Wi-Fi icon. Two networks have the café's EXACT name:
//     the evil twin (Open, listed first: a fake access point near you has the strongest signal) and the real one
//     (Secured, password on the café receipt). A neighbour's secured network is there too.
//     Each row's OnClick calls Join("twin" / "real" / "neighbor"): names repeat, so rows are told apart by id.
//   - CaptivePortal: the twin's fake "sign in" page (reviewer #11). The café page asks you to "Sign in with
//     Goggle", which opens a lookalike of the Goggle account sign-in (Gmail-style: email → Next → password → Next)
//     on a fake address. Signing in "works", but the attacker now has the login
//     (GameProgressManager.credentialsStolen, schoolEmail); Chapter 2 pays it off.
// Both are placed objects, hidden until opened.
public class LaptopWifi : MonoBehaviour
{
    public const string Twin = "twin", Real = "real", Neighbor = "neighbor";
    public const string NeighborName = "PLDT_HOME_7A2C";
    // The rows in order, top to bottom.
    static readonly string[] RowIds = { Twin, Real, Neighbor };
    static readonly bool[] RowSecured = { false, true, true };

    static readonly Color RowColor = new Color(0.19f, 0.2f, 0.24f);
    static readonly Color ConnectedColor = new Color(0.17f, 0.3f, 0.52f);

    [Header("Network list (opened from the taskbar Wi-Fi icon)")]
    public GameObject list;
    public GameObject noNetworksText;
    public GameObject networkRows;
    [Tooltip("The three rows in order: twin (open), real (secured), neighbour (secured).")]
    public Image[] rowImages;
    public TextMeshProUGUI[] rowStatus;
    public GameObject passwordRow;
    public TMP_InputField passwordField;
    public TextMeshProUGUI passwordError;

    [Header("Fake sign-in page (the lookalikes' captive portal)")]
    public GameObject portal;
    [Tooltip("The portal's address bar text: the café page, then the fake sign-in address.")]
    public TextMeshProUGUI portalUrl;
    public TextMeshProUGUI portalNetworkName;
    [Tooltip("The café's welcome page with the \"Sign in with Goggle\" button.")]
    public GameObject portalStart;
    [Tooltip("The lookalike Goggle sign-in card (both steps inside).")]
    public GameObject signInCard;
    public GameObject emailStep;
    public GameObject passwordStep;
    [Tooltip("Password step: the email typed in step 1.")]
    public TextMeshProUGUI emailChip;
    public TMP_InputField emailField;
    public TMP_InputField portalPasswordField;
    public TextMeshProUGUI portalError;
    public GameObject portalConnected;

    const string PortalAddress = "http://login.freewifi-portal.net/cafe";
    // Looks like Goggle's sign-in at a glance; the real one is https://accounts.goggle.com.
    const string FakeSignInAddress = "http://goggle-login.freewifi-portal.net/signin";

    // The joined network's id ("twin" or "real"); "" = not connected.
    public string Network { get; private set; } = "";
    public bool HasInternet => Network != "" && CafeZone.PlayerInCafe; // café Wi-Fi only reaches inside the café

    string waitingForKey; // the secured network whose password row is open

    void Start()
    {
        list.SetActive(false);
        portal.SetActive(false);
    }

    void Update()
    {
        // Out of range: forget the network (coming back in doesn't reconnect by itself).
        if (Network != "" && !CafeZone.PlayerInCafe) Network = "";
    }

    // Called by the taskbar Wi-Fi icon (through LaptopDesktop).
    public void ToggleList()
    {
        if (list.activeSelf) list.SetActive(false);
        else OpenList(false);
    }

    void OpenList(bool askPassword)
    {
        list.SetActive(true);
        bool inCafe = CafeZone.PlayerInCafe;
        noNetworksText.SetActive(!inCafe);
        networkRows.SetActive(inCafe);
        for (int i = 0; i < RowIds.Length; i++)
        {
            bool connected = RowIds[i] == Network;
            rowImages[i].color = connected ? ConnectedColor : RowColor;
            rowStatus[i].text = connected ? "Connected" : RowSecured[i] ? "Secured" : "Open";
        }
        passwordRow.SetActive(inCafe && askPassword && waitingForKey != Network);
        if (passwordRow.activeSelf)
        {
            passwordField.text = "";
            passwordError.text = "";
            passwordField.ActivateInputField();
        }
    }

    // Called by each network row's OnClick with the row's id.
    public void Join(string id)
    {
        if (id == Network) return;
        if (id != Twin)
        {
            waitingForKey = id;
            OpenList(true); // a secured network: show the security-key row
            return;
        }
        Network = ""; // joining a new network drops the old one
        list.SetActive(false);
        OpenPortal(CafeCounter.WifiName); // the twin looks exactly like the café's network
    }

    // Called by the Connect button, and by Enter in the password field.
    public void SubmitPassword() => TryPassword(passwordField.text);
    public void SubmitPasswordOnEnter(string _) => SubmitPassword();

    public bool TryPassword(string password)
    {
        // Only the café's real network takes the receipt password (the neighbour's is unknown).
        if (waitingForKey == Neighbor || password != CafeCounter.WifiPassword)
        {
            passwordError.text = "The network security key isn't correct.";
            return false;
        }
        Network = Real;
        list.SetActive(false);
        if (ThreatLog.Instance != null) ThreatLog.Instance.Record("evil_twin", true);
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.ConnectToCafeWiFi();
        ConsequenceToast.Show();
        return true;
    }

    void OpenPortal(string name)
    {
        portalNetworkName.text = name;
        portalUrl.text = PortalAddress;
        emailField.text = "";
        portalPasswordField.text = "";
        portalError.text = "";
        ShowPortalPage(portalStart);
        portal.SetActive(true);
    }

    void ShowPortalPage(GameObject page)
    {
        portalStart.SetActive(page == portalStart);
        signInCard.SetActive(page == emailStep || page == passwordStep);
        emailStep.SetActive(page == emailStep);
        passwordStep.SetActive(page == passwordStep);
        portalConnected.SetActive(page == portalConnected);
    }

    // Called by the café page's "Sign in with Goggle" button: the lookalike sign-in, on a fake address.
    public void SignInWithGoggle()
    {
        portalUrl.text = FakeSignInAddress;
        portalError.text = "";
        ShowPortalPage(emailStep);
        emailField.ActivateInputField();
    }

    // Called by the email step's Next button, and by Enter in the email field.
    public void EmailNext()
    {
        if (string.IsNullOrWhiteSpace(emailField.text))
        {
            portalError.text = "Enter an email";
            return;
        }
        portalError.text = "";
        emailChip.text = emailField.text.Trim();
        ShowPortalPage(passwordStep);
        portalPasswordField.ActivateInputField();
    }
    public void EmailNextOnEnter(string _) => EmailNext();

    // Called by the password step's Next button, and by Enter in the password field.
    public void SignInClicked() => SignIn(emailField.text, portalPasswordField.text);
    public void SignInOnEnter(string _) => SignInClicked();

    // The trap: it looks exactly like Goggle's own sign-in and everything "works".
    public bool SignIn(string email, string password)
    {
        if (!portal.activeSelf) return false;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            portalError.text = string.IsNullOrWhiteSpace(email) ? "Enter an email" : "Enter a password";
            return false;
        }
        Network = Twin;
        var gp = GameProgressManager.Instance;
        if (gp != null)
        {
            gp.credentialsStolen = true;
            gp.schoolEmail = email.Trim();
            gp.ConnectToCafeWiFi();
        }
        if (ThreatLog.Instance != null) ThreatLog.Instance.Record("evil_twin", false);
        ShowPortalPage(portalConnected);
        StartCoroutine(ClosePortalAfter(2.5f));
        ConsequenceToast.Show();
        return true;
    }

    // Called by the Cancel button.
    public void CancelPortal()
    {
        if (!portal.activeSelf) return;
        portal.SetActive(false);
        if (ThreatLog.Instance != null)
        {
            ThreatLog.Instance.MarkExposed("evil_twin");
            ThreatLog.Instance.Record("evil_twin", true);
        }
        ConsequenceToast.Show();
    }

    // Disconnects and closes everything (used by the editor checks to start each test fresh).
    public void Disconnect()
    {
        Network = "";
        list.SetActive(false);
        portal.SetActive(false);
    }

    IEnumerator ClosePortalAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        portal.SetActive(false);
    }
}
