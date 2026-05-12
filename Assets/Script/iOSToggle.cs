using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class iOSToggle : MonoBehaviour
{
    public Image background;
    public RectTransform handle;
    public Color onColor = new Color(0.2f, 0.78f, 0.35f);  // green
    public Color offColor = new Color(0.47f, 0.47f, 0.47f); // gray

    private Toggle _toggle;
    private float _onX = -2;
    private float _offX = -16f;

    void Start()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(OnToggleChanged);
        SetState(_toggle.isOn, false); // starts as OFF
    }

    void OnToggleChanged(bool isOn)
    {
        SetState(isOn, true);
    }

    void SetState(bool isOn, bool animate)
    {
        float targetX = isOn ? _onX : _offX;
        Color targetColor = isOn ? onColor : offColor;

        if (animate)
        {
            StartCoroutine(AnimateToggle(targetX, targetColor));
        }
        else
        {
            handle.anchoredPosition = new Vector2(targetX, 0);
            background.color = targetColor;
        }
    }

    IEnumerator AnimateToggle(float targetX, Color targetColor)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        float startX = handle.anchoredPosition.x;
        Color startColor = background.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            handle.anchoredPosition = new Vector2(
                Mathf.Lerp(startX, targetX, t), 0);
            background.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        handle.anchoredPosition = new Vector2(targetX, 0);
        background.color = targetColor;
    }
}