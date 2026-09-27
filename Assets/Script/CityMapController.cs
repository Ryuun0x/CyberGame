using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CityMapController : MonoBehaviour
{
    public float minimapRange = 18f;
    public float minimumRange = 8f;
    public float maximumRange = 240f;
    public bool IsExpanded { get; private set; }
    public Vector2 MapPosition => IsIndoor ? layout.home : new Vector2(player.position.x, player.position.z);
    // No map inside the house: only the city scene has map artwork.
    public bool IsIndoor => layout == null || gameObject.scene.name != layout.cityScene;
    public Vector2 ViewCenter => fullImage.Center;
    public float VisibleRange => fullImage.Range;
    public CityMapLayout Layout => layout;
    public Vector2 ProjectPlayer() => fullImage.Project(MapPosition);

    PhoneManager phone;
    PhoneHomeScreen home;
    Transform player;
    RectTransform frame, expanded, viewport, miniRoot, miniArrow, fullArrow, hudRoot;
    CityMapSurface miniImage, fullImage;
    CityMapLayout layout;
    TMP_Text[] placeLabels;
    RectTransform[] placePins;
    RectTransform miniMarker, fullMarker; // objective marker on the café
    Sprite circleSprite, pinSprite;
    CanvasGroup mapFade;
    Coroutine opening;
    public bool reduceMotion;
    public bool IsTransitioning => opening != null;
    Texture2D arrowTexture;
    Sprite arrowSprite;
    Button mapButton;
    Canvas hud;
    Vector3 frameScale, framePosition;
    Quaternion frameRotation;
    GameObject[] portraitParts;
    bool[] portraitStates;
    Vector2 pan;
    float range;
    bool initialized;

    public bool Initialize(PhoneManager owner)
    {
        if (initialized) return true;
        phone = owner;
        if (phone.phoneCanvas == null) return false;
        home = phone.phoneCanvas.GetComponentInChildren<PhoneHomeScreen>(true);
        frame = phone.phoneCanvas.transform.Find("PhoneFrame") as RectTransform;
        if (home == null || frame == null)
        {
            Debug.LogWarning("[CityMap] PhoneFrame or PhoneHomeScreen is missing.", this);
            return false;
        }
        var asset = Resources.Load<TextAsset>("CityMapLayout");
        if (asset == null)
        {
            Debug.LogError("[CityMap] Missing illustrated layout. Run CADSNET/Map/Rebuild Illustrated City Layout in Edit mode.", this);
            return false;
        }
        layout = JsonUtility.FromJson<CityMapLayout>(asset.text);
        if (layout?.shapes == null || layout.places == null || layout.shapes.Length == 0) return false;
        player = phone.firstPersonController != null ? phone.firstPersonController.transform : phone.transform;
        home.mapController = this;
        mapButton = home.transform.Find("MapApp")?.GetComponent<Button>();
        if (mapButton != null)
        {
            mapButton.onClick.AddListener(home.OpenMap);
            mapButton.gameObject.SetActive(!IsIndoor);
        }
        CreateArrow();
        CreateMinimap();
        CreatePhoneMap();
        initialized = true;
        return true;
    }

    void CreateArrow()
    {
        arrowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        arrowTexture.name = "Map Player Arrow";
        var pixels = new Color[1024];
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            float dx = Mathf.Abs(x - 15.5f);
            bool inside = y >= 3 && y < 30 && dx <= (30 - y) * 0.48f && y >= 3 + (1 - dx / 14f) * 7;
            pixels[y * 32 + x] = inside ? Color.white : Color.clear;
        }
        arrowTexture.SetPixels(pixels);
        arrowTexture.Apply();
        arrowSprite = Sprite.Create(arrowTexture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
    }

    void CreateMinimap()
    {
        var go = new GameObject("City Map HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        hud = go.GetComponent<Canvas>();
        hud.renderMode = RenderMode.ScreenSpaceOverlay;
        hud.sortingOrder = 15;
        hudRoot = (RectTransform)go.transform;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        // Round minimap: a dark rim with the map clipped to a circle inside it.
        circleSprite = UIBuild.Icon(256, (x, y) => x * x + y * y <= 1, Color.white);
        pinSprite = UIBuild.Icon(64, IsPin, Color.white);
        miniRoot = Panel("Minimap", hudRoot, new Vector2(226, 226), new Color(0.035f, 0.055f, 0.075f, 0.96f));
        miniRoot.GetComponent<Image>().sprite = circleSprite;
        miniRoot.anchorMin = miniRoot.anchorMax = miniRoot.pivot = Vector2.zero;
        var clip = Panel("Clip", miniRoot, new Vector2(210, 210), Color.white);
        clip.GetComponent<Image>().sprite = circleSprite;
        clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        miniImage = MapImage("Map", clip, new Vector2(210, 210), Vector2.zero);
        miniArrow = Arrow(miniImage.rectTransform, 25);
        miniMarker = Marker(miniRoot, 28); // outside the clip so it can sit on the rim when the café is far away
        var click = miniRoot.gameObject.AddComponent<Button>();
        click.onClick.AddListener(phone.OpenMap);
    }

    RectTransform Marker(Transform parent, float size)
    {
        var rect = Rect("CafeMarker", parent, new Vector2(size, size), Vector2.zero);
        rect.pivot = new Vector2(0.5f, 0.05f); // the pin's tip marks the spot
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = pinSprite;
        image.color = new Color(1f, 0.78f, 0.2f);
        image.raycastTarget = false;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.08f, 0.05f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        rect.gameObject.SetActive(false);
        return rect;
    }

    // Map pin: round head with a hole, tapering to a point at the bottom.
    static bool IsPin(float x, float y)
    {
        float hx = x, hy = y - 0.3f, head = hx * hx + hy * hy;
        bool tip = y < 0.3f && y > -0.95f && Mathf.Abs(x) < (y + 0.95f) * 0.52f;
        return (head < 0.36f || tip) && head > 0.04f;
    }

    // The café marker guides the "head to the café" task and disappears once you're there.
    bool CafeTaskActive()
    {
        var gp = GameProgressManager.Instance;
        return !IsIndoor && gp != null && gp.thesisBackedUp && !gp.arrivedAtCafe;
    }

    void CreatePhoneMap()
    {
        expanded = Panel("CityMapScreen", frame, new Vector2(460, 246), new Color(0.035f, 0.055f, 0.075f));
        expanded.localRotation = Quaternion.Euler(0, 0, 90);
        fullImage = MapImage("Viewport", expanded, new Vector2(460, 246), Vector2.zero);
        viewport = fullImage.rectTransform;

        fullImage.gameObject.AddComponent<CityMapGesture>().map = this;
        fullArrow = Arrow(viewport, 17);
        fullMarker = Marker(viewport, 30);
        mapFade = expanded.gameObject.AddComponent<CanvasGroup>();
        placeLabels = new TMP_Text[layout.places.Length];
        placePins = new RectTransform[layout.places.Length];
        for (int i = 0; i < layout.places.Length; i++)
        {
            var place = layout.places[i];
            placePins[i] = Panel(place.name + " pin", viewport, new Vector2(9, 9), new Color(0.91f, 0.53f, 0.29f));
            placePins[i].GetComponent<Image>().raycastTarget = false;
            placePins[i].localRotation = Quaternion.Euler(0, 0, 45);
            placeLabels[i] = Label(place.name + " label", viewport, place.name.ToUpperInvariant(), new Vector2(76, 18), Vector2.zero, 12);
            placeLabels[i].fontStyle = FontStyles.Bold;
            placeLabels[i].color = new Color(0.12f, 0.21f, 0.22f);
            var outline = placeLabels[i].gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.96f, 0.95f, 0.86f);
            outline.effectDistance = new Vector2(1, -1);
        }
        fullArrow.SetAsLastSibling();
        expanded.gameObject.SetActive(false);
        portraitParts = new GameObject[frame.childCount - 1];
        portraitStates = new bool[portraitParts.Length];
        int index = 0;
        foreach (Transform child in frame)
            if (child != expanded) portraitParts[index++] = child.gameObject;
    }

    public void Open()
    {
        if (!initialized || IsExpanded || IsIndoor) return;
        frameScale = frame.localScale;
        framePosition = frame.localPosition;
        frameRotation = frame.localRotation;
        for (int i = 0; i < portraitParts.Length; i++)
        {
            portraitStates[i] = portraitParts[i].activeSelf;
            // Keep the original phone visible while its frame turns.
        }
        IsExpanded = true;
        range = Mathf.Clamp(32f, minimumRange, maximumRange);
        pan = Vector2.zero;
        expanded.gameObject.SetActive(true);
        mapFade.alpha = 0;
        mapFade.blocksRaycasts = false;
        Canvas.ForceUpdateCanvases();
        UpdateMap();
        opening = StartCoroutine(RevealMap());
    }

    IEnumerator RevealMap()
    {
        Vector3 startScale = frame.localScale, startPosition = frame.localPosition;
        Quaternion startRotation = frame.localRotation;
        float elapsed = 0;
        // Unscaled time keeps the transition running while the phone pauses gameplay.
        while (elapsed < 0.28f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.28f);
            float eased = Mathf.SmoothStep(0, 1, t);
            FitPhone();
            Vector3 targetScale = frame.localScale, targetPosition = frame.localPosition;
            frame.localScale = Vector3.Lerp(startScale, targetScale, reduceMotion ? 1 : eased);
            frame.localPosition = Vector3.Lerp(startPosition, targetPosition, reduceMotion ? 1 : eased);
            frame.localRotation = Quaternion.Slerp(startRotation, Quaternion.Euler(0, 0, -90), reduceMotion ? 1 : eased);
            // Reveal only after the rotation has started, covering the portrait screen smoothly.
            mapFade.alpha = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.45f, 1, t));
            yield return null;
        }
        foreach (var part in portraitParts) part.SetActive(false);
        mapFade.alpha = 1;
        mapFade.blocksRaycasts = true;
        opening = null;
    }

    public void Close()
    {
        if (!IsExpanded) return;
        IsExpanded = false;
        if (opening != null) { StopCoroutine(opening); opening = null; }
        expanded.gameObject.SetActive(false);
        frame.localRotation = frameRotation;
        frame.localScale = frameScale;
        frame.localPosition = framePosition;
        for (int i = 0; i < portraitParts.Length; i++)
            if (portraitParts[i] != null) portraitParts[i].SetActive(portraitStates[i]);
    }

    public void Zoom(float multiplier)
    {
        if (!IsExpanded || multiplier <= 0 || float.IsNaN(multiplier)) return;
        range = Mathf.Clamp(range * multiplier, minimumRange, maximumRange);
    }

    public void Pan(Vector2 delta)
    {
        if (!IsExpanded || viewport.rect.height <= 0) return;
        pan -= delta * (2f * range / viewport.rect.height);
        pan = Vector2.ClampMagnitude(pan, maximumRange * 3f);
    }

    public void Recenter() => pan = Vector2.zero;

    void LateUpdate()
    {
        if (!initialized) return;
        if (IsExpanded && !phone.IsOpen) Close();
        bool visible = !IsIndoor && !phone.IsOpen && Time.timeScale > 0 && (phone.firstPersonController == null || phone.firstPersonController.enabled);
        hud.gameObject.SetActive(visible);
        if (visible)
        {
            Rect safe = Screen.safeArea;
            miniRoot.anchoredPosition = new Vector2(safe.xMin / hud.scaleFactor + 24, safe.yMin / hud.scaleFactor + 24);
        }
        if (IsExpanded && opening == null) FitPhone();
        if (visible || IsExpanded) UpdateMap();
    }

    void FitPhone()
    {
        var canvas = phone.phoneCanvas.GetComponent<Canvas>();
        float factor = canvas != null ? canvas.scaleFactor : 1;
        Rect safe = Screen.safeArea;
        float scale = Mathf.Min(3.2f, safe.width * 0.88f / (560f * factor), safe.height * 0.85f / (280f * factor));
        frame.localScale = Vector3.one * scale;
        var parent = frame.parent as RectTransform;
        if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safe.center,
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out var center))
            frame.localPosition = new Vector3(center.x, center.y, framePosition.z);
    }

    public void FocusPlace(string name)
    {
        foreach (var place in layout.places)
            if (place.name == name) { pan = place.position - MapPosition; range = 25; break; }
    }

    void UpdateMap()
    {
        if (player == null) return;
        Vector2 position = MapPosition;
        miniImage.SetView(position, minimapRange);
        Vector2 center = position + pan;
        center.x = Mathf.Clamp(center.x, layout.min.x, layout.max.x);
        center.y = Mathf.Clamp(center.y, layout.min.y, layout.max.y);
        pan = center - position;
        fullImage.SetView(center, range);
        miniArrow.localRotation = Quaternion.Euler(0, 0, IsIndoor ? 0 : -player.eulerAngles.y);
        fullArrow.localRotation = miniArrow.localRotation;
        fullArrow.anchoredPosition = fullImage.Project(position);
        var rect = viewport.rect;
        fullArrow.gameObject.SetActive(Inside(fullArrow.anchoredPosition, rect, 10, 10));
        for (int i = 0; i < placeLabels.Length; i++)
        {
            Vector2 point = fullImage.Project(layout.places[i].position);
            bool show = Inside(point, rect, 42, 30);
            placePins[i].gameObject.SetActive(show);
            placeLabels[i].gameObject.SetActive(show);
            if (!show) continue;
            placePins[i].anchoredPosition = point;
            placeLabels[i].rectTransform.anchoredPosition = point + new Vector2(0, -18);
        }
        UpdateMarkers(rect);
    }

    void UpdateMarkers(Rect fullRect)
    {
        bool active = CafeTaskActive();
        Vector2 cafe = default;
        foreach (var place in layout.places) if (place.name == "Café") cafe = place.position;
        float pulse = 1 + 0.12f * Mathf.Sin(Time.unscaledTime * 4);

        // Minimap: clamp to the rim so the pin always points toward the café.
        miniMarker.gameObject.SetActive(active);
        if (active)
        {
            miniMarker.anchoredPosition = Vector2.ClampMagnitude(miniImage.Project(cafe), 96);
            miniMarker.localScale = Vector3.one * pulse;
        }
        Vector2 point = fullImage.Project(cafe);
        bool onScreen = active && Inside(point, fullRect, 16, 16);
        fullMarker.gameObject.SetActive(onScreen);
        if (onScreen)
        {
            fullMarker.anchoredPosition = point + new Vector2(0, 6);
            fullMarker.localScale = Vector3.one * pulse;
            fullMarker.SetAsLastSibling();
        }
    }

    static bool Inside(Vector2 point, Rect rect, float xMargin, float yMargin)
    {
        return Mathf.Abs(point.x) < rect.width * 0.5f - xMargin && Mathf.Abs(point.y) < rect.height * 0.5f - yMargin;
    }

    RectTransform Panel(string name, Transform parent, Vector2 size, Color color)
    {
        var rect = Rect(name, parent, size, Vector2.zero);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    CityMapSurface MapImage(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var image = Rect(name, parent, size, position).gameObject.AddComponent<CityMapSurface>();
        image.layout = layout;
        return image;
    }

    RectTransform Arrow(Transform parent, float size)
    {
        var rect = Rect("PlayerArrow", parent, Vector2.one * size, Vector2.zero);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = arrowSprite;
        image.color = new Color(0.2f, 1f, 0.87f);
        image.raycastTarget = false;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.01f, 0.06f, 0.08f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return rect;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    static TMP_Text Label(string name, Transform parent, string text, Vector2 size, Vector2 position, float fontSize)
    {
        var label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = new Color(0.83f, 0.95f, 0.96f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    void OnDisable()
    {
        Close();
        if (hud != null) hud.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (mapButton != null && home != null) mapButton.onClick.RemoveListener(home.OpenMap);
        if (hud != null) Destroy(hud.gameObject);
        if (expanded != null) Destroy(expanded.gameObject);
        if (arrowSprite != null) Destroy(arrowSprite);
        if (arrowTexture != null) Destroy(arrowTexture);
        foreach (var sprite in new[] { circleSprite, pinSprite })
            if (sprite != null) { Destroy(sprite.texture); Destroy(sprite); }
    }
}
