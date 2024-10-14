using PvpStats.Types.Match;
using System.Threading;

namespace PvpStats.Types.Display;
public class RWScoreboardTally : ScoreboardTally {

    public long Ceruleum;
    public long DamageToOther;
    public long Special1;
    public long DamageToPCs => DamageDealt - DamageToOther;

    public override long DamageDealtPerKA => KillsAndAssists > 0 ? DamageToPCs / KillsAndAssists : DamageToPCs;

    public RWScoreboardTally() {
    }

    public RWScoreboardTally(RivalWingsScoreboard scoreboard) {
        Kills = scoreboard.Kills;
        Deaths = scoreboard.Deaths;
        Assists = scoreboard.Assists;
        DamageDealt = scoreboard.DamageDealt;
        DamageTaken = scoreboard.DamageTaken;
        HPRestored = scoreboard.HPRestored;
        Ceruleum = scoreboard.Ceruleum;
        DamageToOther = scoreboard.DamageToOther;
        Special1 = scoreboard.Special1;
    }

    public void AddScoreboard(RWScoreboardTally scoreboard) {
        AddScoreboard(scoreboard as ScoreboardTally);
        Interlocked.Add(ref Ceruleum, scoreboard.Ceruleum);
        Interlocked.Add(ref DamageToOther, scoreboard.DamageToOther);
        Interlocked.Add(ref Special1, scoreboard.Special1);
    }

    public void RemoveScoreboard(RWScoreboardTally scoreboard) {
        RemoveScoreboard(scoreboard as ScoreboardTally);
        Interlocked.Add(ref Ceruleum, -scoreboard.Ceruleum);
        Interlocked.Add(ref DamageToOther, -scoreboard.DamageToOther);
        Interlocked.Add(ref Special1, -scoreboard.Special1);
    }
}
