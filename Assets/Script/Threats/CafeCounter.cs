using System.Collections;
using StarterAssets;
using UnityEngine;

// Café counter: ordering a drink hands over a receipt printed with the real Wi-Fi name and password.
// The first order takes a moment: while the drink is made, a few shots show the café, like a film would: the barista
// at work, the customers, and last the hooded stranger at his laptop (in silence: no line, so nobody is accused and
// no logo is shown). Then the camera glides back into her eyes and the receipt appears.
// Needs a Collider (for the interaction raycast) and optionally an InteractableLabel.
public class CafeCounter : MonoBehaviour, IInteractable
{
    public const string WifiName = "CafeWifi67";
    public const string WifiPassword = "brew2026";

    [Tooltip("The receipt overlay (CafeReceipt object in the UI category).")]
    public CafeReceipt receipt;

    [Header("Waiting for the order")]
    [Tooltip("Camera for the shots (the city's cutscene camera, off until used).")]
    public Camera panCamera;
    [Tooltip("The shots in order: the barista, the customers, last the stranger. The camera starts at each one and " +
             "glides to its child \"End\". Placed under Story & Interactables > CafeOrderShots; move them to reframe.")]
    public Transform[] shots;
    [Tooltip("Seconds per shot, same order.")]
    public float[] shotSeconds = { 3.5f, 3.5f, 4.5f };
    [Tooltip("Seconds for the camera to glide from the last shot back into her eyes.")]
    public float glideSeconds = 1.6f;

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
        if (panCamera == null || shots == null || shots.Length == 0 || player == null || view == null)
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

        // The café while she waits: cut to each shot, drifting slowly to its End.
        Transform cam = panCamera.transform;
        panCamera.enabled = true;
        for (int i = 0; i < shots.Length; i++)
        {
            Transform shot = shots[i], end = shot.Find("End");
            for (float t = 0; t < 1; t += Time.deltaTime / shotSeconds[i])
            {
                float s = Mathf.SmoothStep(0, 1, t);
                cam.SetPositionAndRotation(Vector3.Lerp(shot.position, end.position, s), Quaternion.Slerp(shot.rotation, end.rotation, s));
                yield return null;
            }
        }

        // Her drink is called: glide from the stranger back to exactly what she sees, so switching cameras doesn't show.
        Say("Barista: Iced latte for Alice! The Wi-Fi password's on your receipt.", 3f);
        Vector3 fromPosition = cam.position;
        Quaternion fromRotation = cam.rotation;
        float fromFov = panCamera.fieldOfView;
        for (float t = 0; t < 1; t += Time.deltaTime / glideSeconds)
        {
            float s = Mathf.SmoothStep(0, 1, t);
            cam.SetPositionAndRotation(Vector3.Lerp(fromPosition, view.transform.position, s),
                                       Quaternion.Slerp(fromRotation, view.transform.rotation, s));
            panCamera.fieldOfView = Mathf.Lerp(fromFov, view.fieldOfView, s);
            yield return null;
        }
        panCamera.enabled = false;
        panCamera.fieldOfView = fromFov; // the city arrival uses this camera too
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
        if (!show && crosshair != null && crosshair.promptPanel != null) crosshair.promptPanel.gameObject.SetActive(false);
    }

    void UpdatePrompt()
    {
        var label = GetComponent<InteractableLabel>();
        bool ordered = GameProgressManager.Instance != null && GameProgressManager.Instance.hasReceipt;
        if (label != null) label.promptText = ordered ? "View Receipt" : "Order a Drink";
    }

    void OnDestroy() => Waiting = false;
}
