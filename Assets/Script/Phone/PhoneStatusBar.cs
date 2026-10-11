using UnityEngine;
using TMPro;

public class PhoneStatusBar : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    void Update()
    {
        // Chapter 2 is late at night whatever the real clock says.
        bool night = GameProgressManager.Instance != null && GameProgressManager.Instance.chapter == 2;
        timeText.text = night ? "11:47 PM" : System.DateTime.Now.ToString("h:mm tt");
    }
}