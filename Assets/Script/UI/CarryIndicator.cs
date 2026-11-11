using UnityEngine;

/// <summary>
/// HUD badge "Laptop" (laptop icon) shown while the player carries the laptop (GameProgressManager.laptopAt
/// is "hands"; see LaptopCarry). On HUDCanvas (prefab); the badge is the placed child "CarryIndicator".
/// </summary>
public class CarryIndicator : MonoBehaviour
{
    public GameObject badge;

    void Update()
    {
        var gp = GameProgressManager.Instance;
        bool carrying = gp != null && gp.laptopAt == "hands";
        if (badge.activeSelf != carrying) badge.SetActive(carrying);
    }
}
