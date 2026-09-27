using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CityMapChecks
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[CityMapChecks] " + message);
    }

    [MenuItem("CADSNET/Map/Run Play Mode Checks")]
    public static void Run()
    {
        Check(Application.isPlaying, "Enter Play mode first.");
        var phone = UnityEngine.Object.FindFirstObjectByType<PhoneManager>();
        Check(phone != null && phone.Map != null, "Phone and map initialized.");
        phone.ClosePhone();
        var map = phone.Map;
        Check(!map.IsIndoor, "Run in the city scene; the map is disabled indoors.");
        var home = phone.phoneCanvas.GetComponentInChildren<PhoneHomeScreen>(true);
        var frame = phone.phoneCanvas.transform.Find("PhoneFrame");
        bool gate = phone.requireFlashDrive;
        float time = Time.timeScale;
        var controller = phone.firstPersonController;
        bool control = controller != null && controller.enabled;
        var originalScale = frame.localScale;
        var originalRotation = frame.localRotation;
        phone.requireFlashDrive = false;
        try
        {
            Check(phone.TryOpenPhone(), "Phone opens.");
            home.transform.Find("MapApp").GetComponent<Button>().onClick.Invoke();
            Check(map.IsExpanded, "Map icon opens landscape map.");
            Check(Time.timeScale == 0 && Cursor.lockState == CursorLockMode.None, "Map pauses and unlocks.");
            map.SendMessage("LateUpdate");
            Check(map.IsTransitioning, "Landscape transition starts.");
            Vector3 playerPosition = controller != null ? controller.transform.position : phone.transform.position;
            Vector2 center = map.ProjectPlayer();
            Check(center.magnitude < 0.001f, "Player centered.");
            map.Zoom(0.00001f);
            map.SendMessage("LateUpdate");
            Check(Mathf.Approximately(map.VisibleRange, map.minimumRange), "Minimum zoom clamp.");
            map.Zoom(100000f);
            map.SendMessage("LateUpdate");
            Check(Mathf.Approximately(map.VisibleRange, map.maximumRange), "Maximum zoom clamp.");
            map.Pan(new Vector2(30, 20));
            map.SendMessage("LateUpdate");
            Check(Vector2.Distance(map.ProjectPlayer(), center) > 0.01f, "Pan moves map.");
            var gesture = frame.GetComponentInChildren<CityMapGesture>(true);
            var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            float beforeScroll = map.VisibleRange;
            eventData.scrollDelta = Vector2.up;
            gesture.OnScroll(eventData);
            map.SendMessage("LateUpdate");
            Check(map.VisibleRange < beforeScroll, "Scroll wheel zoom.");
            eventData.pointerId = 10;
            eventData.position = new Vector2(100, 100);
            gesture.OnPointerDown(eventData);
            eventData.pointerId = 11;
            eventData.position = new Vector2(200, 100);
            gesture.OnPointerDown(eventData);
            float beforePinch = map.VisibleRange;
            eventData.position = new Vector2(250, 100);
            gesture.OnDrag(eventData);
            map.SendMessage("LateUpdate");
            Check(map.VisibleRange < beforePinch, "Two-pointer pinch zoom.");
            gesture.OnPointerUp(eventData);
            eventData.pointerId = 10;
            gesture.OnPointerUp(eventData);
            var trackingTarget = controller != null ? controller.transform : phone.transform;
            Vector3 savedPosition = trackingTarget.position;
            try
            {
                trackingTarget.position += new Vector3(10, 0, 15);
                map.Recenter();
                map.SendMessage("LateUpdate");
                Check((map.ViewCenter - map.MapPosition).magnitude < 0.01f, "Map follows player movement.");
            }
            finally { trackingTarget.position = savedPosition; }
            map.Recenter();
            map.SendMessage("LateUpdate");
            Check(map.ProjectPlayer().magnitude < 0.001f, "Recenter works.");
            Check(!UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Any(c => c.name == "City Map Camera"), "No map camera exists.");
            map.FocusPlace("Café");
            map.SendMessage("LateUpdate");
            Check(map.Layout.places.Any(p => p.name == "Café" && (p.position - map.ViewCenter).magnitude < 0.01f), "Cafe shortcut centers destination.");
            home.GoHome();
            Check(!map.IsExpanded && home.homeScreen.activeInHierarchy, "Back restores Home.");
            Check(frame.localScale == originalScale && Quaternion.Angle(frame.localRotation, originalRotation) < 0.1f, "Portrait restored.");
            home.OpenWifi();
            Check(home.wifiScreen.activeInHierarchy, "Wi-Fi still opens.");
            home.GoHome();
            home.OpenAuthenticator();
            Check(home.authenticatorScreen.activeInHierarchy, "Authenticator still opens.");
            home.GoHome();
            phone.ClosePhone();
            Check(Time.timeScale == time && (controller == null || controller.enabled == control), "Pause and controls restored.");
            phone.OpenMap();
            Check(map.IsExpanded, "Direct map shortcut opens.");
            phone.ClosePhone();
            Check(!map.IsExpanded, "Closing phone closes map.");
            Debug.Log("[CityMapChecks] PASS: icon, landscape, pause, player projection and movement, zoom limits, scroll, synthetic pinch, pan, recenter, Home, Wi-Fi, Authenticator, close and reopen.");
        }
        finally
        {
            phone.ClosePhone();
            phone.requireFlashDrive = gate;
        }
    }

    [MenuItem("CADSNET/Map/Preview Expanded In Play Mode")]
    public static void Preview()
    {
        if (!Application.isPlaying) return;
        var phone = UnityEngine.Object.FindFirstObjectByType<PhoneManager>();
        if (phone == null) return;
        bool gate = phone.requireFlashDrive;
        phone.requireFlashDrive = false;
        phone.OpenMap();
        phone.requireFlashDrive = gate;
    }
}
