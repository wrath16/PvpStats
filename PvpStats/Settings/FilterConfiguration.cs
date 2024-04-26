using PvpStats.Windows.Filter;

namespace PvpStats.Settings;
public class FilterConfiguration {

    public MatchTypeFilter? MatchTypeFilter { get; set; }
    public ArenaFilter? ArenaFilter { get; set; }
    public TimeFilter? TimeFilter { get; set; }
    public LocalPlayerFilter? LocalPlayerFilter { get; set; }
    public LocalPlayerJobFilter? LocalPlayerJobFilter { get; set; }
    public MiscFilter? MiscFilter { get; set; }
    public StatSourceFilter? StatSourceFilter { get; set; }

    //player tab
    public uint MinMatches { get; set; } = 1;
    public bool PlayersInheritFromPlayerFilter { get; set; } = true;
}
