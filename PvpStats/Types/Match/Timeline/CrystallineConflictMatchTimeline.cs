using LiteDB;
using PvpStats.Types.Action;
using PvpStats.Types.Display;
using PvpStats.Types.Event;
using PvpStats.Types.Event.CrystallineConflict;
using PvpStats.Types.Player;
using System.Collections.Generic;

namespace PvpStats.Types.Match.Timeline;
internal class CrystallineConflictMatchTimeline : PvpMatchTimeline {
    [BsonIgnore]
    public static uint ActionIdOffset => 0;
    [BsonIgnore]
    public static uint StatusIdOffset => 1000000;
    [BsonIgnore]
    public static uint UnknownId => 2000000;
    [BsonIgnore]
    public static uint MedkitId => 2000001;


    public List<GenericMatchEvent>? MapEvents { get; set; }
    public List<ProgressEvent>? CrystalPosition { get; set; }
    public Dictionary<CrystallineConflictTeamName, List<ProgressEvent>>? TeamProgress { get; set; }
    public Dictionary<CrystallineConflictTeamName, List<ProgressEvent>>? TeamMidProgress { get; set; }
    public List<KnockoutEvent>? Kills { get; set; }
    public List<ActionEvent>? LimitBreakCasts { get; set; }
    public List<ActionEvent>? LimitBreakEffects { get; set; }

    //public Dictionary<string, Dictionary<uint, uint>>? TotalizedCasts { get; set; }
    //public Dictionary<string, uint>? TotalizedMedkits { get; set; }
    //public Dictionary<string, Dictionary<uint, ActionAnalytics>>? PlayerActionAnalytics { get; set; }

    //actor -> action id
    public Dictionary<string, Dictionary<uint, TargetedActionAnalytics>>? PlayerTargetedActionAnalytics { get; set; }
    public Dictionary<uint, Dictionary<uint, TargetedActionAnalytics>>? NameIdTargetedActionAnalytics { get; set; }
    public Dictionary<string, Dictionary<uint, TargetedActionAnalytics>>? PlayerTargetedStatusAnalytics { get; set; }
    public Dictionary<uint, Dictionary<uint, TargetedActionAnalytics>>? NameIdTargetedStatusAnalytics { get; set; }
    public Dictionary<string, ActionAnalytics>? PlayerMedkitAnalytics { get; set; }

    public CrystallineConflictMatchTimeline() : base() {

    }

    public Dictionary<string, Dictionary<uint, FlattenedActionAnalytics>>? SummarizePlayerAnalytics(CrystallineConflictMatch match) {
        var scoreboard = match.GetPlayerScoreboards();
        Dictionary<string, Dictionary<uint, FlattenedActionAnalytics>>? total = new();
        Dictionary<string, Dictionary<uint, TargetedActionAnalytics>>? targeted = new();

        //add actions
        foreach(var x in PlayerTargetedActionAnalytics ?? []) {
            Dictionary<uint, FlattenedActionAnalytics> actionAnalytics = new();
            total.Add(x.Key, actionAnalytics);
            foreach(var y in x.Value) {
                actionAnalytics.Add(y.Key + ActionIdOffset, y.Value.Summarize());
                //remove reflect damage
                foreach(var z in y.Value.PlayerAnalytics) {
                    if(x.Key == z.Key) {
                        actionAnalytics[y.Key].Damage -= z.Value.Damage;
                    }
                }
            }

            ////add unknown sources
            //if(scoreboard?.TryGetValue((PlayerAlias)x.Key, out var playerScoreboard) ?? false) {
            //    var unknowns = new FlattenedActionAnalytics(playerScoreboard);
            //    foreach(var action in actionAnalytics) {
            //        unknowns.Damage -= action.Value.Damage - action.Value.ExemptDamage;
            //        unknowns.Heal -= action.Value.Heal - action.Value.ExemptHeal;
            //    }
            //    actionAnalytics.Add(UnknownId, unknowns);
            //}
        }

        //add statuses
        foreach(var x in PlayerTargetedStatusAnalytics ?? []) {
            Dictionary<uint, FlattenedActionAnalytics>? actionAnalytics;
            if(!total.TryGetValue(x.Key, out actionAnalytics)) {
                actionAnalytics = new();
                total.Add(x.Key, actionAnalytics);
            }
            foreach(var y in x.Value) {
                actionAnalytics.Add(y.Key + StatusIdOffset, y.Value.Summarize());
            }
        }

        foreach(var x in total) {
            //add unknown source
            Dictionary<uint, FlattenedActionAnalytics> actionAnalytics = x.Value;
            if(scoreboard?.TryGetValue((PlayerAlias)x.Key, out var playerScoreboard) ?? false) {
                var unknowns = new FlattenedActionAnalytics(playerScoreboard);
                foreach(var action in actionAnalytics) {
                    unknowns.Damage -= action.Value.Damage - action.Value.ExemptDamage;
                    unknowns.Heal -= action.Value.Heal - action.Value.ExemptHeal;
                }
                actionAnalytics.Add(UnknownId, unknowns);
            }
        }

        //add medkits
        foreach(var x in PlayerMedkitAnalytics ?? []) {
            Dictionary<uint, FlattenedActionAnalytics>? actionAnalytics;
            if(!total.TryGetValue(x.Key, out actionAnalytics)) {
                actionAnalytics = new();
                total.Add(x.Key, actionAnalytics);
            }
            actionAnalytics.Add(MedkitId, new FlattenedActionAnalytics(x.Value));
        }

        return total;
    }

    public Dictionary<uint, Dictionary<uint, FlattenedActionAnalytics>>? SummarizeNameIdAnalytics(CrystallineConflictMatch match) {
        Dictionary<uint, Dictionary<uint, FlattenedActionAnalytics>>? results = new();
        //add actions
        foreach(var x in NameIdTargetedActionAnalytics ?? []) {
            Dictionary<uint, FlattenedActionAnalytics> actionAnalytics = new();
            results.Add(x.Key, actionAnalytics);
            foreach(var y in x.Value) {
                actionAnalytics.Add(y.Key + ActionIdOffset, y.Value.Summarize());
            }
        }
        //add statuses
        foreach(var x in NameIdTargetedStatusAnalytics ?? []) {
            Dictionary<uint, FlattenedActionAnalytics> actionAnalytics = new();
            results.Add(x.Key, actionAnalytics);
            foreach(var y in x.Value) {
                actionAnalytics.Add(y.Key + StatusIdOffset, y.Value.Summarize());
            }
        }
        return results;
    }
}
