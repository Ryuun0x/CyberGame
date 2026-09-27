using System.Collections;
using StarterAssets;
using UnityEngine;

// Chapter 1 wrap-up at the café (added by CafeZone):
// - an invisible wall in the doorway keeps the player inside until the thesis is submitted;
// - spawns the stranger at the next table (CafeStranger);
// - walking out after submitting: a scam text from Kai's hijacked account, narration, fade, debrief.
public class CafeEnding : MonoBehaviour
{
    const string BlockedNarration = "I still need to submit my thesis.";

    BoxCollider wall;
    Vector3 doorCenter, inward;
    CharacterController player;
    float nextNag;
    bool ending;

    void Start()
    {
        CafeStranger.Spawn(FindFirstObjectByType<CityLife>());
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
        // Just inside the frame, clear of the door leaf (it swings outward).
        var go = new GameObject("CafeExitWall");
        go.transform.position = doorCenter + inward * 0.25f;
        wall = go.AddComponent<BoxCollider>();
        wall.size = facesX ? new Vector3(0.2f, b.size.y + 0.5f, b.size.z + 0.4f) : new Vector3(b.size.x + 0.4f, b.size.y + 0.5f, 0.2f);
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

        // View-only text on the real phone. The phone pauses time, so wait in real time.
        var phone = FindFirstObjectByType<PhoneManager>();
        var home = phone != null && phone.phoneCanvas != null ? phone.phoneCanvas.GetComponentInChildren<PhoneHomeScreen>(true) : null;
        if (home != null && phone.TryOpenPhone()) home.ShowMessage("Kai", "Bes, emergency. Send ₱1,000 please.");
        yield return new WaitForSecondsRealtime(3f);
        if (NarrationManager.Instance != null) NarrationManager.Instance.Show("…Kai said not to trust messages from them.", 3f);
        yield return new WaitForSecondsRealtime(4f);
        if (phone != null) phone.ClosePhone();

        var controller = FindFirstObjectByType<FirstPersonController>();
        if (controller != null) controller.enabled = false;
        if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeOut();

        var desktop = FindFirstObjectByType<LaptopDesktop>(FindObjectsInactive.Include);
        if (desktop != null) desktop.EndChapter();
    }
}
