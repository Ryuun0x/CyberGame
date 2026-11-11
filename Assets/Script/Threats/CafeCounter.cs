using System.Collections;
using StarterAssets;
using UnityEngine;

// Café counter: ordering a drink hands over a receipt printed with the real Wi-Fi name and password.
// The first order takes a moment: while the drink is made, the camera pans slowly to the hooded stranger at his
// laptop (a hint that someone in here is up to something; no logo is shown), then back, and the receipt appears.
// Needs a Collider (for the interaction raycast) and optionally an InteractableLabel.
public class CafeCounter : MonoBehaviour, IInteractable
{
    public const string WifiName = "CafeWifi67";
    public const string WifiPassword = "brew2026";

    [Tooltip("The receipt overlay (CafeReceipt object in the UI category).")]
    public CafeReceipt receipt;

    [Header("Waiting for the order")]
    [Tooltip("Camera for the pan (the city's cutscene camera, off until used).")]
    public Camera panCamera;
    [Tooltip("The black bars (UI > CityArrivalLetterbox).")]
    public CanvasGroup letterbox;
    [Tooltip("Who the camera pans to: the stranger.")]
    public Transform suspect;
    public float panSeconds = 4.5f, holdSeconds = 3f;

    public static bool Waiting { get; private set; }

    void Start() => UpdatePrompt();

    public void Interact()
    {
        // The E press that puts the receipt away must not reopen it.
        if (CafeReceipt.IsOpen || Waiting) return;

        var gp = GameProgressManager.Instance;
        if (gp != null && !gp.hasReceipt)
        {
            StartCoroutine(OrderAndWait(gp));
            return;
        }
        receipt.Show();
        UpdatePrompt();
    }

    IEnumerator OrderAndWait(GameProgressManager gp)
    {
        var player = FindFirstObjectByType<FirstPersonController>();
        var view = Camera.main;
        if (panCamera == null || suspect == null || player == null || view == null)
        {
            Served(gp); // nothing to film: just hand over the receipt
            yield break;
        }

        Waiting = true;
        player.enabled = false;
        ShowCrosshair(false);
        Say("One iced latte, please.", 2.2f);
        yield return Wait(2.2f);
        Say("Barista: Coming right up! It'll be a few minutes.", 2.6f);

        // From her own view, turn slowly toward the stranger's head and drift a little closer.
        Transform cam = panCamera.transform;
        Vector3 fromPosition = view.transform.position;
        Quaternion fromRotation = view.transform.rotation;
        Vector3 head = suspect.position + Vector3.up * 1.15f;
        Vector3 toPosition = Vector3.Lerp(fromPosition, head, 0.15f);
        Quaternion toRotation = Quaternion.LookRotation(head - toPosition);
        cam.SetPositionAndRotation(fromPosition, fromRotation);
        panCamera.enabled = true;
        for (float t = 0; t < 1; t += Time.deltaTime / panSeconds)
        {
            float s = Mathf.SmoothStep(0, 1, t);
            cam.SetPositionAndRotation(Vector3.Lerp(fromPosition, toPosition, s), Quaternion.Slerp(fromRotation, toRotation, s));
            if (letterbox != null) letterbox.alpha = Mathf.Min(1, t * 3);
            yield return null;
        }
        Say("...That guy in the hoodie hasn't looked up from his laptop since I walked in.", holdSeconds);
        yield return Wait(holdSeconds);

        Say("Barista: Iced latte for Alice! The Wi-Fi password's on your receipt.", 3f);
        for (float t = 0; t < 1; t += Time.deltaTime / 1f)
        {
            float s = Mathf.SmoothStep(0, 1, t);
            cam.SetPositionAndRotation(Vector3.Lerp(toPosition, fromPosition, s), Quaternion.Slerp(toRotation, fromRotation, s));
            if (letterbox != null) letterbox.alpha = 1 - s;
            yield return null;
        }
        if (letterbox != null) letterbox.alpha = 0;
        panCamera.enabled = false;
        player.enabled = true;
        ShowCrosshair(true);
        Waiting = false;
        Served(gp);
    }

    void Served(GameProgressManager gp)
    {
        if (gp != null && !gp.hasReceipt) gp.OrderDrink();
        receipt.Show();
        UpdatePrompt();
    }

    // Editor checks only: order and get the receipt without the wait.
    public void ServeNow() => Served(GameProgressManager.Instance);

    static void Say(string line, float seconds)
    {
        if (NarrationManager.Instance != null) NarrationManager.Instance.Show(line, seconds);
    }

    static IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds; t += Time.deltaTime) yield return null;
    }

    static void ShowCrosshair(bool show)
    {
        var crosshair = FindFirstObjectByType<RaycastCrosshair>(FindObjectsInactive.Include);
        if (crosshair != null && crosshair.crosshairDot != null) crosshair.crosshairDot.gameObject.SetActive(show);
    }

    void UpdatePrompt()
    {
        var label = GetComponent<InteractableLabel>();
        bool ordered = GameProgressManager.Instance != null && GameProgressManager.Instance.hasReceipt;
        if (label != null) label.promptText = ordered ? "View Receipt" : "Order a Drink";
    }

    void OnDestroy() => Waiting = false;
}
