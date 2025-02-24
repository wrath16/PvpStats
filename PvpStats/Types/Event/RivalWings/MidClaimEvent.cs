using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.RivalWings;
public class MidClaimEvent : MatchEvent {
    public RivalWingsTeamName Team { get; set; }
    public RivalWingsSupplies Kind { get; set; }

    public MidClaimEvent(DateTime timestamp, RivalWingsTeamName team, RivalWingsSupplies kind) : base(timestamp) {
        Team = team;
        Kind = kind;
    }

    public MidClaimEvent(RivalWingsTeamName team, RivalWingsSupplies kind) : base(DateTime.Now) {
        Team = team;
        Kind = kind;
    }
}
