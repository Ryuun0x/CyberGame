using UnityEngine;

// Editor helper: shows a character in a pose (e.g. sitting) in the Scene view instead of a T-pose,
// so it can be placed by hand. In Play mode the Animator takes over and this does nothing.
[ExecuteAlways]
public class PosePreview : MonoBehaviour
{
    [Tooltip("The clip whose first frame is shown in the editor.")]
    public AnimationClip pose;

    void OnEnable() => Show();
    void OnValidate() => Show();

    void Show()
    {
        if (!Application.isPlaying && pose != null) pose.SampleAnimation(gameObject, 0f);
    }
}
