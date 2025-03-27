using LiteDB;

namespace PvpStats.Types.Match;
public class FrontlineTeamScoreboard {
    public int? Placement { get; set; } = null;
    public int TotalPoints { get; set; }
    public int KillPoints { get; set; }
    public int DeathPointLosses { get; set; }
    public int OccupationPoints { get; set; }
    public int TargetablePoints { get; set; }
    public int DronePoints { get; set; }

    [BsonIgnore]
    public int KillPointsDiff => KillPoints - DeathPointLosses;
}
