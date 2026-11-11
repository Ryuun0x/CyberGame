using System;
using UnityEditor;
using UnityEngine;

// Quick automatic test of the café scenarios. Open MainScene(city), enter Play mode, then run
// CADSNET > Cafe > Run Wi-Fi Checks. It uses the objects placed in the scene (counter, laptop Wi-Fi, update popup).
public static class CafeWifiChecks
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[CafeWifiChecks] " + message);
    }

    [MenuItem("CADSNET/Cafe/Run Wi-Fi Checks")]
    public static void Run()
    {
        Check(Application.isPlaying, "Enter Play mode in MainScene(city) first.");
        var wifi = UnityEngine.Object.FindFirstObjectByType<LaptopWifi>(FindObjectsInactive.Include);
        var banner = UnityEngine.Object.FindFirstObjectByType<FakeUpdateBanner>(FindObjectsInactive.Include);
        var counter = UnityEngine.Object.FindFirstObjectByType<CafeCounter>(FindObjectsInactive.Include);
        Check(wifi != null && banner != null && counter != null, "Run this in MainScene(city).");

        // Started straight in the city scene: borrow a GameManager (progress + threat log) for the test.
        GameObject borrowed = null;
        if (GameProgressManager.Instance == null)
        {
            borrowed = new GameObject("GameManager (check)");
            borrowed.AddComponent<GameProgressManager>();
            borrowed.AddComponent<ThreatLog>();
        }
        var gp = GameProgressManager.Instance;
        var log = ThreatLog.Instance;
        bool wasInCafe = CafeZone.PlayerInCafe;
        CafeZone.PlayerInCafe = true;
        // The laptop screen is hidden until the player sits down; the popups need it showing.
        bool wasOpen = wifi.gameObject.activeSelf;
        wifi.gameObject.SetActive(true);
        try
        {
            ThreatLog.Entry Find(string id) { foreach (var e in log.Entries) if (e.id == id) return e; return default; }
            ThreatLog.Entry Evil() => Find("evil_twin");
            void Fresh() { log.ResetLog(); gp.credentialsStolen = false; wifi.Disconnect(); }

            // Ordering prints the receipt with the real password.
            gp.hasReceipt = false;
            counter.ServeNow(); // the order-wait cutscene is skipped here
            Check(gp.hasReceipt && CafeReceipt.IsOpen, "Ordering gives a receipt.");

            // Real network: wrong key refused, right key → Safe.
            Fresh();
            wifi.ToggleList();
            wifi.Join(LaptopWifi.Real);
            Check(!wifi.TryPassword("cafe1234") && !wifi.HasInternet, "Wrong password refused.");
            Check(wifi.TryPassword(CafeCounter.WifiPassword) && wifi.Network == LaptopWifi.Real, "Receipt password connects.");
            Check(Evil().outcome == ThreatOutcome.Safe && !gp.credentialsStolen, "Real network is Safe.");

            // The neighbour's network never accepts the receipt password.
            Fresh();
            wifi.Join(LaptopWifi.Neighbor);
            Check(!wifi.TryPassword(CafeCounter.WifiPassword) && !wifi.HasInternet, "Neighbour network refuses the café password.");

            // Fake network, Cancel → Recovered, no internet.
            Fresh();
            wifi.Join(LaptopWifi.Twin);
            wifi.CancelPortal();
            Check(!wifi.HasInternet && Evil().outcome == ThreatOutcome.Recovered && !gp.credentialsStolen, "Cancel is Recovered.");

            // Fake network, Sign in → works, Compromised, credentials stolen.
            Fresh();
            wifi.Join(LaptopWifi.Twin);
            Check(!wifi.SignIn("", ""), "Empty sign-in refused.");
            Check(wifi.SignIn("student@school.edu", "hunter2") && wifi.HasInternet, "Sign in gives internet.");
            Check(Evil().outcome == ThreatOutcome.Compromised && gp.credentialsStolen && gp.connectedToCafeWiFi, "Sign in is Compromised.");

            // Backing out once doesn't excuse a later sign-in.
            Fresh();
            wifi.Join(LaptopWifi.Twin);
            wifi.CancelPortal();
            wifi.Join(LaptopWifi.Twin);
            wifi.SignIn("student@school.edu", "hunter2");
            Check(Evil().outcome == ThreatOutcome.Compromised, "Later sign-in overrides Recovered.");

            // Leaving the café drops the connection.
            CafeZone.PlayerInCafe = false;
            Check(!wifi.HasInternet, "Leaving the café disconnects.");

            // Fake browser update: X → Safe; Download then Don't run / Scan → Recovered; Download then Run → Compromised
            // + flag, and a later X can't undo it.
            log.ResetLog(); banner.ResetForChecks();
            banner.Show(); banner.Dismiss();
            Check(Find("fake_update").outcome == ThreatOutcome.Safe && !gp.installedFakeUpdate, "Fake update X is Safe.");
            log.ResetLog(); banner.ResetForChecks();
            banner.Show(); banner.Download(); banner.DontRun();
            Check(Find("fake_update").outcome == ThreatOutcome.Recovered && !gp.installedFakeUpdate, "Fake update Don't run is Recovered.");
            log.ResetLog(); banner.ResetForChecks();
            banner.Show(); banner.Download(); banner.Scan();
            Check(Find("fake_update").outcome == ThreatOutcome.Recovered && !gp.installedFakeUpdate, "Fake update Scan is Recovered.");
            log.ResetLog(); banner.ResetForChecks();
            banner.Show(); banner.Run();
            Check(!gp.installedFakeUpdate, "Run needs the download first.");
            banner.Download(); banner.Run(); banner.Dismiss();
            Check(Find("fake_update").outcome == ThreatOutcome.Compromised && gp.installedFakeUpdate, "Fake update Run is Compromised.");
            banner.ResetForChecks();
            Check(ThreatCatalog.Get("fake_update")?.Category == "Malware", "fake_update is in the catalog.");
            Check(ThreatCatalog.Get("login_alert")?.Category == "Accounts", "login_alert is in the catalog.");

            // Pending consequences stay out of the score until resolved.
            log.ResetLog();
            log.Record("evil_twin", false);
            log.MarkPending("evil_twin");
            Check(log.ScorePercent() == 100 && !log.AnyCompromised(), "Pending threats are left out of the score.");
            log.Resolve("evil_twin");
            Check(log.ScorePercent() == 0 && log.AnyCompromised(), "Resolved threats count again.");

            Debug.Log("[CafeWifiChecks] PASS: receipt, wrong/right password, Safe, Cancel → Recovered, Sign in → Compromised + stolen, override, leave café, fake update (X / Don't run / Scan / Run), pending.");
        }
        finally
        {
            log.ResetLog();
            wifi.Disconnect();
            CafeZone.PlayerInCafe = wasInCafe;
            wifi.gameObject.SetActive(wasOpen);
            if (borrowed != null) UnityEngine.Object.DestroyImmediate(borrowed);
            else { gp.credentialsStolen = false; gp.hasReceipt = false; gp.installedFakeUpdate = false; gp.accountHijacked = false; }
        }
    }
}
