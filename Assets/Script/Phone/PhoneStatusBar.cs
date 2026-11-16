using UnityEngine;
using TMPro;

public class PhoneStatusBar : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    void Update()
    {
        timeText.text = GameClock.Now; // the story's time, not the computer's
    }
}