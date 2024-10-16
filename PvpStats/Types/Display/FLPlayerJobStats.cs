namespace PvpStats.Types.Display;
internal class FLPlayerJobStats : PlayerJobStats {
    public FLAggregateStats StatsAll { get; set; } = new();
    public FLScoreboardTally ScoreboardTotal { get; set; } = new();
    public FLScoreboardDouble ScoreboardPerMatch { get; set; } = new();
    public FLScoreboardDouble ScoreboardPerMin { get; set; } = new();
    public FLScoreboardDouble ScoreboardContrib { get; set; } = new();
    public FLScoreboardTally ScoreboardShatter { get; set; } = new();

    public double BattleHighPerLife => ScoreboardPerMin.Deaths != 0 ? ScoreboardPerMin.BattleHigh / ScoreboardPerMin.Deaths : ScoreboardPerMatch.BattleHigh;

    public override int TotalMatches => StatsAll.Matches;

    public double GetBattleHighPerMatch() {
        var halves = (int)ScoreboardPerMatch.Deaths;
        var remainder = ScoreboardPerMatch.Deaths - halves;

        var battleHighLevel = ScoreboardPerMatch.BattleHigh / ScoreboardPerMatch.Deaths;

        for(int i = 0; i < halves; i++) {
            battleHighLevel /= 2;
            battleHighLevel += BattleHighPerLife;
        }

        //add remainder and last divider
        battleHighLevel += ScoreboardPerMatch.BattleHigh - halves * BattleHighPerLife;
        battleHighLevel *= 1 - remainder;
        return battleHighLevel;
    }

    //public static FLPlayerJobStats operator +(FLPlayerJobStats a, FLPlayerJobStats b) {
    //    return new FLPlayerJobStats() {
    //        //StatsAll = a.StatsAll + b.StatsAll,
    //        //StatsPersonal = a.StatsPersonal + b.StatsPersonal,
    //        //StatsTeammate = a.StatsTeammate + b.StatsTeammate,
    //        //StatsOpponent = a.StatsOpponent + b.StatsOpponent,
    //        ScoreboardTotal = a.ScoreboardTotal + b.ScoreboardTotal,
    //    };
    //}
}
