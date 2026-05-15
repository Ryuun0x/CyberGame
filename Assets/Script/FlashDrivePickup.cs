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
    public string pickupMessage = "Found the flash drive. Let's head back to the laptop and back this up.";

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

        // Play the sequential narration (handles both custom UI and global NarrationManager)
        StartCoroutine(PlayPickupNarration());

        // Hide the flash drive model from the scene (Disabling components instead of the GameObject
        // ensures that your HideMessageAfterDelay Coroutine is not instantly cancelled).
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    private System.Collections.IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        if (pickupMessageUI != null)
            pickupMessageUI.SetActive(false);
    }

    private System.Collections.IEnumerator PlayPickupNarration()
    {
        // Dynamically split the message from the Inspector by sentences
        string[] sentences = pickupMessage.Split(new char[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < sentences.Length; i++)
        {
            string sentence = sentences[i].Trim() + "."; // Re-add the period for display

            if (pickupMessageUI != null && pickupMessageText != null)
            {
                pickupMessageText.text = sentence;
                pickupMessageUI.SetActive(true);
            }
            else if (NarrationManager.Instance != null)
            {
                NarrationManager.Instance.Show(sentence, 3f);
            }

            yield return new WaitForSecondsRealtime(3.5f);
        }

        if (pickupMessageUI != null)
        {
            pickupMessageUI.SetActive(false);
        }
    }
}
