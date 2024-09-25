namespace PvpStats.Types.Display;
public class CCAggregateStats : AggregateStats {
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int OtherResult => Matches - Wins - Losses;
    public double WinRate => (double)Wins / (Wins + Losses);
    public int WinDiff => Wins - Losses;

    public static CCAggregateStats operator +(CCAggregateStats a, CCAggregateStats b) {
        return new CCAggregateStats() {
            Matches = a.Matches + b.Matches,
            Wins = a.Wins + b.Wins,
            Losses = a.Losses + b.Losses
        };
    }
}
