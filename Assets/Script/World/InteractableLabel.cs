using UnityEngine;
public class InteractableLabel : MonoBehaviour
{
    [Tooltip("Action text shown in the prompt (e.g. 'Use Laptop')")]
    public string promptText = "Interact";

    [Tooltip("Offset the prompt position from this object's center")]
    public Vector3 promptOffset = new Vector3(0f, 0.5f, 0f);
}
