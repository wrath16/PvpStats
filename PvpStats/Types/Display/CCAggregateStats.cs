using PvpStats.Types.Player;

namespace PvpStats.Types.Display;
public class CCAggregateStats {
    public uint Matches { get; set; }
    public uint Wins { get; set; }
    public uint Losses { get; set; }
    public uint OtherResult => Matches - Wins - Losses;
    public double WinRate => (double)Wins / (Wins + Losses);
    public int WinDiff => (int)Wins - (int)Losses;
    public Job? Job { get; set; }

    public static CCAggregateStats operator +(CCAggregateStats a, CCAggregateStats b) {
        return new CCAggregateStats() {
            Matches = a.Matches + b.Matches,
            Wins = a.Wins + b.Wins,
            Losses = a.Losses + b.Losses
        };
    }
}
