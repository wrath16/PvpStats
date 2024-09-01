using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Display;
public class RWScoreboardDouble : PvpScoreboardDouble, IEquatable<RWScoreboardDouble> {

    public double Ceruleum { get; set; }
    public double DamageToOther { get; set; }
    public double DamageToPCs { get; set; }
    public double Special1 { get; set; }

    public RWScoreboardDouble() {

    }

    public RWScoreboardDouble(PvpScoreboard playerScoreboard, PvpScoreboard teamScoreboard) : base(playerScoreboard, teamScoreboard) {
        if(playerScoreboard is RivalWingsScoreboard && teamScoreboard is RivalWingsScoreboard) {
            var playerRWScoreboard = playerScoreboard as RivalWingsScoreboard;
            var teamRWScoreboard = teamScoreboard as RivalWingsScoreboard;

            Ceruleum = playerRWScoreboard!.Ceruleum != 0 ? (double)playerRWScoreboard.Ceruleum / teamRWScoreboard!.Ceruleum : 0;
            DamageToOther = playerRWScoreboard!.DamageToOther != 0 ? (double)playerRWScoreboard.DamageToOther / teamRWScoreboard!.DamageToOther : 0;
            DamageToPCs = playerRWScoreboard!.DamageToPCs != 0 ? (double)playerRWScoreboard.DamageToPCs / teamRWScoreboard!.DamageToPCs : 0;
            Special1 = playerRWScoreboard!.Special1 != 0 ? (double)playerRWScoreboard.Special1 / teamRWScoreboard!.Special1 : 0;
        }
    }

    public static RWScoreboardDouble operator /(RWScoreboardDouble a, double b) {
        var c = (PvpScoreboardDouble)a / b;
        return new RWScoreboardDouble() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            KillsAndAssists = c.KillsAndAssists,
            Ceruleum = a.Ceruleum / b,
            DamageToPCs = a.DamageToPCs / b,
            DamageToOther = a.DamageToOther / b,
            Special1 = a.Special1 / b,
        };
    }

    public static explicit operator RWScoreboardDouble(RivalWingsScoreboard a) {
        var c = (PvpScoreboardDouble)a;
        return new RWScoreboardDouble() {
            Kills = c.Kills,
            Deaths = c.Deaths,
            Assists = c.Assists,
            DamageDealt = c.DamageDealt,
            DamageTaken = c.DamageTaken,
            HPRestored = c.HPRestored,
            KillsAndAssists = c.KillsAndAssists,
            Ceruleum = a.Ceruleum,
            DamageToPCs = a.DamageToPCs,
            DamageToOther = a.DamageToOther,
            Special1 = a.Special1,
        };
    }

    public bool Equals(RWScoreboardDouble? other) {
        if(other is null) {
            return false;
        }
        var thisPvPScoreboard = (PvpScoreboardDouble)this;
        var otherPvPScoreboard = (PvpScoreboardDouble)other;
        return thisPvPScoreboard.Equals(otherPvPScoreboard)
            && Ceruleum == other.Ceruleum
            && DamageToPCs == other.DamageToPCs
            && DamageToOther == other.DamageToOther
            && Special1 == other.Special1;
    }
}
