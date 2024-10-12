using LiteDB;

namespace PvpStats.Types.Match;
public class FrontlineScoreboard : PvpScoreboard {
    public long Occupations { get; set; }
    public long DamageToOther { get; set; }
    [BsonIgnore]
    public long DamageToPCs => DamageDealt - DamageToOther;
    public long Special1 { get; set; } //believed to be healing received

    [BsonIgnore]
    public override long DamageDealtPerKA => KillsAndAssists > 0 ? DamageToPCs / KillsAndAssists : DamageToPCs;

    public static FrontlineScoreboard operator +(FrontlineScoreboard a, FrontlineScoreboard b) {
        var c = a + (PvpScoreboard)b;
        return new FrontlineScoreboard() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            Occupations = a.Occupations + b.Occupations,
            DamageToOther = a.DamageToOther + b.DamageToOther,
            Special1 = a.Special1 + b.Special1,
        };
    }

    public static FrontlineScoreboard operator -(FrontlineScoreboard a, FrontlineScoreboard b) {
        var c = a - (PvpScoreboard)b;
        return new FrontlineScoreboard() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            Occupations = a.Occupations - b.Occupations,
            DamageToOther = a.DamageToOther - b.DamageToOther,
            Special1 = a.Special1 - b.Special1,
        };
    }
}