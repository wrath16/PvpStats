using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace PvpStats.Windows.List;
internal class FrontlineMatchList : MatchList<FrontlineMatch> {

    //protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Hideable | ImGuiTableFlags.Borders;

    //protected override bool DynamicColumns { get; set; } = false;

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Start Time", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 125f },
        new ColumnParams{Name = "Arena", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 140f },
        new ColumnParams{Name = "Job", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f, Priority = 1 },
        new ColumnParams{Name = "Team", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 65f },
        new ColumnParams{Name = "Duration", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f, Priority = 2 },
        new ColumnParams{Name = "Result", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f },
    };

    public FrontlineMatchList(Plugin plugin, SemaphoreSlim? interlock = null) : base(plugin, plugin.FLCache, interlock) {
    }

    public override void DrawListItem(FrontlineMatch item) {
        ImGui.SameLine(0f * ImGuiHelpers.GlobalScale);
        if(item.IsBookmarked) {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(_plugin.Configuration.Colors.Favorite - new Vector4(0f, 0f, 0f, 0.7f)));
        }
        ImGui.Text($"{item.DutyStartTime:MM/dd/yyyy HH:mm}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(MatchHelper.GetFrontlineArenaName(item.Arena));

        ImGui.TableNextColumn();
        var localPlayerJob = item.LocalPlayerTeamMember!.Job;
        ImGuiHelper.CenterAlignCursor(localPlayerJob.ToString() ?? "");
        ImGui.TextColored(_plugin.Configuration.GetJobColor(localPlayerJob), localPlayerJob.ToString());

        ImGui.TableNextColumn();
        var teamColor = _plugin.Configuration.GetFrontlineTeamColor(item.LocalPlayerTeam);
        ImGui.TextColored(teamColor, item.LocalPlayerTeam.ToString());

        ImGui.TableNextColumn();
        ImGui.Text(ImGuiHelper.GetTimeSpanString(item.MatchDuration ?? TimeSpan.Zero));

        ImGui.TableNextColumn();
        var color = item.Result switch {
            0 => _plugin.Configuration.Colors.Win,
            2 => _plugin.Configuration.Colors.Loss,
            _ => _plugin.Configuration.Colors.Other,
        };
        string resultText = item.Result != null ? ImGuiHelper.AddOrdinal((int)item.Result + 1).ToUpper() : "???";
        ImGuiHelper.CenterAlignCursor(resultText);
        ImGui.TextColored(color, resultText);
    }

    protected override string CSVRow(FrontlineMatch match) {
        string csv = "";
        csv += match.DutyStartTime + ",";
        csv += (match.Arena != null ? MatchHelper.GetFrontlineArenaName((FrontlineMap)match.Arena) : "") + ",";
        csv += match.LocalPlayerTeamMember?.Job + ",";
        csv += match.LocalPlayerTeam + ",";
        csv += match.MatchDuration + ",";
        csv += match.Result + ",";
        csv += "\n";
        return csv;
    }
}
