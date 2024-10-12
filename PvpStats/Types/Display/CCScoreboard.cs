using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Display;
public class CCScoreboard : PvpScoreboard {
    //this should honestly be moved
    public TimeSpan TimeOnCrystal;
    public long TimeOnCrystalTicks;

    public static CCScoreboard operator +(CCScoreboard a, CCScoreboard b) {
        var c = a + (PvpScoreboard)b;
        return new CCScoreboard() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            TimeOnCrystal = a.TimeOnCrystal + b.TimeOnCrystal,
        };
    }

    public static CCScoreboard operator -(CCScoreboard a, CCScoreboard b) {
        var c = a - (PvpScoreboard)b;
        return new CCScoreboard() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            TimeOnCrystal = a.TimeOnCrystal - b.TimeOnCrystal,
        };
    }
}
