using LiteDB;
using PvpStats.Types.Action;
using PvpStats.Types.Display.Action;
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
    public static uint ActionSetOffset => 2000000;
    [BsonIgnore]
    public static uint UnknownId => 3000000;
    [BsonIgnore]
    public static uint MedkitId => 3000001;

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

    public static Dictionary<string, Dictionary<uint, FlattenedActionAnalytics>>? CreateActionSets(Dictionary<string, Dictionary<uint, FlattenedActionAnalytics>> summarized) {
        Dictionary<string, Dictionary<uint, FlattenedActionAnalytics>> results = [];
        Dictionary<uint, uint> actionIdLookup = [];

        //create lookup for id -> action set
        for(int i = 0; i < ActionSet.Sets.Count; i++) {
            var actionSet = ActionSet.Sets[i];
            foreach(var action in actionSet.Actions) {
                actionIdLookup.Add(action.Key, (uint)(i + ActionSetOffset));
            }
        }

        foreach(var player in summarized) {
            Dictionary<uint, FlattenedActionAnalytics> resultAnalytics = [];
            results.Add(player.Key, resultAnalytics);
            foreach(var actionAnalytics in player.Value) {
                if(actionIdLookup.TryGetValue(actionAnalytics.Key, out var setId)) {
                    var setIndex = setId - ActionSetOffset;
                    var actionParams = ActionSet.Sets[(int)setIndex].Actions[actionAnalytics.Key];
                    var transformedAnalytics = actionParams.Transform(actionAnalytics.Value);

                    if(!resultAnalytics.TryGetValue(setId, out var existingSet)) {
                        existingSet = new();
                        resultAnalytics.Add(setId, existingSet);
                    }
                    existingSet += transformedAnalytics;
                    if(!actionParams.IncludeCasts) {
                        existingSet.Casts -= transformedAnalytics.Casts;
                    }
                    if(!actionParams.IncludeTargets) {
                        existingSet.Targets -= transformedAnalytics.Targets;
                    }
                    resultAnalytics[setId] = existingSet;
                } else {
                    resultAnalytics[actionAnalytics.Key] = actionAnalytics.Value;
                }
            }
        }
        return results;
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
