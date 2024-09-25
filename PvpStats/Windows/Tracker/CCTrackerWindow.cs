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
    private readonly CrystallineConflictRankGraph _rank;

    private bool _matchRefreshActive = true;
    private bool _summaryRefreshActive = true;
    private bool _recordsRefreshActive = true;
    private bool _jobRefreshActive = true;
    private bool _playerRefreshActive = true;
    private bool _creditRefreshActive = true;

    internal CCTrackerWindow(Plugin plugin) : base(plugin, plugin.CCStatsEngine, plugin.Configuration.CCWindowConfig, "Crystalline Conflict Tracker") {
        //SizeConstraints = new WindowSizeConstraints {
        //    MinimumSize = new Vector2(425, 400),
        //    MaximumSize = new Vector2(5000, 5000)
        //};
        //Flags = Flags | ImGuiWindowFlags.NoScrollbar;

        var playerFilter = new OtherPlayerFilter(plugin, Refresh);
        var jobStatSourceFilter = new StatSourceFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.JobStatFilters.StatSourceFilter);
        var playerStatSourceFilter = new StatSourceFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.PlayerStatFilters.StatSourceFilter);
        var playerMinMatchFilter = new MinMatchFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.PlayerStatFilters.MinMatchFilter);
        var playerQuickSearchFilter = new PlayerQuickSearchFilter(plugin, Refresh);

        MatchFilters.Add(new MatchTypeFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.MatchTypeFilter));
        MatchFilters.Add(new TierFilter(plugin, Refresh));
        MatchFilters.Add(new ArenaFilter(plugin, Refresh));
        MatchFilters.Add(new TimeFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.LocalPlayerJobFilter));
        MatchFilters.Add(playerFilter);
        MatchFilters.Add(new ResultFilter(plugin, Refresh));
        MatchFilters.Add(new DurationFilter(plugin, Refresh));
        MatchFilters.Add(new BookmarkFilter(plugin, Refresh));
        MatchFilters.Add(new TagFilter(plugin, Refresh));
        MatchFilters.Add(new MiscFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.MiscFilter));

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
        _rank = new(plugin);
        //Plugin.DataQueue.QueueDataOperation(Refresh);
    }

    public override void OnClose() {
        base.OnClose();
    }

    public override async Task Refresh() {
        Stopwatch s0 = new();
        s0.Start();

        _summary.RefreshProgress = 0f;
        _jobs.RefreshProgress = 0f;
        _players.RefreshProgress = 0f;
        _rank.RefreshProgress = 0f;
        _records.RefreshProgress = 0f;
        _summaryRefreshActive = true;
        _matchRefreshActive = true;
        _recordsRefreshActive = true;
        _jobRefreshActive = true;
        _playerRefreshActive = true;
        _creditRefreshActive = true;
        try {
            await RefreshLock.WaitAsync();
            //RefreshActive = true;
            var updatedSet = Plugin.CCStatsEngine.Refresh(MatchFilters);
            await Task.WhenAll([
                Task.Run(() => _matches.Refresh(updatedSet.Matches).ContinueWith(x => _matchRefreshActive = false)),
                Task.Run(() => _summary.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _summaryRefreshActive = false)),
                Task.Run(() => _records.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _recordsRefreshActive = false)),
                Task.Run(() => _jobs.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _jobRefreshActive = false)),
                Task.Run(() => _players.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _playerRefreshActive = false)),
                _rank.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals).ContinueWith(x => _creditRefreshActive = false),
                Task.Run(SaveFilters)
            ]);
        } catch {
            Plugin.Log.Error("CC tracker refresh failed.");
            throw;
        } finally {
            _matchRefreshActive = false;
            _summaryRefreshActive = false;
            _recordsRefreshActive = false;
            _jobRefreshActive = false;
            _playerRefreshActive = false;
            _creditRefreshActive = false;
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
                }, _summaryRefreshActive, _summary.RefreshProgress);
                Tab("Records", () => {
                    using(ImRaii.Child("RecordsChild")) {
                        _records.Draw();
                    }
                }, _recordsRefreshActive, _records.RefreshProgress);
                Tab("Jobs", _jobs.Draw, _jobRefreshActive, _jobs.RefreshProgress);
                Tab("Players", _players.Draw, _playerRefreshActive, _players.RefreshProgress);
                Tab("Credit", () => {
                    using(ImRaii.Child("CreditChild")) {
                        _rank.Draw();
                    }
                }, _creditRefreshActive, _rank.RefreshProgress);
                Tab("Profile", () => {
                    using(ImRaii.Child("ProfileChild")) {
                        _profile.Draw();
                    }
                });
            }
        }

    }
}
