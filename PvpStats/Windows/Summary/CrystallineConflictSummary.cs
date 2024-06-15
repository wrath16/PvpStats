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
using System.Threading;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictSummary {

    private readonly Plugin _plugin;
    internal protected SemaphoreSlim RefreshLock { get; private set; } = new SemaphoreSlim(1);

    public CrystallineConflictSummary(Plugin plugin) {
        _plugin = plugin;
    }

    public void Draw() {
        if(!RefreshLock.Wait(0)) {
            return;
        }
        try {
            if(_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.Matches > 0) {
                DrawResultTable();
            } else {
                ImGui.TextDisabled("No matches for given filters.");
            }

            if(_plugin.CCStatsEngine.LocalPlayerJobStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Jobs Played:");
                DrawJobTable(_plugin.CCStatsEngine.LocalPlayerJobStats);
            }

            if(_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.Matches > 0) {
                ImGui.Separator();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Average Performance:");
                ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: median team contribution per match.");
                DrawMatchStatsTable();
            }

            if(_plugin.CCStatsEngine.ArenaStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Arenas:");
                DrawArenaTable(_plugin.CCStatsEngine.ArenaStats);
            }

            if(_plugin.CCStatsEngine.TeammateJobStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Teammates' Jobs Played:");
                DrawJobTable(_plugin.CCStatsEngine.TeammateJobStats);
            }

            if(_plugin.CCStatsEngine.OpponentJobStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Opponents' Jobs Played:");
                DrawJobTable(_plugin.CCStatsEngine.OpponentJobStats);
            }

            if(_plugin.CCStatsEngine.TeammateStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Top Teammates:");
                DrawPlayerTable(_plugin.CCStatsEngine.TeammateStats);
            }

            if(_plugin.CCStatsEngine.OpponentStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Top Opponents:");
                DrawPlayerTable(_plugin.CCStatsEngine.OpponentStats);
            }
        } finally {
            RefreshLock.Release();
        }
    }

    private void DrawResultTable() {
        using(var table = ImRaii.Table($"StatsSummary", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 148f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
                ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Matches: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.Matches:N0}");
                ImGui.TableNextColumn();

                if(_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.Matches > 0) {
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell("Wins: ", -10f);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.Wins:N0}");
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.WinRate.ToString("P2"));

                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell("Losses: ", -10f);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.Losses:N0}");
                    ImGui.TableNextColumn();

                    if(_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.OtherResult > 0) {
                        ImGui.TableNextColumn();
                        ImGuiHelper.DrawNumericCell("Other: ", -10f);
                        ImGui.TableNextColumn();
                        ImGuiHelper.DrawNumericCell($"{_plugin.CCStatsEngine.LocalPlayerStats.StatsAll.OtherResult:N0}");
                        ImGui.TableNextColumn();
                    }
                    ImGui.TableNextRow();
                    ImGui.TableNextRow();
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell("Average match length: ", -10f);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(_plugin.CCStatsEngine.AverageMatchDuration));
                    ImGui.TableNextColumn();
                }
            }
        }
    }

    private void DrawArenaTable(Dictionary<CrystallineConflictMap, CCAggregateStats> arenaStats) {
        using(var table = ImRaii.Table($"ArenaTable", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                float offset = -1f;
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Name", 0, false, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Matches", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Wins", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Win Rate", 2, true, true, offset);
                foreach(var arena in arenaStats) {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{MatchHelper.GetArenaName(arena.Key)}");

                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(arena.Value.Matches.ToString(), offset);

                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(arena.Value.Wins.ToString(), offset);

                    ImGui.TableNextColumn();
                    if(arena.Value.Matches > 0) {
                        var diffColor = arena.Value.WinDiff > 0 ? _plugin.Configuration.Colors.Win : arena.Value.WinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                        ImGuiHelper.DrawNumericCell(arena.Value.WinRate.ToString("P2"), offset, diffColor);
                    }
                }
            }
        }
    }

    private void DrawJobTable(Dictionary<Job, CCAggregateStats> jobStats) {
        using(var table = ImRaii.Table($"JobTable", 5, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                float offset = -1f;
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn($"Role", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Name", 0, false, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Role", 0, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Matches", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Wins", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Win Rate", 2, true, true, offset);
                foreach(var job in jobStats) {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{PlayerJobHelper.GetNameFromJob(job.Key)}");

                    ImGui.TableNextColumn();
                    ImGui.TextColored(_plugin.Configuration.GetJobColor(job.Key), $"{PlayerJobHelper.GetSubRoleFromJob(job.Key)}");

                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(job.Value.Matches.ToString(), offset);

                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(job.Value.Wins.ToString(), offset);

                    ImGui.TableNextColumn();
                    if(job.Value.Matches > 0) {
                        var diffColor = job.Value.WinDiff > 0 ? _plugin.Configuration.Colors.Win : job.Value.WinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                        ImGuiHelper.DrawNumericCell(job.Value.WinRate.ToString("P2"), offset, diffColor);
                    }
                }
            }
        }
    }

    private void DrawPlayerTable(Dictionary<PlayerAlias, CCAggregateStats> playerStats) {
        using(var table = ImRaii.Table($"PlayerTable", 4, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                float offset = -1f;
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn($"Favored\nJob", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Name", 0, false, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Favored\nJob", 1, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Matches", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Wins", 2, true, true, offset);

                for(int i = 0; i < playerStats.Count && i < 5; i++) {
                    var player = playerStats.ElementAt(i);
                    ImGui.TableNextColumn();
                    ImGui.Text(player.Key.Name);

                    ImGui.TableNextColumn();
                    var jobString = player.Value.Job.ToString() ?? "";
                    ImGuiHelper.CenterAlignCursor(jobString);
                    ImGui.TextColored(_plugin.Configuration.GetJobColor(player.Value.Job), player.Value.Job.ToString());

                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(player.Value.Matches.ToString(), offset);

                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(player.Value.Wins.ToString(), offset);
                }
            }
        }
    }

    private void DrawMatchStatsTable() {
        using(var table = ImRaii.Table($"MatchStatsTable", 7, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                float offset = -1f;
                ImGui.TableSetupColumn("Kills");
                ImGui.TableSetupColumn($"Deaths");
                ImGui.TableSetupColumn($"Assists");
                ImGui.TableSetupColumn("Damage\nDealt");
                ImGui.TableSetupColumn($"Damage\nTaken");
                ImGui.TableSetupColumn($"HP\nRestored");
                ImGui.TableSetupColumn("Time on\nCrystal");

                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Kills", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Deaths", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Assists", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Damage\nDealt", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Damage\nTaken", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("HP\nRestored", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Time on\nCrystal", 2, true, true, offset);

                //per match
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 1.0f, 4.5f, _plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 1.5f, 3.5f, _plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMatch.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 5.0f, 7.5f, _plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 400000f, 850000f, _plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMatch.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 400000f, 850000f, _plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMatch.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 350000f, 1000000f, _plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                var tcpa = _plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMatch.TimeOnCrystal;
                ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(tcpa), (float)tcpa.TotalSeconds, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 35f, 120f, _plugin.Configuration.ColorScaleStats, offset);

                //per min
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMin.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.1f, 0.7f, _plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMin.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 0.25f, 0.55f, _plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMin.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.75f, 1.5f, _plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 75000f, 140000f, _plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMin.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 75000f, 140000f, _plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMin.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 60000f, 185000f, _plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                var tcpm = _plugin.CCStatsEngine.LocalPlayerStats.ScoreboardPerMin.TimeOnCrystal;
                ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(tcpm), (float)tcpm.TotalSeconds, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 6f, 20f, _plugin.Configuration.ColorScaleStats, offset);

                //team contrib
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardContrib.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardContrib.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardContrib.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardContrib.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardContrib.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.LocalPlayerStats.ScoreboardContrib.TimeOnCrystalDouble, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", offset);
            }
        }
    }
}
