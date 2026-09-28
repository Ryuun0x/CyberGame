using UnityEngine;

// Scene-placed standing NPC (e.g. the café cashier). Plays its Animator's idle when running; in the
// editor it shows the first frame of that idle, so it can be placed without pressing Play.
[ExecuteAlways]
public class StandingIdle : MonoBehaviour
{
    void OnEnable()
    {
        if (Application.isPlaying) return;
        var anim = GetComponentInChildren<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null) return;
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name.Contains("Idle")) { clip.SampleAnimation(anim.gameObject, 0); return; }
    }
}
