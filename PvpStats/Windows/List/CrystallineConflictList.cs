using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Windows.Detail;
using System;
using System.Collections.Generic;

namespace PvpStats.Windows.List;
internal class CrystallineConflictList : FilteredList<CrystallineConflictMatch> {

    private CrystallineConflictMatchDetail _detail;

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "time", Flags = ImGuiTableColumnFlags.WidthStretch, Width = ImGuiHelpers.GlobalScale * 100f },
        new ColumnParams{Name = "map", Flags = ImGuiTableColumnFlags.WidthStretch, Width = ImGuiHelpers.GlobalScale * 100f },
        new ColumnParams{Name = "queue", Flags = ImGuiTableColumnFlags.WidthFixed, Width = ImGuiHelpers.GlobalScale * 60f },
        new ColumnParams{Name = "result", Flags = ImGuiTableColumnFlags.WidthFixed, Width = ImGuiHelpers.GlobalScale * 40f },
    };

    public CrystallineConflictList(Plugin plugin) : base(plugin) {
    }

    public override void DrawListItem(CrystallineConflictMatch item) {
        //ImGui.TableNextColumn();
        //if (ImGui.Selectable($"{item.DutyStartTime}###{item.Id}", false, ImGuiSelectableFlags.SpanAllColumns)) {
        //    //select stuff
        //    //SelectedRow = item;
        //    _plugin.Log.Debug($"{item.DutyStartTime.ToString()} selected!");
        //}
        //ImGui.TableNextColumn();
        ImGui.Text($"{item.DutyStartTime}");
        ImGui.TableNextColumn();
        ImGui.Text($"{MatchHelper.GetArenaName(item.Arena)}");
        ImGui.TableNextColumn();
        ImGui.Text($"{item.MatchType}");
        ImGui.TableNextColumn();
        bool noWinner = item.MatchWinner is null;
        bool isWin = item.MatchWinner == item.LocalPlayerTeam?.TeamName;
        bool isSpectated = item.LocalPlayerTeam is null;
        var color = ImGuiColors.DalamudWhite;
        string resultText = "";
        if (isSpectated) {
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

    }

    public override void OpenItemDetail(CrystallineConflictMatch item) {
        _plugin.DataQueue.QueueDataOperation(() => {
            _plugin.Log.Debug($"Opening item detail for...{item.DutyStartTime}");
            var itemDetail = new CrystallineConflictMatchDetail(_plugin, item);
            itemDetail.IsOpen = true;
            try {
                _plugin.WindowManager.AddWindow(itemDetail);
            }
            catch (ArgumentException) {
                //attempt to open existing window
                _plugin.WindowManager.OpenMatchDetailsWindow(item.Id);
            }
        });
    }

    public override void OpenFullEditDetail(CrystallineConflictMatch item) {
        _plugin.DataQueue.QueueDataOperation(() => {
            _plugin.Log.Debug($"Opening full edit details for...{item.Id}");
            var fullEditDetail = new FullEditDetail<CrystallineConflictMatch>(_plugin, item);
            fullEditDetail.IsOpen = true;
            try {
                _plugin.WindowManager.AddWindow(fullEditDetail);
            }
            catch (ArgumentException) {
                //attempt to open existing window
                _plugin.WindowManager.OpenFullEditWindow(item.Id);
            }
        });
    }
}
