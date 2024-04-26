using System;

namespace PvpStats.Types.Display;
public class CCScoreboard {
    public TimeSpan MatchTime { get; set; }
    public ulong Kills { get; set; }
    public ulong Deaths { get; set; }
    public ulong Assists { get; set; }
    public ulong DamageDealt { get; set; }
    public ulong DamageTaken { get; set; }
    public ulong HPRestored { get; set; }
    public TimeSpan TimeOnCrystal { get; set; }
    public ulong KillsAndAssists => Kills + Assists;
    public ulong DamageDealtPerKA => KillsAndAssists > 0 ? DamageDealt / KillsAndAssists : DamageDealt;
    public ulong DamageDealtPerLife => DamageDealt / (Deaths + 1);
    public ulong DamageTakenPerLife => DamageTaken / (Deaths + 1);
    public ulong HPRestoredPerLife => HPRestored / (Deaths + 1);
    public double KDA => (double)KillsAndAssists / ulong.Max(Deaths, 1);

    public static CCScoreboard operator +(CCScoreboard a, CCScoreboard b) {
        return new CCScoreboard() {
            Kills = a.Kills + b.Kills,
            Deaths = a.Deaths + b.Deaths,
            Assists = a.Assists + b.Assists,
            DamageDealt = a.DamageDealt + b.DamageDealt,
            DamageTaken = a.DamageTaken + b.DamageDealt,
            HPRestored = a.HPRestored + b.HPRestored,
            TimeOnCrystal = a.TimeOnCrystal + b.TimeOnCrystal,
            MatchTime = a.MatchTime + b.MatchTime,
        };
    }
}
