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

    public static IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) yield return null;
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

    // Opens the phone and locks it open (Tab/Esc can't close it during a story moment).
    public static PhoneStory OpenPhone()
    {
        phone = Object.FindFirstObjectByType<PhoneManager>();
        phone.TryOpenPhone();
        phone.Locked = true;
        return phone.phoneCanvas.GetComponentInChildren<PhoneStory>(true);
    }

    public static void ClosePhone()
    {
        if (phone == null) return;
        phone.Locked = false;
        phone.ClosePhone();
    }

    // The phone's home screen script (it owns the Messages screen).
    public static PhoneHomeScreen PhoneHome =>
        Object.FindFirstObjectByType<PhoneManager>().phoneCanvas.GetComponentInChildren<PhoneHomeScreen>(true);

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
