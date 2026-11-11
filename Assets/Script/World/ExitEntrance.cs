using UnityEngine;

public class ExitEntrance : MonoBehaviour, IInteractable
{
    public string sceneToLoad = "MainScene(city)";

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
        SceneLoader.Instance.LoadScene(sceneToLoad);
    }
}