using System.Collections;
using StarterAssets;
using UnityEngine;
using static StoryTools;

// Chapter 2, last beat: go to bed, then the epilogue.
//   1. The player walks to the bed and presses E (the BedSleep object in the Interior scene).
//   2. The camera moves to the same bed view as the wake-up scene at the start of the game (lying down).
//   3. Fade out, "Two weeks later...": what the report led to, depending on the player's choices.
//   4. The final results screen.
public static class BedtimeBeat
{
    public static IEnumerator Play(WakeUpSequence wake)
    {
        // 1. Wait for the player to get into bed.
        var bed = Object.FindFirstObjectByType<BedSleep>(FindObjectsInactive.Include);
        bed.gameObject.SetActive(true);
        float nextReminder = Time.unscaledTime + 15;
        while (!bed.Slept)
        {
            if (Time.unscaledTime >= nextReminder)
            {
                Say("I should get some sleep. (Esc to close the laptop, then press E on the bed)", 4f);
                nextReminder = Time.unscaledTime + 15;
            }
            yield return null;
        }
        CompleteObjective(3);

        // 2. Lie down: move the wake-up camera from the player's eyes back to its bed pose.
        var controller = wake.player.GetComponentInChildren<FirstPersonController>();
        if (controller != null) controller.enabled = false;
        foreach (var crosshair in Object.FindObjectsByType<RaycastCrosshair>(FindObjectsSortMode.None))
        {
            crosshair.enabled = false; // no aiming or "Sleep" prompt during the cutscene
            if (crosshair.crosshairDot != null) crosshair.crosshairDot.gameObject.SetActive(false);
            if (crosshair.promptPanel != null) crosshair.promptPanel.gameObject.SetActive(false);
        }
        var bedCamera = wake.cinematicCamera.transform;
        Vector3 bedPosition = bedCamera.position;
        Quaternion bedRotation = bedCamera.rotation;
        Vector3 fromPosition = wake.playerCamera.transform.position;
        Quaternion fromRotation = wake.playerCamera.transform.rotation;
        wake.playerCamera.enabled = false;
        wake.cinematicCamera.enabled = true;
        for (float t = 0; t < 1; t += Time.deltaTime / 2.5f)
        {
            float smooth = Mathf.SmoothStep(0, 1, t);
            bedCamera.SetPositionAndRotation(Vector3.Lerp(fromPosition, bedPosition, smooth), Quaternion.Slerp(fromRotation, bedRotation, smooth));
            yield return null;
        }
        bedCamera.SetPositionAndRotation(bedPosition, bedRotation);
        Say("What a day... At least I reported them.", 3.5f);
        yield return Wait(4f);

        // 3. The epilogue.
        if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeOut();
        yield return Wait(1f);
        Say("Two weeks later...", 3f);
        yield return Wait(3.5f);
        if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeIn();

        int score = ThreatLog.Instance != null ? ThreatLog.Instance.ScorePercent() : 100;
        foreach (var line in EpilogueLines(score))
        {
            Say(line, 4.5f);
            yield return Wait(5f);
        }

        // 4. The final results.
        DebriefScreen.Show("Chapter 2: Ghostline", null, Headline(score));
    }

    // What the report led to: the better the player protected themselves, the stronger their report.
    static string Headline(int score) =>
        score >= 90 ? "Your report helped take Ghostline down."
        : score >= 60 ? "Ghostline was reported and taken down, but not before it cost you."
        : "Your report wasn't enough. Ghostline is still out there.";

    static System.Collections.Generic.List<string> EpilogueLines(int score)
    {
        var lines = new System.Collections.Generic.List<string>
        {
            score >= 90 ? "The police took down Ghostline's accounts. My report connected the fake Wi-Fi, the hacked accounts and the fake shop."
            : score >= 60 ? "Ghostline's accounts were taken down after the reports."
            : "Nothing came of the report. And yesterday a new \"free Wi-Fi\" network showed up at another café..."
        };
        if (score >= 90) lines.Add("Kai finally got their account back.");
        if (Progress.sentMoneyToScammer) lines.Add("Kai never got my ₱1,000. It went straight to Ghostline.");
        if (Progress.paidScamSeller) lines.Add("The calculator from gadget.deals.ph never arrived.");
        if (Progress.accountHijacked) lines.Add("Ghostline used my school account to message my classmates before I got it back.");
        if (Progress.lostFiles) lines.Add("I never got back the files on my old laptop.");
        return lines;
    }
}
