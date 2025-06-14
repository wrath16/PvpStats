using PvpStats.Types.Display;
using System.Collections.Generic;

namespace PvpStats.Types.Action;
internal class TargetedActionAnalytics {

    public int Casts { get; set; }
    public int Targets { get; set; }
    public Dictionary<string, ActionAnalytics> PlayerAnalytics { get; set; } = [];
    public Dictionary<uint, ActionAnalytics> NameIdAnalytics { get; set; } = [];

    public static TargetedActionAnalytics operator +(TargetedActionAnalytics a, TargetedActionAnalytics b) {
        var result = new TargetedActionAnalytics();
        result.Casts = a.Casts + b.Casts;
        result.Targets = a.Targets + b.Targets;
        //result.HostileTargets = a.HostileTargets + b.HostileTargets;
        //result.UnknownTargets = a.UnknownTargets + b.UnknownTargets;
        MergeDictionaries(result.PlayerAnalytics, a.PlayerAnalytics);
        MergeDictionaries(result.PlayerAnalytics, b.PlayerAnalytics);
        MergeDictionaries(result.NameIdAnalytics, a.NameIdAnalytics);
        MergeDictionaries(result.NameIdAnalytics, b.NameIdAnalytics);
        return result;
    }

    private static void MergeDictionaries<TKey>(
        Dictionary<TKey, ActionAnalytics> target,
        Dictionary<TKey, ActionAnalytics> source
    ) where TKey : notnull {
        foreach(var kvp in source) {
            if(target.TryGetValue(kvp.Key, out var existing)) {
                target[kvp.Key] = existing + kvp.Value;
            } else {
                target[kvp.Key] = kvp.Value;
            }
        }
    }

    public FlattenedActionAnalytics Summarize() {
        FlattenedActionAnalytics a = new();
        foreach(var analytics in PlayerAnalytics) {
            a += analytics.Value;
        }
        foreach(var analytics in NameIdAnalytics) {
            a += analytics.Value;
        }
        a.Casts = Casts;
        a.Targets = Targets;
        return a;
    }
}
