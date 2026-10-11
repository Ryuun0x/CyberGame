using TMPro;
using UnityEngine;

/// <summary>
/// The phone screens used by Chapter 2's story moments. They are placed under PhoneFrame/Screen
/// in the PhoneCanvas prefab and stay hidden until a story beat shows one.
/// Every button on these screens calls Choose("...") from its OnClick in the Inspector;
/// the story waits until Choice is set.
/// Attach this to the Screen object.
/// </summary>
public class PhoneStory : MonoBehaviour
{
    [Header("\"Was this you?\" (2FA sign-in prompt)")]
    public GameObject signInAlert;
    public TextMeshProUGUI countdownText;
    public RectTransform countdownFill;

    [Header("Password changed → reset password")]
    public GameObject passwordChanged;
    public GameObject resetPassword;
    public TMP_InputField newPassword;
    public TMP_InputField confirmPassword;
    public TextMeshProUGUI resetError;

    [Header("Phone call with Kai")]
    public GameObject callScreen;
    public TextMeshProUGUI callText;

    [Header("Kai chat buttons (inside the Messages screen)")]
    public GameObject sendMoneyButton;
    public GameObject replyButton;
    public GameObject callKaiButton;

    // The id of the last button pressed (null = nothing pressed yet).
    public string Choice { get; private set; }

    // Called by the buttons' OnClick.
    public void Choose(string id) => Choice = id;

    public void ClearChoice() => Choice = null;

    // Shows one story screen on top of everything else on the phone.
    public void Show(GameObject screen)
    {
        GetComponentInChildren<PhoneHomeScreen>(true).HideAll();
        screen.SetActive(true);
        ClearChoice();
    }

    public void Hide(GameObject screen)
    {
        screen.SetActive(false);
        GetComponentInChildren<PhoneHomeScreen>(true).GoHome();
    }

    public void ShowKaiButtons(bool send, bool reply, bool call)
    {
        sendMoneyButton.SetActive(send);
        replyButton.SetActive(reply);
        callKaiButton.SetActive(call);
        ClearChoice();
    }
}
