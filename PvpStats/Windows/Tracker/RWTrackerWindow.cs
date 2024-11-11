using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Types.Match;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Summary;
using System.Numerics;

namespace PvpStats.Windows.Tracker;
internal class RWTrackerWindow : TrackerWindow<RivalWingsMatch> {

    private readonly RivalWingsMatchList _matches;
    private readonly RivalWingsSummary _summary;
    private readonly RivalWingsJobList _jobs;
    private readonly RivalWingsPlayerList _players;
    private readonly RivalWingsPvPProfile _profile;

    public RWTrackerWindow(Plugin plugin) : base(plugin, plugin.RWStatsEngine, plugin.Configuration.RWWindowConfig, "Rival Wings Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(435, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        var refreshAction = () => Refresh();

        var playerFilter = new OtherPlayerFilter(plugin, refreshAction);
        //var playerStatSourceFilter = new PlayerStatSourceFilter(plugin, refreshAction, plugin.Configuration.RWWindowConfig.PlayerStatFilters.StatSourceFilter);
        //var playerMinMatchFilter = new MinMatchFilter(plugin, refreshAction, plugin.Configuration.RWWindowConfig.PlayerStatFilters.MinMatchFilter);
        //var playerQuickSearchFilter = new PlayerQuickSearchFilter(plugin, refreshAction);

        MatchFilters.Add(new TimeFilter(plugin, refreshAction, WindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, refreshAction, WindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(playerFilter);
        MatchFilters.Add(new ResultFilter(plugin, refreshAction));
        MatchFilters.Add(new DurationFilter(plugin, refreshAction));
        MatchFilters.Add(new BookmarkFilter(plugin, refreshAction));
        MatchFilters.Add(new TagFilter(plugin, refreshAction));

        _matches = new(plugin);
        Tabs.Add(_matches);
        _summary = new(plugin);
        Tabs.Add(_summary);
        _players = new(plugin, WindowConfig.PlayerStatFilters.StatSourceFilter, WindowConfig.PlayerStatFilters.MinMatchFilter, null, playerFilter);
        Tabs.Add(_players);
        _jobs = new(plugin, WindowConfig.JobStatFilters.StatSourceFilter, playerFilter);
        Tabs.Add(_jobs);
        _profile = new(plugin);

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
                Tab("Jobs", _jobs.Draw, _jobs.RefreshActive, _jobs.RefreshProgress);
                Tab("Players", _players.Draw, _players.RefreshActive, _players.RefreshProgress);
                Tab("Profile", () => {
                    using(ImRaii.Child("ProfileChild")) {
                        _profile.Draw();
                    }
                });
            }
        }
    }
}
