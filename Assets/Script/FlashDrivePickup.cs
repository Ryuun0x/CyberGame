using UnityEngine;
using TMPro;
public class FlashDrivePickup : MonoBehaviour, IInteractable
{
    [Header("Pickup Feedback")]
    [Tooltip("Optional: a UI text or narration box to show a message on pickup")]
    public GameObject pickupMessageUI;
    public TextMeshProUGUI pickupMessageText;
    public float messageDuration = 2f;

    [Header("Optional: Narration on Pickup")]
    [Tooltip("Message shown when the player picks up the flash drive")]
    public string pickupMessage = "Found a flash drive. I can use this to backup my thesis.";

    public void Interact()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogError("[FlashDrivePickup] GameProgressManager not found in scene!");
            return;
        }

        if (GameProgressManager.Instance.hasFlashDrive)
        {
            // Already picked up — do nothing
            return;
        }

        // Register pickup in game progress
        GameProgressManager.Instance.PickUpFlashDrive();

        // Show pickup message if UI is assigned
        if (pickupMessageUI != null && pickupMessageText != null)
        {
            pickupMessageText.text = pickupMessage;
            pickupMessageUI.SetActive(true);
            StartCoroutine(HideMessageAfterDelay());
        }

        // Hide the flash drive model from the scene
        gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        if (pickupMessageUI != null)
            pickupMessageUI.SetActive(false);
    }
}
