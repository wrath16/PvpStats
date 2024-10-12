namespace PvpStats.Types.Display;
public class CCAggregateStats : AggregateStats {
    public int Wins;
    public int Losses;
    public int OtherResult => Matches - Wins - Losses;
    public double WinRate => (double)Wins / (Wins + Losses);
    public int WinDiff => Wins - Losses;

    public CCAggregateStats() {

    }

    public CCAggregateStats(CCAggregateStats stats) {
        Matches = stats.Matches;
        Job = stats.Job;
        Wins = stats.Wins;
        Losses = stats.Losses;
    }

    public static CCAggregateStats operator +(CCAggregateStats a, CCAggregateStats b) {
        return new CCAggregateStats() {
            Matches = a.Matches + b.Matches,
            Wins = a.Wins + b.Wins,
            Losses = a.Losses + b.Losses
        };
    }
}
