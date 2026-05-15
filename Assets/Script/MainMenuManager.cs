using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject mainMenuUI;
    public GameObject aboutPage;
    [Tooltip("Optional: Create a black UI Image covering the screen, add a CanvasGroup, and drag it here to fade to black.")]
    public CanvasGroup faderGroup; 

    [Header("Cameras")]
    public Camera menuCamera;
    
    [Header("Scene Transition")]
    [Tooltip("The exact name of the scene to load")]
    public string interiorSceneName = "Interior";
    public float fadeDuration = 1.5f;

    void Start()
    {
        // Ensure menu UI is visible at start
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
        if (aboutPage != null) aboutPage.SetActive(false); // Hide about page on boot
        if (menuCamera != null) menuCamera.enabled = true;
        
        // Ensure screen starts completely visible (not black)
        if (faderGroup != null) faderGroup.alpha = 0f;
    }

    // Connect this to your Start Game Button
    public void PlayGame()
    {
        // Disable UI buttons so player can't spam click
        if (mainMenuUI != null) mainMenuUI.SetActive(false);

        // Start the scene load transition
        StartCoroutine(TransitionToInterior());
    }

    // Connect this to your Quit Button
    public void QuitGame()
    {
        Debug.Log("Quit Game Clicked!");
        Application.Quit();
    }

    // Connect this to your 'About' Button
    public void OpenAbout()
    {
        if (aboutPage != null) aboutPage.SetActive(true);
    }

    // Connect this to the 'X' button inside the About Page
    public void CloseAbout()
    {
        if (aboutPage != null) aboutPage.SetActive(false);
    }

    IEnumerator TransitionToInterior()
    {
        // Fade to black if a CanvasGroup is provided
        if (faderGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                faderGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
        }
        else
        {
            // If no fader is assigned, just wait a tiny split second so the click feels responsive
            yield return new WaitForSeconds(0.2f);
        }

        // Load the new scene!
        SceneManager.LoadScene(interiorSceneName);
    }
}
