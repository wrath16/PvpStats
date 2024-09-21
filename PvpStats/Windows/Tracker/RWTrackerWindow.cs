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

    private bool _matchRefreshActive = true;
    private bool _summaryRefreshActive = true;

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
                }, _matchRefreshActive, 0f);
                Tab("Summary", () => {
                    using(ImRaii.Child("SummaryChild")) {
                        _summary.Draw();
                    }
                }, _summaryRefreshActive, _summary.RefreshProgress);
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
        _summaryRefreshActive = true;
        _matchRefreshActive = true;
        try {
            await RefreshLock.WaitAsync();
            //RefreshActive = true;
            var updatedSet = Plugin.RWStatsEngine.Refresh2(MatchFilters);
            Task.WaitAll([
                Task.Run(() => _matchList.Refresh(updatedSet.Matches).ContinueWith(x => _matchRefreshActive = false)),
                Task.Run(() => _summary.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _summaryRefreshActive = false)),
                Task.Run(SaveFilters)
            ]);
        } catch {
            Plugin.Log.Error("RW tracker refresh failed.");
            throw;
        } finally {
            _matchRefreshActive = false;
            _summaryRefreshActive = false;
            RefreshLock.Release();
            //RefreshActive = false;
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"RW tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }
}
