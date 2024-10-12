using System.Threading;

namespace PvpStats.Types.Display;
public class CCPlayerJobStats : PlayerJobStats {
    public CCAggregateStats StatsAll { get; set; } = new();
    public CCAggregateStats StatsPersonal { get; set; } = new();
    //CCAggregateStats StatsLocalPlayer { get; set; } = new();
    public CCAggregateStats StatsTeammate { get; set; } = new();
    public CCAggregateStats StatsOpponent { get; set; } = new();
    public CCScoreboardTally ScoreboardTotal { get; set; } = new();
    public CCScoreboardDouble ScoreboardPerMatch { get; set; } = new();
    public CCScoreboardDouble ScoreboardPerMin { get; set; } = new();
    public CCScoreboardDouble ScoreboardContrib { get; set; } = new();

    public override int TotalMatches => StatsAll.Matches;

    public CCPlayerJobStats() {
    }

    //public CCPlayerJobStats(CCPlayerJobStats stats) {
    //    StatsAll = new(stats.StatsAll);
    //    StatsPersonal = new(stats.StatsPersonal);
    //    StatsTeammate = new(stats.StatsTeammate);
    //    StatsOpponent = new(stats.StatsOpponent);
    //}

    //public static CCPlayerJobStats operator +(CCPlayerJobStats a, CCPlayerJobStats b) {
    //    return new CCPlayerJobStats() {
    //        StatsAll = a.StatsAll + b.StatsAll,
    //        StatsPersonal = a.StatsPersonal + b.StatsPersonal,
    //        StatsTeammate = a.StatsTeammate + b.StatsTeammate,
    //        StatsOpponent = a.StatsOpponent + b.StatsOpponent,
    //        ScoreboardTotal = a.ScoreboardTotal + b.ScoreboardTotal,
    //    };
    //}
}
