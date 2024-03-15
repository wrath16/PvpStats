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
}
