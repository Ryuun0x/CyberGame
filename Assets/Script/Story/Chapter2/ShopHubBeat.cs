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
        foreach (var field in laptop.shopEmailFields) field.text = Progress.Email; // already filled in, like a browser would
        laptop.OpenPage(laptop.signUpPage, "www.shophub.ph/register");
        yield return WaitForLaptopChoice("reuse", "new");

        if (laptop.Choice == "reuse")
        {
            Progress.reusedPassword = true;
            Record("password_reuse", false);
            ConsequenceToast.Show();
            Say("Same password as always. Easier to remember.", 3f);
            yield break;
        }

        laptop.OpenPage(laptop.newPasswordPage, "www.shophub.ph/register");
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
        if (password.Length < MinPasswordLength) return $"<color=#C62828>Too short ({password.Length}/{MinPasswordLength})</color>";
        bool mixed = System.Text.RegularExpressions.Regex.IsMatch(password, @"\d|[^a-zA-Z0-9]");
        return password.Length >= 16 || mixed ? "<color=#2E7D32>Strength: strong</color>" : "<color=#E07B00>Strength: okay</color>";
    }

    // ── Part 2: choose a seller ──
    // Search results → the item page of the clicked listing (seller info, certification, feedback) → Buy It Now.
    // The clues that give the scam away (reviewer #5): no ratings, joined days ago, few followers, no business
    // registration, far too cheap, GCash only outside ShopHub, and a buyer's comment that the seller went quiet.

    struct Listing { public string title, price, details, seller, reviews; public bool ghost; }

    static Listing Info(string id)
    {
        if (id == "official") return new Listing
        {
            title = "Kalqo FX-991 Scientific Calculator, Brand New, Sealed",
            price = "₱1,200.00",
            details = "<b>Condition:</b> Brand new, sealed box\n<b>Warranty:</b> 1 year (official)\n<b>Shipping:</b> Free, 2–3 days\n<b>Payment:</b> ShopHub Checkout\n<color=#2E7D32>Money-back guarantee if it never arrives</color>",
            seller = "<b>Kalqo Official Store</b>\n<color=#C62828><b>Official Store</b></color>\n<sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"> 4.9 (12,400 ratings)\n99.6% positive feedback\nOn ShopHub since <b>2016</b> · 88.2k followers\nQuezon City\n<color=#2E7D32><sprite name=\"check\"> Authorized distributor · DTI registered</color>",
            reviews = "<sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\">  Original, with warranty card. Fast delivery!  <color=#888888>— mika_r</color>\n<sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\">  Same as the one in the mall, sealed box.  <color=#888888>— j.santos</color>\n<sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star_empty\">  A bit pricey but legit.  <color=#888888>— renz08</color>",
        };
        if (id == "depot") return new Listing
        {
            title = "Kalqo FX-991 Scientific Calculator (Original)",
            price = "₱900.00",
            details = "<b>Condition:</b> Brand new\n<b>Warranty:</b> 7-day replacement\n<b>Shipping:</b> ₱60, 2–4 days\n<b>Payment:</b> Cash on delivery or ShopHub Checkout\n<color=#2E7D32>Money-back guarantee with Checkout</color>",
            seller = "<b>calc.depot.mnl</b>\n<color=#1E4FB5><b>Preferred Seller</b></color>\n<sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"> 4.8 (213 ratings)\n98.1% positive feedback\nOn ShopHub since <b>2019</b> · 3.1k followers\nManila\n<color=#2E7D32><sprite name=\"check\"> DTI registered business</color>",
            reviews = "<sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\">  Arrived in 2 days, works perfectly.  <color=#888888>— anne.c</color>\n<sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star_empty\">  Box a bit dented but the calculator is fine.  <color=#888888>— kyle_m</color>\n<sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\"><sprite name=\"star\">  Paid on delivery, no problems.  <color=#888888>— bea.l</color>",
        };
        return new Listing
        {
            title = "FX-991 Scientific Calculator ORIGINAL!!! 77% OFF LAST ONE",
            price = "₱350.00  <size=60%><color=#888888><s>₱1,500.00</s></color></size>",
            details = "<b>Condition:</b> \"Brand new\"\n<b>Warranty:</b> none stated\n<b>Shipping:</b> \"Message seller\"\n<b>Payment:</b> <color=#C62828>GCash only, sent to the seller (not ShopHub Checkout)</color>\n<b>Stock:</b> Last one! 5 buyers are looking at this",
            seller = "<b>gadget.deals.ph</b>\n<color=#888888>No badge</color>\n<sprite name=\"star_empty\"><sprite name=\"star_empty\"><sprite name=\"star_empty\"><sprite name=\"star_empty\"><sprite name=\"star_empty\"> No ratings yet\nPositive feedback: —\n<color=#C62828>Joined ShopHub 3 days ago</color> · 12 followers\nLocation not shown\n<color=#C62828><sprite name=\"cross\"> No business registration shown</color>",
            reviews = "<b>Q:</b> Is this original? Can I pay COD?  <color=#888888>— marco.p · no answer from seller</color>\n<b>Q:</b> Paid via GCash 2 days ago, seller not replying??  <color=#888888>— tin.v</color>\n<color=#888888>No reviews yet.</color>",
            ghost = true,
        };
    }

    static IEnumerator BuyCalculator()
    {
        var laptop = Laptop;
        while (true)
        {
            laptop.OpenPage(laptop.listingsPage, "www.shophub.ph/search?q=scientific+calculator");
            yield return WaitForLaptopChoice("official", "depot", "scam");
            string pick = laptop.Choice;

            // The item page: look before you buy.
            var item = Info(pick);
            laptop.productTitle.text = item.title;
            laptop.productPrice.text = item.price;
            laptop.productDetails.text = item.details;
            laptop.sellerInfo.text = item.seller;
            laptop.reviewsText.text = item.reviews;
            laptop.sellerAvatar.gameObject.SetActive(item.ghost); // the Ghostline ghost as the scam seller's picture
            laptop.OpenPage(laptop.productPage, "www.shophub.ph/item/" + (pick == "scam" ? "88417" : pick == "depot" ? "51209" : "10293"));
            yield return WaitForLaptopChoice("buy", "back");
            if (laptop.Choice == "back") continue;

            if (pick != "scam")
            {
                Record("online_selling_scam", true);
                ConsequenceToast.Show();
                laptop.orderPlacedText.text = pick == "official"
                    ? "Order placed. Paid through ShopHub Checkout."
                    : "Order placed. You'll pay the courier on delivery.";
                laptop.OpenPage(laptop.orderPlacedPage, "www.shophub.ph/order/confirmed");
                yield return Wait(2.5f);
                break;
            }

            // The scam seller's chat.
            laptop.OpenPage(laptop.scamChatPage, "www.shophub.ph/chat/gadget.deals.ph");
            Progress.sawGhostlineSeller = true;
            Say("Wait... that profile picture. It's the same ghost I saw on that stranger's laptop at the café.", 5f);
            yield return WaitForLaptopChoice("pay", "back");

            if (laptop.Choice == "pay")
            {
                Progress.paidScamSeller = true;
                Record("online_selling_scam", false);
                ConsequenceToast.Show();
                laptop.orderPlacedText.text = "gadget.deals.ph: \"Payment received! Will ship tomorrow :)\"";
                laptop.OpenPage(laptop.orderPlacedPage, "www.shophub.ph/chat/gadget.deals.ph");
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
        yield return PickUpPhone("ShopHub", "Security notice: a data breach exposed customer passwords.");
        var phone = StoryPhone;
        PhoneHome.ShowMessage("ShopHub", "Security notice: a data breach exposed customer emails and passwords. If you use the same password anywhere else, change it now.");
        yield return Wait(4f);
        Say("Same password as my Goggle account...", 3f);
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
            Say("They got into my Goggle account with my ShopHub password...", 4f);
        }
        ClosePhone();
    }
}
