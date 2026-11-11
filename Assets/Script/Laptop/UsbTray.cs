using System.Collections;
using UnityEngine;

/// <summary>
/// The flash drive in the laptop's taskbar, like Windows' "Safely Remove Hardware":
///   - while the flash drive is plugged in (GameProgressManager.flashDriveInserted), a USB icon shows in the taskbar;
///   - clicking it opens a small menu "Eject USB Drive (E:)";
///   - Eject unmounts it and shows "Safe to remove hardware" for a few seconds (then F pulls it out, LaptopUsbPort).
/// Why it matters: a backup only stays safe from malware if it is unplugged (an offline backup).
///
/// On LaptopCanvas (linked prefab). Placed objects, hidden until needed: Taskbar > UsbTrayIcon (calls ToggleMenu),
/// EjectMenu > EjectItem (calls Eject) and EjectedNotice. The last two sit on the canvas itself so windows don't cover them.
/// </summary>
public class UsbTray : MonoBehaviour
{
    [Tooltip("The USB icon in the taskbar (shown only while the flash drive is plugged in).")]
    public GameObject trayIcon;
    [Tooltip("The small \"Eject USB Drive (E:)\" menu above the icon.")]
    public GameObject menu;
    [Tooltip("\"Safe to remove hardware\" notice above the taskbar.")]
    public GameObject ejectedNotice;

    void Start()
    {
        menu.SetActive(false);
        ejectedNotice.SetActive(false);
    }

    void Update()
    {
        bool inserted = GameProgressManager.Instance != null && GameProgressManager.Instance.flashDriveInserted;
        if (trayIcon.activeSelf != inserted) trayIcon.SetActive(inserted);
        if (!inserted && menu.activeSelf) menu.SetActive(false);
    }

    // Called by the taskbar USB icon.
    public void ToggleMenu()
    {
        menu.SetActive(!menu.activeSelf);
        menu.transform.SetAsLastSibling(); // above any open window
    }

    // Called by the menu's "Eject USB Drive (E:)".
    public void Eject()
    {
        menu.SetActive(false);
        if (GameProgressManager.Instance == null || !GameProgressManager.Instance.flashDriveInserted) return;
        if (GetComponent<LaptopDesktop>().Copying) return; // like Windows: the device is still in use
        GameProgressManager.Instance.flashDriveInserted = false; // unmounted; it stays in the port until F (LaptopUsbPort)
        StopAllCoroutines();
        StartCoroutine(ShowEjected());
    }

    IEnumerator ShowEjected()
    {
        ejectedNotice.SetActive(true);
        ejectedNotice.transform.SetAsLastSibling();
        yield return new WaitForSecondsRealtime(3.5f);
        ejectedNotice.SetActive(false);
    }
}
