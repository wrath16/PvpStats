using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;

public enum StatSource {
    LocalPlayer,
    Teammate,
    Opponent,
    Spectated
}

public class StatSourceFilter : DataFilter, IEquatable<StatSourceFilter> {

    public override string Name => "Stat Source";
    internal bool AllSelected { get; set; }
    internal bool InheritFromPlayerFilter { get; set; }
    public Dictionary<StatSource, bool> FilterState { get; set; } = new();

    public static Dictionary<StatSource, string> FilterNames => new() {
        { StatSource.LocalPlayer, "Local Player" },
        { StatSource.Teammate, "Teammates" },
        { StatSource.Opponent, "Opponents" },
        { StatSource.Spectated, "Spectated Matches" }
    };

    public StatSourceFilter() {
        Initialize();
    }

    public StatSourceFilter(StatSourceFilter filter) {
        Initialize(filter);
    }

    internal StatSourceFilter(Plugin plugin, Func<Task> action, StatSourceFilter? filter = null) : base(plugin, action) {
        Initialize(filter);
    }

    private void Initialize(StatSourceFilter? filter = null) {
        FilterState = new() {
                {StatSource.LocalPlayer, true },
                {StatSource.Teammate, true },
                {StatSource.Opponent, true },
                {StatSource.Spectated, true },
            };

        if(filter is not null) {
            foreach(var category in filter.FilterState) {
                FilterState[category.Key] = category.Value;
            }
            InheritFromPlayerFilter = filter.InheritFromPlayerFilter;
        }
        UpdateAllSelected();
    }

    protected void UpdateAllSelected() {
        AllSelected = true;
        foreach(var category in FilterState) {
            AllSelected = AllSelected && category.Value;
        }
    }

    internal override void Draw() {
        using var table = ImRaii.Table("statSourceTable", 4, ImGuiTableFlags.NoClip);
        if(table) {
            ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 4, ImGuiHelpers.GlobalScale * 150f));
            ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 4, ImGuiHelpers.GlobalScale * 150f));
            ImGui.TableSetupColumn($"c3", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 4, ImGuiHelpers.GlobalScale * 150f));
            ImGui.TableSetupColumn($"c4", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 4, ImGuiHelpers.GlobalScale * 200f));
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            bool allSelected = AllSelected;
            if(ImGui.Checkbox($"Select All##{GetHashCode()}", ref allSelected)) {
                Task.Run(async () => {
                    foreach(var category in FilterState) {
                        FilterState[category.Key] = allSelected;
                    }
                    AllSelected = allSelected;
                    await Refresh();
                });
            }
            ImGui.TableNextColumn();
            bool inheritFromPlayerFilter = InheritFromPlayerFilter;
            if(ImGui.Checkbox($"Inherit from player filter##{GetHashCode()}", ref inheritFromPlayerFilter)) {
                Task.Run(async () => {
                    InheritFromPlayerFilter = inheritFromPlayerFilter;
                    await Refresh();
                });
            }
            ImGuiHelper.HelpMarker("Will only include stats for players who match all conditions of the player filter.");
            ImGui.TableNextRow();

            foreach(var category in FilterState) {
                ImGui.TableNextColumn();
                bool filterState = category.Value;
                if(ImGui.Checkbox($"{FilterNames[category.Key]}##{GetHashCode()}", ref filterState)) {
                    Task.Run(async () => {
                        FilterState[category.Key] = filterState;
                        UpdateAllSelected();
                        await Refresh();
                    });
                }
            }
        }
    }

    public bool Equals(StatSourceFilter? other) {
        return FilterState.All(x => x.Value == other?.FilterState[x.Key]) && (InheritFromPlayerFilter == other?.InheritFromPlayerFilter);
    }
}
