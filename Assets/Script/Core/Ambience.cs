using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quiet background sound for where the player is, at the Music volume from Settings. Starts by itself and
/// survives scene loads; nothing to place in a scene. Loops come from Resources/Ambience (made by
/// ArtSource/Sounds/make_ambience.py) and fade in and out as the place changes:
///   home (Interior)  a room tone; at night (Chapter 2) a little quieter, with faint wind outside
///   city             low wind and distant traffic, birds now and then, a horn far off once in a while
///                    (the cars' own engines are on each CityCar)
///   café             the room tone and the lights humming, the traffic faint through the windows
///   laptop in use    its fan, on top of the rest
///   start menu       a slow, dark pad over a deep rumble
/// </summary>
public class Ambience : MonoBehaviour
{
    const float FadeSeconds = 2f;
    readonly Dictionary<string, AudioSource> layers = new Dictionary<string, AudioSource>();
    // The city's occasional sounds (Resources/City, made by ArtSource/Sounds/make_city.py).
    AudioClip[] birds, horns;
    AudioSource birdSource, hornSource;
    float nextBird, nextHorn;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("Ambience");
        DontDestroyOnLoad(go);
        go.AddComponent<Ambience>();
    }

    void Awake()
    {
        foreach (var clip in Resources.LoadAll<AudioClip>("Ambience"))
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = 0;
            source.Play();
            layers[clip.name] = source;
        }
        var city = Resources.LoadAll<AudioClip>("City");
        birds = System.Array.FindAll(city, c => c.name.StartsWith("bird"));
        horns = System.Array.FindAll(city, c => c.name.StartsWith("horn"));
        birdSource = gameObject.AddComponent<AudioSource>();
        hornSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        string scene = SceneManager.GetActiveScene().name;
        bool home = scene == "Interior";
        bool city = scene.StartsWith("MainScene");
        bool cafe = city && CafeZone.PlayerInCafe;
        bool night = GameProgressManager.Instance != null && GameProgressManager.Instance.chapter >= 2;

        Fade("room", home ? (night ? 0.35f : 0.45f) : cafe ? 0.4f : 0);
        Fade("wind", city && !cafe ? 0.35f : home && night ? 0.08f : 0);
        Fade("cafe_hum", cafe ? 0.12f : 0);
        Fade("laptop", LaptopInteraction.InUse ? 0.25f : 0);
        Fade("menu", scene == "StartMenu" ? 0.5f : 0);
        Fade("city", city ? (cafe ? 0.06f : 0.3f) : 0);

        // Outside in the city: a bird every 5-14 s, a horn a few streets away every 45-110 s, from a random side.
        if (city && !cafe)
        {
            if (Time.time > nextBird) { if (nextBird > 0) Once(birdSource, birds, 0.25f); nextBird = Time.time + Random.Range(5f, 14f); }
            if (Time.time > nextHorn) { if (nextHorn > 0) Once(hornSource, horns, 0.12f); nextHorn = Time.time + Random.Range(45f, 110f); }
        }
        else nextBird = nextHorn = 0; // starts counting again on the way out, so nothing plays the moment you step outside
    }

    static void Once(AudioSource source, AudioClip[] clips, float level)
    {
        if (clips.Length == 0) return;
        source.panStereo = Random.Range(-0.7f, 0.7f);
        source.pitch = Random.Range(0.94f, 1.06f);
        source.PlayOneShot(clips[Random.Range(0, clips.Length)], level * GameSettings.Music);
    }

    void Fade(string layer, float level)
    {
        if (!layers.TryGetValue(layer, out var source)) return;
        source.volume = Mathf.MoveTowards(source.volume, level * GameSettings.Music, Time.unscaledDeltaTime / FadeSeconds);
    }
}
