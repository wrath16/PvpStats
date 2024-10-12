using LiteDB;

namespace PvpStats.Types.Match;
public class PvpScoreboard {
    public int Size { get; set; } = 1;
    public long Kills { get; set; }
    public long Deaths { get; set; }
    public long Assists { get; set; }
    public long DamageDealt { get; set; }
    public long DamageTaken { get; set; }
    public long HPRestored { get; set; }
    [BsonIgnore]
    public long KillsAndAssists => Kills + Assists;
    [BsonIgnore]
    public virtual long DamageDealtPerKA => KillsAndAssists > 0 ? DamageDealt / KillsAndAssists : DamageDealt;
    [BsonIgnore]
    public long DamageDealtPerLife => DamageDealt / (Deaths + Size);
    [BsonIgnore]
    public long DamageTakenPerLife => DamageTaken / (Deaths + Size);
    [BsonIgnore]
    public long HPRestoredPerLife => HPRestored / (Deaths + Size);
    [BsonIgnore]
    public double KDA => (double)KillsAndAssists / long.Max(Deaths, 1);

    public static PvpScoreboard operator +(PvpScoreboard a, PvpScoreboard b) {
        return new PvpScoreboard() {
            Kills = a.Kills + b.Kills,
            Deaths = a.Deaths + b.Deaths,
            Assists = a.Assists + b.Assists,
            DamageDealt = a.DamageDealt + b.DamageDealt,
            DamageTaken = a.DamageTaken + b.DamageTaken,
            HPRestored = a.HPRestored + b.HPRestored,
        };
    }

    public static PvpScoreboard operator -(PvpScoreboard a, PvpScoreboard b) {
        return new PvpScoreboard() {
            Kills = a.Kills - b.Kills,
            Deaths = a.Deaths - b.Deaths,
            Assists = a.Assists - b.Assists,
            DamageDealt = a.DamageDealt - b.DamageDealt,
            DamageTaken = a.DamageTaken - b.DamageTaken,
            HPRestored = a.HPRestored - b.HPRestored,
        };
    }
}