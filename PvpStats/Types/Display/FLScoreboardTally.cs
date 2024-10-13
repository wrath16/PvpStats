using LiteDB;
using PvpStats.Types.Match;
using System.Threading;

namespace PvpStats.Types.Display;
public class FLScoreboardTally : ScoreboardTally {
    public long Occupations;
    public long DamageToOther;
    [BsonIgnore]
    public long DamageToPCs => DamageDealt - DamageToOther;
    public long Special1;

    public FLScoreboardTally() {
    }

    public FLScoreboardTally(FrontlineScoreboard scoreboard) {
        Kills = scoreboard.Kills;
        Deaths = scoreboard.Deaths;
        Assists = scoreboard.Assists;
        DamageDealt = scoreboard.DamageDealt;
        DamageTaken = scoreboard.DamageTaken;
        HPRestored = scoreboard.HPRestored;
        Occupations = scoreboard.Occupations;
        DamageToOther = scoreboard.DamageToOther;
        Special1 = scoreboard.Special1;
    }

    public void AddScoreboard(FLScoreboardTally scoreboard) {
        AddScoreboard(scoreboard as ScoreboardTally);
        Interlocked.Add(ref Occupations, scoreboard.Occupations);
        Interlocked.Add(ref DamageToOther, scoreboard.DamageToOther);
    }

    public void RemoveScoreboard(FLScoreboardTally scoreboard) {
        RemoveScoreboard(scoreboard as ScoreboardTally);
        Interlocked.Add(ref Occupations, -scoreboard.Occupations);
        Interlocked.Add(ref DamageToOther, -scoreboard.DamageToOther);
    }
}
