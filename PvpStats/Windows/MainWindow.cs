using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Summary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace PvpStats.Windows;

internal class MainWindow : Window {

    private Plugin _plugin;
    private CrystallineConflictList ccMatches;
    private CrystallineConflictSummary ccSummary;
    private CrystallineConflictPlayerList ccPlayers;
    private string _currentTab = "";
    internal List<DataFilter> Filters { get; private set; } = new();
    internal SemaphoreSlim RefreshLock { get; init; } = new SemaphoreSlim(1, 1);
    private bool _collapseFilters;

    internal MainWindow(Plugin plugin) : base("Crystalline Conflict Tracker") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Always;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(400, 400),
            MaximumSize = new Vector2(1800, 1500)
        };
        Flags = Flags | ImGuiWindowFlags.NoScrollbar;
        _plugin = plugin;
        Filters.Add(new MatchTypeFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.MatchTypeFilter));
        Filters.Add(new ArenaFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.ArenaFilter));
        Filters.Add(new TimeFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.TimeFilter));
        Filters.Add(new LocalPlayerFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.LocalPlayerFilter));
        Filters.Add(new LocalPlayerJobFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.LocalPlayerJobFilter));
        var otherPlayerFilter = new OtherPlayerFilter(plugin, Refresh);
        Filters.Add(otherPlayerFilter);
        Filters.Add(new ResultFilter(plugin, Refresh));
        Filters.Add(new MiscFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.MiscFilter));

        ccMatches = new(plugin);
        ccSummary = new(plugin);
        ccPlayers = new(plugin, ccMatches, otherPlayerFilter);
        _plugin.DataQueue.QueueDataOperation(Refresh);
    }

    public override void OnClose() {
        base.OnClose();
    }

    public override void PreDraw() {
        base.PreDraw();
    }

    public void Refresh() {
        var matches = _plugin.Storage.GetCCMatches().Query().Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        foreach(var filter in Filters) {
            switch(filter.GetType()) {
                case Type _ when filter.GetType() == typeof(MatchTypeFilter):
                    var matchTypeFilter = (MatchTypeFilter)filter;
                    matches = matches.Where(x => matchTypeFilter.FilterState[x.MatchType]).ToList();
                    _plugin.Configuration.MatchWindowFilters.MatchTypeFilter = matchTypeFilter;
                    break;
                case Type _ when filter.GetType() == typeof(ArenaFilter):
                    var arenaFilter = (ArenaFilter)filter;
                    //include unknown maps under all
                    matches = matches.Where(x => (x.Arena == null && arenaFilter.AllSelected) || arenaFilter.FilterState[(CrystallineConflictMap)x.Arena!]).ToList();
                    _plugin.Configuration.MatchWindowFilters.ArenaFilter = arenaFilter;
                    break;
                case Type _ when filter.GetType() == typeof(TimeFilter):
                    var timeFilter = (TimeFilter)filter;
                    switch(timeFilter.StatRange) {
                        case TimeRange.PastDay:
                            matches = matches.Where(x => (DateTime.Now - x.DutyStartTime).TotalHours < 24).ToList();
                            break;
                        case TimeRange.PastWeek:
                            matches = matches.Where(x => (DateTime.Now - x.DutyStartTime).TotalDays < 7).ToList();
                            break;
                        case TimeRange.ThisMonth:
                            matches = matches.Where(x => x.DutyStartTime.Month == DateTime.Now.Month && x.DutyStartTime.Year == DateTime.Now.Year).ToList();
                            break;
                        case TimeRange.LastMonth:
                            var lastMonth = DateTime.Now.AddMonths(-1);
                            matches = matches.Where(x => x.DutyStartTime.Month == lastMonth.Month && x.DutyStartTime.Year == lastMonth.Year).ToList();
                            break;
                        case TimeRange.ThisYear:
                            matches = matches.Where(x => x.DutyStartTime.Year == DateTime.Now.Year).ToList();
                            break;
                        case TimeRange.LastYear:
                            matches = matches.Where(x => x.DutyStartTime.Year == DateTime.Now.AddYears(-1).Year).ToList();
                            break;
                        case TimeRange.Custom:
                            matches = matches.Where(x => x.DutyStartTime > timeFilter.StartTime && x.DutyStartTime < timeFilter.EndTime).ToList();
                            break;
                        case TimeRange.Season:
                            matches = matches.Where(x => x.DutyStartTime > ArenaSeason.Season[timeFilter.Season].StartDate && x.DutyStartTime < ArenaSeason.Season[timeFilter.Season].EndDate).ToList();
                            break;
                        case TimeRange.All:
                        default:
                            break;
                    }
                    _plugin.Configuration.MatchWindowFilters.TimeFilter = timeFilter;
                    break;
                case Type _ when filter.GetType() == typeof(LocalPlayerFilter):
                    var localPlayerFilter = (LocalPlayerFilter)filter;
                    if(localPlayerFilter.CurrentPlayerOnly && _plugin.ClientState.IsLoggedIn && _plugin.GameState.GetCurrentPlayer() != null) {
                        matches = matches.Where(x => x.LocalPlayer != null && x.LocalPlayer.Equals((PlayerAlias)_plugin.GameState.GetCurrentPlayer())).ToList();
                    }
                    _plugin.Configuration.MatchWindowFilters.LocalPlayerFilter = localPlayerFilter;
                    break;
                case Type _ when filter.GetType() == typeof(LocalPlayerJobFilter):
                    var localPlayerJobFilter = (LocalPlayerJobFilter)filter;
                    //_plugin.Log.Debug($"anyjob: {localPlayerJobFilter.AnyJob} role: {localPlayerJobFilter.JobRole} job: {localPlayerJobFilter.PlayerJob}");
                    if(!localPlayerJobFilter.AnyJob) {
                        if(localPlayerJobFilter.JobRole != null) {
                            matches = matches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && PlayerJobHelper.GetSubRoleFromJob(x.LocalPlayerTeamMember.Job) == localPlayerJobFilter.JobRole).ToList();
                        } else {
                            matches = matches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && x.LocalPlayerTeamMember.Job == localPlayerJobFilter.PlayerJob).ToList();
                        }
                    }
                    _plugin.Configuration.MatchWindowFilters.LocalPlayerJobFilter = localPlayerJobFilter;
                    break;
                case Type _ when filter.GetType() == typeof(OtherPlayerFilter):
                    var otherPlayerFilter = (OtherPlayerFilter)filter;
                    //if (!otherPlayerFilter.PlayerNamesRaw.IsNullOrEmpty()) {

                    //}
                    matches = matches.Where(x => {
                        foreach(var team in x.Teams) {
                            if(otherPlayerFilter.TeamStatus == TeamStatus.Teammate && team.Key != x.LocalPlayerTeam?.TeamName) {
                                continue;
                            } else if(otherPlayerFilter.TeamStatus == TeamStatus.Opponent && team.Key == x.LocalPlayerTeam?.TeamName) {
                                continue;
                            }
                            foreach(var player in team.Value.Players) {
                                if(!otherPlayerFilter.AnyJob && player.Job != otherPlayerFilter.PlayerJob) {
                                    continue;
                                } else if(player.Alias.FullName.Contains(otherPlayerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase)) {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }).ToList();
                    //_plugin.Configuration.MatchWindowFilters.OtherPlayerFilter = otherPlayerFilter;
                    break;
                case Type _ when filter.GetType() == typeof(ResultFilter):
                    var resultFilter = (ResultFilter)filter;
                    if(resultFilter.Result == MatchResult.Win) {
                        matches = matches.Where(x => x.IsWin).ToList();
                    } else if(resultFilter.Result == MatchResult.Loss) {
                        matches = matches.Where(x => !x.IsWin && x.MatchWinner != null && !x.IsSpectated).ToList();
                    } else if(resultFilter.Result == MatchResult.Other) {
                        matches = matches.Where(x => x.IsSpectated || x.MatchWinner == null).ToList();
                    }
                    break;
                case Type _ when filter.GetType() == typeof(MiscFilter):
                    var miscFilter = (MiscFilter)filter;
                    if(miscFilter.MustHaveStats) {
                        matches = matches.Where(x => x.PostMatch is not null).ToList();
                    }
                    _plugin.Configuration.MatchWindowFilters.MiscFilter = miscFilter;
                    break;
            }
        }
        try {
            RefreshLock.WaitAsync();
            ccMatches.Refresh(matches);
            ccSummary.Refresh(matches);
            ccPlayers.Refresh(new());
            _plugin.Configuration.Save();
        } finally {
            RefreshLock.Release();
        }
    }

    public override void Draw() {
        if(!_collapseFilters && ImGui.BeginChild("FilterChild",
            new Vector2(ImGui.GetContentRegionAvail().X, _plugin.Configuration.FilterHeight),
            true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysVerticalScrollbar)) {
            DrawFiltersTable();
            ImGui.EndChild();
        }
        //I copied this code from Item Search xD
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, -5 * ImGui.GetIO().FontGlobalScale));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if(ImGui.Button($"{(_collapseFilters ? (char)FontAwesomeIcon.CaretDown : (char)FontAwesomeIcon.CaretUp)}", new Vector2(-1, 10 * ImGui.GetIO().FontGlobalScale))) {
            _collapseFilters = !_collapseFilters;
        }
        ImGui.PopStyleVar(2);
        ImGui.PopFont();
        ImGuiHelper.WrappedTooltip($"{(_collapseFilters ? "Show filters" : "Hide filters")}");

        if(ImGui.BeginTabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(ImGui.BeginTabItem("Matches")) {
                ChangeTab("Matches");
                ccMatches.Draw();
                ImGui.EndTabItem();
            }
            if(ImGui.BeginTabItem("Summary")) {
                ChangeTab("Summary");
                if(ImGui.BeginChild("SummaryChild")) {
                    ccSummary.Draw();
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if(ImGui.BeginTabItem("Players")) {
                ChangeTab("Players");
                ccPlayers.Draw();

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawFiltersTable() {
        if(ImGui.BeginTable("FilterTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersInnerH)) {
            ImGui.TableSetupColumn("filterName", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 110f);
            ImGui.TableSetupColumn($"filters", ImGuiTableColumnFlags.WidthStretch);
            //ImGui.TableNextRow();
            foreach(var filter in Filters) {
                //ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, 4);
                ImGui.TableNextColumn();

                if(filter.HelpMessage != null) {
                    ImGui.AlignTextToFramePadding();
                    ImGuiHelper.HelpMarker(filter.HelpMessage, false);
                    ImGui.SameLine();
                }
                //ImGui.GetStyle().FramePadding.X = ImGui.GetStyle().FramePadding.X - 2f;
                string nameText = $"{filter.Name}:";
                ImGuiHelper.RightAlignCursor(nameText);
                ImGui.AlignTextToFramePadding();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + float.Max(0, 16f - 4f * ImGuiHelpers.GlobalScale));
                ImGui.Text($"{nameText}");
                //ImGui.PopStyleVar();
                //ImGui.GetStyle().FramePadding.X = ImGui.GetStyle().FramePadding.X + 2f;
                ImGui.TableNextColumn();
                //if (filter.GetType() == typeof(TimeFilter)) {
                //    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2f);
                //}
                filter.Draw();
            }
            ImGui.EndTable();
        }
    }

    private void ChangeTab(string tab) {
        if(_currentTab != tab) {
            SaveTabSize(_currentTab);
            _currentTab = tab;
            if(_plugin.Configuration.PersistWindowSizePerTab) {
                LoadTabSize(tab);
            }
        } else {
            SizeCondition = ImGuiCond.Once;
        }
    }

    private void SaveTabSize(string tab) {
        if(tab != "") {
            if(_plugin.Configuration.CCWindowConfig.TabWindowSizes.ContainsKey(tab)) {
                _plugin.Configuration.CCWindowConfig.TabWindowSizes[tab] = ImGui.GetWindowSize();
            } else {
                _plugin.Configuration.CCWindowConfig.TabWindowSizes.Add(tab, ImGui.GetWindowSize());
            }
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
    }

    private void LoadTabSize(string tab) {
        if(_plugin.Configuration.CCWindowConfig.TabWindowSizes.ContainsKey(tab)) {
            Size = _plugin.Configuration.CCWindowConfig.TabWindowSizes[tab];
            SizeCondition = ImGuiCond.Always;
        }
    }
}
