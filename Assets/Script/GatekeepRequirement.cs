using UnityEngine;

public class GatekeepRequirement : MonoBehaviour
{
    [Header("Required Progression Flags (ALL must be true)")]
    [Tooltip("Player must have tried to submit thesis (no-internet moment)")]
    public bool requireTriedSubmit = false;

    [Tooltip("Player must have picked up the flash drive")]
    public bool requireHasFlashDrive = false;

    [Tooltip("Player must have backed up thesis to flash drive")]
    public bool requireThesisBackedUp = false;

    [Tooltip("Player must have enabled 2FA on phone")]
    public bool require2FAEnabled = false;

    [Header("Feedback")]
    [TextArea]
    [Tooltip("Narration shown when the player tries to interact while locked")]
    public string blockedNarration = "I can't do that right now.";

    [Tooltip("If set, replaces the prompt text while locked (leave empty to keep default)")]
    public string lockedPromptOverride = "";

    /// <summary>
    /// Returns true only when every required flag is satisfied.
    /// </summary>
    public bool IsAllowed()
    {
        var gp = GameProgressManager.Instance;
        if (gp == null) return false;

        if (requireTriedSubmit    && !gp.triedSubmitWithoutInternet) return false;
        if (requireHasFlashDrive  && !gp.hasFlashDrive)             return false;
        if (requireThesisBackedUp && !gp.thesisBackedUp)            return false;
        if (require2FAEnabled     && !gp.is2FAEnabled)              return false;

        return true;
    }
}
