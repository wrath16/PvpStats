using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictSummary {

    private Plugin _plugin;

    private int _totalMatches;
    private int _totalWins;
    private int _totalLosses;
    private int _totalOther;

    public CrystallineConflictSummary(Plugin plugin) {
        _plugin = plugin;
    }

    public void Refresh(List<CrystallineConflictMatch> matches) {
        _totalMatches = matches.Count;
        _totalWins = matches.Where(x => x.LocalPlayerTeam != null&& x.MatchWinner != null && x.MatchWinner == x.LocalPlayerTeam.TeamName).Count();
        _totalLosses = matches.Where(x => x.LocalPlayerTeam != null && x.MatchWinner != null && x.MatchWinner != x.LocalPlayerTeam.TeamName).Count();
        _totalOther = _totalMatches - _totalWins - _totalLosses;
    }

    public void Draw() {
        if (ImGui.BeginTable($"StatsSummary", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoKeepColumnsVisible)) {
            ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
            ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            
            ImGui.TableNextColumn();
            ImGui.Text("Matches: ");
            ImGui.TableNextColumn();
            ImGui.Text($"{_totalMatches.ToString("N0")}");
            ImGui.TableNextColumn();

            if(_totalMatches > 0) {
                ImGui.TableNextColumn();
                ImGui.Text("Wins: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{_totalWins.ToString("N0")}");
                ImGui.TableNextColumn();
                ImGui.Text($"{string.Format("{0:P}%", (double)_totalWins / _totalMatches)}");

                ImGui.TableNextColumn();
                ImGui.Text("Losses: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{_totalLosses.ToString("N0")}");
                ImGui.TableNextColumn();

                if(_totalOther > 0) {
                    ImGui.TableNextColumn();
                    ImGui.Text("Other: ");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{_totalOther.ToString("N0")}");
                    ImGui.TableNextColumn();
                }

            }

            ImGui.EndTable();
        }
    }
}
