using System;
using System.Threading;

namespace PvpStats.Types.Display;
public class CCScoreboardTally : ScoreboardTally {
    public TimeSpan TimeOnCrystal;
    public long TimeOnCrystalTicks;

    public void AddScoreboard(CCScoreboardTally scoreboard) {
        AddScoreboard(scoreboard as ScoreboardTally);
        Interlocked.Add(ref TimeOnCrystalTicks, scoreboard.TimeOnCrystalTicks);
    }

    public void RemoveScoreboard(CCScoreboardTally scoreboard) {
        RemoveScoreboard(scoreboard as ScoreboardTally);
        Interlocked.Add(ref TimeOnCrystalTicks, -scoreboard.TimeOnCrystalTicks);
    }
}