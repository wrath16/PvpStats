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
    private readonly FrontlineJobList _jobs;
    private readonly FrontlinePlayerList _players;
    private readonly FrontlinePvPProfile _profile;

    private bool _matchRefreshActive = true;
    private bool _summaryRefreshActive = true;
    private bool _jobRefreshActive = true;
    private bool _playerRefreshActive = true;

    public FLTrackerWindow(Plugin plugin) : base(plugin, plugin.FLStatsEngine, plugin.Configuration.FLWindowConfig, "Frontline Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(435, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        var refreshAction = () => Refresh();

        var playerFilter = new OtherPlayerFilter(plugin, refreshAction);
        var jobStatSourceFilter = new FLStatSourceFilter(plugin, refreshAction);
        var playerStatSourceFilter = new PlayerStatSourceFilter(plugin, refreshAction, plugin.Configuration.FLWindowConfig.PlayerStatFilters.StatSourceFilter);
        var playerMinMatchFilter = new MinMatchFilter(plugin, refreshAction, plugin.Configuration.FLWindowConfig.PlayerStatFilters.MinMatchFilter);
        var playerQuickSearchFilter = new PlayerQuickSearchFilter(plugin, refreshAction);

        MatchFilters.Add(new FrontlineArenaFilter(plugin, refreshAction));
        MatchFilters.Add(new TimeFilter(plugin, refreshAction, plugin.Configuration.FLWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, refreshAction, plugin.Configuration.FLWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, refreshAction));
        MatchFilters.Add(playerFilter);
        MatchFilters.Add(new FLResultFilter(plugin, refreshAction));
        MatchFilters.Add(new DurationFilter(plugin, refreshAction));
        MatchFilters.Add(new BookmarkFilter(plugin, refreshAction));
        MatchFilters.Add(new TagFilter(plugin, refreshAction));

        JobStatFilters.Add(jobStatSourceFilter);
        PlayerStatFilters.Add(playerStatSourceFilter);
        PlayerStatFilters.Add(playerMinMatchFilter);

        _matchList = new(plugin);
        _summary = new(plugin);
        _jobs = new(plugin, jobStatSourceFilter, playerFilter);
        _players = new(plugin, playerStatSourceFilter, playerMinMatchFilter, playerQuickSearchFilter, playerFilter);
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
                    _jobs.Draw();
                }, _jobRefreshActive, _jobs.RefreshProgress);
                Tab("Players", _players.Draw, _playerRefreshActive, _players.RefreshProgress);
                Tab("Profile", () => {
                    using(ImRaii.Child("ProfileChild")) {
                        _profile.Draw();
                    }
                });
            }
        }
    }

    public override async Task Refresh(bool fullRefresh = false) {
        Stopwatch s0 = new();
        s0.Start();
        _summary.RefreshProgress = 0f;
        _jobs.RefreshProgress = 0f;

        _summaryRefreshActive = true;
        _matchRefreshActive = true;
        _jobRefreshActive = true;
        _playerRefreshActive = true;
        try {
            await RefreshLock.WaitAsync();
            //RefreshActive = true;
            var updatedSet = Plugin.FLStatsEngine.Refresh(MatchFilters);

            if(fullRefresh) {
                updatedSet.Removals = updatedSet.Matches;
                updatedSet.Additions = updatedSet.Matches;
            }

            Task.WaitAll([
                Task.Run(() => _matchList.Refresh(updatedSet.Matches).ContinueWith(x => _matchRefreshActive = false)),
                Task.Run(() => _summary.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _summaryRefreshActive = false)),
                Task.Run(() => _jobs.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _jobRefreshActive = false)),
                Task.Run(() => _players.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _playerRefreshActive = false)),
                Task.Run(SaveFilters)
            ]);
        } catch {
            Plugin.Log.Error("FL tracker refresh failed.");
            throw;
        } finally {
            _matchRefreshActive = false;
            _summaryRefreshActive = false;
            _jobRefreshActive = false;
            _playerRefreshActive = false;
            RefreshLock.Release();
            //RefreshActive = false;
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"FL tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }
}
