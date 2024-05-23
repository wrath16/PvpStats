using System;

namespace PvpStats.Types.Display;
public class CCScoreboardDouble {
    public bool IsTeam { get; set; }
    public double Kills { get; set; }
    public double Deaths { get; set; }
    public double Assists { get; set; }
    public double DamageDealt { get; set; }
    public double DamageTaken { get; set; }
    public double HPRestored { get; set; }
    public TimeSpan TimeOnCrystal { get; set; }
    public double TimeOnCrystalDouble { get; set; }
    public double KillsAndAssists { get; set; }
    public double DamageDealtPerKA => DamageDealt / (Kills + Assists);
    public double DamageDealtPerLife => DamageDealt / Deaths + (IsTeam ? 5 : 1);
    public double DamageTakenPerLife => DamageTaken / Deaths + (IsTeam ? 5 : 1);
    public double HPRestoredPerLife => HPRestored / Deaths + (IsTeam ? 5 : 1);
}
