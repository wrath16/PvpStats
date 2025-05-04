using PvpStats.Types.Player;
using System;
using System.Collections.Generic;

namespace PvpStats.Types.Event;
internal class CombinedActionEvent : MatchEvent {

    public uint ActionId { get; set; }
    public PlayerAlias Actor { get; set; }
    public uint? NameIdActor { get; set; }
    public PlayerAlias? PlayerCastTarget { get; set; }
    public uint? NameIdCastTarget { get; set; }
    public DateTime? EffectTime { get; set; }
    public DateTime? CastTime { get; set; }
    public List<PlayerAlias>? AffectedPlayers { get; set; }
    public List<uint>? AffectedNameIds { get; set; }
    public Dictionary<string, BattleCharaSnapshot>? CastSnapshots { get; set; }
    public Dictionary<string, BattleCharaSnapshot>? EffectSnapshots { get; set; }

    public CombinedActionEvent(ActionEvent cast, ActionEvent? impact) : base(impact?.Timestamp ?? cast.Timestamp) {
        Actor = cast.Actor;
        NameIdActor = cast.NameIdActor;
        ActionId = cast.ActionId;
        CastTime = cast.Timestamp;
        CastSnapshots = cast.Snapshots;
        if(cast.PlayerTargets.Count > 0) {
            PlayerCastTarget = cast.PlayerTargets[0];
        } else if(cast.NameIdTargets.Count > 0) {
            NameIdCastTarget = cast.NameIdTargets[0];
        } else {
            throw new InvalidOperationException("Invalid cast event.");
        }
        EffectTime = impact?.Timestamp;
        AffectedPlayers = impact?.PlayerTargets;
        AffectedNameIds = impact?.NameIdTargets;
        EffectSnapshots = impact?.Snapshots;
    }
}
