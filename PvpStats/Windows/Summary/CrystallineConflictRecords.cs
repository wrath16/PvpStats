using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System.Linq;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictRecords {

    private Plugin _plugin;

    internal CrystallineConflictRecords(Plugin plugin) {
        _plugin = plugin;
    }

    public void Draw() {
        if(!_plugin.CCStatsEngine.RefreshLock.Wait(0)) {
            return;
        }
        try {
            using(var table = ImRaii.Table("streaks", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
                if(table) {
                    ImGui.TableSetupColumn("title", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 185f);
                    ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f);

                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, "Longest win streak:");
                    ImGui.TableNextColumn();
                    ImGui.Text(_plugin.CCStatsEngine.LongestWinStreak.ToString());
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, "Longest loss streak:");
                    ImGui.TableNextColumn();
                    ImGui.Text(_plugin.CCStatsEngine.LongestLossStreak.ToString());
                }
            }
            ImGui.Separator();
            foreach(var match in _plugin.CCStatsEngine.Superlatives) {
                var x = match.Value;
                DrawStat(match.Key, match.Value.Select(x => x.Item1).ToArray(), match.Value.Select(x => x.Item2).ToArray());
                ImGui.Separator();
            }
        } finally {
            _plugin.CCStatsEngine.RefreshLock.Release();
        }
    }

    private void DrawStat(CrystallineConflictMatch match, string[] superlatives, string[] values) {
        using(var table = ImRaii.Table("headertable", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("title", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 185f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f);
                ImGui.TableSetupColumn($"examine", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

                for(int i = 0; i < superlatives.Length; i++) {
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, superlatives[i] + ":");
                    ImGui.TableNextColumn();
                    ImGui.Text(values[i]);
                    ImGui.TableNextColumn();
                    if(i == superlatives.Length - 1) {
                        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
                            if(ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}##{match.GetHashCode()}--ViewDetails")) {
                                _plugin.WindowManager.OpenMatchDetailsWindow(match);
                            }
                        }
                    }
                }
            }
        }

        using(var table = ImRaii.Table("match", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 100f);
                ImGui.TableSetupColumn("Arena", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 150f);
                ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);
                ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);

                ImGui.TableNextColumn();
                ImGui.Text($"{match.DutyStartTime:MM/dd/yyyy HH:mm}");
                ImGui.TableNextColumn();
                if(match.Arena != null) {
                    ImGui.Text(MatchHelper.GetArenaName((CrystallineConflictMap)match.Arena));
                }
                ImGui.TableNextColumn();
                if(!match.IsSpectated) {
                    ImGui.TextColored(ImGuiHelper.GetJobColor(match.LocalPlayerTeamMember!.Job), $"{match.LocalPlayerTeamMember!.Job}");
                    ImGui.TableNextColumn();
                    var color = match.IsWin ? ImGuiColors.HealerGreen : match.IsLoss ? ImGuiColors.DalamudRed : ImGuiColors.DalamudGrey;
                    var result = match.IsWin ? "WIN" : match.IsLoss ? "LOSS" : "???";
                    ImGui.TextColored(color, result);
                } else {
                    ImGui.Text($"Spectated");
                }
            }
        }
    }
}
