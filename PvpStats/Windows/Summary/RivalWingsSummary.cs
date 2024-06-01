using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PvpStats.Windows.Summary;
internal class RivalWingsSummary {
    private readonly Plugin Plugin;

    public RivalWingsSummary(Plugin plugin) {
        Plugin = plugin;
    }

    public void Draw() {
        if(Plugin.RWStatsEngine.OverallResults.Matches > 0) {
            DrawOverallResultsTable();
            ImGui.Separator();
            using(var table = ImRaii.Table("MidMercMechColumns", 2, ImGuiTableFlags.None)) {
                ImGui.TableSetupColumn("c1");
                ImGui.TableSetupColumn("c2");

                ImGui.TableNextColumn();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Mech Uptime:");
                DrawMechTable();
                ImGui.TableNextColumn();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Objective Win Rate:");
                DrawMidMercTable();
            }
            if(Plugin.RWStatsEngine.LocalPlayerJobResults.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Jobs Played:");
                ImGuiHelper.HelpMarker("Job is determined by end-game scoreboard.");
                DrawJobTable(Plugin.RWStatsEngine.LocalPlayerJobResults.OrderByDescending(x => x.Value.Matches).ToDictionary());
            }
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Average Performance:");
            ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: median team contribution per match.");
            DrawMatchStatsTable();
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
            ImGui.Text($"{Plugin.RWStatsEngine.OverallResults.Matches:N0}");
            ImGui.TableNextColumn();

            if(Plugin.RWStatsEngine.OverallResults.Matches > 0) {
                ImGui.TableNextColumn();
                ImGui.Text("Wins: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{Plugin.RWStatsEngine.OverallResults.Wins:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{string.Format("{0:P}%", Plugin.RWStatsEngine.OverallResults.WinRate)}");

                ImGui.TableNextColumn();
                ImGui.Text("Losses: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{Plugin.RWStatsEngine.OverallResults.Losses:N0}");
                ImGui.TableNextColumn();

                if(Plugin.RWStatsEngine.OverallResults.OtherResult > 0) {
                    ImGui.TableNextColumn();
                    ImGui.Text("Other: ");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{Plugin.RWStatsEngine.OverallResults.OtherResult:N0}");
                    ImGui.TableNextColumn();
                }

                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Average match length: ");
                ImGui.TableNextColumn();
                ImGui.Text(ImGuiHelper.GetTimeSpanString(Plugin.RWStatsEngine.AverageMatchDuration));
                ImGui.TableNextColumn();
            }
        }
    }

