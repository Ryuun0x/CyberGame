using System.Collections.Generic;
using UnityEngine;

public enum ThreatOutcome { Safe, Recovered, Compromised }

public class ThreatLog : MonoBehaviour
{
    public struct Entry
    {
        public string id;
        public ThreatOutcome outcome;
    }

    public static ThreatLog Instance { get; private set; }

    readonly List<Entry> _entries = new List<Entry>();
    readonly HashSet<string> _exposed = new HashSet<string>();

    public IReadOnlyList<Entry> Entries => _entries;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("ThreatLog");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<ThreatLog>();
    }

    public void MarkExposed(string id)
    {
        _exposed.Add(id);
    }

    public void Record(string id, bool safe)
    {
        if (HasRecord(id)) return;

        ThreatOutcome outcome = !safe ? ThreatOutcome.Compromised
            : _exposed.Contains(id) ? ThreatOutcome.Recovered
            : ThreatOutcome.Safe;

        _entries.Add(new Entry { id = id, outcome = outcome });
        Debug.Log($"[ThreatLog] {id} → {outcome}");
    }

    public bool HasRecord(string id)
    {
        foreach (var e in _entries)
            if (e.id == id) return true;
        return false;
    }

    public bool AnyCompromised()
    {
        foreach (var e in _entries)
        {
            var info = ThreatCatalog.Get(e.id);
            if (info != null && info.Kind == ThreatKind.Threat && e.outcome == ThreatOutcome.Compromised)
                return true;
        }
        return false;
    }

    public int ScorePercent()
    {
        int total = 0, count = 0;
        foreach (var e in _entries)
        {
            var info = ThreatCatalog.Get(e.id);
            if (info == null || info.Kind != ThreatKind.Threat) continue;
            count++;
            total += e.outcome == ThreatOutcome.Safe ? 100 : e.outcome == ThreatOutcome.Recovered ? 60 : 0;
        }
        return count == 0 ? 100 : Mathf.RoundToInt(total / (float)count);
    }

    public static string RatingFor(int percent)
    {
        if (percent >= 90) return "Cyber-Savvy";
        if (percent >= 60) return "Getting There";
        return "At Risk";
    }

    public void ResetLog()
    {
        _entries.Clear();
        _exposed.Clear();
    }
}
