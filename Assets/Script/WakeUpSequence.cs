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

    [Header("Spawn Points")]
    public Transform spawnBed;
    public Transform spawnDoor;


    private string[] _dialogueLines = new string[]
    {
        "Ugh... my head.",
        "I was up all night finishing that thesis...",
        "It's due today! I better check my laptop and submit it right now."
    };

    void Start()
    {
        cinematicCamera.enabled = true;
        playerCamera.enabled = false;
        player.SetActive(false);

        if (narrationBox != null)
            narrationBox.SetActive(false);

        // Check GameProgressManager instead of PlayerPrefs
        // Since GPM is DontDestroyOnLoad, this resets when you restart the game
        // but survives scene transitions (going outside and back inside)
        bool alreadyPlayed = GameProgressManager.Instance != null
                             && GameProgressManager.Instance.wakeUpPlayed;

        if (!alreadyPlayed)
        {
            // First time — spawn at bed, play cinematic
            if (spawnBed != null)
                player.transform.position = spawnBed.position;

            // Mark as played so it won't replay when returning to this scene
            if (GameProgressManager.Instance != null)
                GameProgressManager.Instance.wakeUpPlayed = true;

            StartCoroutine(PlayWakeUpSequence());
        }
        else
        {
            // Returning from outside — spawn at door, skip cinematic
            if (spawnDoor != null)
                player.transform.position = spawnDoor.position;

            SkipToFPS();
        }
    }

    void SkipToFPS()
    {
        cinematicCamera.enabled = false;
        player.SetActive(true);
        playerCamera.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Reset camera rotation so player can look around freely
        if (spawnDoor != null)
            player.transform.rotation = spawnDoor.rotation;

        // Reset camera local rotation
        playerCamera.transform.localRotation = Quaternion.identity;

        FirstPersonController fpc = player.GetComponentInChildren<FirstPersonController>();
        if (fpc != null) fpc.enabled = true;

        if (screenFader != null)
            StartCoroutine(screenFader.FadeIn());
    }

    IEnumerator PlayWakeUpSequence()
    {
        yield return StartCoroutine(Blink());
        yield return new WaitForSeconds(pauseBeforeSequence);
        yield return StartCoroutine(LookAround());
        yield return StartCoroutine(SitUp());
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(PlayDialogue());

        // Fade to black then switch to FPS
        yield return StartCoroutine(screenFader.FadeOut());

        cinematicCamera.enabled = false;
        player.SetActive(true);

        yield return new WaitForEndOfFrame();

        playerCamera.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        FirstPersonController fpc = player.GetComponentInChildren<FirstPersonController>();
        if (fpc != null) fpc.enabled = true;

        yield return StartCoroutine(screenFader.FadeIn());
    }

    IEnumerator Blink()
    {
        yield return StartCoroutine(screenFader.FadeIn());

        for (int i = 0; i < blinkCount; i++)
        {
            yield return StartCoroutine(screenFader.FadeOut());
            yield return new WaitForSeconds(blinkSpeed);
            yield return StartCoroutine(screenFader.FadeIn());
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

            yield return new WaitForSeconds(secondsBetweenLines);
        }

        if (narrationBox != null)
            narrationBox.SetActive(false);
    }
}