using Dalamud.Game.ClientState.Objects.Types;
using LiteDB;
using System.Collections.Generic;

namespace PvpStats.Types.Player;
public class BattleCharaSnapshot {
    public uint MaxHP { get; set; }
    public uint CurrentHP { get; set; }
    public uint MaxMP { get; set; }
    public uint CurrentMP { get; set; }
    public uint ShieldPercents { get; set; }
    public List<StatusSnapshot> Statuses { get; set; } = new();

    [BsonCtor]
    public BattleCharaSnapshot() {
    }

    public BattleCharaSnapshot(uint maxHP, uint currentHP, uint maxMP, uint currentMP, uint shieldPercents, List<StatusSnapshot> statuses) {
        MaxHP = maxHP;
        CurrentHP = currentHP;
        MaxMP = maxMP;
        CurrentMP = currentMP;
        ShieldPercents = shieldPercents;
        Statuses = statuses;
    }

    public BattleCharaSnapshot(IBattleChara character) {
        MaxHP = character.MaxHp;
        CurrentHP = character.CurrentHp;
        MaxMP = character.MaxMp;
        CurrentMP = character.CurrentMp;
        ShieldPercents = character.ShieldPercentage;
        List<StatusSnapshot> statusSnapshots = new();
        foreach(var status in character.StatusList) {
            statusSnapshots.Add(new StatusSnapshot(status.StatusId, status.Param, status.RemainingTime));
        }
        Statuses = statusSnapshots;
    }
}
