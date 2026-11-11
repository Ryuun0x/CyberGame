using System;
using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phone notifications on the HUD (PhoneNotifications prefab, placed in both game scenes):
///   - the hotbar at the bottom centre: the Phone slot (opens the phone; Tab does the same) with a red unread
///     badge, plus a slot for each thing she carries (the flash drive in her pocket, the laptop),
///   - a banner that slides in from the top: app name, message, up to two action buttons.
/// Any script calls PhoneNotifications.Show("Mail", "text") or
/// Show("Messages", "text", ("Reply", Reply), ("Later", null)).
/// Without buttons the banner hides by itself; with buttons it stays until one is pressed, and the mouse is
/// freed for it (the player stands still meanwhile). The badge clears when the phone is opened.
/// </summary>
public class PhoneNotifications : MonoBehaviour
{
    static PhoneNotifications instance;

    [Header("Hotbar")]
    public GameObject hotbar;
    [Tooltip("The flash drive's coloured Icon: shown while it is in her pocket (picked up and not plugged in). The grey Silhouette under it shows otherwise.")]
    public GameObject flashDriveSlot;
    [Tooltip("The laptop's coloured Icon: shown while she carries the laptop.")]
    public GameObject laptopSlot;
    public GameObject badge;
    public TextMeshProUGUI badgeText;

    [Header("Banner (slides down from above the screen)")]
    public RectTransform banner;
    public TextMeshProUGUI appText, messageText;
    [Tooltip("The action buttons, hidden when a notification has none. Each has a TextMeshPro label inside.")]
    public Button[] actionButtons;
    public float shownY = -30f, hiddenY = 260f, slideSeconds = 0.3f, autoHideSeconds = 4f;

    int unread;
    bool waitingForChoice, stoppedPlayer;
    Coroutine showing;
    PhoneManager phone;
    FirstPersonController player;

    void Awake()
    {
        instance = this;
        banner.gameObject.SetActive(false);
    }

    public static void Show(string app, string message, params (string label, Action action)[] buttons)
    {
        if (instance == null) { Debug.LogWarning("[PhoneNotifications] Not in this scene: " + message); return; }
        instance.Open(app, message, buttons);
    }

    void Open(string app, string message, (string label, Action action)[] buttons)
    {
        unread++;
        appText.text = app;
        messageText.text = message;
        for (int i = 0; i < actionButtons.Length; i++)
        {
            bool used = i < buttons.Length;
            actionButtons[i].gameObject.SetActive(used);
            if (!used) continue;
            actionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = buttons[i].label;
            Action action = buttons[i].action;
            actionButtons[i].onClick.RemoveAllListeners();
            actionButtons[i].onClick.AddListener(() => Choose(action));
        }
        waitingForChoice = buttons.Length > 0;
        if (showing != null) StopCoroutine(showing);
        showing = StartCoroutine(Slide());
    }

    void Choose(Action action)
    {
        if (unread > 0) unread--;
        Close();
        action?.Invoke();
    }

    void Close()
    {
        if (showing != null) StopCoroutine(showing);
        banner.gameObject.SetActive(false);
        waitingForChoice = false;
        if (stoppedPlayer && player != null) player.enabled = true;
        stoppedPlayer = false;
    }

    IEnumerator Slide()
    {
        banner.gameObject.SetActive(true);
        yield return Move(hiddenY, shownY);
        if (waitingForChoice) yield break; // stays until a button is pressed
        yield return new WaitForSecondsRealtime(autoHideSeconds);
        yield return Move(shownY, hiddenY);
        banner.gameObject.SetActive(false);
    }

    // Real time: the phone and the pause menu stop game time.
    IEnumerator Move(float from, float to)
    {
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / slideSeconds)
        {
            banner.anchoredPosition = new Vector2(0, Mathf.SmoothStep(from, to, t));
            yield return null;
        }
        banner.anchoredPosition = new Vector2(0, to);
    }

    void Update()
    {
        if (phone == null) phone = FindFirstObjectByType<PhoneManager>();
        bool phoneOpen = phone != null && phone.IsOpen;
        if (phoneOpen) unread = 0;
        hotbar.SetActive(!phoneOpen && !WakeUpSequence.Playing && !CityArrival.Playing && !CafeCounter.Waiting && !BedtimeBeat.Playing && !DebriefScreen.IsOpen);
        var progress = GameProgressManager.Instance;
        flashDriveSlot.SetActive(progress != null && progress.hasFlashDrive && !progress.flashDriveInserted);
        laptopSlot.SetActive(progress != null && progress.laptopAt == "hands");
        badge.SetActive(unread > 0);
        badgeText.text = unread.ToString();

        if (!waitingForChoice) return;
        // The buttons need the mouse: stop the player (PauseMenu re-locks the mouse only while they walk).
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        if (player != null && player.enabled) { player.enabled = false; stoppedPlayer = true; }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Called by the Phone slot's OnClick.
    public void OpenPhone()
    {
        if (phone != null) phone.TogglePhone();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    [ContextMenu("Test: plain banner")]
    void TestPlain() => Show("Messages", "Kai: are you awake?");

    [ContextMenu("Test: banner with buttons")]
    void TestButtons() => Show("Messages", "Kai: can you call me?",
        ("Reply", () => Debug.Log("[PhoneNotifications] Reply")), ("Later", null));
}
