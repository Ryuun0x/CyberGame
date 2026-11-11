using UnityEngine;

/// <summary>
/// No shoes in bed: hides the student's shoes while she lies down or gets up (StudentPhone "Lying" and
/// "GettingUp" states). On the Student (prologue).
/// </summary>
public class WakeUpFit : MonoBehaviour
{
    public Renderer shoes;
    Animator anim;

    void Awake() => anim = GetComponent<Animator>();

    void LateUpdate()
    {
        var state = anim.GetCurrentAnimatorStateInfo(0);
        shoes.enabled = !(state.IsName("Lying") || state.IsName("GettingUp"));
    }
}
