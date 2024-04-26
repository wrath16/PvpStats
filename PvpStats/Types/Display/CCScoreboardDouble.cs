using System;

namespace PvpStats.Types.Display;
public class CCScoreboardDouble {
    public double Kills { get; set; }
    public double Deaths { get; set; }
    public double Assists { get; set; }
    public double DamageDealt { get; set; }
    public double DamageTaken { get; set; }
    public double HPRestored { get; set; }
    public TimeSpan TimeOnCrystal { get; set; }
    public double TimeOnCrystalDouble { get; set; }
    public double DamageDealtPerKA => DamageDealt / (Kills + Assists);
    public double DamageDealtPerLife => DamageDealt / Deaths + 1;
    public double DamageTakenPerLife => DamageTaken / Deaths + 1;
    public double HPRestoredPerLife => HPRestored / Deaths + 1;
}
