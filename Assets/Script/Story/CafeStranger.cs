using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The stranger at the table next to the player's laptop (a seated NPC placed in the city scene, with SeatedPose).
// As soon as the thesis is sent, their screen flashes the Ghostline logo, the lid closes, they get up, walk out
// of the café onto the sidewalk and carry on down the street like the other pedestrians (CityPedestrian).
// Their laptop is the "StrangerLaptop" object on their table (a copy of the café laptop model with no scripts),
// and the logo is its "GhostlineScreen" canvas (hidden until they leave).
public class CafeStranger : MonoBehaviour
{
    [Header("The stranger's laptop (placed on their table)")]
    public Transform laptop;
    [Tooltip("The lid; it folds down when they leave.")]
    public Transform hinge;
    [Tooltip("The GhostlineScreen canvas on the lid. Hidden until they leave.")]
    public GameObject screen;

    // Around the counter, out through the café door and onto the sidewalk.
    static readonly Vector3[] ExitPath =
    {
        new Vector3(-2.6f, 0, 82.6f), new Vector3(-0.9f, 0, 83.3f), new Vector3(-0.61f, 0, 84.8f), new Vector3(-0.61f, 0, 85.8f)
    };
    const float WalkSpeed = 1.4f;

    static CafeStranger _current;

    Animator anim;
    SeatedPose seated;
    bool leaving;

    void Awake() => _current = this;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        seated = GetComponent<SeatedPose>();
        if (screen != null) screen.SetActive(false);
    }

    public static void LeaveIfPresent()
    {
        if (_current != null && !_current.leaving) _current.StartCoroutine(_current.Leave());
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }

    // Ghost: round head, straight sides, wavy hem, two eye holes.
    public static bool IsGhost(float x, float y)
    {
        bool head = y > 0 && x * x + y * y < 0.6f;
        bool body = y <= 0 && Mathf.Abs(x) < 0.77f && y > -0.75f + 0.12f * Mathf.Cos(x * 12f);
        bool eye = (x + 0.28f) * (x + 0.28f) + (y - 0.1f) * (y - 0.1f) < 0.02f || (x - 0.28f) * (x - 0.28f) + (y - 0.1f) * (y - 0.1f) < 0.02f;
        return (head || body) && !eye;
    }

    IEnumerator Leave()
    {
        leaving = true;
        if (screen != null) screen.SetActive(true);
        yield return new WaitForSeconds(2.5f);

        // Fold the lid down onto the keyboard.
        if (hinge != null)
        {
            var keyboard = laptop.Find("KeyboardFrame");
            Vector3 toKeyboard = keyboard != null ? Flat(keyboard.GetComponent<Renderer>().bounds.center - hinge.position) : laptop.right;
            Quaternion open = hinge.rotation, closed = Quaternion.FromToRotation(Vector3.up, toKeyboard) * open;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.7f)
            {
                hinge.rotation = Quaternion.Slerp(open, closed, t * t);
                yield return null;
            }
            hinge.rotation = closed;
        }
        if (screen != null) screen.SetActive(false);
        yield return new WaitForSeconds(0.6f);

        if (laptop != null) Destroy(laptop.gameObject); // takes it with them
        if (seated != null) seated.Stand();
        yield return null;
        anim.SetFloat("Speed", 0.5f); // blend tree: 0.5 = walk
        foreach (var point in ExitPath) yield return WalkTo(point);

        // Along the sidewalk to the nearest street corner, then on like any other pedestrian.
        var city = FindFirstObjectByType<CityLife>();
        if (city == null) { Destroy(gameObject); yield break; }
        var (x, z, sx, sz) = NearestCorner(city);
        yield return WalkTo(city.Node(x, z) + new Vector3(sx, 0, sz) * city.sidewalkOffset);
        if (!CityLife.Movers.Contains(transform)) CityLife.Movers.Add(transform);
        gameObject.AddComponent<CityPedestrian>().Begin(city, x, z, sx, sz);
        Destroy(this); // the pedestrian walks from here; the laptop already went with them
    }

    IEnumerator WalkTo(Vector3 point)
    {
        while (Vector2.Distance(Flat2(transform.position), Flat2(point)) > 0.05f)
        {
            Vector3 here = transform.position;
            Vector3 step = Vector3.MoveTowards(here, new Vector3(point.x, here.y, point.z), WalkSpeed * Time.deltaTime);
            Vector3 dir = Flat(step - here);
            if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8 * Time.deltaTime);
            step.y = CityLife.GroundY(step);
            transform.position = step;
            yield return null;
        }
    }

    // The sidewalk corner (an intersection and which of its four corners) closest to where they stand.
    (int, int, int, int) NearestCorner(CityLife city)
    {
        var best = (0, 0, 1, 1);
        float bestDistance = float.MaxValue;
        for (int x = 0; x < city.roadX.Length; x++)
            for (int z = 0; z < city.roadZ.Length; z++)
            {
                if (!city.HasNode(x, z)) continue;
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 corner = city.Node(x, z) + new Vector3(sx, 0, sz) * city.sidewalkOffset;
                        float d = Vector2.Distance(Flat2(corner), Flat2(transform.position));
                        if (d < bestDistance) { bestDistance = d; best = (x, z, sx, sz); }
                    }
            }
        return best;
    }

    static Vector2 Flat2(Vector3 v) => new Vector2(v.x, v.z);

    void OnDestroy()
    {
        if (!leaving) CityLife.Movers.Remove(transform); // once walking the streets they stay a mover
        if (laptop != null) Destroy(laptop.gameObject);
    }
}
