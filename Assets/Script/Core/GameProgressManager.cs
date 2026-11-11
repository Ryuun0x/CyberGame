using UnityEngine;
using System;

/// <summary>
/// Central singleton that tracks all game progression flags.
/// Attach this to a persistent GameObject in your first scene.
/// Other scripts read/write flags through GameProgressManager.Instance.
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance;

    // ── Progression Flags ──────────────────────────────────
    [Header("Progression State (read-only in Inspector)")]
    public int chapter = 1;                  // 2 = the night after the café (Interior at night)
    public bool wakeUpPlayed = false;
    public bool cityIntroPlayed = false;     // the first-arrival shot over the city was shown (CityArrival)
    public bool hasFlashDrive = false;
    public bool flashDriveInserted = false;  // plugged into the laptop's USB port (eject it from the taskbar)
    public string laptopAt = "home";         // where the laptop is: "home" desk, "hands" (carried) or "cafe" table (LaptopCarry)
    public bool thesisBackedUp = false;
    public bool thesisInCloud = false;       // a copy was saved to the school Drive (only possible online, at the café)
    public bool is2FAEnabled = false;
    public bool mailUnread = false;          // the school's 2FA email arrived (after the backup) and wasn't opened yet
    public bool newsUnread = false;          // the news article (arrives when the school's email is read) wasn't opened yet
    public bool triedSubmitWithoutInternet = false;
    public bool arrivedAtCafe = false;
    public bool connectedToCafeWiFi = false;
    public bool thesisSubmitted = false;     // uploaded from the café; unlocks the café exit and the chapter ending
    public bool hasReceipt = false;          // ordered at the café counter; receipt has the real Wi-Fi password
    public bool credentialsStolen = false;   // typed Goggle login into the evil twin's sign-in page
    public string schoolEmail = "";          // the email typed into that fake sign-in (the mail then sends "from" it)
    // The student's email everywhere else (ShopHub sign-up): what she typed on the fake Wi-Fi page, or her usual address.
    public const string DefaultEmail = "alice.santos@goggle.com";
    public string Email => string.IsNullOrWhiteSpace(schoolEmail) ? DefaultEmail : schoolEmail;
    public bool installedFakeUpdate = false; // clicked Download on the fake browser update; Chapter 2 locks the files
    public bool accountHijacked = false;     // Chapter 2: the stolen login (credentialsStolen) gets used; set by how that plays out
    public bool sentMoneyToScammer = false;  // Chapter 2: sent ₱1,000 to Kai's hijacked account
    public bool reusedPassword = false;      // Chapter 2: used the Goggle password for ShopHub; it leaks later
    public bool paidScamSeller = false;      // Chapter 2: paid gadget.deals.ph upfront; the calculator never comes
    public bool sawGhostlineSeller = false;  // Chapter 2: opened the scam seller's chat and saw the Ghostline logo (a clue)
    public bool lostFiles = false;           // Chapter 2: wiped the laptop with no backup; the thesis icon is gone

    // ── Events (other scripts subscribe to these) ──────────
    public event Action OnFlashDrivePickedUp;
    public event Action OnThesisBackedUp;
    public event Action On2FAEnabled;
    public event Action OnTriedSubmit;
    public event Action OnArrivedAtCafe;
    public event Action OnConnectedToCafeWiFi;
    public event Action OnThesisSubmitted;

    void Awake()
    {
        // Singleton pattern — survives scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public Methods (call these from other scripts) ─────

    public void PickUpFlashDrive()
    {
        if (hasFlashDrive) return; // already picked up

        hasFlashDrive = true;
        Debug.Log("[GameProgress] Flash drive picked up!");
        OnFlashDrivePickedUp?.Invoke();
    }

    public void BackupThesis()
    {
        if (!hasFlashDrive)
        {
            Debug.LogWarning("[GameProgress] Cannot backup — no flash drive!");
            return;
        }
        if (thesisBackedUp) return; // already backed up

        thesisBackedUp = true;
        Debug.Log("[GameProgress] Thesis backed up to flash drive!");
        OnThesisBackedUp?.Invoke();
    }

    public void Enable2FA()
    {
        if (is2FAEnabled) return; // already enabled

        is2FAEnabled = true;
        Debug.Log("[GameProgress] 2FA enabled on phone!");
        On2FAEnabled?.Invoke();
    }

    public void MarkTriedSubmit()
    {
        if (triedSubmitWithoutInternet) return; // already tracked

        triedSubmitWithoutInternet = true;
        Debug.Log("[GameProgress] Player tried to submit without internet.");
        OnTriedSubmit?.Invoke();
    }

    public void ArriveAtCafe()
    {
        if (arrivedAtCafe) return; // already tracked
        
        arrivedAtCafe = true;
        Debug.Log("[GameProgress] Arrived at the cafe!");
        OnArrivedAtCafe?.Invoke();
    }

    public void OrderDrink()
    {
        if (hasReceipt) return;

        hasReceipt = true;
        Debug.Log("[GameProgress] Ordered a drink; got the Wi-Fi receipt.");
    }

    public void ConnectToCafeWiFi()
    {
        if (connectedToCafeWiFi) return; // already tracked
        
        connectedToCafeWiFi = true;
        Debug.Log("[GameProgress] Connected to cafe WiFi.");
        OnConnectedToCafeWiFi?.Invoke();
    }

    public void SubmitThesis()
    {
        if (thesisSubmitted) return;

        thesisSubmitted = true;
        Debug.Log("[GameProgress] Thesis submitted.");
        OnThesisSubmitted?.Invoke();
    }
}
