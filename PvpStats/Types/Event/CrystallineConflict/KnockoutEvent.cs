using LiteDB;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;

namespace PvpStats.Types.Event.CrystallineConflict;
public class KnockoutEvent : MatchEvent {

    public PlayerAlias Victim { get; set; }
    public PlayerAlias? CreditedKiller { get; set; }
    public uint? KillerNameId { get; set; }
    public BattleCharaSnapshot? CreditedKillerSnapshot { get; set; }
    public BattleCharaSnapshot? VictimSnapshot {  get; set; }


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
