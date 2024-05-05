using LiteDB;

namespace PvpStats.Types.Match;
internal class FrontlineScoreboard : PvpScoreboard {
    public long Occupations { get; set; }
    public long DamageToOther { get; set; }
    [BsonIgnore]
    public long DamageToPCs => DamageDealt - DamageToOther;
    public long HealingReceived { get; set; }
}
