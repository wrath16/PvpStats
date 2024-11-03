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

    private readonly RivalWingsMatchList _matches;
    private readonly RivalWingsSummary _summary;
    private readonly RivalWingsPlayerList _players;
    private readonly RivalWingsPvPProfile _profile;

    private bool _matchRefreshActive = true;
    private bool _jobRefreshActive = true;
    private bool _playerRefreshActive = true;

    public RWTrackerWindow(Plugin plugin) : base(plugin, plugin.RWStatsEngine, plugin.Configuration.RWWindowConfig, "Rival Wings Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(435, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        var refreshAction = () => Refresh();

        var playerFilter = new OtherPlayerFilter(plugin, refreshAction);
        var playerStatSourceFilter = new PlayerStatSourceFilter(plugin, refreshAction, plugin.Configuration.RWWindowConfig.PlayerStatFilters.StatSourceFilter);
        var playerMinMatchFilter = new MinMatchFilter(plugin, refreshAction, plugin.Configuration.RWWindowConfig.PlayerStatFilters.MinMatchFilter);
        var playerQuickSearchFilter = new PlayerQuickSearchFilter(plugin, refreshAction);

        MatchFilters.Add(new TimeFilter(plugin, refreshAction, plugin.Configuration.RWWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, refreshAction, plugin.Configuration.RWWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(playerFilter);
        MatchFilters.Add(new ResultFilter(plugin, refreshAction));
        MatchFilters.Add(new DurationFilter(plugin, refreshAction));
        MatchFilters.Add(new BookmarkFilter(plugin, refreshAction));
        MatchFilters.Add(new TagFilter(plugin, refreshAction));

        PlayerStatFilters.Add(playerStatSourceFilter);
        PlayerStatFilters.Add(playerMinMatchFilter);

        _matches = new(plugin);
        _summary = new(plugin);
        _players = new(plugin, playerStatSourceFilter, playerMinMatchFilter, playerQuickSearchFilter, playerFilter);
        _profile = new(plugin);
    }

    public override void DrawInternal() {
        DrawFilters();

        using(var tabBar = ImRaii.TabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(tabBar) {
                Tab("Matches", () => {
                    _matches.Draw();
                }, _matchRefreshActive, 0f);
                Tab("Summary", () => {
                    using(ImRaii.Child("SummaryChild")) {
                        _summary.Draw();
                    }
                }, _summary.RefreshActive, _summary.RefreshProgress);
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
        _matchRefreshActive = true;

        _summary.RefreshProgress = 0f;
        _summary.RefreshActive = true;
        
        _players.RefreshProgress = 0f;
        _playerRefreshActive = true;
        try {
            await RefreshLock.WaitAsync();
            //RefreshActive = true;
            var updatedSet = Plugin.RWStatsEngine.Refresh(MatchFilters);

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
            var playerRefresh = RefreshTab(async () => {
                await _players.Refresh(updatedSet.Matches, updatedSet.Additions, updatedSet.Removals);
                _playerRefreshActive = false;
            });

            await Task.WhenAll([
                Task.Run(SaveFilters),
                matchRefresh.Result,
                summaryRefresh.Result,
                playerRefresh.Result,
            ]);
        } catch {
            Plugin.Log.Error("RW tracker refresh failed.");
            throw;
        } finally {
            _matchRefreshActive = false;
            _summary.RefreshActive = false;
            _playerRefreshActive = false;
            RefreshLock.Release();
            //RefreshActive = false;
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"RW tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }
}
