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
internal class FLTrackerWindow : TrackerWindow<FrontlineMatch> {

    private readonly FrontlineMatchList _matchList;
    private readonly FrontlineSummary _summary;
    private readonly FrontlineJobList _jobStats;
    private readonly FrontlinePvPProfile _profile;

    private bool _matchRefreshActive = true;
    private bool _summaryRefreshActive = true;
    private bool _jobRefreshActive = true;

    public FLTrackerWindow(Plugin plugin) : base(plugin, plugin.FLStatsEngine, plugin.Configuration.FLWindowConfig, "Frontline Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(435, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        var playerFilter = new OtherPlayerFilter(plugin, Refresh);
        var jobStatSourceFilter = new FLStatSourceFilter(plugin, Refresh);

        MatchFilters.Add(new FrontlineArenaFilter(plugin, Refresh));
        MatchFilters.Add(new TimeFilter(plugin, Refresh, plugin.Configuration.FLWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, Refresh, plugin.Configuration.FLWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, Refresh));
        MatchFilters.Add(playerFilter);
        MatchFilters.Add(new FLResultFilter(plugin, Refresh));
        MatchFilters.Add(new DurationFilter(plugin, Refresh));
        MatchFilters.Add(new BookmarkFilter(plugin, Refresh));
        MatchFilters.Add(new TagFilter(plugin, Refresh));

        JobStatFilters.Add(jobStatSourceFilter);

        _matchList = new(plugin);
        _summary = new(plugin);
        _jobStats = new(plugin, jobStatSourceFilter, playerFilter);
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
                Tab("Jobs", () => {
                    _jobStats.Draw();
                }, _jobRefreshActive, _jobStats.RefreshProgress);
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
        _summary.RefreshProgress = 0f;
        _jobStats.RefreshProgress = 0f;

        _summaryRefreshActive = true;
        _matchRefreshActive = true;
        _jobRefreshActive = true;
        try {
            await RefreshLock.WaitAsync();
            //RefreshActive = true;
            var updatedSet = Plugin.FLStatsEngine.Refresh2(MatchFilters);
            Task.WaitAll([
                Task.Run(() => _matchList.Refresh(updatedSet.Matches).ContinueWith(x => _matchRefreshActive = false)),
                Task.Run(() => _summary.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _summaryRefreshActive = false)),
                Task.Run(() => _jobStats.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _jobRefreshActive = false)),
                Task.Run(SaveFilters)
            ]);
        } catch {
            Plugin.Log.Error("FL tracker refresh failed.");
            throw;
        } finally {
            _matchRefreshActive = false;
            _summaryRefreshActive = false;
            _jobRefreshActive = false;
            RefreshLock.Release();
            //RefreshActive = false;
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"FL tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }
}
