using PvpStats.Types.Match;

namespace PvpStats.Types.Display;
internal class FLPlayerJobStats {
    public FLAggregateStats StatsAll { get; set; } = new();
    public FrontlineScoreboard ScoreboardTotal { get; set; } = new();
    public FLScoreboardDouble ScoreboardPerMatch { get; set; } = new();
    public FLScoreboardDouble ScoreboardPerMin { get; set; } = new();
    public FLScoreboardDouble ScoreboardContrib { get; set; } = new();

    public static FLPlayerJobStats operator +(FLPlayerJobStats a, FLPlayerJobStats b) {
        return new FLPlayerJobStats() {
            //StatsAll = a.StatsAll + b.StatsAll,
            //StatsPersonal = a.StatsPersonal + b.StatsPersonal,
            //StatsTeammate = a.StatsTeammate + b.StatsTeammate,
            //StatsOpponent = a.StatsOpponent + b.StatsOpponent,
            ScoreboardTotal = a.ScoreboardTotal + b.ScoreboardTotal,
        };
    }
}
