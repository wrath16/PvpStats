using LiteDB;

namespace PvpStats.Types.Player;
public class StatusSnapshot {

    public uint StatusId { get; set; }
    public uint Param { get; set; }
    public float RemainingTime { get; set; }

    [BsonCtor]
    public StatusSnapshot() { }

    public StatusSnapshot(uint statusId, uint param, float remainingTime) {
        StatusId = statusId;
        Param = param;
        RemainingTime = remainingTime;
    }
}
