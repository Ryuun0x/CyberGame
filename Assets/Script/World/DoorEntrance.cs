using UnityEngine;

public class DoorEntrance : MonoBehaviour, IInteractable
{
    public string sceneToLoad = "Interior";

    public void Interact()
    {
        Sfx.Play("door_through");
        SceneLoader.Instance.LoadScene(sceneToLoad);
    }
}