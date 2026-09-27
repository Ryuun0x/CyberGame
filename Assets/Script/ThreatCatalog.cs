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
            SafeResult = "You got the password from your café receipt and joined the real, password-protected network. Your school login never left your laptop.",
            RecoveredResult = "You opened a lookalike network's sign-in page, but backed out without typing your school login.",
            CompromisedResult = "You typed your school email and password into a fake Wi-Fi sign-in page. The attacker running that network now has your school login.",
            Lesson = "Get the exact network name and password from staff or your receipt. Treat open lookalike \"free\" networks as suspicious, and never type your school or email password into a Wi-Fi sign-in page.",
            Source = "Protections: FTC, \"Are Public Wi-Fi Networks Safe? What You Need To Know\" (2023)"
        });

        Add(new ThreatInfo
        {
            Id = "login_alert",
            Kind = ThreatKind.Threat,
            Category = "Accounts",
            Title = "Stolen password used to sign in",
            RecoveredResult = "Someone tried to sign in with the school password the fake Wi-Fi stole. 2FA asked you first and you denied it, so they stayed out. Change that password now.",
            CompromisedResult = "Someone signed in to your school account with the password the fake Wi-Fi stole: either you approved a sign-in you didn't start, or without 2FA nothing stopped them from changing your password.",
            Lesson = "Never approve a sign-in request you didn't start: deny it, then change your password. 2FA only protects you if you say no to prompts you don't recognize.",
            Source = "CISA, \"Implementing Number Matching in MFA Applications\" (2022): push-fatigue attacks"
        });

        Add(new ThreatInfo
        {
            Id = "fake_update",
            Kind = ThreatKind.Threat,
            Category = "Malware",
            Title = "Fake browser update",
            SafeResult = "A web page said your browser was out of date. You ignored it and nothing was installed.",
            CompromisedResult = "You downloaded an \"update\" from a pop-up on a web page. Real updates don't arrive that way, and whatever you installed is now on your laptop.",
            Lesson = "Only update software through your system's own updater or the developer's official site. Never download an update from a pop-up or banner on a web page.",
            Source = "CISA, Secure Our World: \"Update Software\""
        });

        Add(new ThreatInfo
        {
            Id = "backup",
            Kind = ThreatKind.Habit,
            Category = "Data",
            Title = "Back up before you travel",
            SafeResult = "You copied your thesis to a flash drive before leaving, so one lost or broken laptop can't erase your work.",
            CompromisedResult = "You left home with a single copy of your thesis.",
            Lesson = "Keep at least one copy of important files offline, on a drive you unplug, so malware or theft can't reach every copy.",
            Source = "CISA, #StopRansomware Guide (2023): maintain offline, encrypted backups"
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
            Source = "CISA, Secure Our World: \"Turn On MFA\"; Microsoft Security Blog (Aug 2019)"
        });
    }

    static void Add(ThreatInfo info) => _byId[info.Id] = info;

    public static ThreatInfo Get(string id) => _byId.TryGetValue(id, out var info) ? info : null;
}
