using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.RivalWings;
public class MatchEndEvent : MatchEvent {

    public RivalWingsTeamName? Team { get; set; }

    public MatchEndEvent(DateTime timestamp, RivalWingsTeamName? team) : base(timestamp) {
        Team = team;
    }
}
