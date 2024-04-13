using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using PvpStats.Windows.Summary;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows;

internal class MainWindow : Window {

    private Plugin _plugin;
    private CrystallineConflictList ccMatches;
    private CrystallineConflictSummary ccSummary;
    private CrystallineConflictRecords ccRecords;
    private CrystallineConflictPlayerList ccPlayers;
    private CrystallineConflictJobList ccJobs;
    private CrystallineConflictPvPProfile ccProfile;
    private CrystallineConflictRankGraph ccRank;
    private string _currentTab = "";
    internal List<DataFilter> Filters { get; private set; } = new();
    internal SemaphoreSlim RefreshLock { get; init; } = new SemaphoreSlim(1);
    private bool _collapseFilters;

    private bool _firstDraw, _lastWindowCollapsed, _windowCollapsed;
    private Vector2 _lastWindowSize, _lastWindowPosition, _savedWindowSize;

    internal MainWindow(Plugin plugin) : base("Crystalline Conflict Tracker") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(425, 400),
            MaximumSize = new Vector2(5000, 5000)
        };
        Flags = Flags | ImGuiWindowFlags.NoScrollbar;
        _plugin = plugin;
        _collapseFilters = plugin.Configuration.CCWindowConfig.FiltersCollapsed;
        Filters.Add(new MatchTypeFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.MatchTypeFilter));
        Filters.Add(new ArenaFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.ArenaFilter));
        Filters.Add(new TimeFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.TimeFilter));
        Filters.Add(new LocalPlayerFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.LocalPlayerFilter));
        Filters.Add(new LocalPlayerJobFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.LocalPlayerJobFilter));
        var otherPlayerFilter = new OtherPlayerFilter(plugin, Refresh);
        Filters.Add(otherPlayerFilter);
        Filters.Add(new ResultFilter(plugin, Refresh));
        Filters.Add(new BookmarkFilter(plugin, Refresh));
        Filters.Add(new MiscFilter(plugin, Refresh, _plugin.Configuration.MatchWindowFilters.MiscFilter));

        ccMatches = new(plugin);
        ccSummary = new(plugin);
        ccRecords = new(plugin);
        ccJobs = new(plugin);
        ccPlayers = new(plugin);
        ccProfile = new(plugin);
        ccRank = new(plugin);
        _plugin.DataQueue.QueueDataOperation(Refresh);
    }

    public override void OnClose() {
        base.OnClose();
    }

    public async Task Refresh() {
        DateTime d0 = DateTime.Now;
        try {
            await RefreshLock.WaitAsync();
            await _plugin.CCStatsEngine.Refresh(Filters, ccJobs.StatSourceFilter, ccPlayers.InheritFromPlayerFilter);
            await ccMatches.Refresh(_plugin.CCStatsEngine.Matches);
            await ccPlayers.Refresh(_plugin.CCStatsEngine.Players);
            await ccJobs.Refresh(_plugin.CCStatsEngine.Jobs);
            await ccRank.Refresh(_plugin.CCStatsEngine.Matches);
        } finally {
            RefreshLock.Release();
        }
        SaveFilters();
        _plugin.Log.Debug($"total refresh time: {(DateTime.Now - d0).TotalMilliseconds} ms");
    }

    public override void PreDraw() {
        //_plugin.Log.Debug($"predraw collapsed: {Collapsed}");
        if(_plugin.Configuration.MinimizeWindow && _windowCollapsed && !_lastWindowCollapsed && _firstDraw) {
#if DEBUG
            _plugin.Log.Debug($"collapsed. Position: ({_lastWindowPosition.X},{_lastWindowPosition.Y}) Size: ({_lastWindowSize.X},{_lastWindowSize.Y})");
#endif
            if(!_plugin.Configuration.MinimizeDirectionLeft) {
                SetWindowPosition(new Vector2((_lastWindowPosition.X + (_lastWindowSize.X - 425 * ImGuiHelpers.GlobalScale)), _lastWindowPosition.Y));
            }
            SetWindowSize(new Vector2(425, _lastWindowSize.Y));
            _savedWindowSize = _lastWindowSize / ImGuiHelpers.GlobalScale;
        } else if(_plugin.Configuration.MinimizeWindow && !_windowCollapsed && _lastWindowCollapsed && _savedWindowSize != Vector2.Zero) {

        } else if(_windowCollapsed) {
            PositionCondition = ImGuiCond.Once;
        }

        _lastWindowCollapsed = _windowCollapsed;
        _windowCollapsed = true;
        base.PreDraw();
    }

    public override void Draw() {
        if(!ImGui.IsWindowCollapsed()) {
            _firstDraw = true;
        }
        _windowCollapsed = false;
        SizeCondition = ImGuiCond.Once;
        PositionCondition = ImGuiCond.Once;
        if(_plugin.Configuration.MinimizeWindow && _lastWindowCollapsed && _savedWindowSize != Vector2.Zero) {
#if DEBUG
            _plugin.Log.Debug($"un-collapsed window");
#endif
            if(!_plugin.Configuration.MinimizeDirectionLeft) {
                SetWindowPosition(new Vector2(ImGui.GetWindowPos().X - (_savedWindowSize.X - 425) * ImGuiHelpers.GlobalScale, ImGui.GetWindowPos().Y));
            }
            SetWindowSize(_savedWindowSize);
        }
        _lastWindowSize = ImGui.GetWindowSize();
        _lastWindowPosition = ImGui.GetWindowPos();

        if(!_collapseFilters && ImGui.BeginChild("FilterChild",
            new Vector2(ImGui.GetContentRegionAvail().X, _plugin.Configuration.CCWindowConfig.FilterHeight),
            true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysVerticalScrollbar)) {
            DrawFiltersTable();
            ImGui.EndChild();
        }
        //I copied this code from Item Search xD
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, -5 * ImGui.GetIO().FontGlobalScale));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if(ImGui.Button($"{(_collapseFilters ? (char)FontAwesomeIcon.CaretDown : (char)FontAwesomeIcon.CaretUp)}", new Vector2(-1, 10 * ImGui.GetIO().FontGlobalScale))) {
            int direction = _collapseFilters ? 1 : -1;
            _collapseFilters = !_collapseFilters;
            _plugin.Configuration.CCWindowConfig.FiltersCollapsed = _collapseFilters;
            if(_plugin.Configuration.CCWindowConfig.AdjustWindowHeightOnFilterCollapse) {
                SetWindowSize(new Vector2(ImGui.GetWindowSize().X, ImGui.GetWindowSize().Y + direction * _plugin.Configuration.CCWindowConfig.FilterHeight));
            }
        }
        ImGui.PopStyleVar(2);
        ImGui.PopFont();
        ImGuiHelper.WrappedTooltip($"{(_collapseFilters ? "Show filters" : "Hide filters")}");

        using(var tabBar = ImRaii.TabBar("TabBar", ImGuiTabBarFlags.None)) {
            if(tabBar) {
                if(_plugin.Configuration.ResizeWindowLeft) {
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

    private void SaveFilters() {
        foreach(var filter in Filters) {
            switch(filter.GetType()) {
                case Type _ when filter.GetType() == typeof(MatchTypeFilter):
                    _plugin.Configuration.MatchWindowFilters.MatchTypeFilter = (MatchTypeFilter)filter;
                    break;
                case Type _ when filter.GetType() == typeof(ArenaFilter):
                    //_plugin.Configuration.MatchWindowFilters.ArenaFilter = (ArenaFilter)filter;
                    break;
                case Type _ when filter.GetType() == typeof(TimeFilter):
                    _plugin.Configuration.MatchWindowFilters.TimeFilter = (TimeFilter)filter;
                    break;
                case Type _ when filter.GetType() == typeof(LocalPlayerFilter):
                    _plugin.Configuration.MatchWindowFilters.LocalPlayerFilter = (LocalPlayerFilter)filter;
                    break;
                case Type _ when filter.GetType() == typeof(LocalPlayerJobFilter):
                    //_plugin.Configuration.MatchWindowFilters.LocalPlayerJobFilter = (LocalPlayerJobFilter)filter;
                    break;
                case Type _ when filter.GetType() == typeof(OtherPlayerFilter):
                    //_plugin.Configuration.MatchWindowFilters.OtherPlayerFilter = (OtherPlayerFilter)filter;
                    break;
                case Type _ when filter.GetType() == typeof(ResultFilter):
                    break;
                case Type _ when filter.GetType() == typeof(BookmarkFilter):
                    //_plugin.Configuration.MatchWindowFilters.BookmarkFilter = bookmarkFilter;
                    break;
                case Type _ when filter.GetType() == typeof(MiscFilter):
                    _plugin.Configuration.MatchWindowFilters.MiscFilter = (MiscFilter)filter;
                    break;
            }
        }
        _plugin.Configuration.MatchWindowFilters.StatSourceFilter = ccJobs.StatSourceFilter;
        _plugin.Configuration.MatchWindowFilters.MinMatches = ccPlayers.MinMatches;
        _plugin.Configuration.MatchWindowFilters.PlayersInheritFromPlayerFilter = ccPlayers.InheritFromPlayerFilter;
        _plugin.Configuration.Save();
    }

    private unsafe void Tab(string name, Action action) {
        var flags = ImGuiTabItemFlags.None;
        if(_plugin.Configuration.ResizeWindowLeft) {
            flags |= ImGuiTabItemFlags.Trailing;
        }
        using var tab = ImRaii.TabItem(name);
        if(tab) {
            ChangeTab(name);
            //suppress errors and draw all tabs while a refresh is happening
            bool refreshLockAcquired = RefreshLock.Wait(0);
            try {
                action.Invoke();
            } catch {
                //suppress all exceptions while a refresh is in progress
                if(refreshLockAcquired) {
                    _plugin.Log.Debug("draw error on refresh lock acquired.");
                    throw;
                }
            } finally {
                if(refreshLockAcquired) {
                    RefreshLock.Release();
                }
            }
        }
    }

    private void ChangeTab(string tab) {
        if(_currentTab != tab) {
#if DEBUG
            _plugin.Log.Debug("changing tab to " + tab);
#endif
            SaveTabSize(_currentTab);
            _currentTab = tab;
            if(_plugin.Configuration.PersistWindowSizePerTab) {
                LoadTabSize(tab);
            }
        } else {
            //SizeCondition = ImGuiCond.Once;
            //PositionCondition = ImGuiCond.Once;
        }
    }

    private void SaveTabSize(string tab) {
        if(tab != "") {
            if(_plugin.Configuration.CCWindowConfig.TabWindowSizes.ContainsKey(tab)) {
                _plugin.Configuration.CCWindowConfig.TabWindowSizes[tab] = ImGui.GetWindowSize() / ImGuiHelpers.GlobalScale;
            } else {
                _plugin.Configuration.CCWindowConfig.TabWindowSizes.Add(tab, ImGui.GetWindowSize() / ImGuiHelpers.GlobalScale);
            }
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
    }

    private void LoadTabSize(string tab) {
        if(_plugin.Configuration.CCWindowConfig.TabWindowSizes.ContainsKey(tab)) {
            var currentSize = ImGui.GetWindowSize();
            var newSize = _plugin.Configuration.CCWindowConfig.TabWindowSizes[tab];
            if(_plugin.Configuration.ResizeWindowLeft) {
                var currentPos = ImGui.GetWindowPos();
                SetWindowPosition(new Vector2(currentPos.X - (newSize.X - currentSize.X), currentPos.Y));
            }
            SetWindowSize(newSize);
        }
    }

    private void SetWindowSize(Vector2 size) {
        SizeCondition = ImGuiCond.Always;
        Size = size;
#if DEBUG
        _plugin.Log.Debug($"Setting size to: ({Size.Value.X},{Size.Value.Y})");
#endif
        //_sizeChangeReset = true;
    }

    private void SetWindowPosition(Vector2 pos) {
        PositionCondition = ImGuiCond.Always;
        Position = pos;
#if DEBUG
        _plugin.Log.Debug($"Setting position to: ({Position.Value.X},{Position.Value.Y})");
#endif
        //_positionChangeReset = true;
    }
}
