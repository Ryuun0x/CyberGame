using UnityEngine;

public class DoorEntrance : MonoBehaviour, IInteractable
{
    public string sceneToLoad = "Interior";

    public void Interact()
    {
        SceneLoader.Instance.LoadScene(sceneToLoad);
    }
}