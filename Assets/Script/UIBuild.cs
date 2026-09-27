using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Small helpers for UI built from code (debrief, receipt, consequence toast, laptop Wi-Fi).
public static class UIBuild
{
    public static void Overlay(GameObject go, int sortingOrder)
    {
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return (RectTransform)go.transform;
    }

    public static RectTransform Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static VerticalLayoutGroup Column(RectTransform rect, int padding, float spacing)
    {
        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static TextMeshProUGUI Label(RectTransform parent, string text, float size, Color color, FontStyles style)
    {
        var tmp = NewRect("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Button MakeButton(RectTransform parent, string text, Color color, UnityAction onClick,
        float width = 240, float height = 64, float fontSize = 24)
    {
        var rect = NewRect(text, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        var le = rect.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
        var label = Label(rect, text, fontSize, Color.white, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform);
        return button;
    }

    public static TMP_InputField Input(RectTransform parent, string placeholder, bool password, float height, float fontSize)
    {
        var rect = NewRect(placeholder, parent);
        rect.gameObject.SetActive(false); // configure before TMP_InputField enables
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        var field = rect.gameObject.AddComponent<TMP_InputField>();
        var area = Stretch(NewRect("Text Area", rect));
        area.offsetMin = new Vector2(14, 6);
        area.offsetMax = new Vector2(-14, -6);
        area.gameObject.AddComponent<RectMask2D>();
        var hint = Label(area, placeholder, fontSize, new Color(0.55f, 0.57f, 0.62f), FontStyles.Italic);
        var text = Label(area, "", fontSize, new Color(0.1f, 0.11f, 0.14f), FontStyles.Normal);
        foreach (var t in new[] { hint, text })
        {
            Stretch(t.rectTransform);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.alignment = TextAlignmentOptions.MidlineLeft;
        }
        field.targetGraphic = image;
        field.textViewport = area;
        field.textComponent = text;
        field.placeholder = hint;
        field.pointSize = fontSize;
        if (password)
        {
            field.contentType = TMP_InputField.ContentType.Password;
            field.asteriskChar = '•';
        }
        rect.gameObject.SetActive(true);
        return field;
    }

    // Tiny procedural icons for glyphs the default font lacks (butterfly, check mark).
    public static Sprite Icon(int size, Func<float, float, bool> inside, Color color)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            pixels[y * size + x] = inside(x / (size - 1f) * 2 - 1, y / (size - 1f) * 2 - 1) ? color : Color.clear;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    public static bool Butterfly(float x, float y)
    {
        float ax = Mathf.Abs(x);
        bool upper = Sq((ax - 0.45f) / 0.42f) + Sq((y - 0.25f) / 0.5f) < 1;
        bool lower = Sq((ax - 0.33f) / 0.3f) + Sq((y + 0.4f) / 0.33f) < 1;
        bool body = ax < 0.07f && Mathf.Abs(y) < 0.6f;
        return upper || lower || body;
    }

    public static bool Check(float x, float y) =>
        Segment(x, y, -0.7f, 0f, -0.2f, -0.5f) < 0.16f || Segment(x, y, -0.2f, -0.5f, 0.7f, 0.55f) < 0.16f;

    static float Sq(float v) => v * v;

    static float Segment(float px, float py, float ax, float ay, float bx, float by)
    {
        var p = new Vector2(px, py);
        var a = new Vector2(ax, ay);
        var ab = new Vector2(bx, by) - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }
}
