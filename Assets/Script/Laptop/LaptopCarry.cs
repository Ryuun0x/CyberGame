using System.Collections;
using UnityEngine;

/// <summary>
/// Pick up and put down the laptop (reviewer #8: bring your own laptop to the café).
///   - Hold E on the laptop: the lid closes and the laptop is put away (hidden; the HUD shows "Laptop" while
///     it is carried, CarryIndicator).
///   - While carrying: look at its spot (the desk at home or the café table) and press E (LaptopSpot):
///     it appears there and the lid opens.
/// Where the laptop is lives in GameProgressManager.laptopAt ("home", "hands" or "cafe"), so it is still carried
/// after walking out of the house. Each scene has its own laptop on its spot; it is shown only while
/// laptopAt is that spot. Can't pick it up with the flash drive in the port, or in Chapter 2.
///
/// On the Laptop (next to LaptopInteraction). Placed: the lid's hinge ("ScreenHinge", with the screen UI
/// LaptopCanvas inside it, so the UI follows the laptop) and the spot (a box collider with LaptopSpot, hidden
/// until the laptop is carried; moved under the laptop on start, so only the laptop needs placing).
/// </summary>
public class LaptopCarry : MonoBehaviour, IHoldInteractable
{
    [Tooltip("\"home\" (the desk in Interior) or \"cafe\" (the café table in the city): where this laptop sits.")]
    public string spotId = "home";
    [Tooltip("The table top the player looks at to put the laptop down (has LaptopSpot).")]
    public GameObject spot;
    [Tooltip("The lid's hinge. Its rotation in the scene is the open lid.")]
    public Transform lid;
    [Tooltip("Degrees the lid turns (around the hinge's z axis) to close.")]
    public float closeAngle = -88f;
    public float lidSeconds = 0.4f;

    public bool Busy { get; private set; }

    Quaternion lidOpen;
    Renderer[] renderers;
    Collider[] colliders;

    static GameProgressManager Progress => GameProgressManager.Instance;

    void Awake()
    {
        lidOpen = lid.localRotation;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        PlaceSpotUnderLaptop();
    }

    void Start()
    {
        if (Progress == null) return;
        if (spotId == "home" && Progress.chapter >= 2) Progress.laptopAt = "home"; // Chapter 2 starts at home
        if (spotId == "cafe" && !Progress.wakeUpPlayed) Progress.laptopAt = "cafe"; // city scene started directly (testing)

        if (Progress.laptopAt != spotId) Show(false); // carried, or on the other scene's spot
        spot.SetActive(Progress.laptopAt == "hands");
    }

    void Update()
    {
        // Once the player can leave the house, tell them how to take the laptop along.
        if (spotId == "home" && Progress != null && Progress.chapter < 2 && Progress.laptopAt == "home"
            && Progress.triedSubmitWithoutInternet && Progress.hasFlashDrive)
            Hints.Show("laptop-pickup", "laptop", "Take your laptop", $"Hold {Hints.Cap("E")} on the laptop to pick it up",
                UnityEngine.InputSystem.Key.E);
    }

    Quaternion Closed => lidOpen * Quaternion.Euler(0f, 0f, closeAngle);

    // IHoldInteractable: hold E on the laptop.
    public bool CanHold => !Busy && Progress != null && Progress.chapter < 2 && Progress.laptopAt == spotId;
    public string HoldPrompt => "Pick up";
    public void HoldInteract()
    {
        var port = GetComponent<LaptopUsbPort>();
        if (port != null && port.InPort)
        {
            if (NarrationManager.Instance != null)
                NarrationManager.Instance.Show("I should eject and unplug the flash drive first.", 3f);
            return;
        }
        StartCoroutine(PickUp());
    }

    IEnumerator PickUp()
    {
        Busy = true;
        yield return TurnLid(lidOpen, Closed);
        Show(false);
        Progress.laptopAt = "hands";
        spot.SetActive(true);
        Hints.Show("laptop-place", "laptop", "Laptop", $"Look at a table and press {Hints.Cap("E")} to put it down",
            UnityEngine.InputSystem.Key.E);
        Busy = false;
    }

    // Called by LaptopSpot (E on the table while carrying).
    public void PutDown()
    {
        if (!Busy && Progress != null && Progress.laptopAt == "hands") StartCoroutine(Place());
    }

    IEnumerator Place()
    {
        Busy = true;
        spot.SetActive(false);
        lid.localRotation = Closed;
        Show(true);
        Progress.laptopAt = spotId;
        yield return TurnLid(Closed, lidOpen);
        Busy = false;
    }

    IEnumerator TurnLid(Quaternion from, Quaternion to)
    {
        for (float t = 0; t < 1; t += Time.deltaTime / lidSeconds)
        {
            lid.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        lid.localRotation = to;
    }

    // The spot goes under the laptop's base (not the lid), wherever the laptop was put in the scene,
    // so moving the laptop and its table in the editor is enough.
    void PlaceSpotUnderLaptop()
    {
        Bounds b = new Bounds(transform.position, Vector3.zero);
        foreach (var c in colliders)
            if (!c.transform.IsChildOf(lid)) b.Encapsulate(c.bounds);
        spot.transform.SetPositionAndRotation(new Vector3(b.center.x, b.min.y + 0.01f, b.center.z),
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
    }

    // Hidden while carried: no renderers, and no colliders for the crosshair or the player to hit.
    void Show(bool on)
    {
        foreach (var r in renderers) r.enabled = on;
        foreach (var c in colliders) c.enabled = on;
    }
}
