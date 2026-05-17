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
    public bool wakeUpPlayed = false;
    public bool hasFlashDrive = false;
    public bool thesisBackedUp = false;
    public bool is2FAEnabled = false;
    public bool triedSubmitWithoutInternet = false;
    public bool arrivedAtCafe = false;
    public bool connectedToCafeWiFi = false;

    // ── Events (other scripts subscribe to these) ──────────
    public event Action OnFlashDrivePickedUp;
    public event Action OnThesisBackedUp;
    public event Action On2FAEnabled;
    public event Action OnTriedSubmit;
    public event Action OnArrivedAtCafe;
    public event Action OnConnectedToCafeWiFi;

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

    public void ConnectToCafeWiFi()
    {
        if (connectedToCafeWiFi) return; // already tracked
        
        connectedToCafeWiFi = true;
        Debug.Log("[GameProgress] Connected to the real cafe WiFi!");
        OnConnectedToCafeWiFi?.Invoke();
    }
}
