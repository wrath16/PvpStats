using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Display;
public class CCScoreboardDouble : PvpScoreboardDouble {
    //public TimeSpan TimeOnCrystal { get; set; }
    public double TimeOnCrystal { get; set; }

    public CCScoreboardDouble() {

    }

    public CCScoreboardDouble(PvpScoreboard playerScoreboard, PvpScoreboard teamScoreboard) : base(playerScoreboard, teamScoreboard) {
        if(playerScoreboard is CCScoreboard && teamScoreboard is CCScoreboard) {
            var playerCCScoreboard = playerScoreboard as CCScoreboard;
            var teamCCScoreboard = teamScoreboard as CCScoreboard;

            TimeOnCrystal = playerCCScoreboard!.TimeOnCrystal != TimeSpan.Zero ? playerCCScoreboard.TimeOnCrystal / teamCCScoreboard!.TimeOnCrystal : 0;
        }
    }

    public static CCScoreboardDouble operator /(CCScoreboardDouble a, double b) {
        var c = (PvpScoreboardDouble)a / b;
        return new CCScoreboardDouble() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            TimeOnCrystal = a.TimeOnCrystal / b,
        };
    }
}
