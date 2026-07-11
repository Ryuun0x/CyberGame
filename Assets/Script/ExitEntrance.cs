using UnityEngine;

public class ExitEntrance : MonoBehaviour, IInteractable
{
    public string sceneToLoad = "MainScene(city)";

    public void Interact()
    {
        SceneLoader.Instance.LoadScene(sceneToLoad);
    }
}