using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles WiFi screen internal logic only: the home network in the house.
/// Café Wi-Fi lives on the laptop (LaptopWifi).
/// Navigation (open/close) is handled by PhoneHomeScreen.
/// Attach this to WifiScreen.
/// </summary>
public class WiFiManager : MonoBehaviour
{
    [Header("Network List")]
    public GameObject networkList;

    [Header("Network Items")]
    public GameObject homeWifiItem;

    [Header("Connected Checks")]
    public GameObject checkHome;

    [Header("Status Bar")]
    public GameObject wifiStatusIcon;

    [Header("WiFi Toggle")]
    [Tooltip("Drag the WiFi Toggle (with iOSToggle) here so we can read its initial state")]
    public Toggle wifiToggle;

    [Header("Scene Settings")]
    public string interiorSceneName = "Interior";

    private bool _isInteriorScene = false;
    private bool _wifiEnabled = false; // will be read from toggle in Start()

    void Start()
    {
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
            if (_wifiEnabled)
                AutoConnectHome();
            else
                DisconnectAll(); // WiFi OFF at start — hide everything
        }
        else
        {
            // Outside the house the home network isn't in range
            if (homeWifiItem != null) homeWifiItem.SetActive(false);
        }
    }

    public void ToggleWiFi(bool isOn)
    {
        _wifiEnabled = isOn;
        if (networkList != null) networkList.SetActive(isOn);

        if (!isOn)
        {
            DisconnectAll();
            Debug.Log("[WiFi] Toggle OFF — all hidden");
        }
        else if (_isInteriorScene)
        {
            AutoConnectHome();
        }
    }

    public void AutoConnectHome()
    {
        DisconnectAll();
        if (homeWifiItem != null) homeWifiItem.SetActive(_wifiEnabled);
        if (checkHome != null) checkHome.SetActive(_wifiEnabled);
        if (wifiStatusIcon != null) wifiStatusIcon.SetActive(true);
        Debug.Log("Connected to HomeWifi");
    }

    public void DisconnectAll()
    {
        if (homeWifiItem != null) homeWifiItem.SetActive(false);
        if (checkHome != null) checkHome.SetActive(false);
        if (wifiStatusIcon != null) wifiStatusIcon.SetActive(false);
    }
}
