using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.Match;
internal class PvpScoreboard {
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
    public long DamageDealtPerKA => KillsAndAssists > 0 ? DamageDealt / KillsAndAssists : DamageDealt;
    [BsonIgnore]
    public long DamageDealtPerLife => DamageDealt / (Deaths + Size);
    [BsonIgnore]
    public long DamageTakenPerLife => DamageTaken / (Deaths + Size);
    [BsonIgnore]
    public long HPRestoredPerLife => HPRestored / (Deaths + Size);
    [BsonIgnore]
    public double KDA => (double)KillsAndAssists / long.Max(Deaths, 1);
}
