using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Types.Match;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Records;
using PvpStats.Windows.Summary;

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
        Tabs.Add(_matches);
        _summary = new(plugin);
        Tabs.Add(_summary);
        _records = new(plugin);
        Tabs.Add(_records);
        _jobs = new(plugin, WindowConfig.JobStatFilters.StatSourceFilter, playerFilter);
        Tabs.Add(_jobs);
        _players = new(plugin, WindowConfig.PlayerStatFilters.StatSourceFilter, WindowConfig.PlayerStatFilters.MinMatchFilter, null, playerFilter);
        Tabs.Add(_players);
        _credit = new(plugin);
        Tabs.Add(_credit);
        _profile = new(plugin);

        JobStatFilters.Add(_jobs.StatSourceFilter);
        PlayerStatFilters.Add(_players.StatSourceFilter);
        PlayerStatFilters.Add(_players.MinMatchFilter);
        PlayerStatFilters.Add(_players.PlayerQuickSearchFilter);
    }

    public override void OnClose() {
        base.OnClose();
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