    private void DrawJobTable(Dictionary<Job, CCAggregateStats> jobStats) {
        using var table = ImRaii.Table($"JobTable###{jobStats.GetHashCode()}", 5, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            ImGui.TableSetupColumn("Job");
            ImGui.TableSetupColumn($"Role", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            ImGui.TableNextColumn();
            ImGui.TableHeader("");
            ImGui.TableNextColumn();
            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Role");
            ImGui.TableNextColumn();
            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Matches");
            ImGui.TableNextColumn();
            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Wins");
            ImGui.TableNextColumn();
            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("Win Rate");
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
                    var diffColor = job.Value.WinRate > 0.5f ? Plugin.Configuration.Colors.Win : job.Value.WinRate < 0.5f ? Plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                    ImGui.TextColored(diffColor, $"{string.Format("{0:P}%", job.Value.WinRate)}");
                }
            }
        }
    }

    private void DrawMechTable() {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using var table = ImRaii.Table($"MechTimeTable", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            ImGui.TableSetupColumn("mech", ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 40f);
            ImGui.TableSetupColumn($"uptime", ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 40f);

            //ImGui.TableNextColumn();
            //ImGui.TableHeader("");
            //ImGui.TableNextColumn();
            //ImGui.TableHeader("");

            var size = new Vector2(30f * ImGuiHelpers.GlobalScale, 30f * ImGuiHelpers.GlobalScale);
            var uv0 = new Vector2(0.1f);
            var uv1 = new Vector2(0.9f);

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.ChaserIcons[RivalWingsTeamName.Unknown]?.ImGuiHandle ?? Plugin.WindowManager.Icon0.ImGuiHandle, size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Cruise Chaser");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{string.Format("{0:P}%", Plugin.RWStatsEngine.LocalPlayerMechTime[RivalWingsMech.Chaser])}");

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.OppressorIcons[RivalWingsTeamName.Unknown]?.ImGuiHandle ?? Plugin.WindowManager.Icon0.ImGuiHandle, size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Oppressor");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{string.Format("{0:P}%", Plugin.RWStatsEngine.LocalPlayerMechTime[RivalWingsMech.Oppressor])}");

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.JusticeIcons[RivalWingsTeamName.Unknown]?.ImGuiHandle ?? Plugin.WindowManager.Icon0.ImGuiHandle, size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Brute Justice");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{string.Format("{0:P}%", Plugin.RWStatsEngine.LocalPlayerMechTime[RivalWingsMech.Justice])}");
        }
    }

    private void DrawMidMercTable() {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using var table = ImRaii.Table($"MidMercTable", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 40f);
            ImGui.TableSetupColumn($"winrate", ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 40f);

            var size = new Vector2(30f * ImGuiHelpers.GlobalScale, 30f * ImGuiHelpers.GlobalScale);
            var uv0 = new Vector2(0.15f);
            var uv1 = new Vector2(0.85f);

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.GoblinMercIcon?.ImGuiHandle ?? Plugin.WindowManager.Icon0.ImGuiHandle, size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Mercenaries");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{string.Format("{0:P}%", Plugin.RWStatsEngine.LocalPlayerMercWinRate)}");

            ImGui.TableNextColumn();
            uv0 = new Vector2(0.1f);
            uv1 = new Vector2(0.9f);
            ImGui.Image(Plugin.WindowManager.TrainIcon?.ImGuiHandle ?? Plugin.WindowManager.Icon0.ImGuiHandle, size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Supplies");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{string.Format("{0:P}%", Plugin.RWStatsEngine.LocalPlayerMidWinRate)}");
        }
    }

    //private void DrawMechRow(RivalWingsMech mech) {
    //    var size = new Vector2(30f * ImGuiHelpers.GlobalScale, 30f * ImGuiHelpers.GlobalScale);
    //    var uv0 = new Vector2(0.1f);
    //    var uv1 = new Vector2(0.9f);

    //    ImGui.TableNextColumn();
    //    ImGui.Image(Plugin.WindowManager.ChaserIcons[RivalWingsTeamName.Unknown]?.ImGuiHandle ?? Plugin.WindowManager.Icon0.ImGuiHandle, size, uv0, uv1);
    //    ImGui.TableNextColumn();
    //    ImGui
    //}

    private void DrawMatchStatsTable() {
        using(var table = ImRaii.Table($"MatchStatsTable", 8, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Kills");
                ImGui.TableSetupColumn($"Deaths");
                ImGui.TableSetupColumn($"Assists");
                ImGui.TableSetupColumn("Damage to PCs");
                ImGui.TableSetupColumn("Damage to Other");
                ImGui.TableSetupColumn($"Damage Taken");
                ImGui.TableSetupColumn($"HP Restored");
                ImGui.TableSetupColumn("Ceruleum");

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
                ImGui.TableNextColumn();
                ImGui.TableHeader("Ceru-\nleum");

                //per match
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 2.0f, 8.0f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 2.0f, 5.0f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 10f, 25f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 300000f, 2000000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f, 3000000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 400000f, 1500000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f, 1500000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 10f, 100f, Plugin.Configuration.ColorScaleStats, "#");

                //per min
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0.2f, 0.8f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 0.2f, 0.5f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1.0f, 2.5f, Plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 30000f, 200000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 10000f, 300000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 40000f, 150000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 10000f, 150000f, Plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1f, 10f, Plugin.Configuration.ColorScaleStats, "0.00");

                //team contrib
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
            }
        }
    }
}
