using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.RivalWings;
public class MechCountEvent : MatchEvent {

    public int Count { get; set; }
    public RivalWingsTeamName? Team { get; set; }
    public RivalWingsMech? Mech { get; set; }

    public MechCountEvent(DateTime timestamp, int count) : base(timestamp) {
        Count = count;
    }

    public MechCountEvent(int count) : base(DateTime.UtcNow) {
        Count = count;
    }
}
