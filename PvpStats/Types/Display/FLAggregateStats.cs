namespace PvpStats.Types.Display;
public class FLAggregateStats : AggregateStats {
    public int FirstPlaces { get; set; }
    public int SecondPlaces { get; set; }
    public int ThirdPlaces { get; set; }
    public int Wins => FirstPlaces;
    public int Losses => SecondPlaces + ThirdPlaces;
    public int WinDiff => Wins - Losses;
    public double AveragePlace => (double)(FirstPlaces * 1 + SecondPlaces * 2 + ThirdPlaces * 3) / Matches;

    public double FirstRate => (double)FirstPlaces / Matches;
    public double SecondRate => (double)SecondPlaces / Matches;
    public double ThirdRate => (double)ThirdPlaces / Matches;
    public double WinRate => FirstRate;
    public double LossRate => (double)(SecondPlaces + ThirdPlaces) / Matches;

    public static FLAggregateStats operator +(FLAggregateStats a, FLAggregateStats b) {
        return new FLAggregateStats() {
            Matches = a.Matches + b.Matches,
            FirstPlaces = a.FirstPlaces + b.FirstPlaces,
            SecondPlaces = a.SecondPlaces + b.SecondPlaces,
            ThirdPlaces = a.ThirdPlaces + b.ThirdPlaces
        };
    }

    public static FLAggregateStats operator -(FLAggregateStats a, FLAggregateStats b) {
        return new FLAggregateStats() {
            Matches = a.Matches - b.Matches,
            FirstPlaces = a.FirstPlaces - b.FirstPlaces,
            SecondPlaces = a.SecondPlaces - b.SecondPlaces,
            ThirdPlaces = a.ThirdPlaces - b.ThirdPlaces
        };
    }
}
