using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.Frontline;
internal class TeamPointsEvent : MatchEvent {

    public int Points { get; set; }
    public FrontlineTeamName? Team { get; set; }

    public TeamPointsEvent(DateTime timestamp, int points) : base(timestamp) {
        Points = points;
    }

    public TeamPointsEvent(int points) : base(DateTime.Now) {
        Points = points;
    }
}
