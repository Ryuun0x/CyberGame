using System;
using UnityEditor;
using UnityEngine;

public static class CafeWifiChecks
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[CafeWifiChecks] " + message);
    }

    [MenuItem("CADSNET/Cafe/Run Wi-Fi Checks")]
    public static void Run()
    {
        Check(Application.isPlaying && ThreatLog.Instance != null, "Enter Play mode first.");
        var log = ThreatLog.Instance;
        var gp = GameProgressManager.Instance;
        bool ownGp = gp == null;
        if (ownGp) gp = new GameObject("GameProgressManager (check)").AddComponent<GameProgressManager>();
        bool wasInCafe = CafeZone.PlayerInCafe;
        CafeZone.PlayerInCafe = true;
        var canvas = new GameObject("Laptop (check)", typeof(RectTransform), typeof(Canvas));
        try
        {
            ThreatLog.Entry Find(string id) { foreach (var e in log.Entries) if (e.id == id) return e; return default; }
            ThreatLog.Entry Evil() => Find("evil_twin");
            LaptopWifi Fresh()
            {
                log.ResetLog();
                gp.credentialsStolen = false;
                var old = canvas.GetComponent<LaptopWifi>();
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
                return canvas.AddComponent<LaptopWifi>();
            }

            // Ordering prints the receipt with the real password.
            var counter = new GameObject("Counter (check)").AddComponent<CafeCounter>();
            gp.hasReceipt = false;
            counter.Interact();
            Check(gp.hasReceipt && CafeReceipt.IsOpen, "Ordering gives a receipt.");
            UnityEngine.Object.DestroyImmediate(counter.gameObject);

            // Real network: wrong key refused, right key → Safe.
            var wifi = Fresh();
            wifi.ToggleList();
            wifi.Join(CafeCounter.WifiName);
            Check(!wifi.TryPassword("cafe1234") && !wifi.HasInternet, "Wrong password refused.");
            Check(wifi.TryPassword(CafeCounter.WifiPassword) && wifi.Network == CafeCounter.WifiName, "Receipt password connects.");
            Check(Evil().outcome == ThreatOutcome.Safe && !gp.credentialsStolen, "Real network is Safe.");
            Check(UnityEngine.Object.FindFirstObjectByType<ConsequenceToast>() != null, "Toast after real network.");

            // Fake network, Cancel → Recovered, no internet.
            wifi = Fresh();
            wifi.Join(LaptopWifi.FreeA);
            wifi.CancelPortal();
            Check(!wifi.HasInternet && Evil().outcome == ThreatOutcome.Recovered && !gp.credentialsStolen, "Cancel is Recovered.");

            // Fake network, Sign in → works, Compromised, credentials stolen.
            wifi = Fresh();
            wifi.Join(LaptopWifi.FreeB);
            Check(!wifi.SignIn("", ""), "Empty sign-in refused.");
            Check(wifi.SignIn("student@school.edu", "hunter2") && wifi.HasInternet, "Sign in gives internet.");
            Check(Evil().outcome == ThreatOutcome.Compromised && gp.credentialsStolen && gp.connectedToCafeWiFi, "Sign in is Compromised.");

            // Backing out once doesn't excuse a later sign-in.
            wifi = Fresh();
            wifi.Join(LaptopWifi.FreeA);
            wifi.CancelPortal();
            wifi.Join(LaptopWifi.FreeB);
            wifi.SignIn("student@school.edu", "hunter2");
            Check(Evil().outcome == ThreatOutcome.Compromised, "Later sign-in overrides Recovered.");

            // Leaving the café drops the connection.
            CafeZone.PlayerInCafe = false;
            wifi.SendMessage("Update");
            Check(!wifi.HasInternet, "Leaving the café disconnects.");

            // Fake browser update: X → Safe; Download → Compromised + flag, and a second click can't undo it.
            log.ResetLog();
            var banner = canvas.AddComponent<FakeUpdateBanner>();
            banner.Dismiss();
            Check(Find("fake_update").outcome == ThreatOutcome.Safe && log.HasRecord("fake_update") && !gp.installedFakeUpdate, "Fake update X is Safe.");
            UnityEngine.Object.DestroyImmediate(banner);
            log.ResetLog();
            banner = canvas.AddComponent<FakeUpdateBanner>();
            banner.Download();
            banner.Dismiss();
            Check(Find("fake_update").outcome == ThreatOutcome.Compromised && gp.installedFakeUpdate, "Fake update Download is Compromised.");
            Check(ThreatCatalog.Get("fake_update")?.Category == "Malware", "fake_update is in the catalog.");

            Debug.Log("[CafeWifiChecks] PASS: receipt, wrong/right password, Safe, Cancel → Recovered, Sign in → Compromised + stolen, override, leave café, toast, fake update.");
        }
        finally
        {
            log.ResetLog();
            CafeZone.PlayerInCafe = wasInCafe;
            UnityEngine.Object.DestroyImmediate(canvas);
            foreach (var r in UnityEngine.Object.FindObjectsByType<CafeReceipt>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(r.gameObject);
            if (ownGp) UnityEngine.Object.DestroyImmediate(gp.gameObject);
            else { gp.credentialsStolen = false; gp.hasReceipt = false; gp.installedFakeUpdate = false; }
        }
    }
}
