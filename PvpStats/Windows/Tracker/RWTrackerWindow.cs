using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Tracker;
internal class RWTrackerWindow : TrackerWindow {

    private readonly RivalWingsMatchList _matchList;

    public RWTrackerWindow(Plugin plugin) : base(plugin, plugin.Configuration.RWWindowConfig, "Rival Wings Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(435, 400),
            MaximumSize = new Vector2(5000, 5000)
        };
        MatchFilters.Add(new TimeFilter(plugin, Refresh, plugin.Configuration.RWWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, Refresh, plugin.Configuration.RWWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, Refresh));
        MatchFilters.Add(new OtherPlayerFilter(plugin, Refresh));
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

    public override async Task Refresh() {
        Stopwatch s0 = new();
        s0.Start();
        try {
            await RefreshLock.WaitAsync();
            await Plugin.RWStatsEngine.Refresh(MatchFilters, new(), new());
            Stopwatch s1 = new();
            s1.Start();
            Task.WaitAll([
                _matchList.Refresh(Plugin.RWStatsEngine.Matches),
            ]);
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"all window modules", s1.ElapsedMilliseconds.ToString()));
            s1.Restart();
            SaveFilters();
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"save config", s1.ElapsedMilliseconds.ToString()));
        } catch {
            Plugin.Log.Error("Refresh on rw stats window failed.");
            throw;
        } finally {
            RefreshLock.Release();
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"RW tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }
}
