using System.Collections;
using UnityEngine;
using static StoryTools;

// Chapter 2: Ghostline. The night after the café, in the bedroom (the Interior scene at night).
// WakeUpSequence adds this component when GameProgressManager.chapter == 2.
//
// This file only holds the ORDER of the story. Each beat lives in its own file:
//   1. StolenLoginBeat  - only if the school login was typed into the fake café Wi-Fi
//   2. KaiMessageBeat   - Kai's hacked account asks for money
//   3. LockedFilesBeat  - only if the fake browser update was installed at the café
//   4. ShopHubBeat      - in the laptop browser: sign up, buy a calculator, maybe meet the scam seller
//   5. FinaleBeat       - report Ghostline (the page only confirms it was received)
//   6. BedtimeBeat      - go to bed, the epilogue ("Two weeks later..."), the final results
public class Chapter2 : MonoBehaviour
{
    // Standing at the desk, facing the laptop.
    static readonly Vector3 DeskSpot = new Vector3(-10.0f, -4.28f, 2.78f);
    const float DeskYaw = 270;

    public void Begin(GameObject player)
    {
        var controller = player.GetComponentInChildren<CharacterController>();
        if (controller != null) controller.enabled = false;
        var body = controller != null ? controller.transform : player.transform;
        body.SetPositionAndRotation(DeskSpot, Quaternion.Euler(0, DeskYaw, 0));
        if (controller != null) controller.enabled = true;
        Night();
        StartCoroutine(Story());
    }

    IEnumerator Story()
    {
        yield return Wait(1.5f);
        Say("It's late... I can't sleep. Big exam tomorrow too.", 3.5f);
        yield return Wait(4.5f);

        // Beat 1: the stolen school login from the fake café Wi-Fi.
        if (Progress.credentialsStolen && !Logged("login_alert"))
        {
            yield return StolenLoginBeat.Play();
            yield return Wait(4f);
        }

        // Beat 2: Kai's hacked account.
        yield return KaiMessageBeat.Play();
        CompleteObjective(0);
        yield return Wait(4f); // let the last line of Kai's beat finish
        Say("Right, the exam. I still need a scientific calculator... I'll look for one on my laptop.", 4.5f);

        // Beats 3 and 4 happen on the laptop: open it, then Browser > ShopHub.
        yield return WaitFor(() => LaptopIsOpen, "I should buy that calculator. My laptop's on the desk.");
        if (Progress.installedFakeUpdate) yield return LockedFilesBeat.Play();
        Laptop.ClearChoice();
        yield return WaitFor(() => Laptop.Choice == "shophub", "On the laptop: open the Browser, then click ShopHub.");
        yield return ShopHubBeat.PlaySignUpAndShop();

        // The phone can't open while the laptop is in use, so the password leak waits for the player to
        // close it (and for the camera to finish moving back, until then the player can't move).
        if (Progress.reusedPassword)
        {
            Say("My phone's buzzing... (press Esc to close the laptop)", 4f);
            yield return WaitFor(() => !LaptopIsOpen && PlayerCanMove, "My phone's buzzing... (press Esc to close the laptop)");
            yield return ShopHubBeat.PasswordLeak();
            yield return Wait(4f);
        }

        // Beat 5: the finale. The report bookmark appears in the browser.
        Say(Progress.sawGhostlineSeller
            ? "The ghost logo again... Ghostline is behind all of this. I should report them."
            : "The fake Wi-Fi, Kai's hacked account... that stranger with the Ghostline logo. I should report them.", 5f);
        Laptop.hotlineBookmark.SetActive(true);
        Laptop.ClearChoice();
        yield return WaitFor(() => Laptop.Choice == "hotline", "I need to report Ghostline: laptop > Browser > Report a Cybercrime.");
        yield return FinaleBeat.Play();

        // Last beat: go to bed, then the epilogue ("Two weeks later...") and the final results.
        yield return Wait(4f); // let "Done. I hope they actually catch them." finish
        Say("It's really late. I should get some sleep. (Esc to close the laptop, then press E on the bed)", 5f);
        yield return BedtimeBeat.Play(GetComponent<WakeUpSequence>());
    }

    // Waits until done() is true. If the player takes a while, the reminder line repeats every
    // 15 seconds so they always know what to do next.
    static IEnumerator WaitFor(System.Func<bool> done, string reminder)
    {
        const float ReminderEvery = 15;
        float nextReminder = Time.unscaledTime + ReminderEvery;
        while (!done())
        {
            if (Time.unscaledTime >= nextReminder)
            {
                Say(reminder, 4f);
                nextReminder = Time.unscaledTime + ReminderEvery;
            }
            yield return null;
        }
    }

    // Dark sky and windows, moonlight, ceiling lights off; only the desk lamp stays on.
    static void Night()
    {
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional) { light.intensity = 0.08f; light.color = new Color(0.55f, 0.62f, 0.85f); }
            else if (light.type == LightType.Spot) light.enabled = false;
            else if (light.type == LightType.Point) light.intensity = Mathf.Max(light.intensity, 2f);
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.11f, 0.17f);
        RenderSettings.skybox = null;
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
        }
    }
}
