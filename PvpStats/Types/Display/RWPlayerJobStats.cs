using PvpStats.Types.Match;

namespace PvpStats.Types.Display;
internal class RWPlayerJobStats : PlayerJobStats {
    public CCAggregateStats StatsAll { get; set; } = new();
    //public CCAggregateStats StatsPersonal { get; set; } = new();
    ////CCAggregateStats StatsLocalPlayer { get; set; } = new();
    //public CCAggregateStats StatsTeammate { get; set; } = new();
    //public CCAggregateStats StatsOpponent { get; set; } = new();
    public RivalWingsScoreboard ScoreboardTotal { get; set; } = new();
    public RWScoreboardDouble ScoreboardPerMatch { get; set; } = new();
    public RWScoreboardDouble ScoreboardPerMin { get; set; } = new();
    public RWScoreboardDouble ScoreboardContrib { get; set; } = new();

    public override int TotalMatches => StatsAll.Matches;

    public static RWPlayerJobStats operator +(RWPlayerJobStats a, RWPlayerJobStats b) {
        return new RWPlayerJobStats() {
            //StatsAll = a.StatsAll + b.StatsAll,
            //StatsPersonal = a.StatsPersonal + b.StatsPersonal,
            //StatsTeammate = a.StatsTeammate + b.StatsTeammate,
            //StatsOpponent = a.StatsOpponent + b.StatsOpponent,
            ScoreboardTotal = a.ScoreboardTotal + b.ScoreboardTotal,
        };
    }
}
