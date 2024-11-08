using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Types.Match;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Records;
using PvpStats.Windows.Summary;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Tracker;
internal class FLTrackerWindow : TrackerWindow<FrontlineMatch> {

    private readonly FrontlineMatchList _matches;
    private readonly FrontlineSummary _summary;
    private readonly FrontlineRecords _records;
    private readonly FrontlineJobList _jobs;
    private readonly FrontlinePlayerList _players;
    private readonly FrontlinePvPProfile _profile;

    public FLTrackerWindow(Plugin plugin) : base(plugin, plugin.FLStatsEngine, plugin.Configuration.FLWindowConfig, "Frontline Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(435, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        var refreshAction = () => Refresh();

        var playerFilter = new OtherPlayerFilter(plugin, refreshAction);
        //var jobStatSourceFilter = new FLStatSourceFilter(plugin, refreshAction);
        //var playerStatSourceFilter = new PlayerStatSourceFilter(plugin, refreshAction, plugin.Configuration.FLWindowConfig.PlayerStatFilters.StatSourceFilter);
        //var playerMinMatchFilter = new MinMatchFilter(plugin, refreshAction, plugin.Configuration.FLWindowConfig.PlayerStatFilters.MinMatchFilter);
        //var playerQuickSearchFilter = new PlayerQuickSearchFilter(plugin, refreshAction);

        MatchFilters.Add(new FrontlineArenaFilter(plugin, refreshAction));
        MatchFilters.Add(new TimeFilter(plugin, refreshAction, WindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, refreshAction, WindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, refreshAction));
        MatchFilters.Add(playerFilter);
        MatchFilters.Add(new FLResultFilter(plugin, refreshAction));
        MatchFilters.Add(new DurationFilter(plugin, refreshAction));
        MatchFilters.Add(new BookmarkFilter(plugin, refreshAction));
        MatchFilters.Add(new TagFilter(plugin, refreshAction));

        _matches = new(plugin);
        _summary = new(plugin);
        _records = new(plugin);
        _jobs = new(plugin, WindowConfig.JobStatFilters.StatSourceFilter, playerFilter);
        _players = new(plugin, WindowConfig.PlayerStatFilters.StatSourceFilter, WindowConfig.PlayerStatFilters.MinMatchFilter, null, playerFilter);
        _profile = new(plugin);

        JobStatFilters.Add(_jobs.StatSourceFilter);
        PlayerStatFilters.Add(_players.StatSourceFilter);
        PlayerStatFilters.Add(_players.MinMatchFilter);
        PlayerStatFilters.Add(_players.PlayerQuickSearchFilter);
    }

    public override void DrawInternal() {
        DrawFilters();

        using(var tabBar = ImRaii.TabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(tabBar) {
                Tab("Matches", () => {
                    _matches.Draw();
                }, _matches.RefreshActive, 0f);
                Tab("Summary", () => {
                    using(ImRaii.Child("SummaryChild")) {
                        _summary.Draw();
                    }
                }, _summary.RefreshActive, _summary.RefreshProgress);
                Tab("Records", () => {
                    using(ImRaii.Child("RecordsChild")) {
                        _records.Draw();
                    }
                }, _records.RefreshActive, _records.RefreshProgress);
                Tab("Jobs", () => {
                    _jobs.Draw();
                }, _jobs.RefreshActive, _jobs.RefreshProgress);
                Tab("Players", _players.Draw, _players.RefreshActive, _players.RefreshProgress);
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

        _matches.RefreshProgress = 0f;
        _matches.RefreshActive = true;

        _summary.RefreshProgress = 0f;
        _summary.RefreshActive = true;

        _records.RefreshProgress = 0f;
        _records.RefreshActive = true;

        _jobs.RefreshProgress = 0f;
        _jobs.RefreshActive = true;

        _players.RefreshProgress = 0f;
        _players.RefreshActive = true;

        try {
            var updatedSet = Plugin.FLStatsEngine.Refresh(MatchFilters);

            if(fullRefresh) {
                updatedSet.Removals = updatedSet.Matches;
                updatedSet.Additions = updatedSet.Matches;
            }

            var matchRefresh = RefreshTab(async () => {
                await _matches.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
            });
            var summaryRefresh = RefreshTab(async () => {
                await _summary.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
            });
            var recordsRefresh = RefreshTab(async () => {
                await _records.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
            });
            var jobRefresh = RefreshTab(async () => {
                await _jobs.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
            });
            var playerRefresh = RefreshTab(async () => {
                await _players.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
            });
            await Task.WhenAll([
                Task.Run(SaveFilters),
                matchRefresh.Result,
                summaryRefresh.Result,
                recordsRefresh.Result,
                jobRefresh.Result,
                playerRefresh.Result,
            ]);
        } catch {
            Plugin.Log.Error("FL tracker refresh failed.");
            throw;
        } finally {
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"FL tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }

    //public async Task RefreshJobs() {
    //    var jobRefresh = RefreshTab(async () => {
    //        await _jobs.Refresh(Plugin.FLStatsEngine.Matches, Plugin.FLStatsEngine.Matches, Plugin.FLStatsEngine.Matches);
    //        _jobRefreshActive = false;
    //    });
    //    await Task.WhenAll([
    //        jobRefresh.Result,
    //    ]);
    //}
}
