using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The first time she steps out into the city (Chapter 1): a few street-level shots of the traffic and the streets
/// (never from above, so the edge of the city stays out of view), letterboxed, her goal as a subtitle on the last
/// shot, then back to first person. Any key skips (after the first second).
/// On "CityArrival" in MainScene(city) (Story & Interactables). Each child of Shots is one shot: the camera starts
/// there and glides to its child "End". Move them to reframe. Cars are added around each shot just before it plays.
/// </summary>
public class CityArrival : MonoBehaviour
{
    public static bool Playing { get; private set; }

    [Tooltip("The shots' camera (off until the shots play).")]
    public Camera shotCamera;
    [Tooltip("The shots in order. The camera starts at each one and glides to its child \"End\".")]
    public Transform[] shots;
    [Tooltip("Shot lengths in seconds, same order.")]
    public float[] shotSeconds = { 5f, 5f, 6f };
    [Tooltip("Cars added on the roads in front of each shot, so there is traffic to see.")]
    public int carsPerShot = 4;
    [Tooltip("The black bars at the top and bottom (UI > CityArrivalLetterbox).")]
    public CanvasGroup letterbox;
    [TextArea] public string line = "I need to find a café with Wi-Fi to send my thesis.";

    IEnumerator Start()
    {
        shotCamera.enabled = false;
        letterbox.alpha = 0;
        var progress = GameProgressManager.Instance;
        // Not when the city scene is started on its own for testing (no wake-up played), and only once.
        if (progress == null || progress.chapter != 1 || !progress.wakeUpPlayed || progress.cityIntroPlayed) yield break;
        progress.cityIntroPlayed = true;

        var player = FindFirstObjectByType<FirstPersonController>();
        Playing = true;
        player.enabled = false;
        shotCamera.enabled = true;
        ShowCrosshair(false); // no aiming dot during the shots
        yield return null; // CityLife's and NarrationManager's own Start run first
        var city = FindFirstObjectByType<CityLife>();

        float started = Time.time;
        for (int i = 0; i < shots.Length && !Skipped(started); i++)
        {
            Transform shot = shots[i], end = shot.Find("End");
            if (city != null) city.AddCars(carsPerShot, shot.position + shot.forward * 35f, 50f);
            bool last = i == shots.Length - 1;
            if (last && NarrationManager.Instance != null) NarrationManager.Instance.Show(line, shotSeconds[i] - 0.5f);
            Transform cam = shotCamera.transform;
            for (float t = 0; t < 1 && !Skipped(started); t += Time.deltaTime / shotSeconds[i])
            {
                float s = Mathf.SmoothStep(0, 1, t);
                cam.SetPositionAndRotation(Vector3.Lerp(shot.position, end.position, s), Quaternion.Slerp(shot.rotation, end.rotation, s));
                // Bars in over the first second, out over the last second.
                letterbox.alpha = Mathf.Min(1f, (Time.time - started) / 1f, last ? (1 - t) * shotSeconds[i] : 1f);
                yield return null;
            }
        }
        letterbox.alpha = 0;
        shotCamera.enabled = false;
        player.enabled = true;
        ShowCrosshair(true);
        Playing = false;
    }

    static bool Skipped(float started) =>
        Time.time - started > 1f && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

    // The dot sits on the HUD, so it stays on screen unless it is switched off.
    static void ShowCrosshair(bool show)
    {
        var crosshair = FindFirstObjectByType<RaycastCrosshair>(FindObjectsInactive.Include);
        if (crosshair != null && crosshair.crosshairDot != null) crosshair.crosshairDot.gameObject.SetActive(show);
    }

    void OnDestroy() => Playing = false;
}
