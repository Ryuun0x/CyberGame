using UnityEngine;
using TMPro;

public class PhoneStatusBar : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    void Update()
    {
        timeText.text = System.DateTime.Now.ToString("h:mm tt");
    }
}