using PvpStats.Types.Event.Frontline;
using System;
using System.Collections.Generic;

namespace PvpStats.Types.Match.Timeline;
internal class FrontlineMatchTimeline : PvpMatchTimeline {

    public Dictionary<FrontlineTeamName, List<TeamPointsEvent>>? TeamPoints { get; set; }
    public List<BattleHighLevelEvent>? SelfBattleHigh { get; set; }
    public FrontlineMatchTimeline() : base() {

    }

    public Dictionary<FrontlineTeamName, int>? GetTeamPoints(DateTime time) {
        if(TeamPoints == null) return null;

        var maelPoints = TeamPoints[FrontlineTeamName.Maelstrom].FindLast(x => x.Timestamp <= time)?.Points ?? 0;
        var adderPoints = TeamPoints[FrontlineTeamName.Adders].FindLast(x => x.Timestamp <= time)?.Points ?? 0;
        var flamePoints = TeamPoints[FrontlineTeamName.Flames].FindLast(x => x.Timestamp <= time)?.Points ?? 0;
        return new() {
            {FrontlineTeamName.Maelstrom, maelPoints },
            {FrontlineTeamName.Adders, adderPoints },
            {FrontlineTeamName.Flames, flamePoints }
        };
    }

    public List<TeamPointsEvent>? GetFlattenedTeamPoints() {
        if(TeamPoints == null) return null;
        //var list = TeamPoints?.SelectMany(x => {
        //    x.Value.ForEach(y => y.Team = x.Key);
        //    return x.Value;
        //}).ToList();
        List<TeamPointsEvent> list = new();

        FrontlineTeamName[] teams = [FrontlineTeamName.Maelstrom, FrontlineTeamName.Adders, FrontlineTeamName.Flames];
        foreach(var team in teams) {
            foreach(var pEvent in TeamPoints[team]) {
                list.Add(new TeamPointsEvent(pEvent.Timestamp, pEvent.Points) {
                    Team = team
                });
            }
        }

        list?.Sort();
        return list;
    }
}
