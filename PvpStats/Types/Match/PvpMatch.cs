using LiteDB;
using PvpStats.Types.Player;
using System;

namespace PvpStats.Types.Match;
public class PvpMatch {
    [BsonId]
    public ObjectId Id { get; init; }
    public int Version { get; init; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsBookmarked { get; set; }
    public DateTime DutyStartTime { get; init; }
    public DateTime? MatchStartTime { get; set; }
    public DateTime? MatchEndTime { get; set; }
    [BsonIgnore]
    public TimeSpan? MatchDuration => MatchEndTime - MatchStartTime;
    public PlayerAlias? LocalPlayer { get; set; }

    public uint DutyId { get; set; }
    public uint TerritoryId { get; set; }
    public string? DataCenter { get; set; }

    public PvpMatch() {
        Id = new ();
        DutyStartTime = DateTime.Now;
    }

    public override int GetHashCode() {
        return Id.GetHashCode();
    }
}
