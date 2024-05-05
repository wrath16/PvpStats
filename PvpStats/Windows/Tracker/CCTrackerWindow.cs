using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Summary;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Tracker;

internal class CCTrackerWindow : TrackerWindow {

    private CrystallineConflictList ccMatches;
    private CrystallineConflictSummary ccSummary;
    private CrystallineConflictRecords ccRecords;
    private CrystallineConflictPlayerList ccPlayers;
    private CrystallineConflictJobList ccJobs;
    private CrystallineConflictPvPProfile ccProfile;
    private CrystallineConflictRankGraph ccRank;

    internal CCTrackerWindow(Plugin plugin) : base(plugin, plugin.Configuration.CCWindowConfig, "Crystalline Conflict Tracker") {
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
        MatchFilters.Add(new MiscFilter(plugin, Refresh, plugin.Configuration.CCWindowConfig.MatchFilters.MiscFilter));

        ccMatches = new(plugin);
        ccSummary = new(plugin);
        ccRecords = new(plugin);
        ccJobs = new(plugin, this);
        ccPlayers = new(plugin, this);
        ccProfile = new(plugin);
        ccRank = new(plugin);
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
            await Plugin.CCStatsEngine.Refresh(MatchFilters, ccJobs.StatSourceFilter, ccPlayers.StatSourceFilter.InheritFromPlayerFilter);
            Stopwatch s1 = new();
            s1.Start();
            Task.WaitAll([
                ccMatches.Refresh(Plugin.CCStatsEngine.Matches),
                ccPlayers.Refresh(Plugin.CCStatsEngine.Players),
                ccJobs.Refresh(Plugin.CCStatsEngine.Jobs),
                ccRank.Refresh(Plugin.CCStatsEngine.Matches),
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
            Plugin.Log.Information(string.Format("{0,-25}: {1,4} ms", $"CC tracker refresh time", s0.ElapsedMilliseconds.ToString()));
        }
    }

    public override void DrawInternal() {

        DrawFilters();

        using(var tabBar = ImRaii.TabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(tabBar) {
                if(Plugin.Configuration.ResizeWindowLeft) {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20f);
                }
                Tab("Matches", ccMatches.Draw);
                Tab("Summary", () => {
                    using(ImRaii.Child("SummaryChild")) {
                        ccSummary.Draw();
                    }
                });
                Tab("Records", () => {
                    using(ImRaii.Child("RecordsChild")) {
                        ccRecords.Draw();
                    }
                });
                Tab("Jobs", ccJobs.Draw);
                Tab("Players", ccPlayers.Draw);
                Tab("Credit", () => {
                    using(ImRaii.Child("CreditChild")) {
                        ccRank.Draw();
                    }
                });
                Tab("Profile", () => {
                    using(ImRaii.Child("ProfileChild")) {
                        ccProfile.Draw();
                    }
                });
            }
        }

    }
}
