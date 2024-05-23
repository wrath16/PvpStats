using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PvpStats.Windows.List;
internal class CrystallineConflictMatchList : MatchList<CrystallineConflictMatch> {

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Start Time", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 125f },
        new ColumnParams{Name = "Arena", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 145f },
        new ColumnParams{Name = "Job", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f, Priority = 1 },
        new ColumnParams{Name = "Queue", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 50f },
        new ColumnParams{Name = "Duration", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f, Priority = 2 },
        new ColumnParams{Name = "Result", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f },
        new ColumnParams{Name = "RankAfter", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 125f, Priority = 3 },
    };

    public CrystallineConflictMatchList(Plugin plugin) : base(plugin, plugin.CCCache, plugin.CCStatsEngine.RefreshLock) {
    }

    public override void DrawListItem(CrystallineConflictMatch item) {
        ImGui.SameLine(0f * ImGuiHelpers.GlobalScale);
        if(item.IsBookmarked) {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(_plugin.Configuration.Colors.Favorite - new Vector4(0f, 0f, 0f, 0.7f)));
        }
        ImGui.Text($"{item.DutyStartTime:MM/dd/yyyy HH:mm}");
        ImGui.TableNextColumn();
        if(item.Arena != null) {
            ImGui.Text($"{MatchHelper.GetArenaName((CrystallineConflictMap)item.Arena)}");
        }
        ImGui.TableNextColumn();
        if(!item.IsSpectated) {
            var localPlayerJob = item.LocalPlayerTeamMember!.Job;
            ImGuiHelper.CenterAlignCursor(localPlayerJob.ToString() ?? "");
            ImGui.TextColored(_plugin.Configuration.GetJobColor(localPlayerJob), localPlayerJob.ToString());
        }
        ImGui.TableNextColumn();
        ImGui.Text($"{item.MatchType}");
        ImGui.TableNextColumn();
        ImGui.Text(ImGuiHelper.GetTimeSpanString(item.MatchDuration ?? TimeSpan.Zero));
        ImGui.TableNextColumn();
        bool noWinner = item.MatchWinner is null;
        bool isWin = item.MatchWinner == item.LocalPlayerTeam?.TeamName;
        bool isSpectated = item.LocalPlayerTeam is null;
        Vector4 color;
        string resultText;
        if(isSpectated) {
            color = ImGuiColors.DalamudWhite;
            resultText = "N/A";
        } else {
            color = isWin ? _plugin.Configuration.Colors.Win : _plugin.Configuration.Colors.Loss;
            color = noWinner ? _plugin.Configuration.Colors.Other : color;
            resultText = isWin ? "WIN" : "LOSS";
            resultText = noWinner ? "???" : resultText;
        }
        ImGuiHelper.CenterAlignCursor(resultText);
        ImGui.TextColored(color, resultText);
        ImGui.TableNextColumn();
        if(item.MatchType == CrystallineConflictMatchType.Ranked) {
            ImGui.Text(item.PostMatch?.RankAfter?.ToString() ?? "");
        }
        //ImGui.TableNextColumn();
        //ImGui.TableNextRow();
    }

    protected override string CSVRow(CrystallineConflictMatch match) {
        string csv = "";
        csv += match.DutyStartTime + ",";
        csv += (match.Arena != null ? MatchHelper.GetArenaName((CrystallineConflictMap)match.Arena) : "") + ",";
        csv += (!match.IsSpectated ? match.LocalPlayerTeamMember!.Job : "") + ",";
        csv += match.MatchType + ",";
        csv += match.MatchDuration + ",";
        csv += (match.IsWin ? "WIN" : match.MatchWinner != null ? "LOSS" : "???") + ",";
        csv += (match.MatchType == CrystallineConflictMatchType.Ranked && match.PostMatch != null ? match.PostMatch.RankAfter?.ToString() : "") + ",";
        csv += "\n";
        return csv;
    }

    //public override void OpenItemDetail(CrystallineConflictMatch item) {
    //    _plugin.DataQueue.QueueDataOperation(() => {
    //        _plugin.WindowManager.OpenMatchDetailsWindow(item);
    //    });
    //}
}
