using System.Collections.Generic;
using UnityEngine;

public enum ThreatOutcome { Safe, Recovered, Compromised }

public class ThreatLog : MonoBehaviour
{
    public struct Entry
    {
        public string id;
        public ThreatOutcome outcome;
        public int chapter; // the chapter whose rundown shows it: where it was recorded, or later resolved
    }

    static int CurrentChapter => GameProgressManager.Instance != null ? GameProgressManager.Instance.chapter : 1;

    // Placed on the GameManager object (next to GameProgressManager), which survives scene loads.
    public static ThreatLog Instance;

    readonly List<Entry> _entries = new List<Entry>();
    readonly HashSet<string> _exposed = new HashSet<string>();
    readonly HashSet<string> _pending = new HashSet<string>();

    public IReadOnlyList<Entry> Entries => _entries;
    public IReadOnlyCollection<string> Pending => _pending;

    void Awake()
    {
        // Same singleton rule as GameProgressManager: keep the first one, drop copies from reloaded scenes.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void MarkExposed(string id)
    {
        _exposed.Add(id);
    }

    // A choice whose consequence plays out later (e.g. in Chapter 2): the debrief shows it as pending and
    // leaves it out of the score until Resolve. It may or may not have an entry recorded already.
    public void MarkPending(string id) => _pending.Add(id);
    // Resolved: its result is final now, so it moves to this chapter's rundown.
    public void Resolve(string id)
    {
        _pending.Remove(id);
        int i = _entries.FindIndex(e => e.id == id);
        if (i >= 0)
        {
            var e = _entries[i];
            e.chapter = CurrentChapter;
            _entries[i] = e;
        }
    }
    public bool IsPending(string id) => _pending.Contains(id);

    public void Record(string id, bool safe)
    {
        int existing = _entries.FindIndex(e => e.id == id);
        if (existing >= 0)
        {
            // A later unsafe choice still counts (e.g. backing out once, then signing in on another fake network).
            if (!safe && _entries[existing].outcome != ThreatOutcome.Compromised)
            {
                _entries[existing] = new Entry { id = id, outcome = ThreatOutcome.Compromised, chapter = _entries[existing].chapter };
                Debug.Log($"[ThreatLog] {id} → {ThreatOutcome.Compromised}");
            }
            return;
        }

        ThreatOutcome outcome = !safe ? ThreatOutcome.Compromised
            : _exposed.Contains(id) ? ThreatOutcome.Recovered
            : ThreatOutcome.Safe;

        _entries.Add(new Entry { id = id, outcome = outcome, chapter = CurrentChapter });
        Debug.Log($"[ThreatLog] {id} → {outcome}");
    }

    public bool HasRecord(string id)
    {
        foreach (var e in _entries)
            if (e.id == id) return true;
        return false;
    }

    // chapter 0: the whole game.
    public bool AnyCompromised(int chapter = 0)
    {
        foreach (var e in _entries)
        {
            if (chapter != 0 && e.chapter != chapter) continue;
            var info = ThreatCatalog.Get(e.id);
            if (info != null && info.Kind == ThreatKind.Threat && e.outcome == ThreatOutcome.Compromised && !_pending.Contains(e.id))
                return true;
        }
        return false;
    }

    // chapter 0: the whole game (the epilogue's ending); otherwise that chapter's rundown.
    public int ScorePercent(int chapter = 0)
    {
        int total = 0, count = 0;
        foreach (var e in _entries)
        {
            if (chapter != 0 && e.chapter != chapter) continue;
            var info = ThreatCatalog.Get(e.id);
            if (info == null || info.Kind != ThreatKind.Threat || _pending.Contains(e.id)) continue;
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
        _pending.Clear();
    }
}
