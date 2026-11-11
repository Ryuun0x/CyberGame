using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;
    private Image _image;

    void Awake()
    {
        Instance = this;
        _image = GetComponent<Image>();
        _image.color = new Color(0, 0, 0, 0);
    }

    public IEnumerator FadeOut()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            _image.color = new Color(0, 0, 0, t);
            yield return null;
        }
    }

    public void SetBlack() => _image.color = Color.black;

    // Fades to any darkness (0 = clear, 1 = black) in real time, so it also works while the phone pauses the game.
    public IEnumerator FadeTo(float alpha, float seconds)
    {
        float from = _image.color.a;
        for (float t = 0; t < 1; t += (PauseMenu.IsPaused ? 0 : Time.unscaledDeltaTime) / seconds)
        {
            _image.color = new Color(0, 0, 0, Mathf.Lerp(from, alpha, t));
            yield return null;
        }
        _image.color = new Color(0, 0, 0, alpha);
    }

    public IEnumerator FadeIn()
    {
        float t = 1;
        while (t > 0)
        {
            t -= Time.deltaTime;
            _image.color = new Color(0, 0, 0, t);
            yield return null;
        }
    }
}