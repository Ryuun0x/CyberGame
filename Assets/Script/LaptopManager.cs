using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LaptopDesktop : MonoBehaviour
{
    [Header("Windows")]
    public GameObject thesisWindow;
    public GameObject noInternetWindow; // Build this UI later

    [Header("Taskbar")]
    public TextMeshProUGUI timeText;
    public GameObject noInternetIcon;

    [Header("Settings")]
    public float internetLostDelay = 3f; // Seconds before internet "drops"

    private bool _hasInternet = true;

    void Start()
    {
        if (thesisWindow != null)
            thesisWindow.SetActive(false);
        if (noInternetWindow != null)
            noInternetWindow.SetActive(false);
        if (noInternetIcon != null)
            noInternetIcon.SetActive(false);
    }

    void Update()
    {
        if (timeText != null)
            timeText.text = System.DateTime.Now.ToString("h:mm tt");
    }

    // Call this when laptop is opened
    public void OnLaptopOpened()
    {
        StartCoroutine(LoseInternetAfterDelay());
    }

    IEnumerator LoseInternetAfterDelay()
    {
        _hasInternet = true;

        yield return new WaitForSeconds(internetLostDelay);

        // Internet drops
        _hasInternet = false;
        if (noInternetIcon != null)
            noInternetIcon.SetActive(true);
    }

    public void OpenThesisWindow()
    {
        if (thesisWindow != null)
            thesisWindow.SetActive(true);
    }

    public void CloseThesisWindow()
    {
        if (thesisWindow != null)
            thesisWindow.SetActive(false);
    }

    public void CloseNoInternetWindow()
    {
        if (noInternetWindow != null)
            noInternetWindow.SetActive(false);
    }

    public void SubmitThesis()
    {
        if (_hasInternet)
        {
            // Internet still works (unlikely with delay)
            Debug.Log("Thesis Submitted!");
            CloseThesisWindow();
        }
        else
        {
            // No internet — show error
            Debug.Log("No Internet! Cannot submit.");
            CloseThesisWindow();
            if (noInternetWindow != null)
                noInternetWindow.SetActive(true);
        }
    }
}
