using System.Collections;
using static StoryTools;

// Chapter 2, beat 4: buying a scientific calculator on "ShopHub" (a made-up marketplace) in the laptop browser.
//   Part 1 (scenario S8): sign up. Reuse the school password (Compromised) or make a new long one (Safe).
//   Part 2 (scenario S6): pick a seller. "gadget.deals.ph" is a scam: new account, no reviews, too cheap,
//          GCash only. Its profile picture is the Ghostline ghost from the café stranger's laptop (a clue).
//   Part 3: if the password was reused, ShopHub gets breached and the school account is attacked again.
// Pages: LaptopCanvas > NightApps > Browser > SignUpPage, NewPasswordPage, ListingsPage, ScamChatPage, OrderPlacedPage.
public static class ShopHubBeat
{
    const int MinPasswordLength = 12;

    // Parts 1 and 2, on the laptop. Chapter2 runs Part 3 (PasswordLeak) after the laptop is closed.
    public static IEnumerator PlaySignUpAndShop()
    {
        yield return SignUp();
        yield return BuyCalculator();
        Laptop.OpenPage(Laptop.startPage, "start.page");
    }

    // ── Part 1: sign up (reused vs. new password) ──

    static IEnumerator SignUp()
    {
        var laptop = Laptop;
        laptop.OpenPage(laptop.signUpPage, "ShopHub > Create account");
        yield return WaitForLaptopChoice("reuse", "new");

        if (laptop.Choice == "reuse")
        {
            Progress.reusedPassword = true;
            Record("password_reuse", false);
            ConsequenceToast.Show();
            Say("Same password as always. Easier to remember.", 3f);
            yield break;
        }

        laptop.OpenPage(laptop.newPasswordPage, "ShopHub > Create account");
        laptop.newPasswordField.text = "";
        laptop.newPasswordField.ActivateInputField();
        while (laptop.Choice != "create" || laptop.newPasswordField.text.Length < MinPasswordLength)
        {
            if (laptop.Choice == "create") laptop.ClearChoice(); // too short: the button does nothing yet
            laptop.passwordStrength.text = StrengthOf(laptop.newPasswordField.text);
            yield return null;
        }
        Record("password_reuse", true);
        ConsequenceToast.Show();
    }

    // Length matters most; mixing in numbers or symbols helps a little.
    static string StrengthOf(string password)
    {
        if (password.Length == 0) return "";
        if (password.Length < MinPasswordLength) return $"Too short ({password.Length}/{MinPasswordLength})";
        bool mixed = System.Text.RegularExpressions.Regex.IsMatch(password, @"\d|[^a-zA-Z0-9]");
        return password.Length >= 16 || mixed ? "Strength: strong" : "Strength: okay";
    }

    // ── Part 2: choose a seller ──

    static IEnumerator BuyCalculator()
    {
        var laptop = Laptop;
        while (true)
        {
            laptop.OpenPage(laptop.listingsPage, "ShopHub > scientific calculator");
            yield return WaitForLaptopChoice("official", "depot", "scam");
            string pick = laptop.Choice;

            if (pick != "scam")
            {
                Record("online_selling_scam", true);
                ConsequenceToast.Show();
                laptop.orderPlacedText.text = pick == "official"
                    ? "Order placed. Paid through ShopHub Checkout."
                    : "Order placed. You'll pay the courier on delivery.";
                laptop.OpenPage(laptop.orderPlacedPage, "ShopHub > Order placed");
                yield return Wait(2.5f);
                break;
            }

            // The scam seller's chat.
            laptop.OpenPage(laptop.scamChatPage, "ShopHub > Chat · gadget.deals.ph");
            Progress.sawGhostlineSeller = true;
            Say("Wait... that profile picture. It's the same ghost I saw on that stranger's laptop at the café.", 5f);
            yield return WaitForLaptopChoice("pay", "back");

            if (laptop.Choice == "pay")
            {
                Progress.paidScamSeller = true;
                Record("online_selling_scam", false);
                ConsequenceToast.Show();
                laptop.orderPlacedText.text = "gadget.deals.ph: \"Payment received! Will ship tomorrow :)\"";
                laptop.OpenPage(laptop.orderPlacedPage, "ShopHub > Chat · gadget.deals.ph");
                yield return Wait(2.5f);
                break;
            }
            Exposed("online_selling_scam"); // backed out: picking a safe seller now counts as Recovered
        }
        CompleteObjective(1);
        Say("Okay, the calculator's sorted.", 3f);
    }

    // ── Part 3: the reused password leaks (runs on the phone, after the laptop is closed) ──

    public static IEnumerator PasswordLeak()
    {
        var phone = OpenPhone();
        PhoneHome.ShowMessage("ShopHub", "Security notice: a data breach exposed customer emails and passwords. If you use the same password anywhere else, change it now.");
        yield return Wait(4f);
        Say("Same password as my school account...", 3f);
        yield return Wait(1.5f);

        if (Progress.is2FAEnabled)
        {
            yield return StolenLoginBeat.AskWasThisYou(phone);
            if (!StolenLoginBeat.Denied) Progress.accountHijacked = true;
            Say(StolenLoginBeat.Denied ? "2FA stopped them. I'm changing that password now." : "Oh no... I let them in.", 4f);
        }
        else
        {
            Progress.accountHijacked = true;
            yield return StolenLoginBeat.PasswordWasChanged(phone);
            Say("They got into my school account with my ShopHub password...", 4f);
        }
        ClosePhone();
    }
}
