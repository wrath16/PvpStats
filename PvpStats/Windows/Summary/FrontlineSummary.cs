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
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Average Performance:");
            ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: median team contribution per match.\n\n'Damage to Other' only counts Shatter matches.");
            DrawMatchStatsTable();
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

    private void DrawMatchStatsTable() {
        using(var table = ImRaii.Table($"MatchStatsTable", 7, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Kills");
                ImGui.TableSetupColumn($"Deaths");
                ImGui.TableSetupColumn($"Assists");
                ImGui.TableSetupColumn("Damage to PCs");
                ImGui.TableSetupColumn("Damage to Other");
                ImGui.TableSetupColumn($"Damage Taken");
                ImGui.TableSetupColumn($"HP Restored");

                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
                ImGui.TableHeader("Kills");
                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
                ImGui.TableHeader("Deaths");
                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
                ImGui.TableHeader("Assists");
                ImGui.TableNextColumn();
                ImGui.TableHeader("Damage\nto PCs");
                ImGui.TableNextColumn();
                ImGui.TableHeader("Damage\nto Other");
                ImGui.TableNextColumn();
                ImGui.TableHeader("Damage\nTaken");
                ImGui.TableNextColumn();
                ImGui.TableHeader("HP\nRestored");

                //per match
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0.5f, 10f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 0.5f, 5.0f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 5f, 35f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f, 1800000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f, 2000000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 300000f, 1500000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMatch.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f, 1750000f, Plugin.Configuration.ColorScaleStats, "#");

                //per min
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMin.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0.5f / 15, 10f / 15, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMin.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 0.5f / 15, 5.0f / 15, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMin.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 5f / 15, 35f / 15, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f / 15, 1800000f / 15, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f / 15, 2000000f / 15, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 300000f / 15, 1500000f / 15, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardPerMin.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f / 15, 1750000f / 15, Plugin.Configuration.ColorScaleStats, "#");

                //team contrib
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardContrib.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0 / 48f, 4 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardContrib.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 0 / 48f, 4 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardContrib.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0 / 48f, 4 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0 / 48f, 4 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0 / 48f, 4 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0 / 48f, 4 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.FLStatsEngine.LocalPlayerStats.ScoreboardContrib.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0 / 48f, 4 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
            }
        }
    }
}
