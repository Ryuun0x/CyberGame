using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuEffects : MonoBehaviour
{
    [Header("Camera Panning")]
    [Tooltip("Leave empty to automatically use the Main Camera")]
    public Transform cameraTransform;
    [Tooltip("How fast the camera sweeps left and right")]
    public float panSpeed = 0.5f;
    [Tooltip("The leftmost Y rotation angle (e.g., -71.26)")]
    public float minYRotation = -71.26f;
    [Tooltip("The rightmost Y rotation angle (e.g., -40.68)")]
    public float maxYRotation = -40.68f;

    private Vector3 _baseRotation;

    [Header("Light Flicker")]
    [Tooltip("Drag the directional or point light here")]
    public Light roomLight;
    public float minLightIntensity = 0.6f;
    public float maxLightIntensity = 1.2f;
    [Tooltip("Max delay between flickers")]
    public float lightFlickerSpeed = 0.15f;

    [Header("UI Flicker")]
    [Tooltip("Drag any CanvasGroups you want to flicker here (Menu UI, Laptop Screen, etc.)")]
    public CanvasGroup[] uiCanvasGroups;
    public float minUIAlpha = 0.75f;
    public float maxUIAlpha = 1f;
    [Tooltip("Max delay between UI flickers")]
    public float uiFlickerSpeed = 0.1f;

    [Header("Scene Loading")]
    [Tooltip("Exact name of your main game scene")]
    public string gameSceneName = "Interior";

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            // Store the initial rotation so we don't mess up X and Z
            _baseRotation = cameraTransform.eulerAngles;
        }
        
        StartCoroutine(FlickerLight());
        StartCoroutine(FlickerUI());
    }

    private void Update()
    {
        // Sweep camera smoothly back and forth (Ping-Pong effect)
        if (cameraTransform != null)
        {
            // Mathf.Sin creates a smooth wave. We convert it to a 0 to 1 range.
            float t = (Mathf.Sin(Time.time * panSpeed) + 1f) / 2f;
            
            // Lerp between your specific Y angles
            float currentY = Mathf.Lerp(minYRotation, maxYRotation, t);
            
            // Apply the new Y rotation while keeping X and Z completely locked
            cameraTransform.eulerAngles = new Vector3(_baseRotation.x, currentY, _baseRotation.z);
        }
    }

    private IEnumerator FlickerLight()
    {
        if (roomLight == null) yield break;

        // Save the baseline intensity so it doesn't drift
        float baseIntensity = roomLight.intensity;

        while (true)
        {
            // 80% chance to just stay at normal light, 20% chance to flicker
            if (Random.value > 0.2f)
            {
                roomLight.intensity = baseIntensity;
                yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
            }
            else
            {
                // Rapid flicker burst
                int burstCount = Random.Range(2, 5);
                for (int i = 0; i < burstCount; i++)
                {
                    roomLight.intensity = Random.Range(minLightIntensity, maxLightIntensity);
                    yield return new WaitForSeconds(Random.Range(0.02f, lightFlickerSpeed));
                }
            }
        }
    }

    private IEnumerator FlickerUI()
    {
        while (true)
        {
            // 80% chance to be completely normal
            if (Random.value > 0.2f)
            {
                foreach (var cg in uiCanvasGroups)
                {
                    if (cg != null) cg.alpha = 1f;
                }
                yield return new WaitForSeconds(Random.Range(1.0f, 3.0f));
            }
            else
            {
                // Rapid stutter/glitch burst!
                int burstCount = Random.Range(3, 8);
                for (int i = 0; i < burstCount; i++)
                {
                    float randomAlpha = Random.Range(minUIAlpha, maxUIAlpha);
                    foreach (var cg in uiCanvasGroups)
                    {
                        if (cg != null) cg.alpha = randomAlpha;
                    }
                    yield return new WaitForSeconds(Random.Range(0.02f, uiFlickerSpeed));
                }
            }
        }
    }

}
