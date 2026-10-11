using UnityEngine;

/// <summary>
/// The bed at the end of Chapter 2: press E on it to go to sleep.
/// Placed in the Interior scene as "BedSleep" (an invisible box over the bed) and kept hidden
/// until the story asks the player to go to bed.
/// </summary>
public class BedSleep : MonoBehaviour, IInteractable
{
    public bool Slept { get; private set; }

    public void Interact() => Slept = true;
}
