using System.Collections.Generic;

public enum ThreatKind { Threat, Habit }

public class ThreatInfo
{
    public string Id;
    public ThreatKind Kind;
    public string Category;
    public string Title;
    public string SafeResult;
    public string RecoveredResult;
    public string CompromisedResult;
    public string Lesson;
    public string Better;   // shown when the player got it wrong: the right action and what it would have done
    public string Source;
}

public static class ThreatCatalog
{
    static readonly Dictionary<string, ThreatInfo> _byId = new Dictionary<string, ThreatInfo>();

    static ThreatCatalog()
    {
        Add(new ThreatInfo
        {
            Id = "evil_twin",
            Kind = ThreatKind.Threat,
            Category = "Network",
            Title = "Fake café Wi-Fi (evil twin)",
            SafeResult = "You got the password from your café receipt and joined the real, password-protected network. Your Goggle login never left your laptop.",
            RecoveredResult = "You picked the open copy of the café's network and saw its sign-in page, but backed out without typing your login.",
            CompromisedResult = "You typed your email and password into a fake Goggle sign-in on the open copy of the café's Wi-Fi. Its address was goggle-login.freewifi-portal.net, not accounts.goggle.com. The attacker running that network now has your email login, and you sent your thesis from that account.",
            Lesson = "Attackers copy a café's exact Wi-Fi name. Join the network that asks for the password on your receipt or from staff, treat an open copy with the same name as suspicious, and never type your email password into a Wi-Fi sign-in page. A real sign-in page is on the service's own address (https://accounts.goggle.com), never on the Wi-Fi's.",
            Better = "Join the secured \"CafeWifi67\" with the password on your receipt → your login never reaches the attacker.",
            Source = "Protections: FTC, \"Are Public Wi-Fi Networks Safe? What You Need To Know\" (2023)"
        });

        Add(new ThreatInfo
        {
            Id = "login_alert",
            Kind = ThreatKind.Threat,
            Category = "Accounts",
            Title = "Stolen password used to sign in",
            RecoveredResult = "Someone tried to sign in with the Goggle password the fake Wi-Fi stole. 2FA asked you first and you denied it, so they stayed out. Change that password now.",
            CompromisedResult = "Someone signed in to your Goggle account with the password the fake Wi-Fi stole: either you approved a sign-in you didn't start, or without 2FA nothing stopped them from changing your password.",
            Lesson = "Never approve a sign-in request you didn't start: deny it, then change your password. 2FA only protects you if you say no to prompts you don't recognize.",
            Better = "Deny the sign-in you didn't start, then change your password → the attacker stays locked out.",
            Source = "CISA, \"Implementing Number Matching in MFA Applications\" (2022): push-fatigue attacks"
        });

        Add(new ThreatInfo
        {
            Id = "friend_scam",
            Kind = ThreatKind.Threat,
            Category = "Social engineering",
            Title = "Friend's hacked account asks for money",
            SafeResult = "Kai's account asked you to send ₱1,000. You called Kai first, found out the account was hacked, and sent nothing.",
            CompromisedResult = "You sent ₱1,000 to a scammer using Kai's hacked account. Kai never got it.",
            Lesson = "When a friend's account suddenly asks for money, verify through another channel, like a phone call. Hacked accounts send friends fake pleas for help or money, and replying in the same chat only reaches the scammer.",
            Better = "Call Kai before sending anything → you find out the account was hacked and keep your ₱1,000.",
            Source = "FTC, \"How to Recover Your Hacked Email or Social Media Account\""
        });

        Add(new ThreatInfo
        {
            Id = "fake_update",
            Kind = ThreatKind.Threat,
            Category = "Malware",
            Title = "Fake browser update",
            SafeResult = "A web page said your browser was out of date. You closed it and nothing was downloaded.",
            RecoveredResult = "You downloaded the \"update\" from a pop-up, but stopped at the security warning: you didn't run it, or the antivirus scan found ransomware and removed it. Nothing was installed.",
            CompromisedResult = "You downloaded an \"update\" from a pop-up and ran it, even though the warning said \"Unknown Publisher\". Real updates don't arrive that way, and it installed ransomware on your laptop.",
            Lesson = "Only update software through your system's own updater or the developer's official site. Never download an update from a pop-up on a web page, and if you did, scan the file and don't run anything from an unknown publisher.",
            Better = "Close the pop-up (or scan the file before running it) and update only through your system's own updater → nothing harmful gets installed.",
            Source = "CISA, Secure Our World: \"Update Software\""
        });

        Add(new ThreatInfo
        {
            Id = "backup",
            Kind = ThreatKind.Habit,
            Category = "Data",
            Title = "Back up your files",
            SafeResult = "You kept two copies: one on a flash drive and one on your school Drive. A lost drive or a broken laptop can't erase your work.",
            RecoveredResult = "You made one backup. Better than none, but if that one copy is lost or locked, your work is gone.",
            CompromisedResult = "You left home with a single copy of your thesis.",
            Lesson = "Keep at least two backups in different places: one offline (a flash drive you unplug) and one in the cloud. Malware or a lost drive can't reach both.",
            Better = "Copy it to the flash drive at home, then save it to your Drive once online → two copies that can't be lost together.",
            Source = "CISA, #StopRansomware Guide (2023): maintain offline, encrypted backups"
        });

        Add(new ThreatInfo
        {
            Id = "ransomware",
            Kind = ThreatKind.Threat,
            Category = "Malware",
            Title = "Files locked for ransom",
            SafeResult = "The fake update locked your files and demanded ₱5,000. You didn't pay: you reset the laptop and restored your thesis from your backup (the USB drive or Goggle Drive), if you had one.",
            CompromisedResult = "You paid the ransom. The attackers just asked for more, and the files stayed locked.",
            Lesson = "Never pay a ransom: it doesn't guarantee your files come back and it funds the attackers. A backup kept somewhere else (a USB drive, a cloud drive, ideally both) is what lets you recover.",
            Better = "Don't pay: reset the laptop and restore from your USB or cloud backup → you get your files back and the attackers get nothing.",
            Source = "CISA, #StopRansomware Guide (2023)"
        });

        Add(new ThreatInfo
        {
            Id = "password_reuse",
            Kind = ThreatKind.Threat,
            Category = "Passwords",
            Title = "Reused password",
            SafeResult = "You made a new, long password just for ShopHub, so a leak there can't open your other accounts.",
            CompromisedResult = "You reused your Goggle password on ShopHub. When ShopHub was breached, that password opened your Goggle account too.",
            Lesson = "Use a different, long password for every account (CISA recommends at least 16 characters). A password manager remembers them for you.",
            Better = "Make a new, long password just for ShopHub → a leak there can't open your Goggle account.",
            Source = "CISA, Secure Our World: \"Use Strong Passwords\""
        });

        Add(new ThreatInfo
        {
            Id = "online_selling_scam",
            Kind = ThreatKind.Threat,
            Category = "Online scams",
            Title = "Fake online seller",
            SafeResult = "You bought from a seller with a long history and good reviews, paying in a way that protects you.",
            RecoveredResult = "You opened the too-cheap seller's chat, but backed out and bought from a trusted seller instead.",
            CompromisedResult = "You paid a brand-new seller with no reviews upfront by GCash. The calculator never arrived.",
            Lesson = "Check the seller's reviews and account age, be wary of prices far below market, and avoid paying strangers upfront by e-wallet. Use the platform's checkout or cash on delivery.",
            Better = "Buy from the official store with years of good reviews and pay through checkout → you get the calculator.",
            Source = "FTC, \"Buying From an Online Marketplace\"; PNP-ACG: online selling scams were the most common cybercrime in 2024–2025"
        });

        Add(new ThreatInfo
        {
            Id = "mfa",
            Kind = ThreatKind.Habit,
            Category = "Accounts",
            Title = "Two-factor authentication",
            SafeResult = "You turned on 2FA, so a stolen password alone is not enough to get into your account.",
            CompromisedResult = "Your account was protected by a password only.",
            Lesson = "Turn on MFA for school, email and banking accounts. Microsoft found accounts using MFA are over 99.9% less likely to be compromised.",
            Better = "Turn on the authenticator → a stolen password alone can't get into your account.",
            Source = "CISA, Secure Our World: \"Turn On MFA\"; Microsoft Security Blog (Aug 2019)"
        });
    }

    static void Add(ThreatInfo info) => _byId[info.Id] = info;

    public static ThreatInfo Get(string id) => _byId.TryGetValue(id, out var info) ? info : null;
}
