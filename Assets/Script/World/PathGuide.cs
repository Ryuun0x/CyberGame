using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Assist option (Settings > Controls > Path guide), like The Last of Us Part II's navigation assistance arrow:
/// one white arrowhead lying on the floor a few metres in front of her, turning to point along the way to go
/// (the baked NavMesh path; each game scene has a NavMeshSurface). If the way is behind her it points back.
///   Follows the objective on screen. Chapter 1: laptop, flash drive, laptop, front door, café, counter, laptop.
///   Chapter 2: laptop (calculator, report), then the bed.
/// On the Player prefab. The arrow is a flat sprite made while playing.
/// </summary>
public class PathGuide : MonoBehaviour
{
    public Sprite arrow;
    public Color color = new Color(1f, 1f, 1f, 0.9f);
    [Tooltip("Where the arrow sits: this many degrees below the middle of the screen (2-8 m ahead on the floor).")]
    public float belowCenter = 11f, minAhead = 2f, maxAhead = 8f;
    [Tooltip("How far along the path it aims (metres).")]
    public float lookAhead = 4f;
    [Tooltip("Arrow size at 4 m (it grows with distance, so it stays the same size on screen).")]
    public float arrowSize = 0.55f, heightAboveFloor = 0.04f;
    [Tooltip("Seen flatter than this (degrees), the arrow tilts up toward the camera so it stays readable.")]
    public float minViewAngle = 55f;
    [Tooltip("The arrow hides this close to the goal (metres).")]
    public float arriveDistance = 2.5f;

    NavMeshPath path;
    FirstPersonController controller;
    SpriteRenderer sprite;
    Vector3 aim;
    bool found;
    float nextRefresh, alpha;

    void Awake()
    {
        path = new NavMeshPath();
        controller = GetComponent<FirstPersonController>();
        sprite = new GameObject("PathGuideArrow").AddComponent<SpriteRenderer>();
        sprite.sprite = arrow;
        sprite.transform.localScale = Vector3.one * arrowSize;
        sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sprite.receiveShadows = false;
        sprite.gameObject.SetActive(false);
    }

    void Update()
    {
        // Only while she can walk: not in cutscenes, at the laptop or phone, or paused.
        bool want = GameSettings.PathGuide && controller != null && controller.enabled && !PauseMenu.IsPaused;
        if (want && Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + 0.25f;
            found = Refresh();
        }
        alpha = Mathf.MoveTowards(alpha, want && found ? 1 : 0, Time.unscaledDeltaTime * 4f);
        Camera cam = Camera.main;
        sprite.gameObject.SetActive(alpha > 0 && cam != null);
        if (!sprite.gameObject.activeSelf) return;

        // On the floor in front of her, a little below the middle of the screen (closer when she looks down).
        Vector3 forward = cam.transform.forward;
        float down = Mathf.Clamp(Mathf.Asin(-forward.y) * Mathf.Rad2Deg + belowCenter, 5f, 80f);
        float eye = Mathf.Max(0.5f, cam.transform.position.y - transform.position.y);
        float ahead = Mathf.Clamp(eye / Mathf.Tan(down * Mathf.Deg2Rad), minAhead, maxAhead);
        forward.y = 0;
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;
        // Something in the way (a wall, a railing): bring the arrow in front of it.
        Vector3 eyeToSpot = transform.position + forward.normalized * ahead - cam.transform.position;
        if (Physics.Raycast(cam.transform.position, eyeToSpot, out var hit, eyeToSpot.magnitude, ~(1 << 8), QueryTriggerInteraction.Ignore))
            ahead = Mathf.Max(minAhead, ahead * hit.distance / eyeToSpot.magnitude - 0.4f);
        Vector3 spot = transform.position + forward.normalized * ahead;
        spot.y = NavMesh.SamplePosition(spot, out var floor, 1.5f, NavMesh.AllAreas) ? floor.position.y : transform.position.y;

        // Tilt up toward the camera when seen too flat; the tip points along the way.
        float tilt = Mathf.Max(0, minViewAngle - Mathf.Atan2(eye, ahead) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        Vector3 normal = Vector3.up * Mathf.Cos(tilt) - forward.normalized * Mathf.Sin(tilt);
        Vector3 direction = aim - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.01f) direction = forward;
        var target = Quaternion.LookRotation(-normal, Vector3.ProjectOnPlane(direction, normal));
        bool appearing = alpha < 0.05f;
        float size = arrowSize * ahead / 4f;
        sprite.transform.localScale = Vector3.one * size;
        sprite.transform.position = spot + Vector3.up * (heightAboveFloor + Mathf.Sin(tilt) * size * 0.64f);
        sprite.transform.rotation = appearing ? target : Quaternion.Slerp(sprite.transform.rotation, target, 1 - Mathf.Exp(-10f * Time.deltaTime));
        Color c = color;
        c.a *= alpha;
        sprite.color = c;
    }

