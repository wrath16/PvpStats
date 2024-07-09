﻿using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class RivalWingsMatchList : MatchList<RivalWingsMatch> {

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Start Time", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 125f },
        new ColumnParams{Name = "Arena", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 140f },
        new ColumnParams{Name = "Job", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f, Priority = 1 },
        new ColumnParams{Name = "Team", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 55f },
        new ColumnParams{Name = "Duration", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f, Priority = 2 },
        new ColumnParams{Name = "Result", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f },
    };

    public RivalWingsMatchList(Plugin plugin, SemaphoreSlim? interlock = null) : base(plugin, plugin.RWCache, interlock) {
    }
    protected override void PreChildDraw() {
        ImGuiHelper.CSVButton(() => {
            _plugin.DataQueue.QueueDataOperation(() => {
                ListCSV = CSVHeader();
                foreach(var row in DataModel) {
                    ListCSV += CSVRow(row);
                }
                Task.Run(() => {
                    ImGui.SetClipboardText(ListCSV);
                });
            });
        });
        ImGui.SameLine();
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Ban.ToIconString()}##CloseAllMatches")) {
                _plugin.DataQueue.QueueDataOperation(_plugin.WindowManager.CloseAllMatchWindows);
            }
        }
        ImGuiHelper.WrappedTooltip("Close all open match windows");
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudRed, "Rival Wings match tracking temporarily disabled!");
        ImGuiHelper.HelpMarker("Due to the game-breaking changes introduced in 7.0 and a current lack of ability to test the game mode, the Rival Wings match tracker has been disabled for the time being.");
        ImGui.SameLine();
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            ImGuiHelper.RightAlignCursor(FontAwesomeIcon.Heart.ToIconString());
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X);
        }
        ImGuiHelper.DonateButton();
    }

    public override void DrawListItem(RivalWingsMatch item) {
        ImGui.SameLine(0f * ImGuiHelpers.GlobalScale);
        if(item.IsBookmarked) {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(_plugin.Configuration.Colors.Favorite - new Vector4(0f, 0f, 0f, 0.7f)));
        }
        ImGui.Text($"{item.DutyStartTime:MM/dd/yyyy HH:mm}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(MatchHelper.GetArenaName(item.Arena));

        ImGui.TableNextColumn();
        var localPlayerJob = item.LocalPlayerTeamMember?.Job;
        ImGuiHelper.CenterAlignCursor(localPlayerJob?.ToString() ?? "");
        ImGui.TextColored(_plugin.Configuration.GetJobColor(localPlayerJob), localPlayerJob?.ToString());

        ImGui.TableNextColumn();
        var teamColor = _plugin.Configuration.GetRivalWingsTeamColor(item.LocalPlayerTeam);
        ImGui.TextColored(teamColor, item.LocalPlayerTeam.ToString());

        ImGui.TableNextColumn();
        var timeSpanString = ImGuiHelper.GetTimeSpanString(item.MatchDuration ?? TimeSpan.Zero);
        ImGuiHelper.DrawNumericCell(timeSpanString, -10f);

        ImGui.TableNextColumn();
        bool isWin = item.IsWin;
        bool isLoss = item.IsLoss;

        var color = isWin ? _plugin.Configuration.Colors.Win : isLoss ? _plugin.Configuration.Colors.Loss : _plugin.Configuration.Colors.Other;
        string resultText = isWin ? "WIN" : isLoss ? "LOSS" : "???";
        ImGuiHelper.CenterAlignCursor(resultText);
        ImGui.TextColored(color, resultText);
    }

    protected override string CSVRow(RivalWingsMatch match) {
        string csv = "";
        csv += match.DutyStartTime + ",";
        csv += (match.Arena != null ? MatchHelper.GetArenaName((RivalWingsMap)match.Arena) : "") + ",";
        csv += match.LocalPlayerTeamMember?.Job + ",";
        csv += match.LocalPlayerTeam + ",";
        csv += match.MatchDuration + ",";
        csv += match.IsWin + ",";
        csv += "\n";
        return csv;
    }
}
