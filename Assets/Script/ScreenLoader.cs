using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    void Awake()
    {
        Instance = this;
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadWithFade(sceneName));
    }

    private IEnumerator LoadWithFade(string sceneName)
    {
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeOut());

        // Load the scene
        SceneManager.LoadScene(sceneName);
    }
}