using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using System.Threading.Tasks;

namespace PvpStats.Windows.Tracker;
internal class FLTrackerWindow : TrackerWindow {

    private FrontlineMatchList _matchList;

    public FLTrackerWindow(Plugin plugin) : base(plugin, plugin.Configuration.FLWindowConfig, "Frontline Tracker") {
        MatchFilters.Add(new TimeFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new DurationFilter(plugin, Refresh));
        MatchFilters.Add(new BookmarkFilter(plugin, Refresh));

        _matchList = new(plugin);
    }

    public override void DrawInternal() {
        DrawFilters();

        using(var tabBar = ImRaii.TabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(tabBar) {
                Tab("Matches", _matchList.Draw);
            }
        }
    }

    public override Task Refresh() {
        return Task.CompletedTask;
    }
}
