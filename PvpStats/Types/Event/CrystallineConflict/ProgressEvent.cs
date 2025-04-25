using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.CrystallineConflict;
internal class ProgressEvent : MatchEvent {

    public int Points { get; set; }
    public CrystallineConflictTeamName? Team { get; set; }

    public ProgressEvent(DateTime timestamp, int points) : base(timestamp) {
        Points = points;
    }

    public ProgressEvent(int points) : base(DateTime.Now) {
        Points = points;
    }
}
