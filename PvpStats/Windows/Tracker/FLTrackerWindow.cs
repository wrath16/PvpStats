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

    public FLTrackerWindow(Plugin plugin) : base(plugin, plugin.FLStatsEngine, plugin.Configuration.FLWindowConfig, "Frontline Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(435, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        MatchFilters.Add(new FrontlineArenaFilter(plugin, Refresh));
        MatchFilters.Add(new TimeFilter(plugin, Refresh, plugin.Configuration.FLWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, Refresh, plugin.Configuration.FLWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, Refresh));
        MatchFilters.Add(new OtherPlayerFilter(plugin, Refresh));
        MatchFilters.Add(new FLResultFilter(plugin, Refresh));
        MatchFilters.Add(new DurationFilter(plugin, Refresh));
        MatchFilters.Add(new BookmarkFilter(plugin, Refresh));
        MatchFilters.Add(new TagFilter(plugin, Refresh));

        _matchList = new(plugin);
        _summary = new(plugin);
        _jobStats = new(plugin, this);
        _profile = new(plugin);
    }

    public override void DrawInternal() {
        DrawFilters();

        using(var tabBar = ImRaii.TabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(tabBar) {
                Tab("Matches", () => {
                    _matchList.Draw();
                });
                Tab("Summary", () => {
                    using(ImRaii.Child("SummaryChild")) {
                        _summary.Draw();
                    }
                });
                Tab("Jobs", () => {
                    _jobStats.Draw();
                });
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
        try {
            await RefreshLock.WaitAsync();
            RefreshActive = true;
            await Plugin.FLStatsEngine.Refresh(MatchFilters, [_jobStats.StatSourceFilter], new());
            Stopwatch s1 = new();
            s1.Start();
            Task.WaitAll([
                _matchList.Refresh(Plugin.FLStatsEngine.Matches),
                _jobStats.Refresh(Plugin.FLStatsEngine.Jobs),
            ]);
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"all window modules", s1.ElapsedMilliseconds.ToString()));
            s1.Restart();
            SaveFilters();
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"save config", s1.ElapsedMilliseconds.ToString()));
        } catch {
            Plugin.Log.Error("Refresh on fl stats window failed.");
            throw;
        } finally {
            RefreshLock.Release();
            RefreshActive = false;
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"FL tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }
}
