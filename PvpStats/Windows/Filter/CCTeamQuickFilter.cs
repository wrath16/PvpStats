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
internal class CCTeamQuickFilter : DataFilter {

    public override string Name => "Team";
    internal bool AllSelected { get; set; }
    public Dictionary<CrystallineConflictTeamName, bool> FilterState { get; set; } = new();

    public CCTeamQuickFilter() { }

    internal CCTeamQuickFilter(Plugin plugin, Func<Task> action, CCTeamQuickFilter? filter = null) : base(plugin, action) {
        FilterState = new() {
                {CrystallineConflictTeamName.Astra, true },
                {CrystallineConflictTeamName.Umbra, true },
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
        using var table = ImRaii.Table("ccteamquickfilter", 2);
        if(table) {
            ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            foreach(var category in FilterState) {
                ImGui.TableNextColumn();
                bool filterState = category.Value;
                if(ImGui.Checkbox($"##{category.Key}{GetHashCode()}", ref filterState)) {
                    Task.Run(async() => {
                        FilterState[category.Key] = filterState;
                        UpdateAllSelected();
                        await Refresh();
                    });
                }
                ImGui.SameLine();
                DrawCCTeamIcon(category.Key, 25f);
                ImGuiHelper.WrappedTooltip(category.Key.ToString());
            }
        }
    }

    internal void DrawCCTeamIcon(CrystallineConflictTeamName team, float size) {
        Vector2 uv0, uv1;
        switch(team) {
            case CrystallineConflictTeamName.Astra:
            default:
                uv0 = new Vector2(0.939f, 0.22f);
                uv1 = new Vector2(1.0f, 0.44f);
                break;
            case CrystallineConflictTeamName.Umbra:
                uv0 = new Vector2(0.939f, 0.00f);
                uv1 = new Vector2(1.0f, 0.22f);
                break;
        };
        ImGui.Image(_plugin.WindowManager.GetTextureHandle(TextureHelper.CCTeamIconTexture), new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), uv0, uv1);
    }
}
