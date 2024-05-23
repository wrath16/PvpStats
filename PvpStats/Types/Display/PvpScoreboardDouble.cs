using PvpStats.Types.Match;

namespace PvpStats.Types.Display;
internal class PvpScoreboardDouble {
    public int LifeCount { get; set; } = 1;
    public double Kills { get; set; }
    public double Deaths { get; set; }
    public double Assists { get; set; }
    public double DamageDealt { get; set; }
    public double DamageTaken { get; set; }
    public double HPRestored { get; set; }
    public double KillsAndAssists => Kills + Assists;
    public double DamageDealtPerKA => KillsAndAssists > 0 ? DamageDealt / KillsAndAssists : DamageDealt;
    public double DamageDealtPerLife => DamageDealt / Deaths + LifeCount;
    public double DamageTakenPerLife => DamageTaken / Deaths + LifeCount;
    public double HPRestoredPerLife => HPRestored / Deaths + LifeCount;

    public PvpScoreboardDouble(PvpScoreboard playerScoreboard, PvpScoreboard teamScoreboard) {
        Kills = teamScoreboard.Kills != 0 ? (double)playerScoreboard.Kills / teamScoreboard.Kills : 0;
        Deaths = teamScoreboard.Deaths != 0 ? (double)playerScoreboard.Deaths / teamScoreboard.Deaths : 0;
        Assists = teamScoreboard.Assists != 0 ? (double)playerScoreboard.Assists / teamScoreboard.Assists : 0;
        DamageDealt = teamScoreboard.DamageDealt != 0 ? (double)playerScoreboard.DamageDealt / teamScoreboard.DamageDealt : 0;
        DamageTaken = teamScoreboard.DamageTaken != 0 ? (double)playerScoreboard.DamageTaken / teamScoreboard.DamageTaken : 0;
        HPRestored = teamScoreboard.HPRestored != 0 ? (double)playerScoreboard.HPRestored / teamScoreboard.HPRestored : 0;
    }
}
