using System.Collections;
using static StoryTools;

// Chapter 2, beat 2 (scenario S7): Kai's hacked account asks for ₱1,000.
//   Send   → Compromised (the money goes to the scammer; the finale reveals it).
//   Reply  → the scammer answers "yes it's me", which proves nothing. The player can still choose.
//   Call   → a different channel reaches the real Kai: "That's not me!" → Safe.
// Screens: the Messages screen (with the SendMoney / Reply / CallKai buttons) and CallScreen.
public static class KaiMessageBeat
{
    public static IEnumerator Play()
    {
        var phone = OpenPhone();
        var home = PhoneHome;
        home.OpenChat("Kai");
        home.AddChat("Bes, emergency. Can you send ₱1,000 to 0917-xxx-xxxx? I'll pay you back tomorrow, please hurry.", false);

        bool replied = false;
        string choice;
        while (true)
        {
            phone.ShowKaiButtons(true, !replied, true);
            while (phone.Choice == null) yield return null;
            choice = phone.Choice;
            phone.ShowKaiButtons(false, false, false);
            if (choice != "reply") break;

            replied = true;
            home.AddChat("Kai? Is this really you?", true);
            yield return Wait(1.5f);
            home.AddChat("Yes it's me! Please hurry, no time to call.", false);
            yield return Wait(0.8f);
        }

        if (choice == "send")
        {
            home.AddChat("Sent ₱1,000 via GCash.", true);
            Progress.sentMoneyToScammer = true;
            Record("friend_scam", false);
            ConsequenceToast.Show();
            yield return Wait(2f);
            ClosePhone();
            Say("There. I hope Kai's okay...", 3.5f);
            yield break;
        }

        phone.Show(phone.callScreen);
        phone.callText.text = "Calling...";
        yield return Wait(1.8f);
        phone.callText.text = "\"That's not me! They still have my account. My cousin also got scammed buying a phone online.\"";
        Record("friend_scam", true);
        phone.ClearChoice();
        while (phone.Choice == null) yield return null; // End call
        phone.Hide(phone.callScreen);
        ConsequenceToast.Show();
        ClosePhone();
        Say("So it really was a scammer. Good thing I called.", 3.5f);
    }
}
