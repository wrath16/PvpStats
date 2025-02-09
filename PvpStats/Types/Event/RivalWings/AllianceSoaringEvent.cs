using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Event.RivalWings;
public class AllianceSoaringEvent : MatchEvent {

    public int Count { get; set; }
    public RivalWingsTeamName? Team { get; set; }
    public int? Alliance { get; set; }

    public AllianceSoaringEvent(DateTime timestamp, int count) : base(timestamp) {
        Count = count;
    }

    public AllianceSoaringEvent(int count) : base(DateTime.Now) {
        Count = count;
    }
}
