using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CADSNET > Map > Place Map UI in Scene: builds the city map's UI once as normal Hierarchy objects
/// (what CityMapController used to create at runtime), so it can be adjusted in the Inspector.
///   UI > City Map HUD (the minimap) and PhoneCanvas > PhoneFrame > CityMapScreen (the phone map).
/// Its three icons are saved to Assets/Sprites/Map. Run it in each scene that has the phone (Interior, city).
/// Skips a scene that already has the map UI, so adjustments are never overwritten.
/// </summary>
public static class CityMapUIPlacer
{
    const string SpriteFolder = "Assets/Sprites/Map";

    [MenuItem("CADSNET/Map/Place Map UI in Scene")]
    public static void Place()
    {
        var phone = Object.FindFirstObjectByType<PhoneManager>(FindObjectsInactive.Include);
        if (phone == null || phone.phoneCanvas == null) { Debug.LogError("[CityMap] No PhoneManager with a phone canvas in this scene."); return; }
        var map = phone.GetComponent<CityMapController>() ?? Undo.AddComponent<CityMapController>(phone.gameObject);
        if (map.hud != null) { Debug.Log("[CityMap] Map UI is already placed in this scene.", map.hud); return; }
        var frame = phone.phoneCanvas.transform.Find("PhoneFrame");
        var asset = Resources.Load<TextAsset>("CityMapLayout");
        if (frame == null || asset == null) { Debug.LogError("[CityMap] PhoneFrame or Resources/CityMapLayout is missing."); return; }
        var layout = JsonUtility.FromJson<CityMapLayout>(asset.text);

        var circle = SaveIcon("MapCircle", UIBuild.Icon(256, (x, y) => x * x + y * y <= 1, Color.white));
        var pin = SaveIcon("MapPin", UIBuild.Icon(64, IsPin, Color.white));
        var arrow = SaveIcon("MapArrow", UIBuild.Icon(32, IsArrow, Color.white));

        // HUD: round minimap in the top-right corner (a dark rim, the map clipped to a circle inside it).
        var uiRoot = GameObject.Find("UI");
        var hudGo = new GameObject("City Map HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(hudGo, "Place map UI");
        if (uiRoot != null) hudGo.transform.SetParent(uiRoot.transform, false);
        hudGo.layer = 5;
        var hud = hudGo.GetComponent<Canvas>();
        hud.renderMode = RenderMode.ScreenSpaceOverlay;
        hud.sortingOrder = 15;
        var scaler = hudGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var miniRoot = Panel("Minimap", hudGo.transform, new Vector2(226, 226), new Color(0.035f, 0.055f, 0.075f, 0.96f), circle);
        miniRoot.anchorMin = miniRoot.anchorMax = miniRoot.pivot = Vector2.one; // top-right corner
        miniRoot.anchoredPosition = new Vector2(-24, -24);
        var clip = Panel("Clip", miniRoot, new Vector2(210, 210), Color.white, circle);
        clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var miniImage = Rect("Map", clip, new Vector2(210, 210)).gameObject.AddComponent<CityMapSurface>();
        var miniArrow = Arrow(miniImage.rectTransform, 25, arrow);
        var miniMarker = Marker(miniRoot, 28, pin); // outside the clip so it can sit on the rim when the café is far away
        var click = miniRoot.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(click.onClick, phone.OpenMap);

        // Phone map: turned sideways inside the phone frame.
        var expanded = Panel("CityMapScreen", frame, new Vector2(460, 246), new Color(0.035f, 0.055f, 0.075f), null);
        Undo.RegisterCreatedObjectUndo(expanded.gameObject, "Place map UI");
        expanded.localRotation = Quaternion.Euler(0, 0, 90);
        var fullImage = Rect("Viewport", expanded, new Vector2(460, 246)).gameObject.AddComponent<CityMapSurface>();
        fullImage.gameObject.AddComponent<CityMapGesture>().map = map;
        var fullArrow = Arrow(fullImage.rectTransform, 17, arrow);
        var fullMarker = Marker(fullImage.rectTransform, 30, pin);
        var fade = expanded.gameObject.AddComponent<CanvasGroup>();
        var pins = new RectTransform[layout.places.Length];
        var labels = new TMP_Text[layout.places.Length];
        for (int i = 0; i < layout.places.Length; i++)
        {
            string name = layout.places[i].name;
            pins[i] = Panel(name + " pin", fullImage.rectTransform, new Vector2(9, 9), new Color(0.91f, 0.53f, 0.29f), null);
            pins[i].GetComponent<Image>().raycastTarget = false;
            pins[i].localRotation = Quaternion.Euler(0, 0, 45);
            var label = Rect(name + " label", fullImage.rectTransform, new Vector2(76, 18)).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = name.ToUpperInvariant();
            label.fontSize = 12;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.12f, 0.21f, 0.22f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.96f, 0.95f, 0.86f);
            outline.effectDistance = new Vector2(1, -1);
            labels[i] = label;
        }
        fullArrow.SetAsLastSibling();
        expanded.gameObject.SetActive(false);
        hudGo.SetActive(false); // CityMapController shows it while walking in the city

        Undo.RecordObject(map, "Place map UI");
        map.hud = hud;
        map.miniRoot = miniRoot; map.miniImage = miniImage; map.miniArrow = miniArrow; map.miniMarker = miniMarker;
        map.expanded = expanded; map.fullImage = fullImage; map.fullArrow = fullArrow; map.fullMarker = fullMarker;
        map.mapFade = fade; map.placePins = pins; map.placeLabels = labels;
        EditorUtility.SetDirty(map);
        EditorSceneManager.MarkSceneDirty(phone.gameObject.scene);
        Debug.Log($"[CityMap] Placed the map UI in {phone.gameObject.scene.name} ({labels.Length} places).", hudGo);
    }

