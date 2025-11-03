using LiteDB;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;

namespace PvpStats.Types.Event;
internal class ActionEvent : MatchEvent {

    public uint ActionId { get; set; }
    public PlayerAlias Actor { get; set; }
    public uint? NameIdActor { get; set; }
    //public PlayerAlias? PrimaryTarget { get; set; }
    public List<PlayerAlias> PlayerTargets { get; set; } = new();
    public List<uint> NameIdTargets { get; set; } = new();
    public int? Variation { get; set; }
    public Dictionary<string, BattleCharaSnapshot>? Snapshots { get; set; }

    //[BsonIgnore]

    [BsonCtor]
    public ActionEvent(DateTime timestamp, uint actionId) : base(timestamp) {
        Timestamp = timestamp;
        ActionId = actionId;
    }

    public ActionEvent(DateTime timestamp, uint actionId, PlayerAlias actor) : base(timestamp) {
        Timestamp = timestamp;
        ActionId = actionId;
        Actor = actor;
    }

    public ActionEvent(uint actionId, PlayerAlias actor) : base(DateTime.UtcNow) {
        ActionId = actionId;
        Actor = actor;
    }
}
