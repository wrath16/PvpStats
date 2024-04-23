using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public class MatchTypeFilter : DataFilter {

    public override string Name => "Queue";
    internal bool AllSelected { get; set; }
    public Dictionary<CrystallineConflictMatchType, bool> FilterState { get; set; } = new();

    public MatchTypeFilter() { }

    internal MatchTypeFilter(Plugin plugin, Func<Task> action, MatchTypeFilter? filter = null) : base(plugin, action) {
        FilterState = new() {
                {CrystallineConflictMatchType.Casual, true },
                {CrystallineConflictMatchType.Ranked, true },
                {CrystallineConflictMatchType.Custom, true },
                {CrystallineConflictMatchType.Unknown, true },
            };

        if(filter is not null) {
            foreach(var category in filter.FilterState) {
                FilterState[category.Key] = category.Value;
            }
        }
        UpdateAllSelected();
    }

    private void UpdateAllSelected() {
        AllSelected = true;
        foreach(var category in FilterState) {
            AllSelected = AllSelected && category.Value;
        }
    }

    internal override void Draw() {
        bool allSelected = AllSelected;
        if(ImGui.Checkbox($"Select All##{GetHashCode()}", ref allSelected)) {
            RateLimitRefresh(() => {
                foreach(var category in FilterState) {
                    FilterState[category.Key] = allSelected;
                }
                AllSelected = allSelected;
            });
            //_plugin!.DataQueue.QueueDataOperation(async () => {
            //    foreach(var category in FilterState) {
            //        FilterState[category.Key] = allSelected;
            //    }
            //    AllSelected = allSelected;
            //    await Refresh();
            //});
        }

        ImGui.BeginTable("matchTypeFilterTable", 2);
        ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 2, ImGuiHelpers.GlobalScale * 400f));
        ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 2, ImGuiHelpers.GlobalScale * 400f));
        ImGui.TableNextRow();

        foreach(var category in FilterState) {
            ImGui.TableNextColumn();
            bool filterState = category.Value;
            if(ImGui.Checkbox($"{category.Key}##{GetHashCode()}", ref filterState)) {
                //if(CurrentRefresh is null || CurrentRefresh.Result.IsCompleted) {
                //    CurrentRefresh = _plugin!.DataQueue.QueueDataOperation(async () => {
                //        FilterState[category.Key] = filterState;
                //        UpdateAllSelected();
                //        await Refresh();
                //    });
                //}
                RateLimitRefresh(() => {
                    FilterState[category.Key] = filterState;
                    UpdateAllSelected();
                });
            }
        }
        ImGui.EndTable();
    }
}
