using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Settings;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.Tracker;
internal abstract class TrackerWindow : Window {
    protected Plugin Plugin;
    protected WindowConfiguration WindowConfig;
    protected bool CollapseFilters;
    protected string CurrentTab = "";

    private bool _firstDraw, _lastWindowCollapsed, _windowCollapsed;
    private Vector2 _lastWindowSize, _lastWindowPosition, _savedWindowSize;
    private int _drawCycles;
    private float _longestDraw, _longestPreDraw;

    internal List<DataFilter> MatchFilters { get; private set; } = new();
    internal List<DataFilter> JobStatFilters { get; private set; } = new();
    internal List<DataFilter> PlayerStatFilters { get; private set; } = new();
    internal SemaphoreSlim RefreshLock { get; init; } = new SemaphoreSlim(1);

    protected TrackerWindow(Plugin plugin, WindowConfiguration config, string name) : base(name) {
        Plugin = plugin;
        CollapseFilters = config.FiltersCollapsed;
        WindowConfig = config;

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(425, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Flags |= ImGuiWindowFlags.NoScrollbar;
    }

    public abstract Task Refresh();
    public abstract void DrawInternal();

    protected void SaveFilters() {
        WindowConfig.MatchFilters.SetFilters(MatchFilters);
        WindowConfig.JobStatFilters.SetFilters(JobStatFilters);
        WindowConfig.PlayerStatFilters.SetFilters(PlayerStatFilters);
        Plugin.Configuration.Save();
    }

    public override void PreDraw() {
        //Plugin.Log.Debug($"predraw collapsed: {Collapsed}");
        if(Plugin.Configuration.MinimizeWindow && _windowCollapsed && !_lastWindowCollapsed && _firstDraw) {
#if DEBUG
            Plugin.Log.Debug($"collapsed. Position: ({_lastWindowPosition.X},{_lastWindowPosition.Y}) Size: ({_lastWindowSize.X},{_lastWindowSize.Y})");
#endif
            if(!Plugin.Configuration.MinimizeDirectionLeft) {
                SetWindowPosition(new Vector2(_lastWindowPosition.X + (_lastWindowSize.X - 425 * ImGuiHelpers.GlobalScale), _lastWindowPosition.Y));
            }
            SetWindowSize(new Vector2(425, _lastWindowSize.Y));
            _savedWindowSize = _lastWindowSize / ImGuiHelpers.GlobalScale;
        } else if(Plugin.Configuration.MinimizeWindow && !_windowCollapsed && _lastWindowCollapsed && _savedWindowSize != Vector2.Zero) {

        } else if(_windowCollapsed) {
            PositionCondition = ImGuiCond.Once;
        }

        _lastWindowCollapsed = _windowCollapsed;
        _windowCollapsed = true;
        base.PreDraw();
    }

    public override void Draw() {
        Stopwatch s1 = new();
        s1.Start();
        if(!ImGui.IsWindowCollapsed()) {
            _firstDraw = true;
        }
        _windowCollapsed = false;
        SizeCondition = ImGuiCond.Once;
        PositionCondition = ImGuiCond.Once;
        if(Plugin.Configuration.MinimizeWindow && _lastWindowCollapsed && _savedWindowSize != Vector2.Zero) {
#if DEBUG
            Plugin.Log.Debug($"un-collapsed window");
#endif
            if(!Plugin.Configuration.MinimizeDirectionLeft) {
                SetWindowPosition(new Vector2(ImGui.GetWindowPos().X - (_savedWindowSize.X - 425) * ImGuiHelpers.GlobalScale, ImGui.GetWindowPos().Y));
            }
            SetWindowSize(_savedWindowSize);
        }
        _lastWindowSize = ImGui.GetWindowSize();
        _lastWindowPosition = ImGui.GetWindowPos();
        DrawInternal();
        s1.Stop();
        if(_drawCycles % 5000 == 0) {
            _drawCycles = 0;
            if(s1.ElapsedMilliseconds > _longestDraw) {
                _longestDraw = s1.ElapsedMilliseconds;
            }
#if DEBUG
            Plugin.Log.Debug($"longest draw: {_longestDraw}");
#endif
        }
        _drawCycles++;
    }

    protected void DrawFilters() {
        if(!CollapseFilters) {
            using var child = ImRaii.Child("FilterChild",
            new Vector2(ImGui.GetContentRegionAvail().X, WindowConfig.FilterHeight),
            true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysVerticalScrollbar);
            if(child) {
                using var table = ImRaii.Table("FilterTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersInnerH);
                if(!table) return;
                ImGui.TableSetupColumn("filterName", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 110f);
                ImGui.TableSetupColumn($"filters", ImGuiTableColumnFlags.WidthStretch);
                foreach(var filter in MatchFilters) {
                    ImGui.TableNextColumn();

                    if(filter.HelpMessage != null) {
                        ImGui.AlignTextToFramePadding();
                        ImGuiHelper.HelpMarker(filter.HelpMessage, false);
                        ImGui.SameLine();
                    }
                    string nameText = $"{filter.Name}:";
                    ImGuiHelper.RightAlignCursor2(nameText, -5f * ImGuiHelpers.GlobalScale);
                    ImGui.AlignTextToFramePadding();
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + float.Max(0, 16f - 4f * ImGuiHelpers.GlobalScale));
                    ImGui.TextUnformatted(nameText);
                    ImGui.TableNextColumn();
                    filter.Draw();
                }
            }
        }
        //I copied this from Item Search xD
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            using var style1 = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(0, -5 * ImGuiHelpers.GlobalScale)).Push(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            if(ImGui.Button($"{(CollapseFilters ? (char)FontAwesomeIcon.CaretDown : (char)FontAwesomeIcon.CaretUp)}", new Vector2(-1, 10 * ImGuiHelpers.GlobalScale))) {
                int direction = CollapseFilters ? 1 : -1;
                CollapseFilters = !CollapseFilters;
                WindowConfig.FiltersCollapsed = CollapseFilters;
                if(Plugin.Configuration.AdjustWindowHeightOnFilterCollapse) {
                    SetWindowSize(new Vector2(ImGui.GetWindowSize().X, ImGui.GetWindowSize().Y + direction * WindowConfig.FilterHeight));
                }
            }
        }
        ImGuiHelper.WrappedTooltip($"{(CollapseFilters ? "Show filters" : "Hide filters")}");
    }

