using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Types.Match;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Records;
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

    internal CCTrackerWindow(Plugin plugin) : base(plugin, plugin.CCStatsEngine, plugin.Configuration.CCWindowConfig, "Crystalline Conflict Tracker") {
        //SizeConstraints = new WindowSizeConstraints {
        //    MinimumSize = new Vector2(425, 400),
        //    MaximumSize = new Vector2(5000, 5000)
        //};
        //Flags = Flags | ImGuiWindowFlags.NoScrollbar;

        var refreshAction = () => Refresh();

        var playerFilter = new OtherPlayerFilter(plugin, refreshAction);

        MatchFilters.Add(new MatchTypeFilter(plugin, refreshAction, WindowConfig.MatchFilters.MatchTypeFilter));
        MatchFilters.Add(new TierFilter(plugin, refreshAction));
        MatchFilters.Add(new ArenaFilter(plugin, refreshAction));
        MatchFilters.Add(new TimeFilter(plugin, refreshAction, WindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, refreshAction, WindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, refreshAction, WindowConfig.MatchFilters.LocalPlayerJobFilter));
        MatchFilters.Add(playerFilter);
        MatchFilters.Add(new ResultFilter(plugin, refreshAction));
        MatchFilters.Add(new DurationFilter(plugin, refreshAction));
        MatchFilters.Add(new BookmarkFilter(plugin, refreshAction));
        MatchFilters.Add(new TagFilter(plugin, refreshAction));
        MatchFilters.Add(new MiscFilter(plugin, refreshAction, WindowConfig.MatchFilters.MiscFilter));

        _matches = new(plugin);
        _summary = new(plugin);
        _records = new(plugin);
        _jobs = new(plugin, WindowConfig.JobStatFilters.StatSourceFilter, playerFilter);
        _players = new(plugin, WindowConfig.PlayerStatFilters.StatSourceFilter, WindowConfig.PlayerStatFilters.MinMatchFilter, null, playerFilter);
        _profile = new(plugin);
        _credit = new(plugin);

        JobStatFilters.Add(_jobs.StatSourceFilter);
        PlayerStatFilters.Add(_players.StatSourceFilter);
        PlayerStatFilters.Add(_players.MinMatchFilter);
        PlayerStatFilters.Add(_players.PlayerQuickSearchFilter);

        //Plugin.DataQueue.QueueDataOperation(Refresh);
    }

    public override void OnClose() {
        base.OnClose();
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

        _credit.RefreshProgress = 0f;
        _credit.RefreshActive = true;

        _jobs.RefreshProgress = 0f;
        _jobs.RefreshActive = true;

        _players.RefreshProgress = 0f;
        _players.RefreshActive = true;

        _credit.RefreshProgress = 0f;
        _credit.RefreshActive = true;
        try {
            var updatedSet = Plugin.CCStatsEngine.Refresh(MatchFilters);

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
            var creditRefresh = RefreshTab(async () => {
                await _credit.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
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
                Tab("Matches", _matches.Draw, _matches.RefreshActive, 0f);
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
                Tab("Jobs", _jobs.Draw, _jobs.RefreshActive, _jobs.RefreshProgress);
                Tab("Players", _players.Draw, _players.RefreshActive, _players.RefreshProgress);
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
