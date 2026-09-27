using UnityEngine;

// Drives the right-hand lane between intersections and picks a random turn at each one.
// Brakes for anything in its lane ahead: other cars, pedestrians or the player.
public class CityCar : MonoBehaviour
{
    const float TurnIn = 6f, CruiseSpeed = 7f, Accel = 5f;
    // One car inside an intersection at a time; the others wait at the stop line.
    static readonly System.Collections.Generic.Dictionary<(int, int), CityCar> Claimed = new System.Collections.Generic.Dictionary<(int, int), CityCar>();
    CityLife city;
    int ax, az, bx, bz; // driving from intersection A to B
    Vector3 p0, p1, p2; // current piece: straight p0->p2, or turn curve p0->(p1)->p2
    bool turning;
    float t, length, speed, blockedFor, ignoreUntil;
    Transform[] wheels;

    public bool Begin(CityLife owner, int fromX, int fromZ, int toX, int toZ, float progress)
    {
        city = owner;
        (ax, az, bx, bz) = (fromX, fromZ, toX, toZ);
        StartLane();
        t = progress;
        Vector3 spawn = Point();
        foreach (var other in CityLife.Movers)
            if ((other.position - spawn).sqrMagnitude < 12 * 12) return false;
        var body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        wheels = System.Array.FindAll(GetComponentsInChildren<Transform>(), w => System.Text.RegularExpressions.Regex.IsMatch(w.name, @"_[FR][LR]\d*$"));
        speed = CruiseSpeed;
        Place();
        CityLife.Movers.Add(transform);
        return true;
    }

    Vector3 Dir(int fx, int fz, int tx, int tz) => (city.Node(tx, tz) - city.Node(fx, fz)).normalized;
    static Vector3 Right(Vector3 d) => new Vector3(d.z, 0, -d.x);

    void StartLane()
    {
        Vector3 d = Dir(ax, az, bx, bz), r = Right(d) * city.laneOffset;
        p0 = city.Node(ax, az) + r + d * TurnIn;
        p2 = city.Node(bx, bz) + r - d * TurnIn;
        turning = false;
        length = Vector3.Distance(p0, p2);
        t = 0;
    }

    void StartTurn()
    {
        var options = city.Neighbours(bx, bz);
        options.Remove((ax, az));
        if (options.Count == 0) options.Add((ax, az)); // dead end: U-turn
        var (cx, cz) = options[Random.Range(0, options.Count)];
        Vector3 d1 = Dir(ax, az, bx, bz), d2 = Dir(bx, bz, cx, cz), b = city.Node(bx, bz);
        p0 = p2;
        p2 = b + Right(d2) * city.laneOffset + d2 * TurnIn;
        // Control point where the two lanes meet; straight on just uses the midpoint.
        p1 = Mathf.Abs(Vector3.Dot(d1, d2)) > 0.9f ? (p0 + p2) / 2 : b + (Right(d1) + Right(d2)) * city.laneOffset;
        length = Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2);
        turning = true;
        t = 0;
        (ax, az, bx, bz) = (bx, bz, cx, cz);
    }

    Vector3 Point() => turning
        ? Vector3.Lerp(Vector3.Lerp(p0, p1, t), Vector3.Lerp(p1, p2, t), t)
        : Vector3.Lerp(p0, p2, t);

    void Update()
    {
        float target = Time.time < ignoreUntil ? CruiseSpeed : Mathf.Min(CruiseSpeed, SafeSpeed());
        if (turning) target = Mathf.Min(target, 4.5f);
        speed = Mathf.MoveTowards(speed, target, Accel * 2 * Time.deltaTime);
        // Mutual blocking at a crossing: after a while, nudge through.
        blockedFor = speed < 0.1f ? blockedFor + Time.deltaTime : 0;
        if (blockedFor > 5) { ignoreUntil = Time.time + 1.5f; blockedFor = 0; }

        t += speed * Time.deltaTime / Mathf.Max(0.01f, length);
        if (t >= 1)
        {
            if (turning) { Release(); StartLane(); }
            else if (Claim((bx, bz))) StartTurn();
            else { t = 1; speed = 0; }
        }
        Place();
        // Spin about the car's right axis: the wheel meshes' own X axes don't all point the same way.
        foreach (var wheel in wheels) wheel.Rotate(transform.right, speed / 0.32f * Mathf.Rad2Deg * Time.deltaTime, Space.World);
    }

    void Place()
    {
        Vector3 here = Point();
        float ahead = Mathf.Min(1, t + 0.5f / Mathf.Max(1, length));
        float saved = t;
        t = ahead;
        Vector3 forward = Point() - here;
        t = saved;
        transform.position = here;
        if (forward.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(forward);
    }

    // ponytail: O(movers) scan per car, fine for a few dozen; use a spatial grid if the city grows.
    float SafeSpeed()
    {
        Vector3 forward = transform.forward, right = transform.right, position = transform.position;
        float best = CruiseSpeed;
        foreach (var other in CityLife.Movers)
        {
            if (other == transform || other == null) continue;
            Vector3 v = other.position - position;
            float along = Vector3.Dot(v, forward), side = Mathf.Abs(Vector3.Dot(v, right));
            if (along <= 0 || along > 12 || side > 1.8f) continue;
            best = Mathf.Min(best, Mathf.Max(0, (along - 4.5f) * 1.2f));
        }
        return best;
    }

    bool Claim((int, int) node)
    {
        if (Claimed.TryGetValue(node, out var owner) && owner != null && owner != this) return false;
        Claimed[node] = this;
        return true;
    }

    void Release()
    {
        var node = (ax, az); // StartTurn already moved A to the intersection being left
        if (Claimed.TryGetValue(node, out var owner) && owner == this) Claimed.Remove(node);
    }

    void OnDestroy()
    {
        Release();
        CityLife.Movers.Remove(transform);
    }
}
