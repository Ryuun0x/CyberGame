using System.Collections;
using StarterAssets;
using UnityEngine;

// Chapter 1 wrap-up at the café (on the CafeZone object):
// - an invisible wall in the doorway (the "CafeExitWall" object) keeps the player inside until the thesis is submitted;
// - walking out after submitting: the phone buzzes (a text from Kai's hijacked account), she picks it up (Tab),
//   reads it, narration, fade, debrief.
public class CafeEnding : MonoBehaviour
{
    const string BlockedNarration = "I still need to submit my thesis.";

    [Tooltip("The invisible wall in the café doorway (CafeExitWall). Switched on only while the player is inside and hasn't submitted.")]
    public BoxCollider wall;

    Vector3 doorCenter, inward;
    CharacterController player;
    float nextNag;
    bool ending;

    void Start()
    {
        player = FindFirstObjectByType<CharacterController>();
        var door = FindFirstObjectByType<CafeDoor>();
        var zone = GetComponentInChildren<Collider>();
        if (door == null || zone == null) return;

        // The door starts closed, so its collider spans the doorway; "inward" points into the café.
        var b = door.GetComponent<Collider>().bounds;
        doorCenter = b.center;
        bool facesX = b.size.x < b.size.z;
        inward = facesX ? new Vector3(Mathf.Sign(zone.bounds.center.x - b.center.x), 0, 0)
                        : new Vector3(0, 0, Mathf.Sign(zone.bounds.center.z - b.center.z));
        wall.enabled = false;
    }

    public static bool ExitLocked(GameProgressManager gp) => gp != null && !gp.thesisSubmitted;

    void Update()
    {
        if (wall == null || player == null) return;
        Vector3 offset = player.transform.position - doorCenter;
        offset.y = 0;
        float depth = Vector3.Dot(offset, inward);
        // Switch on only once the player is well inside (never on top of them); it then holds them in.
        if (!ExitLocked(GameProgressManager.Instance) || depth < 0) wall.enabled = false;
        else if (depth > 0.8f) wall.enabled = true;

        bool nearDoor = depth < 1.4f && (offset - inward * depth).magnitude < 1f;
        bool headingOut = Vector3.Dot(player.velocity, inward) < -0.3f;
        if (wall.enabled && nearDoor && headingOut && Time.time > nextNag && NarrationManager.Instance != null)
        {
            NarrationManager.Instance.Show(BlockedNarration, 3f);
            nextNag = Time.time + 4f;
        }
    }

    // CafeZone trigger exit.
    public void PlayerLeft()
    {
        var gp = GameProgressManager.Instance;
        if (ending || gp == null || !gp.thesisSubmitted) return;
        ending = true;
        StartCoroutine(EndChapter());
    }

    IEnumerator EndChapter()
    {
        yield return new WaitForSeconds(1.5f);

        // The phone buzzes: a banner and a badge. She picks it up herself (Tab); after a while it opens anyway.
        const string KaiText = "Bes, emergency. Send ₱1,000 please.";
        PhoneNotifications.Show("Kai", KaiText);
        var phone = FindFirstObjectByType<PhoneManager>();
        var home = phone != null && phone.phoneCanvas != null ? phone.phoneCanvas.GetComponentInChildren<PhoneHomeScreen>(true) : null;
        if (phone != null)
        {
            Hints.ShowUntil("kai-text", "phone", "New message", $"Press {Hints.Cap("TAB")} to check your phone", () => phone.IsOpen, now: true);
            for (float t = 0; t < 12f && !phone.IsOpen; t += Time.unscaledDeltaTime) yield return null;
            if (!phone.IsOpen) phone.TryOpenPhone();
        }
        if (home != null && phone.IsOpen) home.ShowMessage("Kai", KaiText);

        // The phone pauses time, so wait in real time.
        yield return new WaitForSecondsRealtime(3f);
        if (NarrationManager.Instance != null) NarrationManager.Instance.Show("…Kai said not to trust messages from them.", 3f);
        yield return new WaitForSecondsRealtime(3.5f);
        if (NarrationManager.Instance != null) NarrationManager.Instance.Show("I'll deal with this when I get home.", 3f);
        yield return new WaitForSecondsRealtime(3.5f);
        if (phone != null) phone.ClosePhone();

        var controller = FindFirstObjectByType<FirstPersonController>();
        if (controller != null) controller.enabled = false;
        if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeOut();

        var desktop = FindFirstObjectByType<LaptopDesktop>(FindObjectsInactive.Include);
        if (desktop != null) desktop.EndChapter();
    }

}
