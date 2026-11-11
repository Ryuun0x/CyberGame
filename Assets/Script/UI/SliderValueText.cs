using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The number next to a Settings slider. It only shows the slider's value:
// volumes as 0-100, mouse sensitivity as a multiplier (1.0 = normal).
public class SliderValueText : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI text;
    [Tooltip("On: show 0-100 (volumes). Off: show one decimal (sensitivity).")]
    public bool percent = true;

    void LateUpdate()
    {
        text.text = percent ? Mathf.RoundToInt(slider.value * 100f).ToString() : slider.value.ToString("0.0");
    }
}
