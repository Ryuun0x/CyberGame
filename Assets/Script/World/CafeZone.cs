using UnityEngine;

public class CafeZone : MonoBehaviour
{
    public static bool PlayerInCafe = false;

    [Tooltip("The CafeEnding component on this object (blocks the exit until the thesis is submitted, then ends Chapter 1).")]
    public CafeEnding ending;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInCafe = true;
            Debug.Log("Entered cafe zone");

            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.ArriveAtCafe();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInCafe = false;
            Debug.Log("Left cafe zone");
            ending.PlayerLeft();
        }
    }
}