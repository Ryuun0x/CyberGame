using System.Collections;
using static StoryTools;

// Chapter 2, beat 3: the "browser update" the player downloaded at the café was ransomware.
// Only plays if GameProgressManager.installedFakeUpdate is true, the first time the laptop is opened.
//   Pay ₱5,000         → the attackers ask for more and nothing unlocks (Compromised: never pay).
//   Restore from backup → only if the thesis was backed up to the flash drive in Chapter 1.
//   Wipe the laptop     → files are gone (shown when there is no backup).
// Screen: LaptopCanvas → NightApps → LockedFiles.
public static class LockedFilesBeat
{
    public static IEnumerator Play()
    {
        var laptop = Laptop;
        bool backedUp = Progress.thesisBackedUp;
        bool paid = false;
        laptop.ShowScreen(laptop.lockedFiles);

        while (true)
        {
            laptop.lockedMessage.text = paid
                ? "Payment received. To unlock your files, send another ₱10,000 within 24 hours."
                : "Send ₱5,000 to unlock them. If you don't pay within 24 hours, your files are deleted forever.";
            laptop.payButton.SetActive(!paid);
            laptop.restoreButton.SetActive(backedUp);
            laptop.wipeButton.SetActive(!backedUp);
            laptop.ClearChoice();
            while (laptop.Choice == null) yield return null;
            if (laptop.Choice != "pay") break;
            paid = true;
            Say("I paid... and they just want more?!", 3.5f);
        }

        bool restored = laptop.Choice == "restore";
        Progress.lostFiles = !restored;
        laptop.thesisIcon.SetActive(restored); // the thesis file is gone if there was no backup
        Record("ransomware", !paid);
        Resolve("fake_update");
        ConsequenceToast.Show();

        laptop.payButton.SetActive(false);
        laptop.restoreButton.SetActive(false);
        laptop.wipeButton.SetActive(false);
        laptop.lockedMessage.text = restored
            ? "Laptop wiped. Thesis and documents restored from the flash drive."
            : "Laptop wiped. All files on it are gone.";
        yield return Wait(2.5f);
        Say(restored ? "Good thing I backed everything up to the flash drive." : "Everything's gone... I should have kept a backup.", 4f);
        laptop.ShowDesktop();
    }
}
