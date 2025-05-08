using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.CrystallineConflict;
public class GenericMatchEvent : MatchEvent {

    public override int SortPriority => 1;

    public CrystallineConflictMatchEvent Type { get; set; }

    public GenericMatchEvent(DateTime timestamp, CrystallineConflictMatchEvent type) : base(timestamp) {
        Type = type;
    }
}
