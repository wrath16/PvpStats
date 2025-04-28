using LiteDB;
using PvpStats.Types.Player;
using System;

namespace PvpStats.Types.Event.CrystallineConflict;
public class KnockoutEvent : MatchEvent {

    public PlayerAlias Victim { get; set; }
    public PlayerAlias? CreditedKiller { get; set; }
    public uint? KillerNameId { get; set; }

    //need this because doesn't work with player alias for some reason
    [BsonCtor]
    public KnockoutEvent(DateTime timestamp) : base(timestamp) {
    }

    public KnockoutEvent(DateTime timestamp, PlayerAlias victim) : base(timestamp) {
        Victim = victim;
    }

    public KnockoutEvent(PlayerAlias victim) : base(DateTime.Now) {
        Victim = victim;
    }
}
