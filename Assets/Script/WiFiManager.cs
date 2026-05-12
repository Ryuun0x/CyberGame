using UnityEngine;
using TMPro;

public class WiFiManager : MonoBehaviour
{
    public GameObject wifiScreen;
    public GameObject homeScreen;
    public GameObject networkList;
    public GameObject connectedCheck;

    private string _connectedNetwork = "";

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
        if (!isOn) Disconnect();
    }

    public void ConnectToCafeWifi()
    {
        _connectedNetwork = "cafeWifi67";
        if (connectedCheck != null) connectedCheck.SetActive(true);
        Debug.Log("Connected to: cafeWifi67");
    }

    public void Disconnect()
    {
        _connectedNetwork = "";
        if (connectedCheck != null) connectedCheck.SetActive(false);
    }
}