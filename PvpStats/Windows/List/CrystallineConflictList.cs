using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Windows.Detail;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        var color = isWin ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
        color = noWinner ? ImGuiColors.DalamudGrey : color;
        string resultText = isWin ? "WIN" : "LOSS";
        resultText = noWinner ? "UNKNOWN" : resultText;
        ImGui.TextColored(color, resultText);
        //ImGui.TableNextColumn();
        //ImGui.TableNextRow();
    }

    public override void RefreshDataModel() {
        DataModel = _plugin.StorageManager.GetCCMatches().Query().Where(m => !m.IsDeleted).OrderByDescending(m => m.DutyStartTime).ToList();
    }

    public override void OpenItemDetail(CrystallineConflictMatch item) {
        Task.Run(() => {
            _plugin.Log.Debug($"Opening item detail for...{item.DutyStartTime}");
            var itemDetail = new CrystallineConflictMatchDetail(_plugin, item);
            itemDetail.IsOpen = true;
            try {
                _plugin.WindowSystem.AddWindow(itemDetail);
            }
            catch {
                //attempt to open existing window
                var window = _plugin.WindowSystem.Windows.Where(w => w.WindowName == $"Match Details: {item.Id}").FirstOrDefault();
                if (window is not null) {
                    window.BringToFront();
                    window.IsOpen = true;
                }
            }
        });
    }

    public override void OpenFullEditDetail(CrystallineConflictMatch item) {
        var fullEditDetail = new FullEditDetail<CrystallineConflictMatch>(_plugin, item);
        fullEditDetail.IsOpen = true;
        try {
            _plugin.WindowSystem.AddWindow(fullEditDetail);
        }
        catch {
            ////attempt to open existing window
            //var window = _plugin.WindowSystem.Windows.Where(w => w.WindowName == $"Match Details: {item.Id}").FirstOrDefault();
            //if (window is not null) {
            //    window.BringToFront();
            //    window.IsOpen = true;
            //}
        }
    }
}
