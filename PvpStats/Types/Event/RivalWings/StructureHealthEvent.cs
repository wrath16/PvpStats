using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.RivalWings;
public class StructureHealthEvent : MatchEvent {

    public int Health { get; set; }
    public RivalWingsTeamName? Team { get; set; }
    public RivalWingsStructure? Structure { get; set; }

    public StructureHealthEvent(DateTime timestamp, int health) : base(timestamp) {
        Health = health;
    }

    public StructureHealthEvent(int health) : base(DateTime.Now) {
        Health = health;
    }
}
