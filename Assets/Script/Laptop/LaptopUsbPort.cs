using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The laptop's USB port. While the laptop is open and the player has the flash drive, a hint says
/// "Press F to plug it in". F plays a short shot: the camera moves close to the port, the flash drive
/// slides in (its Animator plays FlashDriveInsert when the model is shown), then the camera goes back to the screen.
/// Plugged in = GameProgressManager.flashDriveInserted: the USB icon shows in the taskbar and the Back-up to USB
/// button appears. Ejecting from the taskbar (UsbTray) only unmounts it: the drive stays in the port until F pulls it
/// out (same close-up, the "FlashDriveRemove" state: the insert clip reversed) and it goes back in the pocket.
/// F before ejecting: Alice says to eject it first.
///
/// On the laptop (next to LaptopInteraction). Placed children: "PluggedFlashDrive" (the model in the port, hidden
/// until plugged in) and "PortViewPoint" (where the camera looks at the port from).
/// </summary>
public class LaptopUsbPort : MonoBehaviour
{
    [Tooltip("The laptop screen UI (only active while the player is using the laptop).")]
    public GameObject laptopCanvas;
    public LaptopDesktop desktop;
    [Tooltip("The flash drive model placed in the laptop's USB port (hidden until plugged in).")]
    public GameObject pluggedDrive;

    [Header("Close-up shot")]
    [Tooltip("Where the camera looks at the port from while the flash drive goes in.")]
    public Transform portViewPoint;
    [Tooltip("Seconds to move the camera to the port (and back).")]
    public float moveTime = 0.6f;
    [Tooltip("Seconds to watch the flash drive go in or out (the length of the insert animation).")]
    public float watchTime = 1.1f;

    // True during the close-up: LaptopInteraction ignores Esc until the camera is back at the screen.
    public bool Busy { get; private set; }

    bool wasInserted;

    void Start()
    {
        pluggedDrive.SetActive(Inserted);
        wasInserted = Inserted;
    }

    public bool InPort => pluggedDrive.activeSelf; // still in the port after Eject, until F pulls it out

    bool AtHome { get { var carry = GetComponent<LaptopCarry>(); return carry == null || carry.spotId == "home"; } }

    static bool Typing
    {
        get
        {
            var selected = UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
            var field = selected != null ? selected.GetComponent<TMPro.TMP_InputField>() : null;
            return field != null && field.isFocused;
        }
    }

    static GameProgressManager Progress => GameProgressManager.Instance;
    static bool Inserted => Progress != null && Progress.flashDriveInserted;

    void Update()
    {
        if (Progress == null) return;

        // Only the home laptop: the backup happens at home, and the café laptop never needs the drive.
        bool usingLaptop = laptopCanvas.activeInHierarchy && AtHome;
        // F typed into a text box (a Wi-Fi password, a sign-in) is just a letter.
        bool f = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && !Typing;
        if (usingLaptop && !Busy)
        {
            if (Progress.hasFlashDrive && !InPort)
            {
                Hints.ShowUntil("usb-insert", "usb", "Flash drive", $"Press {Hints.Cap("F")} to plug it into the laptop",
                    () => Inserted || !laptopCanvas.activeInHierarchy, now: true);
                if (f) StartCoroutine(PortShot(PlugIn));
            }
            else if (InPort && !Inserted) // ejected
            {
                Hints.ShowUntil("usb-remove", "usb", "Flash drive", $"Press {Hints.Cap("F")} to unplug it",
                    () => !InPort || !laptopCanvas.activeInHierarchy, now: true);
                if (f) StartCoroutine(PortShot(PullOut));
            }
            else if (InPort && f) // still mounted
            {
                if (NarrationManager.Instance != null)
                    NarrationManager.Instance.Show("I should eject it first, or the files could get corrupted.", 3.5f);
                Hints.ShowUntil("usb-eject", "usb", "Eject first", "Click the USB icon in the taskbar, then Eject",
                    () => !Inserted || !laptopCanvas.activeInHierarchy, now: true);
            }
        }

        // Plugged in (F) or ejected (taskbar): show/hide the Back-up to USB button.
        if (Inserted != wasInserted)
        {
            wasInserted = Inserted;
            desktop.RefreshBackupButton();
        }
    }

    // At the port: show the drive (its Animator plays FlashDriveInsert on enable) and mount it.
    IEnumerator PlugIn()
    {
        pluggedDrive.SetActive(true);
        Progress.flashDriveInserted = true;
        yield return new WaitForSeconds(watchTime);
    }

    // At the port: the drive slides out (FlashDriveRemove = the insert reversed), then it's back in the pocket.
    IEnumerator PullOut()
    {
        pluggedDrive.GetComponent<Animator>().Play("FlashDriveRemove");
        yield return new WaitForSeconds(watchTime);
        pluggedDrive.SetActive(false);
    }

    // Camera to the port → flash drive goes in or out → camera back to the screen.
    IEnumerator PortShot(System.Func<IEnumerator> atPort)
    {
        Busy = true;
        Transform cam = Camera.main.transform;
        Vector3 screenPos = cam.position;
        Quaternion screenRot = cam.rotation;

        yield return MoveCamera(cam, screenPos, screenRot, portViewPoint.position, portViewPoint.rotation);
        yield return atPort();
        yield return MoveCamera(cam, portViewPoint.position, portViewPoint.rotation, screenPos, screenRot);
        Busy = false;
    }

    IEnumerator MoveCamera(Transform cam, Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot)
    {
        for (float t = 0; t < 1; t += Time.deltaTime / moveTime)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            cam.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, s), Quaternion.Slerp(fromRot, toRot, s));
            yield return null;
        }
        cam.SetPositionAndRotation(toPos, toRot);
    }
}
