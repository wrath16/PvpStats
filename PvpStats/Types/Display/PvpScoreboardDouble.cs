using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Display;
public class PvpScoreboardDouble : IEquatable<PvpScoreboardDouble> {
    public int LifeCount { get; set; } = 1;
    public double Kills { get; set; }
    public double Deaths { get; set; }
    public double Assists { get; set; }
    public double DamageDealt { get; set; }
    public double DamageTaken { get; set; }
    public double HPRestored { get; set; }
    public double KillsAndAssists { get; set; }
    //public double DamageDealtPerKA => KillsAndAssists > 0 ? DamageDealt / KillsAndAssists : DamageDealt;
    //public double DamageDealtPerLife => DamageDealt / Deaths + LifeCount;
    //public double DamageTakenPerLife => DamageTaken / Deaths + LifeCount;
    //public double HPRestoredPerLife => HPRestored / Deaths + LifeCount;

    public PvpScoreboardDouble() {

    }

    //get rid of this
    public PvpScoreboardDouble(PvpScoreboard playerScoreboard, PvpScoreboard teamScoreboard) {
        Kills = teamScoreboard.Kills != 0 ? (double)playerScoreboard.Kills / teamScoreboard.Kills : 0;
        Deaths = teamScoreboard.Deaths != 0 ? (double)playerScoreboard.Deaths / teamScoreboard.Deaths : 0;
        Assists = teamScoreboard.Assists != 0 ? (double)playerScoreboard.Assists / teamScoreboard.Assists : 0;
        DamageDealt = teamScoreboard.DamageDealt != 0 ? (double)playerScoreboard.DamageDealt / teamScoreboard.DamageDealt : 0;
        DamageTaken = teamScoreboard.DamageTaken != 0 ? (double)playerScoreboard.DamageTaken / teamScoreboard.DamageTaken : 0;
        HPRestored = teamScoreboard.HPRestored != 0 ? (double)playerScoreboard.HPRestored / teamScoreboard.HPRestored : 0;
        KillsAndAssists = ((double)playerScoreboard.Kills + playerScoreboard.Assists) / (teamScoreboard.Kills + teamScoreboard.Assists);
    }

    public PvpScoreboardDouble(ScoreboardTally playerScoreboard, ScoreboardTally teamScoreboard) {
        Kills = teamScoreboard.Kills != 0 ? (double)playerScoreboard.Kills / teamScoreboard.Kills : 0;
        Deaths = teamScoreboard.Deaths != 0 ? (double)playerScoreboard.Deaths / teamScoreboard.Deaths : 0;
        Assists = teamScoreboard.Assists != 0 ? (double)playerScoreboard.Assists / teamScoreboard.Assists : 0;
        DamageDealt = teamScoreboard.DamageDealt != 0 ? (double)playerScoreboard.DamageDealt / teamScoreboard.DamageDealt : 0;
        DamageTaken = teamScoreboard.DamageTaken != 0 ? (double)playerScoreboard.DamageTaken / teamScoreboard.DamageTaken : 0;
        HPRestored = teamScoreboard.HPRestored != 0 ? (double)playerScoreboard.HPRestored / teamScoreboard.HPRestored : 0;
        KillsAndAssists = ((double)playerScoreboard.Kills + playerScoreboard.Assists) / (teamScoreboard.Kills + teamScoreboard.Assists);
    }

    public static PvpScoreboardDouble operator /(PvpScoreboardDouble a, double b) {
        return new PvpScoreboardDouble() {
            Kills = a.Kills / b,
            Deaths = a.Deaths / b,
            Assists = a.Assists / b,
            DamageDealt = a.DamageDealt / b,
            DamageTaken = a.DamageTaken / b,
            HPRestored = a.HPRestored / b,
            KillsAndAssists = a.KillsAndAssists / b,
        };
    }

    public static explicit operator PvpScoreboardDouble(PvpScoreboard a) {
        return new PvpScoreboardDouble() {
            Kills = a.Kills,
            Deaths = a.Deaths,
            Assists = a.Assists,
            DamageDealt = a.DamageDealt,
            DamageTaken = a.DamageTaken,
            HPRestored = a.HPRestored,
            KillsAndAssists = a.KillsAndAssists,
        };
    }

    public static explicit operator PvpScoreboardDouble(ScoreboardTally a) {
        return new PvpScoreboardDouble() {
            Kills = a.Kills,
            Deaths = a.Deaths,
            Assists = a.Assists,
            DamageDealt = a.DamageDealt,
            DamageTaken = a.DamageTaken,
            HPRestored = a.HPRestored,
            KillsAndAssists = a.KillsAndAssists,
        };
    }

    public bool Equals(PvpScoreboardDouble? other) {
        return Kills == other?.Kills
            && Deaths == other?.Deaths
            && Assists == other?.Assists
            && DamageDealt == other?.DamageDealt
            && DamageTaken == other?.DamageTaken
            && HPRestored == other?.HPRestored
            && KillsAndAssists == other?.KillsAndAssists;
    }

    public override int GetHashCode() {
        return (Kills, Deaths, Assists, DamageDealt, DamageTaken, HPRestored, KillsAndAssists).GetHashCode();
    }
}
