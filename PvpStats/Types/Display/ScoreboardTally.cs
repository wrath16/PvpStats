using System;
using System.Threading;

namespace PvpStats.Types.Display;
public class ScoreboardTally {
    public int Size = 1;
    public long Kills;
    public long Deaths;
    public long Assists;
    public long DamageDealt;
    public long DamageTaken;
    public long HPRestored;
    public long TeamKills;
    public long KillsAndAssists => Kills + Assists;
    public virtual long DamageDealtPerKA => KillsAndAssists > 0 ? DamageDealt / KillsAndAssists : DamageDealt;
    public long DamageDealtPerLife => DamageDealt / Math.Max(Deaths + Size, 1);
    public long DamageTakenPerLife => DamageTaken / Math.Max(Deaths + Size, 1);
    public long HPRestoredPerLife => HPRestored / Math.Max(Deaths + Size, 1);
    public double KDA => (double)KillsAndAssists / long.Max(Deaths, 1);

    public void AddScoreboard(ScoreboardTally scoreboard) {
        Interlocked.Add(ref Size, scoreboard.Size);
        Interlocked.Add(ref Kills, scoreboard.Kills);
        Interlocked.Add(ref Deaths, scoreboard.Deaths);
        Interlocked.Add(ref Assists, scoreboard.Assists);
        Interlocked.Add(ref DamageDealt, scoreboard.DamageDealt);
        Interlocked.Add(ref DamageTaken, scoreboard.DamageTaken);
        Interlocked.Add(ref HPRestored, scoreboard.HPRestored);
        Interlocked.Add(ref TeamKills, scoreboard.TeamKills);
    }

    public void RemoveScoreboard(ScoreboardTally scoreboard) {
        Interlocked.Add(ref Size, -scoreboard.Size);
        Interlocked.Add(ref Kills, -scoreboard.Kills);
        Interlocked.Add(ref Deaths, -scoreboard.Deaths);
        Interlocked.Add(ref Assists, -scoreboard.Assists);
        Interlocked.Add(ref DamageDealt, -scoreboard.DamageDealt);
        Interlocked.Add(ref DamageTaken, -scoreboard.DamageTaken);
        Interlocked.Add(ref HPRestored, -scoreboard.HPRestored);
        Interlocked.Add(ref TeamKills, -scoreboard.TeamKills);
    }
}
