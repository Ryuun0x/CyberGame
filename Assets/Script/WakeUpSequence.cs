using StarterAssets;
using System.Collections;
using TMPro;
using UnityEngine;

public class WakeUpSequence : MonoBehaviour
{
    [Header("Cameras")]
    public Camera cinematicCamera;
    public Camera playerCamera;

    [Header("Player")]
    public GameObject player;

    [Header("Cinematic Movement")]
    public float lookAroundDuration = 3f;
    public float sitUpDuration = 2f;
    public Vector3 lookAroundRotation = new Vector3(5f, 120f, 0f);
    public Vector3 sitUpPosition;
    public Vector3 sitUpRotation = new Vector3(15f, 90f, 0f);

    [Header("Narration")]
    public GameObject narrationBox;
    public TextMeshProUGUI narrationText;
    public float secondsBetweenLines = 2.5f;

    [Header("Screen Fader")]
    public ScreenFader screenFader;

    [Header("Timing")]
    public float blinkCount = 3;
    public float blinkSpeed = 0.3f;
    public float pauseBeforeSequence = 2f;

    private string[] _dialogueLines = new string[]
    {
        "Another morning...",
        "Today I'm heading to the cafe to finish some work.",
        "Better check my phone first.",
    };

    void Start()
    {
        cinematicCamera.enabled = true;
        playerCamera.enabled = false;
        player.SetActive(false);

        if (narrationBox != null)
            narrationBox.SetActive(false);

        StartCoroutine(PlayWakeUpSequence());
    }

    IEnumerator PlayWakeUpSequence()
    {
        // Blinking effect — fade in and out a few times
        yield return StartCoroutine(Blink());

        // Pause before sequence starts
        yield return new WaitForSeconds(pauseBeforeSequence);

        // Look around slowly
        yield return StartCoroutine(LookAround());

        // Sit up
        yield return StartCoroutine(SitUp());

        // Small pause before dialogue
        yield return new WaitForSeconds(0.5f);

        // Show dialogue auto advancing
        yield return StartCoroutine(PlayDialogue());

        // Fade to black
        yield return StartCoroutine(screenFader.FadeOut());

        // Switch to FPS
        cinematicCamera.enabled = false;
        player.SetActive(true);

        yield return new WaitForEndOfFrame();

        playerCamera.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        FirstPersonController fpc = player.GetComponentInChildren<FirstPersonController>();
        if (fpc != null) fpc.enabled = true;

        // Fade back in
        yield return StartCoroutine(screenFader.FadeIn());
    }

    IEnumerator Blink()
    {
        // Start fully black
        yield return StartCoroutine(screenFader.FadeIn());

        for (int i = 0; i < blinkCount; i++)
        {
            // Fade out (close eyes)
            yield return StartCoroutine(screenFader.FadeOut());
            yield return new WaitForSeconds(blinkSpeed);

            // Fade in (open eyes)
            yield return StartCoroutine(screenFader.FadeIn());

            // Eyes open a little longer each time
            yield return new WaitForSeconds(blinkSpeed + (i * 0.2f));
        }
    }

    IEnumerator LookAround()
    {
        float elapsed = 0f;
        Quaternion startRot = cinematicCamera.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(lookAroundRotation);

        while (elapsed < lookAroundDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lookAroundDuration);
            cinematicCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
    }

    IEnumerator SitUp()
    {
        float elapsed = 0f;
        Vector3 startPos = cinematicCamera.transform.position;
        Quaternion startRot = cinematicCamera.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(sitUpRotation);

        while (elapsed < sitUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / sitUpDuration);
            cinematicCamera.transform.position = Vector3.Lerp(startPos, sitUpPosition, t);
            cinematicCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
    }

    IEnumerator PlayDialogue()
    {
        if (narrationBox != null)
            narrationBox.SetActive(true);

        foreach (string line in _dialogueLines)
        {
            if (narrationText != null)
                narrationText.text = line;

            // Auto advance after delay — no input needed
            yield return new WaitForSeconds(secondsBetweenLines);
        }

        if (narrationBox != null)
            narrationBox.SetActive(false);
    }
}