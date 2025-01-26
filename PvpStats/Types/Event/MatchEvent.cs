using System;

namespace PvpStats.Types.Event;
public class MatchEvent : IComparable<MatchEvent> {
    public DateTime Timestamp { get; set; }

    public MatchEvent(DateTime timestamp) {
        Timestamp = timestamp;
    }

    public int CompareTo(MatchEvent? other) {
        return Timestamp.CompareTo(other?.Timestamp);
    }
}
