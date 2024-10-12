using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Display;
public class CCScoreboardDouble : PvpScoreboardDouble, IEquatable<CCScoreboardDouble> {
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
            KillsAndAssists = c.KillsAndAssists,
            TimeOnCrystal = a.TimeOnCrystal / b,
        };
    }

    public static explicit operator CCScoreboardDouble(CCScoreboard a) {
        var c = (PvpScoreboardDouble)a;
        return new CCScoreboardDouble() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            KillsAndAssists = c.KillsAndAssists,
            TimeOnCrystal = a.TimeOnCrystal.TotalSeconds
        };
    }

    public bool Equals(CCScoreboardDouble? other) {
        if(other is null) {
            return false;
        }
        var thisPvPScoreboard = (PvpScoreboardDouble)this;
        var otherPvPScoreboard = (PvpScoreboardDouble)other;
        return thisPvPScoreboard.Equals(otherPvPScoreboard)
            && TimeOnCrystal == other.TimeOnCrystal;
    }

    public override int GetHashCode() {
        return (base.GetHashCode(), TimeOnCrystal).GetHashCode();
    }
}
