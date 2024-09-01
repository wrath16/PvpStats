using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Display;
public class CCScoreboard : PvpScoreboard {
    //this should honestly be moved
    public TimeSpan MatchTime { get; set; }
    public TimeSpan TimeOnCrystal { get; set; }

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
            MatchTime = a.MatchTime + b.MatchTime,
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
            MatchTime = a.MatchTime - b.MatchTime,
        };
    }
}
