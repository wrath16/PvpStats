using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.CrystallineConflict;
internal class ProgressEvent : MatchEvent {

    public int Points { get; set; }
    public CrystallineConflictTeamName? Team { get; set; }

    //0 = crystal, 1 = mid
    public int? Type { get; set; }

    public ProgressEvent(DateTime timestamp, int points) : base(timestamp) {
        Points = points;
    }

    public ProgressEvent(int points) : base(DateTime.UtcNow) {
        Points = points;
    }
}
