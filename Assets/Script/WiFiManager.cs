using UnityEngine;
using UnityEngine.UI;
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

    [Header("WiFi Toggle")]
    [Tooltip("Drag the WiFi Toggle (with iOSToggle) here so we can read its initial state")]
    public Toggle wifiToggle;

    [Header("Scene Settings")]
    public string interiorSceneName = "Interior";

    public bool isCompromised = false;
    private string _connectedNetwork = "";
    private string _savedNetwork = "";
    private string _cafePassword = "cafe1234";

    // Track last known cafe zone state to avoid updating every frame unnecessarily
    private bool _lastCafeState = false;
    private bool _isInteriorScene = false;
    private bool _wifiEnabled = false; // will be read from toggle in Start()

    void Start()
    {
        if (passwordInput != null)
        {
            passwordInput.inputType = TMP_InputField.InputType.Password;
            passwordInput.asteriskChar = '\u2022';
            passwordInput.ForceLabelUpdate();
        }

        // Read the toggle's actual state and subscribe to changes
        if (wifiToggle != null)
        {
            _wifiEnabled = wifiToggle.isOn;
            wifiToggle.onValueChanged.AddListener(ToggleWiFi);
        }

        _isInteriorScene = SceneManager.GetActiveScene().name == interiorSceneName;

        // Sync the network list visibility with the actual toggle state
        if (networkList != null)
            networkList.SetActive(_wifiEnabled);

        if (_isInteriorScene)
        {
            if (cafeReal != null) cafeReal.SetActive(false);
            if (cafeEvil1 != null) cafeEvil1.SetActive(false);
            if (cafeEvil2 != null) cafeEvil2.SetActive(false);

            if (_wifiEnabled)
            {
                AutoConnectHome();
            }
            else
            {
                // WiFi OFF at start — hide everything
                if (homeWifiItem != null) homeWifiItem.SetActive(false);
                if (checkHome != null) checkHome.SetActive(false);
                if (wifiStatusIcon != null) wifiStatusIcon.SetActive(false);
            }
        }
        else
        {
            if (homeWifiItem != null) homeWifiItem.SetActive(false);
            if (_wifiEnabled) UpdateCafeNetworks();
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
        bool show = CafeZone.PlayerInCafe && _wifiEnabled;
        if (cafeReal != null) cafeReal.SetActive(show);
        if (cafeEvil1 != null) cafeEvil1.SetActive(show);
        if (cafeEvil2 != null) cafeEvil2.SetActive(show);
    }

    public void ToggleWiFi(bool isOn)
    {
        _wifiEnabled = isOn;
        networkList.SetActive(isOn);

        if (!isOn)
        {
            _savedNetwork = _connectedNetwork;

            // Hide the list container
            if (networkList != null) networkList.SetActive(false);

            // Explicitly hide ALL network items (in case they're outside networkList)
            if (homeWifiItem != null) homeWifiItem.SetActive(false);
            if (cafeReal != null) cafeReal.SetActive(false);
            if (cafeEvil1 != null) cafeEvil1.SetActive(false);
            if (cafeEvil2 != null) cafeEvil2.SetActive(false);

            // Hide all checks and status
            if (checkHome != null) checkHome.SetActive(false);
            if (checkCafeReal != null) checkCafeReal.SetActive(false);
            if (checkEvil1 != null) checkEvil1.SetActive(false);
            if (checkEvil2 != null) checkEvil2.SetActive(false);
            if (wifiStatusIcon != null) wifiStatusIcon.SetActive(false);

            _connectedNetwork = "";
            isCompromised = false;

            Debug.Log("[WiFi] Toggle OFF — all hidden");
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
            else if (_isInteriorScene) AutoConnectHome(); // first toggle-on in interior

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
        if (homeWifiItem != null) homeWifiItem.SetActive(_wifiEnabled);
        if (checkHome != null) checkHome.SetActive(_wifiEnabled);
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

            // Complete the Objective Sub-Task for finding the correct Wi-Fi by routing it through GameProgressManager
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.ConnectToCafeWiFi();
            }
            else if (ObjectiveManager.Instance != null) 
            {
                // Fallback just in case GameProgressManager isn't there
                ObjectiveManager.Instance.CompleteSubTask(4, 0);
            }
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

    // Allows the Laptop to check which WiFi the phone is connected to
    public string GetConnectedNetwork()
    {
        return _connectedNetwork;
    }
}
