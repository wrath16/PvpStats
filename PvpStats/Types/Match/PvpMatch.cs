using LiteDB;
using PvpStats.Types.Player;
using System;

namespace PvpStats.Types.Match;
public abstract class PvpMatch : IEquatable<PvpMatch> {
    [BsonId]
    public ObjectId Id { get; init; }
    public int Version { get; init; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsQuarantined { get; set; }
    public bool IsPickup { get; set; }
    public bool IsBookmarked { get; set; }
    public string Tags { get; set; } = "";
    public DateTime DutyStartTime { get; init; }
    public DateTime? MatchStartTime { get; set; }
    public DateTime? MatchEndTime { get; set; }
    [BsonIgnore]
    public TimeSpan? MatchDuration => MatchEndTime - MatchStartTime;
    public PlayerAlias? LocalPlayer { get; set; }
    //[BsonIgnore]
    //public abstract PvpPlayer? LocalPlayerTeamMember { get; set; }
    //public abstract List<PlayerAlias> Players { get; set; }

    public uint DutyId { get; set; }
    public uint TerritoryId { get; set; }
    public string? DataCenter { get; set; }
    public string? GameVersion { get; set; }
    public string? PluginVersion { get; set; }

    public ObjectId? TimelineId { get; set; }

    public PvpMatch() {
        Id = new();
        DutyStartTime = DateTime.Now;
    }

    public override int GetHashCode() {
        return Id.GetHashCode();
    }

    public bool Equals(PvpMatch? other) {
        return Id.Equals(other?.Id);
    }
}
