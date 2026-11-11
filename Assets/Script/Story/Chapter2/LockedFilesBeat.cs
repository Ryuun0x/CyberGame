using System.Collections;
using UnityEngine;
using static StoryTools;

// Chapter 2, beat 3: the "browser update" the player ran at the café was ransomware.
// Only plays if GameProgressManager.installedFakeUpdate is true, the first time the laptop is opened.
//   Pay ₱5,000 → the attackers ask for more and nothing unlocks (never pay).
//   Reset the laptop → "Resetting this PC" (reinstall, ~10 s) → a fresh desktop without the thesis →
//   "Restore your files": from the USB drive (backed up in Chapter 1) or Goggle Drive (saved there), whichever
//   exists. No backup → the thesis is gone.
// Screens: LaptopCanvas → NightApps → LockedFiles, ResetScreen, RestoreWindow.
public static class LockedFilesBeat
{
    public static IEnumerator Play()
    {
        var laptop = Laptop;
        bool paid = false;
        laptop.ShowScreen(laptop.lockedFiles);

        while (true)
        {
            laptop.lockedMessage.text = paid
                ? "Payment received. To unlock your files, send another ₱10,000 within 24 hours."
                : "Send ₱5,000 to unlock them. If you don't pay within 24 hours, your files are deleted forever.";
            laptop.payButton.SetActive(!paid);
            laptop.restoreButton.SetActive(false); // restoring comes after the reset now
            laptop.wipeButton.SetActive(true);
            laptop.ClearChoice();
            while (laptop.Choice == null) yield return null;
            if (laptop.Choice != "pay") break;
            paid = true;
            Say("I paid... and they just want more?!", 3.5f);
        }
        Record("ransomware", !paid);
        Resolve("fake_update");

        yield return ResetLaptop(laptop);
        yield return RestoreFiles(laptop);
    }

    // "Resetting this PC": erases everything and reinstalls the system and the apps.
    static IEnumerator ResetLaptop(LaptopNight laptop)
    {
        laptop.thesisIcon.SetActive(false);
        laptop.ShowScreen(laptop.resetScreen);
        var steps = new (string text, float seconds)[]
        {
            ("Resetting this PC", 3.5f),
            ("Installing Windows", 3.5f),
            ("Installing apps", 2f),
        };
        foreach (var (text, seconds) in steps)
            for (float t = 0; t < 1; t += Time.deltaTime / seconds)
            {
                laptop.resetText.text = $"{text}  {Mathf.FloorToInt(t * 100)}%\n<size=60%>Don't turn off your PC</size>";
                Spin(laptop);
                yield return null;
            }
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            laptop.resetText.text = "Getting things ready";
            Spin(laptop);
            yield return null;
        }
        laptop.ShowDesktop(); // fresh desktop: the apps are back, the thesis is not
        yield return Wait(0.8f);
    }

    static void Spin(LaptopNight laptop) => laptop.resetSpinner.Rotate(0, 0, -240f * Time.deltaTime);

    // "Restore your files": from whichever backup exists. The thesis icon comes back once copied.
    static IEnumerator RestoreFiles(LaptopNight laptop)
    {
        bool usb = Progress.thesisBackedUp, cloud = Progress.thesisInCloud;
        laptop.ShowScreen(laptop.restoreWindow);
        laptop.restoreProgress.SetActive(false);
        laptop.restoreMessage.text = usb || cloud
            ? "Your laptop was reset and your files were erased. Restore your thesis from a backup:"
            : "Your laptop was reset and your files were erased. No backups were found.";
        laptop.usbButton.gameObject.SetActive(usb || cloud);
        laptop.cloudButton.gameObject.SetActive(usb || cloud);
        laptop.usbButton.interactable = usb;
        laptop.cloudButton.interactable = cloud;
        laptop.usbLabel.text = usb ? "USB Drive (E:)" : "USB Drive (E:)\n<size=70%>No backup found</size>";
        laptop.cloudLabel.text = cloud ? "Goggle Drive" : "Goggle Drive\n<size=70%>No backup found</size>";
        laptop.noBackupButton.gameObject.SetActive(!usb && !cloud);

        laptop.ClearChoice();
        while (laptop.Choice == null) yield return null;
        bool fromUsb = laptop.Choice == "usb";
        bool restored = fromUsb || laptop.Choice == "cloud";
        string from = fromUsb ? "USB Drive (E:)" : "Goggle Drive";
        Progress.lostFiles = !restored;

        if (restored)
        {
            laptop.usbButton.gameObject.SetActive(false);
            laptop.cloudButton.gameObject.SetActive(false);
            laptop.restoreProgress.SetActive(true);
            for (float t = 0; t < 1; t += Time.deltaTime / 3f)
            {
                laptop.restoreMessage.text = $"Copying Thesis_Final.pdf from {from}...  {Mathf.FloorToInt(t * 100)}%";
                laptop.restoreFill.anchorMax = new Vector2(t, 1);
                yield return null;
            }
            laptop.restoreFill.anchorMax = Vector2.one;
            laptop.restoreMessage.text = $"Thesis_Final.pdf restored from {from}.";
            laptop.thesisIcon.SetActive(true);
            yield return Wait(1.5f);
        }
        laptop.ShowDesktop();
        ConsequenceToast.Show();
        Say(restored ? $"Good thing I had a backup on {(fromUsb ? "the flash drive" : "my Drive")}."
                     : "Everything's gone... I should have kept a backup.", 4f);
    }
}
