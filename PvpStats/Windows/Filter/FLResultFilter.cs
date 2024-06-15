using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public class FLResultFilter : DataFilter {

    public override string Name => "Result";
    internal bool AllSelected { get; set; }
    public Dictionary<int, bool> FilterState { get; set; } = new();

    public FLResultFilter() { }

    internal FLResultFilter(Plugin plugin, Func<Task> action, FLResultFilter? filter = null) : base(plugin, action) {
        FilterState = new() {
                {0, true },
                {1, true },
                {2, true },
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
        }

        using var table = ImRaii.Table("FlResultFilterTable", 3);
        if(table) {
            ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 400f));
            ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 400f));
            ImGui.TableSetupColumn($"c3", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 400f));
            ImGui.TableNextRow();

            foreach(var category in FilterState) {
                ImGui.TableNextColumn();
                bool filterState = category.Value;
                if(ImGui.Checkbox($"{ImGuiHelper.AddOrdinal(category.Key + 1).ToUpper()}##{GetHashCode()}", ref filterState)) {
                    RateLimitRefresh(() => {
                        FilterState[category.Key] = filterState;
                        UpdateAllSelected();
                    });
                }
            }
        }
    }
}