    static Sprite SaveIcon(string name, Sprite generated)
    {
        string path = $"{SpriteFolder}/{name}.png";
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(SpriteFolder);
            File.WriteAllBytes(path, generated.texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        Object.DestroyImmediate(generated.texture);
        Object.DestroyImmediate(generated);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Map pin: round head with a hole, tapering to a point at the bottom.
    static bool IsPin(float x, float y)
    {
        float hx = x, hy = y - 0.3f, head = hx * hx + hy * hy;
        bool tip = y < 0.3f && y > -0.95f && Mathf.Abs(x) < (y + 0.95f) * 0.52f;
        return (head < 0.36f || tip) && head > 0.04f;
    }

    // Player arrow pointing up with a notch at the back (same shape the controller used to draw).
    static bool IsArrow(float x, float y)
    {
        float px = (x + 1) * 15.5f, py = (y + 1) * 15.5f, dx = Mathf.Abs(px - 15.5f);
        return py >= 3 && py < 30 && dx <= (30 - py) * 0.48f && py >= 3 + (1 - dx / 14f) * 7;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = size;
        return rect;
    }

    static RectTransform Panel(string name, Transform parent, Vector2 size, Color color, Sprite sprite)
    {
        var rect = Rect(name, parent, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        return rect;
    }

    static RectTransform Arrow(Transform parent, float size, Sprite sprite)
    {
        var rect = Panel("PlayerArrow", parent, Vector2.one * size, new Color(0.2f, 1f, 0.87f), sprite);
        rect.GetComponent<Image>().raycastTarget = false;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.01f, 0.06f, 0.08f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return rect;
    }

    static RectTransform Marker(Transform parent, float size, Sprite sprite)
    {
        var rect = Panel("CafeMarker", parent, new Vector2(size, size), new Color(1f, 0.78f, 0.2f), sprite);
        rect.pivot = new Vector2(0.5f, 0.05f); // the pin's tip marks the spot
        rect.GetComponent<Image>().raycastTarget = false;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.08f, 0.05f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        rect.gameObject.SetActive(false);
        return rect;
    }
}
