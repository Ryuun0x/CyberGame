using UnityEngine;

public class CafeZone : MonoBehaviour
{
    public static bool PlayerInCafe = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInCafe = true;
            Debug.Log("Entered cafe zone");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInCafe = false;
            Debug.Log("Left cafe zone");
        }
    }
}