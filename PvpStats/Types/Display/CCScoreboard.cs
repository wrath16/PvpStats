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
    public ulong DamageDealtPerKA => (Kills + Assists) > 0 ? DamageDealt / (Kills + Assists) : DamageDealt;
    public ulong DamageDealtPerLife => DamageDealt / (Deaths + 1);
    public ulong DamageTakenPerLife => DamageTaken / (Deaths + 1);
    public ulong HPRestoredPerLife => HPRestored / (Deaths + 1);
}
