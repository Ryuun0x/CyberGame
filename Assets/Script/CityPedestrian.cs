using UnityEngine;

// Walks sidewalk corners of the road grid: along a block, or across a crosswalk.
public class CityPedestrian : MonoBehaviour
{
    CityLife city;
    Animator anim;
    int x, z, sx, sz; // intersection index and which corner (+1/-1 on each axis)
    int lastMove = -1;
    Vector3 target;
    float speed, pace, side, stuckFor, ignoreUntil;

    public void Begin(CityLife owner, int nodeX, int nodeZ, int cornerX, int cornerZ)
    {
        city = owner;
        anim = GetComponentInChildren<Animator>();
        x = nodeX; z = nodeZ; sx = cornerX; sz = cornerZ;
        speed = pace = Random.Range(1.2f, 1.6f);
        // Everyone keeps to the right at their own distance, so oncoming people pass side by side.
        side = Random.Range(0.35f, 0.8f);
        var start = Corner();
        start.y = CityLife.GroundY(start);
        transform.position = start;
        Pick();
    }

    Vector3 Corner() => city.Node(x, z) + new Vector3(sx, 0, sz) * city.sidewalkOffset;

    // 0 cross X road, 1 cross Z road, 2 walk along X, 3 walk along Z.
    void Pick()
    {
        Vector3 from = Corner();
        for (int tries = 0; tries < 8; tries++)
        {
            int move = Random.value < 0.3f ? Random.Range(0, 2) : Random.Range(2, 4);
            if (move == lastMove) continue; // don't turn straight back
            if (move == 0) sx = -sx;
            else if (move == 1) sz = -sz;
            else if (move == 2 && city.HasNode(x + sx, z)) { x += sx; sx = -sx; }
            else if (move == 3 && city.HasNode(x, z + sz)) { z += sz; sz = -sz; }
            else continue;
            lastMove = move;
            Aim(from);
            return;
        }
        lastMove = -1;
        sx = -sx;
        Aim(from);
    }

    void Aim(Vector3 from)
    {
        target = Corner();
        Vector3 d = (target - from).normalized;
        target += new Vector3(d.z, 0, -d.x) * side;
    }

    // Slow down behind whoever is just ahead instead of walking through them.
    float SafePace(Vector3 forward)
    {
        if (Time.time < ignoreUntil) return speed;
        Vector3 right = new Vector3(forward.z, 0, -forward.x), position = transform.position;
        float best = speed;
        foreach (var other in CityLife.Movers)
        {
            if (other == transform || other == null) continue;
            Vector3 v = other.position - position;
            float along = v.x * forward.x + v.z * forward.z;
            if (along <= 0 || along > 1.6f || Mathf.Abs(v.x * right.x + v.z * right.z) > 0.55f) continue;
            best = Mathf.Min(best, speed * Mathf.Clamp01((along - 0.7f) / 0.9f));
        }
        return best;
    }

    void Update()
    {
        Vector3 to = target - transform.position;
        to.y = 0;
        if (to.magnitude < 0.1f) { Pick(); return; }
        pace = Mathf.MoveTowards(pace, SafePace(to.normalized), 3 * Time.deltaTime);
        // Two people blocking each other at a corner: one steps through after a moment.
        stuckFor = pace < 0.1f ? stuckFor + Time.deltaTime : 0;
        if (stuckFor > 2) { ignoreUntil = Time.time + 1; stuckFor = 0; }
        Vector3 step = Vector3.ClampMagnitude(to, pace * Time.deltaTime);
        Vector3 next = transform.position + step;
        next.y = Mathf.MoveTowards(next.y, CityLife.GroundY(next), 2 * Time.deltaTime); // step up curbs smoothly
        transform.position = next;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 8 * Time.deltaTime);
        anim.SetFloat("Speed", 0.5f * Mathf.Clamp01(pace / 0.6f), 0.15f, Time.deltaTime);
        anim.speed = Mathf.Max(0.7f, pace / 1.4f);
    }
}
