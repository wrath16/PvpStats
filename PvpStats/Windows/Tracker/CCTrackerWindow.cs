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

    private readonly CrystallineConflictMatchList _ccMatches;
    private readonly CrystallineConflictSummary _ccSummary;
    private readonly CrystallineConflictRecords _ccRecords;
    private readonly CrystallineConflictPlayerList _ccPlayers;
    private readonly CrystallineConflictJobList _ccJobs;
    private readonly CrystallineConflictPvPProfile _ccProfile;
    private readonly CrystallineConflictRankGraph _ccRank;

    internal CCTrackerWindow(Plugin plugin) : base(plugin, plugin.CCStatsEngine, plugin.Configuration.CCWindowConfig, "Crystalline Conflict Tracker") {
        //SizeConstraints = new WindowSizeConstraints {
        //    MinimumSize = new Vector2(425, 400),
        //    MaximumSize = new Vector2(5000, 5000)
        //};
        //Flags = Flags | ImGuiWindowFlags.NoScrollbar;
        MatchFilters.Add(new MatchTypeFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.MatchTypeFilter));
        MatchFilters.Add(new TierFilter(plugin, Refresh));
        MatchFilters.Add(new ArenaFilter(plugin, Refresh));
        MatchFilters.Add(new TimeFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.TimeFilter));
        MatchFilters.Add(new LocalPlayerFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.LocalPlayerFilter));
        MatchFilters.Add(new LocalPlayerJobFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.LocalPlayerJobFilter));
        MatchFilters.Add(new OtherPlayerFilter(plugin, Refresh));
        MatchFilters.Add(new ResultFilter(plugin, Refresh));
        MatchFilters.Add(new DurationFilter(plugin, Refresh));
        MatchFilters.Add(new BookmarkFilter(plugin, Refresh));
        MatchFilters.Add(new TagFilter(plugin, Refresh));
        MatchFilters.Add(new MiscFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.MiscFilter));

        _ccMatches = new(plugin);
        _ccSummary = new(plugin);
        _ccRecords = new(plugin);
        _ccJobs = new(plugin, this);
        _ccPlayers = new(plugin, this);
        _ccProfile = new(plugin);
        _ccRank = new(plugin);
        //Plugin.DataQueue.QueueDataOperation(Refresh);
    }

    public override void OnClose() {
        base.OnClose();
    }

    public override async Task Refresh() {
        Stopwatch s0 = new();
        s0.Start();
        try {
            await RefreshLock.WaitAsync();
            RefreshActive = true;
            await Plugin.CCStatsEngine.Refresh(MatchFilters, [_ccJobs.StatSourceFilter], [_ccPlayers.StatSourceFilter]);
            Stopwatch s1 = new();
            s1.Start();
            Task.WaitAll([
                _ccMatches.Refresh(Plugin.CCStatsEngine.Matches),
                _ccPlayers.Refresh(Plugin.CCStatsEngine.Players),
                _ccJobs.Refresh(Plugin.CCStatsEngine.Jobs),
                _ccRank.Refresh(Plugin.CCStatsEngine.Matches),
            ]);
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"all window modules", s1.ElapsedMilliseconds.ToString()));
            s1.Restart();
            SaveFilters();
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"save config", s1.ElapsedMilliseconds.ToString()));
        } catch {
            Plugin.Log.Error("Refresh on cc stats window failed.");
            throw;
        } finally {
            RefreshLock.Release();
            RefreshActive = false;
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
                Tab("Matches", _ccMatches.Draw);
                Tab("Summary", () => {
                    using(ImRaii.Child("SummaryChild")) {
                        _ccSummary.Draw();
                    }
                });
                Tab("Records", () => {
                    using(ImRaii.Child("RecordsChild")) {
                        _ccRecords.Draw();
                    }
                });
                Tab("Jobs", _ccJobs.Draw);
                Tab("Players", _ccPlayers.Draw);
                Tab("Credit", () => {
                    using(ImRaii.Child("CreditChild")) {
                        _ccRank.Draw();
                    }
                });
                Tab("Profile", () => {
                    using(ImRaii.Child("ProfileChild")) {
                        _ccProfile.Draw();
                    }
                });
            }
        }

    }
}
