using PvpStats.Utility.Interface;
using System;

namespace PvpStats.Types.Event;
public class MatchEvent : IComparable<MatchEvent>, ISortPrioritizable {

    public virtual int SortPriority => 0;

    public DateTime Timestamp { get; set; }

    public MatchEvent(DateTime timestamp) {
        Timestamp = timestamp;
    }

    public int CompareTo(MatchEvent? other) {
        var comparison = Timestamp.CompareTo(other?.Timestamp);
        if(comparison == 0) {
            var thisPriority = (this as ISortPrioritizable).SortPriority;
            var otherPriority = (other as ISortPrioritizable).SortPriority;
            return thisPriority.CompareTo(otherPriority);
        } else {
            return comparison;
        }
    }
}
