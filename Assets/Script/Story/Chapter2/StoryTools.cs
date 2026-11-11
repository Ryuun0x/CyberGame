using System.Collections;
using UnityEngine;

// Shared helpers for the Chapter 2 beats, so each beat file only contains its own story.
//   Say       - show a narration line
//   Record    - log a choice in ThreatLog (safe = true / false)
//   Wait      - wait in real time (opening the phone pauses the game, so normal waits would freeze)
//   OpenPhone - open the phone and keep it open until the beat closes it; returns its story screens
//   Laptop    - the laptop's night screens (LaptopNight on the LaptopCanvas)
public static class StoryTools
{
    public static GameProgressManager Progress => GameProgressManager.Instance;

    public static void Say(string line, float seconds)
    {
        if (NarrationManager.Instance != null) NarrationManager.Instance.Show(line, seconds);
    }

    public static void Record(string id, bool safe)
    {
        if (ThreatLog.Instance != null) ThreatLog.Instance.Record(id, safe);
    }

    // The player made a mistake first but fixed it: the next Record(id, true) counts as Recovered.
    public static void Exposed(string id)
    {
        if (ThreatLog.Instance != null) ThreatLog.Instance.MarkExposed(id);
    }

    // A consequence from Chapter 1 has now happened, so it counts in the score again.
    public static void Resolve(string id)
    {
        if (ThreatLog.Instance != null) ThreatLog.Instance.Resolve(id);
    }

    public static bool Logged(string id) => ThreatLog.Instance != null && ThreatLog.Instance.HasRecord(id);

    public static void CompleteObjective(int index)
    {
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.CompleteSubTask(index, 0);
    }

    // Real-time wait that stands still while the pause menu is open.
    public static IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds; t += PauseMenu.IsPaused ? 0 : Time.unscaledDeltaTime) yield return null;
    }

    public static bool PlayerCanMove
    {
        get
        {
            var controller = Object.FindFirstObjectByType<StarterAssets.FirstPersonController>();
            return controller != null && controller.enabled;
        }
    }

    // ── Phone ──

    static PhoneManager phone;

    // The phone buzzes (a notification banner and a badge) and the story waits until she picks it up herself
    // (Tab). It never opens on its own; ignoring it, she mentions it now and then. Once open it is locked open
    // (Tab/Esc can't close it) for the story moment. Then use StoryPhone.
    public static IEnumerator PickUpPhone(string app, string preview)
    {
        phone = Object.FindFirstObjectByType<PhoneManager>();
        PhoneNotifications.Show(app, preview);
        Hints.ShowUntil("phone-check", "phone", "New notification", $"Press {Hints.Cap("TAB")} to check your phone",
            () => phone.IsOpen, now: true);
        float nextReminder = Time.time + 20f;
        while (!phone.IsOpen)
        {
            if (Time.time > nextReminder && PlayerCanMove)
            {
                Say("My phone keeps buzzing... (Tab)", 3f);
                nextReminder = Time.time + 20f;
            }
            yield return null;
        }
        phone.Locked = true;
    }

    public static PhoneStory StoryPhone => phone.phoneCanvas.GetComponentInChildren<PhoneStory>(true);

    public static void ClosePhone()
    {
        if (phone == null) return;
        phone.Locked = false;
        phone.ClosePhone();
    }

    // The phone's home screen script (it owns the Messages screen).
    public static PhoneHomeScreen PhoneHome =>
        Object.FindFirstObjectByType<PhoneManager>(FindObjectsInactive.Include).phoneCanvas.GetComponentInChildren<PhoneHomeScreen>(true);

    // ── Laptop ──

    public static LaptopNight Laptop => Object.FindFirstObjectByType<LaptopNight>(FindObjectsInactive.Include);

    public static bool LaptopIsOpen => Laptop != null && Laptop.gameObject.activeInHierarchy;

    // Waits until a laptop button with this id is pressed.
    public static IEnumerator WaitForLaptopChoice(params string[] ids)
    {
        var laptop = Laptop;
        while (System.Array.IndexOf(ids, laptop.Choice) < 0) yield return null;
    }
}