    // Finds the path to the goal and the point "lookAhead" metres along it. False = nothing to show.
    bool Refresh()
    {
        Vector3? target = Target();
        // The goal is looked up at her own floor height, so a table top or a roof above it isn't picked instead.
        if (target == null
            || !NavMesh.SamplePosition(transform.position, out var from, 2f, NavMesh.AllAreas)
            || !NavMesh.SamplePosition(new Vector3(target.Value.x, transform.position.y, target.Value.z), out var to, 3f, NavMesh.AllAreas)
            || !NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)
            || path.corners.Length < 2)
            return false;

        Vector3[] corners = path.corners;
        float total = 0;
        for (int i = 0; i < corners.Length - 1; i++) total += Vector3.Distance(corners[i], corners[i + 1]);
        if (total < arriveDistance) return false;

        aim = corners[corners.Length - 1];
        float walked = 0;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            float length = Vector3.Distance(corners[i], corners[i + 1]);
            if (walked + length >= lookAhead)
            {
                aim = Vector3.Lerp(corners[i], corners[i + 1], (lookAhead - walked) / length);
                break;
            }
            walked += length;
        }
        return true;
    }

    // Where the objective on screen sends her (null = no guide). Chapter 1 only.
    static Vector3? Target()
    {
        var progress = GameProgressManager.Instance;
        var objectives = ObjectiveManager.Instance;
        if (progress == null || objectives == null) return null;
        if (progress.chapter == 2)
            switch (objectives.Current)
            {
                case 1: // Exam Tomorrow: buy the calculator on the laptop
                case 2: // Report Ghostline: on the laptop
                    return At<LaptopCarry>();
                case 3: return At<BedSleep>(); // Get Some Sleep
                default: return null;          // Late Night: the phone, nowhere to walk
            }
        if (progress.chapter != 1) return null;
        bool city = FindFirstObjectByType<CafeZone>() != null;
        switch (objectives.Current)
        {
            case 0: // Morning Routine: try to submit at the laptop
            case 2: // Secure Your Work: back up at the laptop
                return city ? null : At<LaptopCarry>();
            // While a finished objective plays its tick animation (~3 s), already point at the next goal.
            case 1: return progress.hasFlashDrive ? At<LaptopCarry>() : At<FlashDrivePickup>(); // No Connection
            case 3: // Time to Go: take the laptop, then the front door; in the city, the café's entrance
                if (city) return CafeEntrance();
                return progress.laptopAt == "hands" ? At<ExitEntrance>() : At<LaptopCarry>();
            case 4: return progress.connectedToCafeWiFi ? At<LaptopCarry>() : At<CafeCounter>(); // Get Connected: the counter
            case 5: return At<LaptopCarry>();  // Submit Thesis: the laptop on the café table
            default: return null;              // not shown yet, or Head Out (the door is right there)
        }
    }

    // Just outside the café's front door, on the street side (away from the middle of the café), so the guide
    // leads to the way in and not to the nearest wall at the back.
    static Vector3? CafeEntrance()
    {
        var zone = FindFirstObjectByType<CafeZone>();
        var door = FindFirstObjectByType<CafeDoor>();
        if (zone == null) return null;
        if (door == null) return zone.transform.position;
        // The door's own facing, flipped if needed so it points out of the café.
        Vector3 outward = door.transform.forward;
        outward.y = 0;
        if (Vector3.Dot(outward, door.transform.position - zone.transform.position) < 0) outward = -outward;
        return door.transform.position + outward.normalized * 2f;
    }

    static Vector3? At<T>() where T : Component
    {
        var found = FindFirstObjectByType<T>();
        return found != null ? found.transform.position : (Vector3?)null;
    }

    void OnDestroy()
    {
        if (sprite != null) Destroy(sprite.gameObject);
    }
}
