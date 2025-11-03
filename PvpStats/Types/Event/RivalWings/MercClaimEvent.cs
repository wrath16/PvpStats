using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.RivalWings;
public class MercClaimEvent : MatchEvent {
    public RivalWingsTeamName Team { get; set; }

    public MercClaimEvent(DateTime timestamp, RivalWingsTeamName team) : base(timestamp) {
        Team = team;
    }

    public MercClaimEvent(RivalWingsTeamName team) : base(DateTime.UtcNow) {
        Team = team;
    }
}
