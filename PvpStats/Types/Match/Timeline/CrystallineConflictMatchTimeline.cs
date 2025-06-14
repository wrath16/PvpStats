using PvpStats.Types.Action;
using PvpStats.Types.Display;
using PvpStats.Types.Event;
using PvpStats.Types.Event.CrystallineConflict;
using System.Collections.Generic;

namespace PvpStats.Types.Match.Timeline;
internal class CrystallineConflictMatchTimeline : PvpMatchTimeline {

    public List<GenericMatchEvent>? MapEvents { get; set; }
    public List<ProgressEvent>? CrystalPosition { get; set; }
    public Dictionary<CrystallineConflictTeamName, List<ProgressEvent>>? TeamProgress { get; set; }
    public Dictionary<CrystallineConflictTeamName, List<ProgressEvent>>? TeamMidProgress { get; set; }
    public List<KnockoutEvent>? Kills { get; set; }
    public List<ActionEvent>? LimitBreakCasts { get; set; }
    public List<ActionEvent>? LimitBreakEffects { get; set; }

    //public Dictionary<string, Dictionary<uint, uint>>? TotalizedCasts { get; set; }
    public Dictionary<string, uint>? TotalizedMedkits { get; set; }
    //public Dictionary<string, Dictionary<uint, ActionAnalytics>>? PlayerActionAnalytics { get; set; }

    //actor -> action id
    public Dictionary<string, Dictionary<uint, TargetedActionAnalytics>>? PlayerTargetedActionAnalytics { get; set; }
    public Dictionary<uint, Dictionary<uint, TargetedActionAnalytics>>? NameIdTargetedActionAnalytics { get; set; }

    public CrystallineConflictMatchTimeline() : base() {

    }

    public Dictionary<string, Dictionary<uint, FlattenedActionAnalytics>>? SummarizePlayerAnalytics() =>
    SummarizeAnalytics(PlayerTargetedActionAnalytics);

    public Dictionary<uint, Dictionary<uint, FlattenedActionAnalytics>>? SummarizeNameIdAnalytics() =>
        SummarizeAnalytics(NameIdTargetedActionAnalytics);

    private static Dictionary<TKey, Dictionary<uint, FlattenedActionAnalytics>>? SummarizeAnalytics<TKey>(
        Dictionary<TKey, Dictionary<uint, TargetedActionAnalytics>>? analytics
    ) where TKey : notnull {
        if(analytics == null) return null;

        var results = new Dictionary<TKey, Dictionary<uint, FlattenedActionAnalytics>>();
        foreach(var (outerKey, innerDict) in analytics) {
            var summarizedInner = new Dictionary<uint, FlattenedActionAnalytics>();
            results[outerKey] = summarizedInner;
            foreach(var (actionId, actionAnalytics) in innerDict) {
                summarizedInner[actionId] = actionAnalytics.Summarize();
            }
        }
        return results;
    }

    //public Dictionary<string, Dictionary<uint, FlattenedActionAnalytics>>? SummarizePlayerAnalytics() {
    //    Dictionary<string, Dictionary<uint, FlattenedActionAnalytics>>? results = new();
    //    foreach(var x in PlayerTargetedActionAnalytics ?? []) {
    //        Dictionary<uint, FlattenedActionAnalytics> actionAnalytics = new();
    //        results.Add(x.Key, actionAnalytics);
    //        foreach(var y in x.Value) {
    //            actionAnalytics.Add(y.Key, y.Value.Summarize());
    //        }
    //    }
    //    return results;
    //}

    //public Dictionary<uint, Dictionary<uint, FlattenedActionAnalytics>>? SummarizeNameIdAnalytics() {
    //    Dictionary<uint, Dictionary<uint, FlattenedActionAnalytics>>? results = new();
    //    foreach(var x in NameIdTargetedActionAnalytics ?? []) {
    //        Dictionary<uint, FlattenedActionAnalytics> actionAnalytics = new();
    //        results.Add(x.Key, actionAnalytics);
    //        foreach(var y in x.Value) {
    //            actionAnalytics.Add(y.Key, y.Value.Summarize());
    //        }
    //    }
    //    return results;
    //}
}
