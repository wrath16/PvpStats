using System;

namespace PvpStats.Types.Display;
public class FLScoreboardDouble : PvpScoreboardDouble, IEquatable<FLScoreboardDouble> {

    public static int BattleHighPerKill => 10;
    public static int BattleHighPerAssist => 2;

    public double Occupations { get; set; }
    public double DamageToOther { get; set; }
    public double DamageToPCs { get; set; }
    public double Special1 { get; set; }

    public double BattleHigh => Kills * BattleHighPerKill + Assists * BattleHighPerAssist;

    public FLScoreboardDouble() {

    }

    public FLScoreboardDouble(ScoreboardTally playerScoreboard, ScoreboardTally teamScoreboard) : base(playerScoreboard, teamScoreboard) {
        if(playerScoreboard is FLScoreboardTally && teamScoreboard is FLScoreboardTally) {
            var playerFLScoreboard = playerScoreboard as FLScoreboardTally;
            var teamFLScoreboard = teamScoreboard as FLScoreboardTally;

            Occupations = playerFLScoreboard!.Occupations != 0 ? (double)playerFLScoreboard.Occupations / teamFLScoreboard!.Occupations : 0;
            DamageToOther = playerFLScoreboard!.DamageToOther != 0 ? (double)playerFLScoreboard.DamageToOther / teamFLScoreboard!.DamageToOther : 0;
            DamageToPCs = playerFLScoreboard!.DamageToPCs != 0 ? (double)playerFLScoreboard.DamageToPCs / teamFLScoreboard!.DamageToPCs : 0;
            Special1 = playerFLScoreboard!.Special1 != 0 ? (double)playerFLScoreboard.Special1 / teamFLScoreboard!.Special1 : 0;
        }
    }

    public static FLScoreboardDouble operator /(FLScoreboardDouble a, double b) {
        var c = (PvpScoreboardDouble)a / b;
        return new FLScoreboardDouble() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            KillsAndAssists = c.KillsAndAssists,
            Occupations = a.Occupations / b,
            DamageToPCs = a.DamageToPCs / b,
            DamageToOther = a.DamageToOther / b,
            Special1 = a.Special1 / b,
        };
    }

    public static explicit operator FLScoreboardDouble(FLScoreboardTally a) {
        var c = (PvpScoreboardDouble)a;
        return new FLScoreboardDouble() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            KillsAndAssists = c.KillsAndAssists,
            Occupations = a.Occupations,
            DamageToPCs = a.DamageToPCs,
            DamageToOther = a.DamageToOther,
            Special1 = a.Special1,
        };
    }

    public bool Equals(FLScoreboardTally? other) {
        if(other is null) {
            return false;
        } else {
            return Equals((FLScoreboardDouble)other);
        }
    }

    public bool Equals(FLScoreboardDouble? other) {
        if(other is null) {
            return false;
        }
        var thisPvPScoreboard = (PvpScoreboardDouble)this;
        var otherPvPScoreboard = (PvpScoreboardDouble)other;
        return thisPvPScoreboard.Equals(otherPvPScoreboard)
            && Occupations == other.Occupations
            && DamageToPCs == other.DamageToPCs
            && DamageToOther == other.DamageToOther
            && Special1 == other.Special1;
    }

    public override int GetHashCode() {
        return HashCode.Combine(base.GetHashCode(), Occupations, DamageToPCs, DamageToOther, Special1);
    }
}
