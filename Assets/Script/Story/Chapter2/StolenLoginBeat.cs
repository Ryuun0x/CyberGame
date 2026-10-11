using System.Collections;
using UnityEngine;
using static StoryTools;

// Chapter 2, beat 1: the school password the player typed into the fake café Wi-Fi gets used.
// Only plays if GameProgressManager.credentialsStolen is true.
//   2FA on  → "Was this you?" with a 10 second timer. Deny = Recovered, Approve or no answer = Compromised.
//   2FA off → nothing stopped the attacker: "Your password was changed" → reset it. Compromised.
// Screens: PhoneCanvas → Screen → SignInAlertScreen, PasswordChangedScreen, ResetPasswordScreen.
public static class StolenLoginBeat
{
    const float PromptSeconds = 10;

    // Set by AskWasThisYou: true if the player pressed Deny in time.
    public static bool Denied;

    public static IEnumerator Play()
    {
        var phone = OpenPhone();
        if (Progress.is2FAEnabled)
        {
            yield return AskWasThisYou(phone);
            if (Denied) Exposed("login_alert");
            Record("login_alert", Denied);
            Progress.accountHijacked = !Denied;
            ConsequenceToast.Show();
            if (Denied) Say("That wasn't me... I should change my password.", 4f);
        }
        else
        {
            Record("login_alert", false);
            Progress.accountHijacked = true;
            yield return PasswordWasChanged(phone);
            Say("Someone changed my password... At least I got my account back.", 4f);
        }
        Resolve("login_alert");
        ClosePhone();
    }

    // The 2FA prompt. Also used by ShopHubBeat when a reused password leaks.
    public static IEnumerator AskWasThisYou(PhoneStory phone)
    {
        phone.Show(phone.signInAlert);
        for (float left = PromptSeconds; phone.Choice == null; left -= Time.unscaledDeltaTime)
        {
            if (left <= 0) phone.Choose("approve"); // not answering lets the sign-in through
            phone.countdownText.text = $"Expires in {Mathf.CeilToInt(Mathf.Max(left, 0))}s";
            phone.countdownFill.anchorMax = new Vector2(Mathf.Max(left, 0) / PromptSeconds, 1);
            yield return null;
        }
        Denied = phone.Choice == "deny";
        phone.Hide(phone.signInAlert);
    }

    // Also used by ShopHubBeat when a reused password leaks and there is no 2FA.
    public static IEnumerator PasswordWasChanged(PhoneStory phone)
    {
        phone.Show(phone.passwordChanged);
        while (phone.Choice == null) yield return null;
        phone.Hide(phone.passwordChanged);

        phone.Show(phone.resetPassword);
        phone.newPassword.text = "";
        phone.confirmPassword.text = "";
        phone.resetError.text = "At least 8 characters.";
        while (true)
        {
            while (phone.Choice == null) yield return null;
            if (phone.newPassword.text.Length < 8) phone.resetError.text = "Use at least 8 characters.";
            else if (phone.newPassword.text != phone.confirmPassword.text) phone.resetError.text = "The passwords don't match.";
            else break;
            phone.ClearChoice();
        }
        phone.Hide(phone.resetPassword);
    }
}
