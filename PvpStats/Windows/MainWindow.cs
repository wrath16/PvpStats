using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Windows.Filter;
using PvpStats.Windows.List;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;

namespace PvpStats.Windows;

internal class MainWindow : Window {

    private Plugin _plugin;
    private CrystallineConflictList ccMatches;
    internal List<DataFilter> Filters { get; private set; } = new();
    internal SemaphoreSlim RefreshLock { get; init; } = new SemaphoreSlim(1, 1);

    internal MainWindow(Plugin plugin) : base("Pvp Stats") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Always;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(750, 1500)
        };
        _plugin = plugin;
        Filters.Add(new MatchTypeFilter(plugin, Refresh));

        ccMatches = new(plugin);
        _plugin.DataQueue.QueueDataOperation(Refresh);
    }

    public override void OnClose() {
        base.OnClose();
    }

    public override void PreDraw() {
        base.PreDraw();
    }

    public void Refresh() {
        try {
            RefreshLock.Wait();
            var matches = _plugin.Storage.GetCCMatches().Query().Where(m => !m.IsDeleted).OrderByDescending(m => m.DutyStartTime).ToList();
            foreach (var filter in Filters) {
                switch (filter.GetType()) {
                    case Type _ when filter.GetType() == typeof(MatchTypeFilter):
                        var matchTypeFilter = (MatchTypeFilter)filter;
                        matches = matches.Where(x => matchTypeFilter.FilterState[x.MatchType]).ToList();
                        break;
                }
            }
            ccMatches.Refresh(matches);
        } finally {
            RefreshLock.Release();
        }
    }

    public override void Draw() {

        if(ImGui.BeginChild("FilterChild", new Vector2(ImGui.GetContentRegionAvail().X, float.Max(ImGuiHelpers.GlobalScale * 150, ImGui.GetWindowHeight() / 4f)), true, ImGuiWindowFlags.AlwaysAutoResize)) {
            if (ImGui.BeginTable("FilterTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersInnerH)) {
                ImGui.BeginTable("FilterTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersInner);
                ImGui.TableSetupColumn("filterName", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 110f);
                ImGui.TableSetupColumn($"filters", ImGuiTableColumnFlags.WidthStretch);
                //ImGui.TableNextRow();

                foreach (var filter in Filters) {
                    //ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, 4);
                    ImGui.TableNextColumn();

                    if (filter.HelpMessage != null) {
                        ImGui.AlignTextToFramePadding();
                        ImGuiHelper.HelpMarker(filter.HelpMessage);
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
            ImGui.EndChild();
        }

        if (ImGui.BeginTabBar("TabBar", ImGuiTabBarFlags.None)) {
            if (ImGui.BeginTabItem("Matches")) {

                ccMatches.Draw();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Summary")) {

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

    }
}
