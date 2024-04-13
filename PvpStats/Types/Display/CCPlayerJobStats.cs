namespace PvpStats.Types.Display;
public class CCPlayerJobStats {
    public CCAggregateStats StatsAll { get; set; } = new();
    public CCAggregateStats StatsPersonal { get; set; } = new();
    //CCAggregateStats StatsLocalPlayer { get; set; } = new();
    public CCAggregateStats StatsTeammate { get; set; } = new();
    public CCAggregateStats StatsOpponent { get; set; } = new();
    public CCScoreboard ScoreboardTotal { get; set; } = new();
    public CCScoreboardDouble ScoreboardPerMatch { get; set; } = new();
    public CCScoreboardDouble ScoreboardPerMin { get; set; } = new();
    public CCScoreboardDouble ScoreboardContrib { get; set; } = new();

    public static CCPlayerJobStats operator +(CCPlayerJobStats a, CCPlayerJobStats b) {
        return new CCPlayerJobStats() {
            StatsAll = a.StatsAll + b.StatsAll,
            StatsPersonal = a.StatsPersonal + b.StatsPersonal,
            StatsTeammate = a.StatsTeammate + b.StatsTeammate,
            StatsOpponent = a.StatsOpponent + b.StatsOpponent,
            ScoreboardTotal = a.ScoreboardTotal + b.ScoreboardTotal,
        };
    }
}
