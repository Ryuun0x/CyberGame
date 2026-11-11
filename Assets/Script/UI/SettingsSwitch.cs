using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The ON/OFF switch in Settings. It only follows its Toggle: slides the knob, colors the track
// and writes ON or OFF in the label. The Toggle's On Value Changed still calls SettingsMenu.
[RequireComponent(typeof(Toggle))]
public class SettingsSwitch : MonoBehaviour
{
    static readonly Color OnColor = new Color32(0x7E, 0xD6, 0x70, 0xFF); // the menu's green
    static readonly Color OffColor = new Color(1f, 1f, 1f, 0.25f);

    public RectTransform knob;
    public Image track;
    public TextMeshProUGUI label;

    Toggle toggle;

    void Awake() => toggle = GetComponent<Toggle>();

    void LateUpdate()
    {
        // Slide toward the right (ON) or left (OFF) side of the track.
        float x = Mathf.MoveTowards(knob.anchorMin.x, toggle.isOn ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        knob.anchorMin = knob.anchorMax = knob.pivot = new Vector2(x, 0.5f);
        knob.anchoredPosition = new Vector2(Mathf.Lerp(3f, -3f, x), 0f);
        track.color = toggle.isOn ? OnColor : OffColor;
        label.text = toggle.isOn ? "ON" : "OFF";
    }
}
