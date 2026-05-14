using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles WiFi screen internal logic only.
/// Navigation (open/close) is handled by PhoneHomeScreen.
/// Attach this to WifiScreen.
/// </summary>
public class WiFiManager : MonoBehaviour
{
    [Header("Network List")]
    public GameObject networkList;

    [Header("Network Items")]
    public GameObject homeWifiItem;
    public GameObject cafeReal;
    public GameObject cafeEvil1;
    public GameObject cafeEvil2;

    [Header("Connected Checks")]
    public GameObject checkHome;
    public GameObject checkCafeReal;
    public GameObject checkEvil1;
    public GameObject checkEvil2;

    [Header("Password Popup")]
    public GameObject passwordPopup;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI passwordError;

    [Header("Status Bar")]
    public GameObject wifiStatusIcon;

    [Header("Scene Settings")]
    public string interiorSceneName = "Interior";

    public bool isCompromised = false;
    private string _connectedNetwork = "";
    private string _pendingNetwork = "";
    private string _savedNetwork = "";
    private string _cafePassword = "cafe1234";

    // Track last known cafe zone state to avoid updating every frame unnecessarily
    private bool _lastCafeState = false;
    private bool _isInteriorScene = false;

    void Start()
    {
        if (passwordInput != null)
        {
            passwordInput.inputType = TMP_InputField.InputType.Password;
            passwordInput.asteriskChar = '\u2022';
            passwordInput.ForceLabelUpdate();
        }

        _isInteriorScene = SceneManager.GetActiveScene().name == interiorSceneName;

        if (_isInteriorScene)
        {
            if (homeWifiItem != null) homeWifiItem.SetActive(true);
            if (cafeReal != null) cafeReal.SetActive(false);
            if (cafeEvil1 != null) cafeEvil1.SetActive(false);
            if (cafeEvil2 != null) cafeEvil2.SetActive(false);
            AutoConnectHome();
        }
        else
        {
            if (homeWifiItem != null) homeWifiItem.SetActive(false);
            UpdateCafeNetworks();
        }
    }

    void Update()
    {
        // Only run in main scene, and only when something changed
        if (_isInteriorScene) return;

        bool inCafe = CafeZone.PlayerInCafe;
        if (inCafe != _lastCafeState)
        {
            _lastCafeState = inCafe;
            UpdateCafeNetworks();

            // If player left cafe while connected to cafe network, disconnect
            if (!inCafe && (_connectedNetwork == "CafeReal" ||
                _connectedNetwork == "Evil1" ||
                _connectedNetwork == "Evil2"))
            {
                DisconnectAll();
                _savedNetwork = "";
                Debug.Log("Left cafe - disconnected from cafe network");
            }
        }
    }

    void UpdateCafeNetworks()
    {
        bool inCafe = CafeZone.PlayerInCafe;
        if (cafeReal != null) cafeReal.SetActive(inCafe);
        if (cafeEvil1 != null) cafeEvil1.SetActive(inCafe);
        if (cafeEvil2 != null) cafeEvil2.SetActive(inCafe);
    }

    public void ToggleWiFi(bool isOn)
    {
        networkList.SetActive(isOn);

        if (!isOn)
        {
            _savedNetwork = _connectedNetwork;
            DisconnectAll();

            if (_isInteriorScene)
                if (homeWifiItem != null) homeWifiItem.SetActive(true);
        }
        else
        {
            if (_savedNetwork == "CafeReal")
            {
                _connectedNetwork = "CafeReal";
                isCompromised = false;
                if (checkCafeReal != null) checkCafeReal.SetActive(true);
                if (wifiStatusIcon != null) wifiStatusIcon.SetActive(true);
                Debug.Log("Auto-reconnected to CafeWifi67 (real)");
            }
            else if (_savedNetwork == "Evil1") ConnectEvilTwin1();
            else if (_savedNetwork == "Evil2") ConnectEvilTwin2();
            else if (_savedNetwork == "HomeWifi") AutoConnectHome();

            // Restore correct network visibility
            if (!_isInteriorScene) UpdateCafeNetworks();
        }
    }

    public void AutoConnectHome()
    {
        DisconnectAll();
        _connectedNetwork = "HomeWifi";
        _savedNetwork = "HomeWifi";
        isCompromised = false;
        if (homeWifiItem != null) homeWifiItem.SetActive(true);
        if (checkHome != null) checkHome.SetActive(true);
        if (wifiStatusIcon != null) wifiStatusIcon.SetActive(true);
        Debug.Log("Connected to HomeWifi");
    }

    public void ConnectEvilTwin1()
    {
        DisconnectAll();
        _connectedNetwork = "Evil1";
        _savedNetwork = "Evil1";
        isCompromised = true;
        if (checkEvil1 != null) checkEvil1.SetActive(true);
        if (wifiStatusIcon != null) wifiStatusIcon.SetActive(true);
        Debug.Log("COMPROMISED - Evil Twin 1");
    }

    public void ConnectEvilTwin2()
    {
        DisconnectAll();
        _connectedNetwork = "Evil2";
        _savedNetwork = "Evil2";
        isCompromised = true;
        if (checkEvil2 != null) checkEvil2.SetActive(true);
        if (wifiStatusIcon != null) wifiStatusIcon.SetActive(true);
        Debug.Log("COMPROMISED - Evil Twin 2");
    }

    public void ConnectCafeReal()
    {
        if (_savedNetwork == "CafeReal")
        {
            DisconnectAll();
            _connectedNetwork = "CafeReal";
            isCompromised = false;
            if (checkCafeReal != null) checkCafeReal.SetActive(true);
            if (wifiStatusIcon != null) wifiStatusIcon.SetActive(true);
            Debug.Log("Reconnected to CafeWifi67 - no password needed");
            return;
        }

        _pendingNetwork = "CafeReal";
        ShowPasswordPopup();
    }

    void ShowPasswordPopup()
    {
        if (passwordPopup != null)
        {
            passwordPopup.SetActive(true);
            passwordInput.text = "";
            if (passwordError != null)
                passwordError.text = "";
        }
    }

    public void SubmitPassword()
    {
        if (passwordInput.text == _cafePassword)
        {
            passwordPopup.SetActive(false);
            DisconnectAll();
            _connectedNetwork = "CafeReal";
            _savedNetwork = "CafeReal";
            isCompromised = false;
            if (checkCafeReal != null) checkCafeReal.SetActive(true);
            if (wifiStatusIcon != null) wifiStatusIcon.SetActive(true);
            Debug.Log("Connected to real CafeWifi67 safely!");
        }
        else
        {
            if (passwordError != null)
                passwordError.text = "Incorrect password!";
        }
    }

    public void CancelPassword()
    {
        passwordPopup.SetActive(false);
        _pendingNetwork = "";
    }

    public void DisconnectAll()
    {
        _connectedNetwork = "";
        isCompromised = false;
        if (homeWifiItem != null) homeWifiItem.SetActive(false);
        if (checkHome != null) checkHome.SetActive(false);
        if (checkCafeReal != null) checkCafeReal.SetActive(false);
        if (checkEvil1 != null) checkEvil1.SetActive(false);
        if (checkEvil2 != null) checkEvil2.SetActive(false);
        if (wifiStatusIcon != null) wifiStatusIcon.SetActive(false);
    }

    public void ForgetNetwork()
    {
        _savedNetwork = "";
        DisconnectAll();
        Debug.Log("Network forgotten");
    }
}
