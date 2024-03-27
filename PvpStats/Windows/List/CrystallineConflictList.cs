using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PvpStats.Windows.List;
internal class CrystallineConflictList : FilteredList<CrystallineConflictMatch> {

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Start Time", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 125f },
        new ColumnParams{Name = "Arena", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 145f },
        new ColumnParams{Name = "Job", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f, Priority = 1 },
        new ColumnParams{Name = "Queue", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 50f },
        new ColumnParams{Name = "Duration", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f, Priority = 2 },
        new ColumnParams{Name = "Result", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f },
        new ColumnParams{Name = "RankAfter", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 125f, Priority = 3 },
    };
    //protected override List<ColumnParams> Columns { get; set; } = new() {
    //    new ColumnParams{Name = "time" },
    //    new ColumnParams{Name = "map" },
    //    new ColumnParams{Name = "queue" },
    //    new ColumnParams{Name = "result" },
    //};

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Hideable;
    protected override ImGuiWindowFlags ChildFlags { get; set; } = ImGuiWindowFlags.AlwaysVerticalScrollbar;
    protected override bool ContextMenu { get; set; } = true;
    protected override bool DynamicColumns { get; set; } = true;

    public CrystallineConflictList(Plugin plugin) : base(plugin) {
    }
    protected override void PreChildDraw() {
        ImGuiHelper.CSVButton(ListCSV);
        ImGui.SameLine();
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Ban.ToIconString()}##CloseAllMatches")) {
                _plugin.DataQueue.QueueDataOperation(_plugin.WindowManager.CloseAllMatchWindows);
            }
        }
        ImGuiHelper.WrappedTooltip("Close all open match windows");
    }

    public override void DrawListItem(CrystallineConflictMatch item) {
        if(item.IsBookmarked) {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(ImGuiColors.DalamudYellow - new Vector4(0f, 0f, 0f, 0.7f)));
        }
        ImGui.Text($"{item.DutyStartTime:MM/dd/yyyy HH:mm}");
        ImGui.TableNextColumn();
        if(item.Arena != null) {
            ImGui.Text($"{MatchHelper.GetArenaName((CrystallineConflictMap)item.Arena)}");
        }
        ImGui.TableNextColumn();
        if(!item.IsSpectated) {
            ImGui.TextColored(ImGuiHelper.GetJobColor(item.LocalPlayerTeamMember!.Job), item.LocalPlayerTeamMember.Job.ToString());
        }
        ImGui.TableNextColumn();
        ImGui.Text($"{item.MatchType}");
        ImGui.TableNextColumn();
        ImGui.Text(ImGuiHelper.GetTimeSpanString(item.MatchDuration ?? TimeSpan.Zero));
        ImGui.TableNextColumn();
        bool noWinner = item.MatchWinner is null;
        bool isWin = item.MatchWinner == item.LocalPlayerTeam?.TeamName;
        bool isSpectated = item.LocalPlayerTeam is null;
        var color = ImGuiColors.DalamudWhite;
        string resultText = "";
        if(isSpectated) {
            color = ImGuiColors.DalamudWhite;
            resultText = "N/A";
        } else {
            color = isWin ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
            color = noWinner ? ImGuiColors.DalamudGrey : color;
            resultText = isWin ? "WIN" : "LOSS";
            resultText = noWinner ? "???" : resultText;
        }
        ImGui.TextColored(color, resultText);
        ImGui.TableNextColumn();
        if(item.MatchType == CrystallineConflictMatchType.Ranked && item.PostMatch != null) {
            ImGui.Text(item.PostMatch.RankAfter.ToString());
        }
        //ImGui.TableNextColumn();
        //ImGui.TableNextRow();
    }

    public override void RefreshDataModel() {
        //#if DEBUG
        //        DataModel = _plugin.Storage.GetCCMatches().Query().Where(m => !m.IsDeleted).OrderByDescending(m => m.DutyStartTime).ToList();
        //#else
        //        DataModel = _plugin.Storage.GetCCMatches().Query().Where(m => !m.IsDeleted && m.IsCompleted).OrderByDescending(m => m.DutyStartTime).ToList();
        //#endif
        foreach(var match in DataModel) {
            ListCSV += CSVRow(match);
        }
    }

    public override void OpenItemDetail(CrystallineConflictMatch item) {
        _plugin.DataQueue.QueueDataOperation(() => {
            _plugin.WindowManager.OpenMatchDetailsWindow(item);
        });
    }

    public override void OpenFullEditDetail(CrystallineConflictMatch item) {
        _plugin.DataQueue.QueueDataOperation(() => {
            _plugin.WindowManager.OpenFullEditWindow(item);
        });
    }

    protected override void ContextMenuItems(CrystallineConflictMatch item) {
        bool isBookmarked = item.IsBookmarked;
        if(ImGui.MenuItem($"Favorite##{item!.GetHashCode()}--AddBookmark", null, isBookmarked)) {
            item.IsBookmarked = !item.IsBookmarked;
            _plugin.DataQueue.QueueDataOperation(() => {
                _plugin.Storage.UpdateCCMatch(item, false);
            });
        }

#if DEBUG
        if(ImGui.MenuItem($"Edit document##{item!.GetHashCode()}--FullEditContext")) {
            OpenFullEditDetail(item);
        }
#endif
    }

    private string CSVRow(CrystallineConflictMatch match) {
        string csv = "";
        csv += match.DutyStartTime + ",";
        csv += (match.Arena != null ? MatchHelper.GetArenaName((CrystallineConflictMap)match.Arena) : "") + ",";
        csv += (!match.IsSpectated ? match.LocalPlayerTeamMember.Job : "") + ",";
        csv += match.MatchType + ",";
        csv += match.MatchDuration + ",";
        csv += (match.IsWin ? "WIN" : match.MatchWinner != null ? "LOSS" : "???") + ",";
        csv += (match.MatchType == CrystallineConflictMatchType.Ranked && match.PostMatch != null ? match.PostMatch.RankAfter?.ToString() : "") + ",";
        csv += "\n";
        return csv;
    }
}