    protected unsafe void Tab(string name, Action action) {
        var flags = ImGuiTabItemFlags.None;
        if(Plugin.Configuration.ResizeWindowLeft) {
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
                    Plugin.Log.Debug("draw error on refresh lock acquired.");
                    throw;
                }
            } finally {
                if(refreshLockAcquired) {
                    RefreshLock.Release();
                }
            }
        }
    }

    protected void ChangeTab(string tab) {
        if(CurrentTab != tab) {
#if DEBUG
            Plugin.Log.Debug("changing tab to " + tab);
#endif
            SaveTabSize(CurrentTab);
            CurrentTab = tab;
            if(Plugin.Configuration.PersistWindowSizePerTab) {
                LoadTabSize(tab);
            }
        } else {
            //SizeCondition = ImGuiCond.Once;
            //PositionCondition = ImGuiCond.Once;
        }
    }

    protected void SaveTabSize(string tab) {
        if(tab != "") {
            if(WindowConfig.TabWindowSizes.ContainsKey(tab)) {
                WindowConfig.TabWindowSizes[tab] = ImGui.GetWindowSize() / ImGuiHelpers.GlobalScale;
            } else {
                WindowConfig.TabWindowSizes.Add(tab, ImGui.GetWindowSize() / ImGuiHelpers.GlobalScale);
            }
        }
    }

    protected void LoadTabSize(string tab) {
        if(WindowConfig.TabWindowSizes.ContainsKey(tab)) {
            var currentSize = ImGui.GetWindowSize();
            var newSize = WindowConfig.TabWindowSizes[tab];
            if(Plugin.Configuration.ResizeWindowLeft) {
                var currentPos = ImGui.GetWindowPos();
                SetWindowPosition(new Vector2(currentPos.X - (newSize.X - currentSize.X), currentPos.Y));
            }
            SetWindowSize(newSize);
        }
    }

    protected void SetWindowSize(Vector2 size) {
        SizeCondition = ImGuiCond.Always;
        Size = size;
#if DEBUG
        Plugin.Log.Debug($"Setting size to: ({Size.Value.X},{Size.Value.Y})");
#endif
        //_sizeChangeReset = true;
    }

    protected void SetWindowPosition(Vector2 pos) {
        PositionCondition = ImGuiCond.Always;
        Position = pos;
#if DEBUG
        Plugin.Log.Debug($"Setting position to: ({Position.Value.X},{Position.Value.Y})");
#endif
        //_positionChangeReset = true;
    }
}
