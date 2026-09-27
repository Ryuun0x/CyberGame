using System;
using System.Collections.Generic;
using UnityEngine;

// Fills the city with walking pedestrians, café regulars and simple traffic.
// Everything moves on the road grid measured from the city layout: cars drive the right-hand
// lane between intersections, pedestrians walk the sidewalks and cross at the crosswalks.
public class CityLife : MonoBehaviour
{
    [Serializable] public struct Spot { public Vector3 position; public float yaw; }

    [Header("Road grid (world X / Z of road centerlines)")]
    public float[] roadX = { -162, -126, -90, -54, -18, 18, 54 };
    public float[] roadZ = { -54, -18, 18, 54, 90 };
    [Tooltip("Roads west of this X only exist between suburbRows.x and suburbRows.y.")]
    public float suburbEdge = -90;
    public Vector2 suburbRows = new Vector2(-18, 54);
    public float laneOffset = 1.6f;
    public float sidewalkOffset = 5.3f;

    [Header("People")]
    public GameObject[] characters;
    public float personHeight = 1.72f;
    public RuntimeAnimatorController animator;
    public int walkers = 24;
    public Spot[] cafeSpots;

    [Header("Traffic")]
    public GameObject[] carPrefabs;
    public int cars = 10;

    // Everything cars must not drive into: pedestrians, other cars and the player.
    public static readonly List<Transform> Movers = new List<Transform>();

    void Start()
    {
        var player = FindFirstObjectByType<CharacterController>();
        if (player != null) Movers.Add(player.transform);
        if (characters != null && characters.Length > 0)
        {
            for (int i = 0; i < walkers; i++)
            {
                var (x, z) = RandomNode();
                var walker = Person("Pedestrian", Vector3.zero, 0).AddComponent<CityPedestrian>();
                walker.Begin(this, x, z, UnityEngine.Random.value < 0.5f ? 1 : -1, UnityEngine.Random.value < 0.5f ? 1 : -1);
            }
            foreach (var spot in cafeSpots) Person("Café customer", spot.position, spot.yaw);
        }
        if (carPrefabs != null && carPrefabs.Length > 0)
        {
            int placed = 0;
            for (int attempt = 0; attempt < cars * 10 && placed < cars; attempt++)
            {
                var (x, z) = RandomNode();
                var next = Neighbours(x, z);
                if (next.Count == 0) continue;
                var (nx, nz) = next[UnityEngine.Random.Range(0, next.Count)];
                var prefab = carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Length)];
                var car = Instantiate(prefab, transform).AddComponent<CityCar>();
                if (!car.Begin(this, x, z, nx, nz, UnityEngine.Random.value)) { Destroy(car.gameObject); continue; }
                placed++;
            }
        }
    }

    public GameObject Person(string name, Vector3 position, float yaw)
    {
        position.y = GroundY(position);
        var go = Instantiate(characters[UnityEngine.Random.Range(0, characters.Length)], position, Quaternion.Euler(0, yaw, 0), transform);
        go.name = name;
        // Packs are modelled at different heights; bring everyone to a normal adult height.
        var renderers = go.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        go.transform.localScale *= personHeight / Mathf.Max(0.5f, bounds.size.y) * UnityEngine.Random.Range(0.94f, 1.05f);
        var anim = go.GetComponentInChildren<Animator>();
        anim.runtimeAnimatorController = animator;
        anim.applyRootMotion = false;
        go.AddComponent<PinHips>();
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        // Desynchronise identical idles so the crowd doesn't move in lockstep.
        anim.Update(UnityEngine.Random.value * 3);
        Movers.Add(go.transform);
        return go;
    }

    void OnDestroy() => Movers.Clear();

    // Curbs and floors are raised above the road, so feet follow whatever is underneath.
    public static float GroundY(Vector3 p)
    {
        const int notPlayer = ~(1 << 8);
        return Physics.Raycast(p + Vector3.up * 0.8f, Vector3.down, out var hit, 3f, notPlayer, QueryTriggerInteraction.Ignore)
            ? hit.point.y : p.y;
    }

    public bool HasNode(int x, int z) =>
        x >= 0 && z >= 0 && x < roadX.Length && z < roadZ.Length &&
        (roadX[x] >= suburbEdge || (roadZ[z] >= suburbRows.x && roadZ[z] <= suburbRows.y));

    public Vector3 Node(int x, int z) => new Vector3(roadX[x], 0, roadZ[z]);

    public List<(int, int)> Neighbours(int x, int z)
    {
        var list = new List<(int, int)>(4);
        if (HasNode(x + 1, z)) list.Add((x + 1, z));
        if (HasNode(x - 1, z)) list.Add((x - 1, z));
        if (HasNode(x, z + 1)) list.Add((x, z + 1));
        if (HasNode(x, z - 1)) list.Add((x, z - 1));
        return list;
    }

    (int, int) RandomNode()
    {
        while (true)
        {
            int x = UnityEngine.Random.Range(0, roadX.Length), z = UnityEngine.Random.Range(0, roadZ.Length);
            if (HasNode(x, z)) return (x, z);
        }
    }
}
