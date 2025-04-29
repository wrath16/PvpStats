using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.CrystallineConflict;
public class GenericMatchEvent : MatchEvent {

    public CrystallineConflictMatchEvent Type { get; set; }

    public GenericMatchEvent(DateTime timestamp, CrystallineConflictMatchEvent type) : base(timestamp) {
        Type = type;
    }
}
