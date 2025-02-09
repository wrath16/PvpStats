using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
internal class FLTeamQuickFilter : DataFilter {

    public override string Name => "Team";
    internal bool AllSelected { get; set; }
    public Dictionary<FrontlineTeamName, bool> FilterState { get; set; } = new();

    public FLTeamQuickFilter() { }

    internal FLTeamQuickFilter(Plugin plugin, Func<Task> action, FLTeamQuickFilter? filter = null) : base(plugin, action) {
        FilterState = new() {
                {FrontlineTeamName.Maelstrom, true },
                {FrontlineTeamName.Adders, true },
                {FrontlineTeamName.Flames, true },
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
        //bool allSelected = AllSelected;
        //if(ImGui.Checkbox($"Select All##{GetHashCode()}", ref allSelected)) {
        //    RateLimitRefresh(() => {
        //        foreach(var category in FilterState) {
        //            FilterState[category.Key] = allSelected;
        //        }
        //        AllSelected = allSelected;
        //    });
        //}
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0f));
        using var table = ImRaii.Table("flteamquickfilter", 3);
        if(table) {
            ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"c3", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            foreach(var category in FilterState) {
                ImGui.TableNextColumn();
                var image = TextureHelper.FrontlineTeamIcons[category.Key];
                var size = 25f;
                //ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f * ImGuiHelpers.GlobalScale);
                //ImGui.AlignTextToFramePadding();

                bool filterState = category.Value;
                if(ImGui.Checkbox($"##{category.Key}{GetHashCode()}", ref filterState)) {
                    Task.Run(async () => {
                        FilterState[category.Key] = filterState;
                        UpdateAllSelected();
                        await Refresh();
                    });
                }
                ImGui.SameLine();
                ImGui.Image(_plugin.WindowManager.GetTextureHandle(image), new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0f), new Vector2(1f));

            }
        }
    }
}
