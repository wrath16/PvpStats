using PvpStats.Types.Player;

namespace PvpStats.Types.Display;
public class AggregateStats {
    public int Matches;
    public Job? Job { get; set; }

    public AggregateStats() {

    }

    public AggregateStats(AggregateStats stats) {
        Matches = stats.Matches;
        Job = stats.Job;
    }

}
