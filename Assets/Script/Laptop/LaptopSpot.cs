using UnityEngine;

/// <summary>
/// The table top where the carried laptop goes down (E while looking at it). Shown only while the laptop is
/// carried (LaptopCarry turns it on and off). A thin box collider on the desk at home or the café table,
/// with an InteractableLabel "Place Laptop".
/// </summary>
public class LaptopSpot : MonoBehaviour, IInteractable
{
    public LaptopCarry laptop;

    public void Interact() => laptop.PutDown();
}
