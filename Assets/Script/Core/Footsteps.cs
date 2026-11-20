using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The player's footsteps, at the SFX volume from Settings. Starts by itself and follows the FirstPersonController in
/// each scene; nothing to place. A step every <see cref="Stride"/> metres walked on the ground, on the floor for where
/// the player is (Resources/Footsteps, made by ArtSource/Sounds/make_footsteps.py): carpet at home, wood in the
/// café, concrete in the city.
/// </summary>
public class Footsteps : MonoBehaviour
{
    const float Stride = 2f;    // metres per step: about 2 steps a second walking, 3 sprinting
    const float Volume = 0.5f;

    Dictionary<string, AudioClip[]> floors;
    FirstPersonController player;
    CharacterController body;
    AudioSource source;
    float travelled;
    int last = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("Footsteps");
        DontDestroyOnLoad(go);
        go.AddComponent<Footsteps>();
    }

    void Awake()
    {
        floors = Resources.LoadAll<AudioClip>("Footsteps")   // carpet_0..4, wood_0..4, concrete_0..4
            .GroupBy(clip => clip.name.Substring(0, clip.name.LastIndexOf('_')))
            .ToDictionary(takes => takes.Key, takes => takes.ToArray());
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void Update()
    {
        if (player == null)  // a new scene, or the player not spawned yet: look again now and then
        {
            if (Time.frameCount % 30 != 0 || (player = FindFirstObjectByType<FirstPersonController>()) == null) return;
            body = player.GetComponent<CharacterController>();
        }
        Vector3 v = body.velocity;
        float speed = new Vector2(v.x, v.z).magnitude;
        if (!player.enabled || !player.Grounded || speed < 0.5f)
        {
            travelled = Stride * 0.6f; // starting again: the first step comes soon
            return;
        }
        travelled += speed * Time.deltaTime;
        if (travelled < Stride) return;
        travelled = 0;

        string scene = SceneManager.GetActiveScene().name;
        string floor = scene == "Interior" ? "carpet" : CafeZone.PlayerInCafe ? "wood" : "concrete";
        if (!floors.TryGetValue(floor, out var takes)) return;
        int pick = Random.Range(0, takes.Length - 1);
        if (pick >= last) pick++;          // never the same take twice in a row
        if (pick >= takes.Length) pick = 0;
        last = pick;
        source.pitch = Random.Range(0.93f, 1.05f);
        source.PlayOneShot(takes[pick], Volume * GameSettings.Sfx);
    }
}
