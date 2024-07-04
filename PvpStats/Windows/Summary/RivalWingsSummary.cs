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
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Personal Mech Uptime:");
                DrawMechTable();
                ImGui.TableNextColumn();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Objective Win Rate:");
                DrawMidMercTable();
            }
            if(Plugin.RWStatsEngine.LocalPlayerJobResults.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Jobs Played:");
                ImGuiHelper.HelpMarker("Job is determined by the post-match scoreboard.");
                DrawJobTable(Plugin.RWStatsEngine.LocalPlayerJobResults.OrderByDescending(x => x.Value.Matches).ToDictionary(), 0);
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
            ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 148f);
            ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell("Matches: ", -10f);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell($"{Plugin.RWStatsEngine.OverallResults.Matches:N0}");
            ImGui.TableNextColumn();

            if(Plugin.RWStatsEngine.OverallResults.Matches > 0) {
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Wins: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Plugin.RWStatsEngine.OverallResults.Wins:N0}");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(Plugin.RWStatsEngine.OverallResults.WinRate.ToString("P2"));

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Losses: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Plugin.RWStatsEngine.OverallResults.Losses:N0}");
                ImGui.TableNextColumn();

                if(Plugin.RWStatsEngine.OverallResults.OtherResult > 0) {
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell("Other: ", -10f);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{Plugin.RWStatsEngine.OverallResults.OtherResult:N0}");
                    ImGui.TableNextColumn();
                }

                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Average match length: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(Plugin.RWStatsEngine.AverageMatchDuration));
                ImGui.TableNextColumn();
            }
        }
    }

    private void DrawJobTable(Dictionary<Job, CCAggregateStats> jobStats, int id) {
        using var table = ImRaii.Table($"JobTable###{id}", 5, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            float offset = -1f;
            ImGui.TableSetupColumn("Job");
            ImGui.TableSetupColumn($"Role", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);
            ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);
            ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);
            ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);

            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Job", 0, false, false, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Role", 0, true, false, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Matches", 2, true, false, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Wins", 2, true, false, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Win Rate", 2, true, false, offset);
            foreach(var job in jobStats) {
                ImGui.TableNextColumn();
                ImGui.Text($"{PlayerJobHelper.GetNameFromJob(job.Key)}");

                ImGui.TableNextColumn();
                var roleString = PlayerJobHelper.GetSubRoleFromJob(job.Key).ToString() ?? "";
                //ImGuiHelper.CenterAlignCursor(roleString);
                ImGui.TextColored(Plugin.Configuration.GetJobColor(job.Key), roleString);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(job.Value.Matches.ToString(), offset);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(job.Value.Wins.ToString(), offset);

                ImGui.TableNextColumn();
                if(job.Value.Matches > 0) {
                    var diffColor = job.Value.WinRate > 0.5f ? Plugin.Configuration.Colors.Win : job.Value.WinRate < 0.5f ? Plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                    ImGuiHelper.DrawNumericCell(job.Value.WinRate.ToString("P2"), offset, diffColor);
                }
            }
        }
    }

    private void DrawMechTable() {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using var table = ImRaii.Table($"MechTimeTable", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            ImGui.TableSetupColumn("mech", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);
            ImGui.TableSetupColumn($"uptime", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);

            //ImGui.TableNextColumn();
            //ImGui.TableHeader("");
            //ImGui.TableNextColumn();
            //ImGui.TableHeader("");

            var size = new Vector2(30f * ImGuiHelpers.GlobalScale, 30f * ImGuiHelpers.GlobalScale);
            var uv0 = new Vector2(0.1f);
            var uv1 = new Vector2(0.9f);

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.ChaserIcons[RivalWingsTeamName.Unknown]), size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Cruise Chaser");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGuiHelper.DrawNumericCell(Plugin.RWStatsEngine.LocalPlayerMechTime[RivalWingsMech.Chaser].ToString("P2"), -1f);

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.OppressorIcons[RivalWingsTeamName.Unknown]), size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Oppressor");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGuiHelper.DrawNumericCell(Plugin.RWStatsEngine.LocalPlayerMechTime[RivalWingsMech.Oppressor].ToString("P2"), -1f);

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.JusticeIcons[RivalWingsTeamName.Unknown]), size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Brute Justice");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGuiHelper.DrawNumericCell(Plugin.RWStatsEngine.LocalPlayerMechTime[RivalWingsMech.Justice].ToString("P2"), -1f);
        }
    }

    private void DrawMidMercTable() {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using var table = ImRaii.Table($"MidMercTable", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);
            ImGui.TableSetupColumn($"winrate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);

            var size = new Vector2(30f * ImGuiHelpers.GlobalScale, 30f * ImGuiHelpers.GlobalScale);
            var uv0 = new Vector2(0.15f);
            var uv1 = new Vector2(0.85f);

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.GoblinMercIcon), size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Mercenaries");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGuiHelper.DrawNumericCell(Plugin.RWStatsEngine.LocalPlayerMercWinRate.ToString("P2"), -1f);

            ImGui.TableNextColumn();
            uv0 = new Vector2(0.1f);
            uv1 = new Vector2(0.9f);
            ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.TrainIcon), size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Supplies");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGuiHelper.DrawNumericCell(Plugin.RWStatsEngine.LocalPlayerMidWinRate.ToString("P2"), -1f);
        }
    }

    private void DrawMatchStatsTable() {
        using(var table = ImRaii.Table($"MatchStatsTable", 8, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                float offset = -1f;
                ImGui.TableSetupColumn("Kills");
                ImGui.TableSetupColumn($"Deaths");
                ImGui.TableSetupColumn($"Assists");
                ImGui.TableSetupColumn("Damage to PCs");
                ImGui.TableSetupColumn("Damage to Other");
                ImGui.TableSetupColumn($"Damage Taken");
                ImGui.TableSetupColumn($"HP Restored");
                ImGui.TableSetupColumn("Ceruleum");

                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Kills", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Deaths", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Assists", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Damage\nto PCs", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Damage\nto Other", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Damage\nTaken", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("HP\nRestored", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Ceru-\nleum", 2, true, true, offset);

                //per match
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 2.0f, 8.0f, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 2.0f, 5.0f, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 10f, 25f, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 300000f, 2000000f, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f, 3000000f, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 400000f, 1500000f, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 100000f, 1500000f, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 10f, 100f, Plugin.Configuration.ColorScaleStats, "#", offset);

                //per min
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 0.2f, 0.8f, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 0.2f, 0.5f, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1.0f, 2.5f, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 30000f, 200000f, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 10000f, 300000f, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 40000f, 150000f, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 10000f, 150000f, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardPerMin.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1f, 10f, Plugin.Configuration.ColorScaleStats, "0.00", offset);

                //team contrib
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Plugin.RWStatsEngine.LocalPlayerStats.ScoreboardContrib.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, 1 / 48f, 3 / 48f, Plugin.Configuration.ColorScaleStats, "P1", offset);
            }
        }
    }
}
