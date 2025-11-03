using System;

namespace PvpStats.Types.Event.Frontline;
internal class BattleHighLevelEvent : MatchEvent {

    public int Count { get; set; }

    public BattleHighLevelEvent(DateTime timestamp, int count) : base(timestamp) {
        Count = count;
    }

    public BattleHighLevelEvent(int count) : base(DateTime.UtcNow) {
        Count = count;
    }
}
