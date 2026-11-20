using UnityEngine;

public class ExitEntrance : MonoBehaviour, IInteractable
{
    public string sceneToLoad = "MainScene(city)";
    bool remindedBackup;

    public void Interact()
    {
        if (GameProgressManager.Instance != null && GameProgressManager.Instance.chapter >= 2)
        {
            if (NarrationManager.Instance != null) NarrationManager.Instance.Show("It's too late to go out. I'm staying in tonight.");
            return;
        }
        if (GameProgressManager.Instance != null && GameProgressManager.Instance.laptopAt == "home")
        {
            if (NarrationManager.Instance != null) NarrationManager.Instance.Show("I should bring my laptop with me.");
            return;
        }
        // Leaving without a backup is her choice (it shows in the results), so it isn't blocked:
        // she says it once, and pressing E again goes out anyway.
        var gp = GameProgressManager.Instance;
        if (gp != null && !gp.thesisBackedUp && !gp.thesisInCloud && !remindedBackup)
        {
            remindedBackup = true;
            if (NarrationManager.Instance != null)
                NarrationManager.Instance.Show(gp.hasFlashDrive
                    ? "I haven't backed up my thesis yet. I should copy it to the flash drive before I go."
                    : "My thesis is only on this laptop. I should find my flash drive and back it up first.", 4f);
            return;
        }
        Sfx.Play("door_through");
        SceneLoader.Instance.LoadScene(sceneToLoad);
    }
}