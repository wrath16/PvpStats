using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Player;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Windows.Summary;
internal class FrontlineSummary {

    private readonly Plugin Plugin;

    public FrontlineSummary(Plugin plugin) {
        Plugin = plugin;
    }

    public void Draw() {
        if(Plugin.FLStatsEngine.OverallResults.Matches > 0) {
            DrawOverallResultsTable();
            if(Plugin.FLStatsEngine.LocalPlayerJobResults.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Jobs Played:");
                DrawJobTable(Plugin.FLStatsEngine.LocalPlayerJobResults.OrderByDescending(x => x.Value.Matches).ToDictionary());
            }
            if(Plugin.FLStatsEngine.MapResults.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Maps:");
                DrawMapResultsTable();
            }
        } else {
            ImGui.TextDisabled("No matches for given filters.");
        }
    }

    private void DrawOverallResultsTable() {
        using var table = ImRaii.Table($"OverallResults", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
            ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

            ImGui.TableNextColumn();
            ImGui.Text("Matches: ");
            ImGui.TableNextColumn();
            ImGui.Text($"{Plugin.FLStatsEngine.OverallResults.Matches:N0}");
            ImGui.TableNextColumn();

            if(Plugin.FLStatsEngine.OverallResults.Matches > 0) {
                ImGui.TableNextColumn();
                ImGui.Text("First places: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{Plugin.FLStatsEngine.OverallResults.FirstPlaces:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{string.Format("{0:P}%", Plugin.FLStatsEngine.OverallResults.FirstRate)}");

                ImGui.TableNextColumn();
                ImGui.Text("Second places: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{Plugin.FLStatsEngine.OverallResults.SecondPlaces:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{string.Format("{0:P}%", Plugin.FLStatsEngine.OverallResults.SecondRate)}");

                ImGui.TableNextColumn();
                ImGui.Text("Third places: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{Plugin.FLStatsEngine.OverallResults.ThirdPlaces:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{string.Format("{0:P}%", Plugin.FLStatsEngine.OverallResults.ThirdRate)}");

                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Average place: ");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.OverallResults.AveragePlace, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 1.5f, 2.5f, Plugin.Configuration.ColorScaleStats, "0.00");
                //ImGui.Text(string.Format("{0:0.00}", Plugin.FLStatsEngine.OverallResults.AveragePlace));
                ImGui.TableNextColumn();

                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Average match length: ");
                ImGui.TableNextColumn();
                ImGui.Text(ImGuiHelper.GetTimeSpanString(Plugin.FLStatsEngine.AverageMatchDuration));
                ImGui.TableNextColumn();
            }
        }
    }

    private void DrawMapResultsTable() {
        using var table = ImRaii.Table($"MapTable", 5, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            ImGui.TableSetupColumn("");
            ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Avg.\nPlace", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            ImGui.TableNextColumn();
            ImGui.TableHeader("");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Matches");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Wins");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Win Rate");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Average\nPlace");
            foreach(var map in Plugin.FLStatsEngine.MapResults.OrderByDescending(x => x.Value.Matches).ToDictionary()) {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(MatchHelper.GetFrontlineArenaName(map.Key));

                ImGui.TableNextColumn();
                ImGui.Text($"{map.Value.Matches}");

                ImGui.TableNextColumn();
                ImGui.Text($"{map.Value.Wins}");

                ImGui.TableNextColumn();
                if(map.Value.Matches > 0) {
                    var diffColor = map.Value.WinRate > 1 / 3f ? Plugin.Configuration.Colors.Win : map.Value.WinRate < 1 / 3f ? Plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                    ImGui.TextColored(diffColor, $"{string.Format("{0:P}%", map.Value.WinRate)}");
                }

                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)map.Value.AveragePlace, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 1.5f, 2.5f, Plugin.Configuration.ColorScaleStats, "0.00");
            }
        }
    }

    private void DrawJobTable(Dictionary<Job, FLAggregateStats> jobStats) {
        using var table = ImRaii.Table($"JobTable###{jobStats.GetHashCode()}", 6, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            ImGui.TableSetupColumn("");
            ImGui.TableSetupColumn($"Role", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Avg.\nPlace", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            ImGui.TableNextColumn();
            ImGui.TableHeader("");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Role");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Matches");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Wins");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Win Rate");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Average\nPlace");
            foreach(var job in jobStats) {
                ImGui.TableNextColumn();
                ImGui.Text($"{PlayerJobHelper.GetNameFromJob(job.Key)}");

                ImGui.TableNextColumn();
                ImGui.TextColored(Plugin.Configuration.GetJobColor(job.Key), $"{PlayerJobHelper.GetSubRoleFromJob(job.Key)}");

                ImGui.TableNextColumn();
                ImGui.Text($"{job.Value.Matches}");

                ImGui.TableNextColumn();
                ImGui.Text($"{job.Value.Wins}");

                ImGui.TableNextColumn();
                if(job.Value.Matches > 0) {
                    var diffColor = job.Value.WinRate > 1 / 3f ? Plugin.Configuration.Colors.Win : job.Value.WinRate < 1 / 3f ? Plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                    ImGui.TextColored(diffColor, $"{string.Format("{0:P}%", job.Value.WinRate)}");
                }

                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)job.Value.AveragePlace, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 1.5f, 2.5f, Plugin.Configuration.ColorScaleStats, "0.00");
            }
        }
    }
}
