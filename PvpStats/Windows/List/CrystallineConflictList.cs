using Dalamud.Interface.Colors;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Numerics;

namespace PvpStats.Windows.List;
internal class CrystallineConflictList : FilteredList<CrystallineConflictMatch> {

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Start Time", Flags = ImGuiTableColumnFlags.WidthStretch, Width = 95f },
        new ColumnParams{Name = "Arena", Flags = ImGuiTableColumnFlags.WidthStretch, Width = 100f },
        new ColumnParams{Name = "Queue", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 50f },
        new ColumnParams{Name = "Result", Flags = ImGuiTableColumnFlags.WidthFixed, Width = 40f },
    };
    //protected override List<ColumnParams> Columns { get; set; } = new() {
    //    new ColumnParams{Name = "time" },
    //    new ColumnParams{Name = "map" },
    //    new ColumnParams{Name = "queue" },
    //    new ColumnParams{Name = "result" },
    //};

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.SizingStretchProp;
    protected override ImGuiWindowFlags ChildFlags { get; set; } = ImGuiWindowFlags.AlwaysVerticalScrollbar;
    protected override bool ContextMenu { get; set; } = true;

    public CrystallineConflictList(Plugin plugin) : base(plugin) {
    }
    protected override void PreChildDraw() {
        ImGuiHelper.CSVButton(ListCSV);
    }

    public override void DrawListItem(CrystallineConflictMatch item) {
        if(item.IsBookmarked) {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(ImGuiColors.DalamudYellow - new Vector4(0f, 0f, 0f, 0.7f)));
        }
        ImGui.Text($"{item.DutyStartTime.ToString("MM/dd/yyyy HH:mm")}");
        ImGui.TableNextColumn();
        if(item.Arena != null) {
            ImGui.Text($"{MatchHelper.GetArenaName((CrystallineConflictMap)item.Arena)}");
        }
        ImGui.TableNextColumn();
        ImGui.Text($"{item.MatchType}");
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
        csv += match.MatchType + ",";
        csv += match.IsWin ? "WIN" : match.MatchWinner != null ? "LOSS" : "???" + ",";
        csv += "\n";
        return csv;
    }
}
