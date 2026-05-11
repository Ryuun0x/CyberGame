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