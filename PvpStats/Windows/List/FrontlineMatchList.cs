using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace PvpStats.Windows.List;
internal class FrontlineMatchList : MatchList<FrontlineMatch> {

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Start Time", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 125f },
    };

    public FrontlineMatchList(Plugin plugin, SemaphoreSlim? interlock = null) : base(plugin, plugin.FLCache, interlock) {
    }

    public override void DrawListItem(FrontlineMatch item) {
        ImGui.SameLine(0f * ImGuiHelpers.GlobalScale);
        if(item.IsBookmarked) {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(_plugin.Configuration.Colors.Favorite - new Vector4(0f, 0f, 0f, 0.7f)));
        }
        ImGui.Text($"{item.DutyStartTime:MM/dd/yyyy HH:mm}");
    }

    protected override string CSVRow(FrontlineMatch match) {
        return "";
    }
}
