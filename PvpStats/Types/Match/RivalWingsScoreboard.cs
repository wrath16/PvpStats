using LiteDB;

namespace PvpStats.Types.Match;
public class RivalWingsScoreboard : PvpScoreboard {
    public long Ceruleum { get; set; }
    public long DamageToOther { get; set; }
    [BsonIgnore]
    public long DamageToPCs => DamageDealt - DamageToOther;
    public long Special1 { get; set; } //believed to be healing received

    public static RivalWingsScoreboard operator +(RivalWingsScoreboard a, RivalWingsScoreboard b) {
        var c = a + (PvpScoreboard)b;
        return new RivalWingsScoreboard() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            Ceruleum = a.Ceruleum + b.Ceruleum,
            DamageToOther = a.DamageToOther + b.DamageToOther,
            Special1 = a.Special1 + b.Special1,
        };
    }
}
