using TMPro;
using UnityEngine;

/// <summary>
/// The laptop at night (Chapter 2). It uses the normal desktop (same wallpaper and taskbar) with a Browser icon
/// next to the thesis icon. The browser window (ShopHub and the cybercrime report) and the ransomware screen
/// are placed under NightApps in LaptopCanvas and stay hidden until needed.
/// Every story button calls Choose("...") from its OnClick in the Inspector; the story waits for Choice.
/// Attach this to LaptopCanvas (next to LaptopDesktop).
/// </summary>
public class LaptopNight : MonoBehaviour
{
    [Header("Desktop icons")]
    public GameObject thesisIcon;
    [Tooltip("Hidden in Chapter 1, shown at night.")]
    public GameObject browserIcon;

    [Header("Windows and full screens (hidden until needed)")]
    public GameObject browser;
    public GameObject lockedFiles;

    [Header("Browser")]
    public TextMeshProUGUI addressText;
    public GameObject hotlineBookmark;
    public GameObject startPage;
    public GameObject signUpPage;
    public GameObject newPasswordPage;
    public GameObject listingsPage;
    public GameObject scamChatPage;
    public GameObject orderPlacedPage;
    public GameObject reportPage;
    public GameObject reportSentPage;

    [Header("Page parts the story changes")]
    public TMP_InputField newPasswordField;
    public TextMeshProUGUI passwordStrength;
    public TextMeshProUGUI orderPlacedText;
    public GameObject sellerClue;

    [Header("Locked files (ransomware)")]
    public TextMeshProUGUI lockedMessage;
    public GameObject payButton;
    public GameObject restoreButton;
    public GameObject wipeButton;

    // The id of the last button pressed (null = nothing pressed yet).
    public string Choice { get; private set; }

    // Called by the buttons' OnClick.
    public void Choose(string id) => Choice = id;

    public void ClearChoice() => Choice = null;

    GameObject[] Pages => new[] { startPage, signUpPage, newPasswordPage, listingsPage, scamChatPage, orderPlacedPage, reportPage, reportSentPage };

    // At night the Browser icon appears. The thesis icon disappears if the files were wiped without a backup.
    void OnEnable()
    {
        var progress = GameProgressManager.Instance;
        bool night = progress != null && progress.chapter >= 2;
        browserIcon.SetActive(night);
        thesisIcon.SetActive(!(night && progress.lostFiles));
    }

    // Shows one window or full screen; null = just the desktop.
    public void ShowScreen(GameObject screen)
    {
        browser.SetActive(screen == browser);
        lockedFiles.SetActive(screen == lockedFiles);
        ClearChoice();
    }

    public void ShowDesktop() => ShowScreen(null);

    // Called by the Browser icon. Reopening continues on the page that was open.
    public void OpenBrowser()
    {
        ShowScreen(browser);
        if (System.Array.Find(Pages, p => p.activeSelf) == null) OpenPage(startPage, "start.page");
    }

    // Called by the browser window's close button (X).
    public void CloseBrowser() => browser.SetActive(false);

    // Shows one page inside the browser, with its web address in the address bar.
    public void OpenPage(GameObject page, string address)
    {
        if (!browser.activeSelf) ShowScreen(browser);
        foreach (var p in Pages) p.SetActive(p == page);
        addressText.text = address;
        ClearChoice();
    }
}
