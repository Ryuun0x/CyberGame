using UnityEngine;

public class CafeZone : MonoBehaviour
{
    public static bool PlayerInCafe = false;

    CafeEnding ending;

    void Awake() => ending = gameObject.AddComponent<CafeEnding>();

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