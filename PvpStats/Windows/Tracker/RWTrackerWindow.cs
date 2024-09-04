using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Types.Match;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Summary;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Tracker;
internal class RWTrackerWindow : TrackerWindow<RivalWingsMatch> {

    private readonly RivalWingsMatchList _matchList;
    private readonly RivalWingsSummary _summary;
    private readonly RivalWingsPvPProfile _profile;

    public RWTrackerWindow(Plugin plugin) : base(plugin, plugin.RWStatsEngine, plugin.Configuration.RWWindowConfig, "Rival Wings Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(435, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        MatchFilters.Add(new TimeFilter(plugin, Refresh, plugin.Configuration.RWWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, Refresh, plugin.Configuration.RWWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, Refresh));
        MatchFilters.Add(new OtherPlayerFilter(plugin, Refresh));
        MatchFilters.Add(new ResultFilter(plugin, Refresh));
        MatchFilters.Add(new DurationFilter(plugin, Refresh));
        MatchFilters.Add(new BookmarkFilter(plugin, Refresh));
        MatchFilters.Add(new TagFilter(plugin, Refresh));

        _matchList = new(plugin);
        _summary = new(plugin);
        _profile = new(plugin);
    }

    public override void DrawInternal() {
        DrawFilters();

        using(var tabBar = ImRaii.TabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(tabBar) {
                Tab("Matches", () => {
                    _matchList.Draw();
                });
                Tab("Summary", () => {
                    using(ImRaii.Child("SummaryChild")) {
                        _summary.Draw();
                    }
                });
                Tab("Profile", () => {
                    using(ImRaii.Child("ProfileChild")) {
                        _profile.Draw();
                    }
                });
            }
        }
    }

    public override async Task Refresh() {
        Stopwatch s0 = new();
        s0.Start();
        try {
            await RefreshLock.WaitAsync();
            RefreshActive = true;
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
            RefreshActive = false;
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"RW tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }
}
