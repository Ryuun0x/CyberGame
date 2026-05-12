using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WiFiManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject wifiScreen;
    public GameObject homeScreen;
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
    public string interiorSceneName = "Interior"; // name of your interior scene

    public bool isCompromised = false;
    private string _connectedNetwork = "";
    private string _pendingNetwork = "";
    private string _savedNetwork = "";
    private string _cafePassword = "cafe1234";

    void Start()
    {
        // set password dot character
        if (passwordInput != null)
        {
            passwordInput.inputType = TMP_InputField.InputType.Password;
            passwordInput.asteriskChar = '\u2022';
            passwordInput.ForceLabelUpdate();
        }

        // auto connect home if we are in interior scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == interiorSceneName)
        {
            AutoConnectHome();
        }
        else
        {
            // hide home wifi when not in interior
            if (homeWifiItem != null)
                homeWifiItem.SetActive(false);
        }
    }

    public void OpenWiFi()
    {
        wifiScreen.SetActive(true);
        homeScreen.SetActive(false);
    }

    public void CloseWiFi()
    {
        wifiScreen.SetActive(false);
        homeScreen.SetActive(true);
    }

    public void ToggleWiFi(bool isOn)
    {
        networkList.SetActive(isOn);

        if (!isOn)
        {
            _savedNetwork = _connectedNetwork;
            DisconnectAll();

            // keep homewifi visible if in interior scene
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == interiorSceneName)
            {
                if (homeWifiItem != null)
                    homeWifiItem.SetActive(true);
            }
        }
        else
        {
            // auto reconnect to saved network
            if (_savedNetwork == "CafeReal")
            {
                DisconnectAll();
                _connectedNetwork = "CafeReal";
                isCompromised = false;
                if (checkCafeReal != null) checkCafeReal.SetActive(true);
                if (wifiStatusIcon != null) wifiStatusIcon.SetActive(true);
                Debug.Log("Auto-reconnected to CafeWifi67 (real)");
            }
            else if (_savedNetwork == "Evil1")
            {
                ConnectEvilTwin1();
            }
            else if (_savedNetwork == "Evil2")
            {
                ConnectEvilTwin2();
            }
            else if (_savedNetwork == "HomeWifi")
            {
                AutoConnectHome();
            }
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
