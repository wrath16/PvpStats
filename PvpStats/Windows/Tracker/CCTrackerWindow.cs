using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Types.Match;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Summary;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Tracker;

internal class CCTrackerWindow : TrackerWindow<CrystallineConflictMatch> {

    private readonly CrystallineConflictMatchList _matches;
    private readonly CrystallineConflictSummary _summary;
    private readonly CrystallineConflictRecords _records;
    private readonly CrystallineConflictPlayerList _players;
    private readonly CrystallineConflictJobList _jobs;
    private readonly CrystallineConflictPvPProfile _profile;
    private readonly CrystallineConflictRankGraph _credit;

    private bool _matchRefreshActive = true;
    private bool _jobRefreshActive = true;
    private bool _playerRefreshActive = true;

    internal CCTrackerWindow(Plugin plugin) : base(plugin, plugin.CCStatsEngine, plugin.Configuration.CCWindowConfig, "Crystalline Conflict Tracker") {
        //SizeConstraints = new WindowSizeConstraints {
        //    MinimumSize = new Vector2(425, 400),
        //    MaximumSize = new Vector2(5000, 5000)
        //};
        //Flags = Flags | ImGuiWindowFlags.NoScrollbar;

        var refreshAction = () => Refresh();

        var playerFilter = new OtherPlayerFilter(plugin, refreshAction);
        var jobStatSourceFilter = new StatSourceFilter(plugin, refreshAction, plugin.Configuration.CCWindowConfig.JobStatFilters.StatSourceFilter);
        var playerStatSourceFilter = new PlayerStatSourceFilter(plugin, refreshAction, plugin.Configuration.CCWindowConfig.PlayerStatFilters.StatSourceFilter);
        var playerMinMatchFilter = new MinMatchFilter(plugin, refreshAction, plugin.Configuration.CCWindowConfig.PlayerStatFilters.MinMatchFilter);
        var playerQuickSearchFilter = new PlayerQuickSearchFilter(plugin, refreshAction);

        MatchFilters.Add(new MatchTypeFilter(plugin, refreshAction, plugin.Configuration.CCWindowConfig.MatchFilters.MatchTypeFilter));
        MatchFilters.Add(new TierFilter(plugin, refreshAction));
        MatchFilters.Add(new ArenaFilter(plugin, refreshAction));
        MatchFilters.Add(new TimeFilter(plugin, refreshAction, plugin.Configuration.CCWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, refreshAction, plugin.Configuration.CCWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, refreshAction, plugin.Configuration.CCWindowConfig.MatchFilters.LocalPlayerJobFilter));
        MatchFilters.Add(playerFilter);
        MatchFilters.Add(new ResultFilter(plugin, refreshAction));
        MatchFilters.Add(new DurationFilter(plugin, refreshAction));
        MatchFilters.Add(new BookmarkFilter(plugin, refreshAction));
        MatchFilters.Add(new TagFilter(plugin, refreshAction));
        MatchFilters.Add(new MiscFilter(plugin, refreshAction, plugin.Configuration.CCWindowConfig.MatchFilters.MiscFilter));

        JobStatFilters.Add(jobStatSourceFilter);
        PlayerStatFilters.Add(playerStatSourceFilter);
        PlayerStatFilters.Add(playerMinMatchFilter);
        PlayerStatFilters.Add(playerQuickSearchFilter);

        _matches = new(plugin);
        _summary = new(plugin);
        _records = new(plugin);
        _jobs = new(plugin, jobStatSourceFilter, playerFilter);
        _players = new(plugin, playerStatSourceFilter, playerMinMatchFilter, playerQuickSearchFilter, playerFilter);
        _profile = new(plugin);
        _credit = new(plugin);
        //Plugin.DataQueue.QueueDataOperation(Refresh);
    }

    public override void OnClose() {
        base.OnClose();
    }

    public override async Task Refresh(bool fullRefresh = false) {
        Stopwatch s0 = new();
        s0.Start();

        _summary.RefreshProgress = 0f;
        _summary.RefreshActive = true;

        _records.RefreshProgress = 0f;
        _records.RefreshActive = true;

        _credit.RefreshProgress = 0f;
        _credit.RefreshActive = true;

        _jobs.RefreshProgress = 0f;
        _players.RefreshProgress = 0f;
        _credit.RefreshProgress = 0f;

        _matchRefreshActive = true;
        _jobRefreshActive = true;
        _playerRefreshActive = true;
        try {
            await RefreshLock.WaitAsync();
            //RefreshActive = true;
            var updatedSet = Plugin.CCStatsEngine.Refresh(MatchFilters);

            if(fullRefresh) {
                updatedSet.Removals = updatedSet.Matches;
                updatedSet.Additions = updatedSet.Matches;
            }

            var matchRefresh = RefreshTab(async () => {
                await _matches.Refresh(updatedSet.Matches);
                _matchRefreshActive = false;
            });
            var summaryRefresh = RefreshTab(async () => {
                await _summary.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
                _summary.RefreshActive = false;
            });
            var recordsRefresh = RefreshTab(async () => {
                await _records.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
                _records.RefreshActive = false;
            });
            var jobRefresh = RefreshTab(async () => {
                await _jobs.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
                _jobRefreshActive = false;
            });
            var playerRefresh = RefreshTab(async () => {
                await _players.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
                _playerRefreshActive = false;
            });
            var creditRefresh = RefreshTab(async () => {
                await _credit.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
                _credit.RefreshActive = false;
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
            Plugin.Log.Error("CC tracker refresh failed.");
            throw;
        } finally {
            _matchRefreshActive = false;
            _summary.RefreshActive = false;
            _records.RefreshActive = false;
            _jobRefreshActive = false;
            _playerRefreshActive = false;
            _credit.RefreshActive = false;
            RefreshLock.Release();
            //RefreshActive = false;
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"CC tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }

    public override void DrawInternal() {
        DrawFilters();
        using(var tabBar = ImRaii.TabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(tabBar) {
                if(Plugin.Configuration.ResizeWindowLeft) {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20f * ImGuiHelpers.GlobalScale);
                }
                Tab("Matches", _matches.Draw, _matchRefreshActive, 0f);
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
                Tab("Jobs", _jobs.Draw, _jobRefreshActive, _jobs.RefreshProgress);
                Tab("Players", _players.Draw, _playerRefreshActive, _players.RefreshProgress);
                Tab("Credit", () => {
                    using(ImRaii.Child("CreditChild")) {
                        _credit.Draw();
                    }
                }, _credit.RefreshActive, _credit.RefreshProgress);
                Tab("Profile", () => {
                    using(ImRaii.Child("ProfileChild")) {
                        _profile.Draw();
                    }
                });
            }
        }

    }
}
