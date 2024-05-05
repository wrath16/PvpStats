using PvpStats.Settings;
using PvpStats.Windows.Filter;
using System.Threading.Tasks;

namespace PvpStats.Windows.Tracker;
internal class FLTrackerWindow : TrackerWindow {
    public FLTrackerWindow(Plugin plugin, WindowConfiguration config, string name) : base(plugin, config, name) {
        MatchFilters.Add(new TimeFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new DurationFilter(plugin, Refresh));
        MatchFilters.Add(new BookmarkFilter(plugin, Refresh));
    }

    public override void DrawInternal() {
        DrawFilters();
    }

    public override Task Refresh() {
        return Task.CompletedTask;
    }
}
