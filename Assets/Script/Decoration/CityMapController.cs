using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// The city map: the round minimap in the top-right corner (HUD) and the map screen on the phone.
/// All of its UI is placed in the Hierarchy (CADSNET > Map > Place Map UI in Scene builds it once):
///   UI > City Map HUD > Minimap > Clip > Map (+ PlayerArrow), Minimap > CafeMarker
///   PhoneCanvas > PhoneFrame > CityMapScreen > Viewport (+ place pins and labels, PlayerArrow, CafeMarker)
/// The positions of the arrows, pins, labels and the minimap are set here every frame; their size, colour and
/// font can be changed in the Inspector.
/// </summary>
public class CityMapController : MonoBehaviour
{
    public float minimapRange = 18f;
    public float minimumRange = 8f;
    public float maximumRange = 240f;
    [Tooltip("Gap between the minimap and the top-right corner of the screen.")]
    public float minimapMargin = 24f;

    [Header("Placed map UI")]
    public Canvas hud;
    public RectTransform miniRoot, miniArrow, miniMarker;
    public CityMapSurface miniImage;
    public RectTransform expanded, fullArrow, fullMarker;
    public CityMapSurface fullImage;
    public CanvasGroup mapFade;
    [Tooltip("One pin and one label per place in the map layout, in the same order.")]
    public RectTransform[] placePins;
    public TMP_Text[] placeLabels;

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
    RectTransform frame, viewport;
    CityMapLayout layout;
    Coroutine opening;
    public bool reduceMotion;
    public bool IsTransitioning => opening != null;
    Button mapButton;
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
        if (hud == null || expanded == null || placeLabels == null || placeLabels.Length != layout.places.Length)
        {
            Debug.LogError("[CityMap] The map UI isn't placed in this scene (or the places changed). Run CADSNET/Map/Place Map UI in Scene.", this);
            return false;
        }
        player = phone.firstPersonController != null ? phone.firstPersonController.transform : phone.transform;
        home.mapController = this;
        mapButton = home.transform.Find("MapApp")?.GetComponent<Button>();
        if (mapButton != null)
        {
            mapButton.onClick.AddListener(home.OpenMap);
            mapButton.gameObject.SetActive(!IsIndoor);
        }
        miniImage.layout = fullImage.layout = layout;
        viewport = fullImage.rectTransform;
        miniMarker.gameObject.SetActive(false);
        fullMarker.gameObject.SetActive(false);
        expanded.gameObject.SetActive(false);
        portraitParts = new GameObject[frame.childCount - 1];
        portraitStates = new bool[portraitParts.Length];
        int index = 0;
        foreach (Transform child in frame)
            if (child != expanded) portraitParts[index++] = child.gameObject;
        initialized = true;
        return true;
    }

    // The café marker guides the "head to the café" task and disappears once you're there.
    bool CafeTaskActive()
    {
        var gp = GameProgressManager.Instance;
        return !IsIndoor && gp != null && gp.thesisBackedUp && !gp.arrivedAtCafe;
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
        range = Mathf.Clamp(32f, minimumRange, MaxRange());
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
        range = Mathf.Clamp(range * multiplier, minimumRange, MaxRange());
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
            miniRoot.anchoredPosition = new Vector2((safe.xMax - Screen.width) / hud.scaleFactor - minimapMargin, (safe.yMax - Screen.height) / hud.scaleFactor - minimapMargin);
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
        // Keep the whole view on the artwork, not just its centre, so no blank space shows past the border.
        Vector2 center = position + pan, half = HalfView();
        center.x = ClampInside(center.x, layout.min.x + half.x, layout.max.x - half.x);
        center.y = ClampInside(center.y, layout.min.y + half.y, layout.max.y - half.y);
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

    // range is the half-height of the view in metres; the viewport is wider than tall.
    Vector2 HalfView()
    {
        var rect = viewport.rect;
        return new Vector2(range * (rect.height > 0 ? rect.width / rect.height : 1), range);
    }

    // Widest zoom that still fits inside the city artwork.
    float MaxRange()
    {
        var rect = viewport.rect;
        float aspect = rect.height > 0 ? rect.width / rect.height : 1;
        Vector2 size = layout.max - layout.min;
        return Mathf.Max(minimumRange, Mathf.Min(maximumRange, size.y / 2, size.x / 2 / aspect));
    }

    static float ClampInside(float value, float low, float high) => low <= high ? Mathf.Clamp(value, low, high) : (low + high) / 2;

    void UpdateMarkers(Rect fullRect)
    {
        bool active = CafeTaskActive();
        Vector2 cafe = default;
        foreach (var place in layout.places) if (place.name == "Café") cafe = place.position;
        float pulse = 1 + 0.12f * Mathf.Sin(Time.unscaledTime * 4);

        // Minimap: on the map the pin stands upright on the café. Off the map it sits on the rim and turns so
        // its tip points toward the café from where the player is (the pivot is the tip, so it stays on the rim).
        miniMarker.gameObject.SetActive(active);
        if (active)
        {
            Vector2 toCafe = miniImage.Project(cafe);
            bool offMap = toCafe.magnitude > MiniRim;
            miniMarker.anchoredPosition = Vector2.ClampMagnitude(toCafe, MiniRim);
            miniMarker.localRotation = offMap ? Quaternion.Euler(0, 0, Mathf.Atan2(toCafe.x, -toCafe.y) * Mathf.Rad2Deg) : Quaternion.identity;
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

    const float MiniRim = 96f; // distance from the minimap's centre to its rim (UI units)

    static bool Inside(Vector2 point, Rect rect, float xMargin, float yMargin)
    {
        return Mathf.Abs(point.x) < rect.width * 0.5f - xMargin && Mathf.Abs(point.y) < rect.height * 0.5f - yMargin;
    }

    void OnDisable()
    {
        Close();
        if (hud != null) hud.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (mapButton != null && home != null) mapButton.onClick.RemoveListener(home.OpenMap);
    }
}
