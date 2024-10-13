using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Managers.Stats;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.Summary;
internal class FrontlineSummary {

    private readonly Plugin Plugin;

    public float RefreshProgress { get; set; } = 0f;

    internal FLAggregateStats OverallResults { get; private set; } = new();
    internal Dictionary<FrontlineMap, FLAggregateStats> MapResults { get; private set; } = new();
    internal FLPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, FLAggregateStats> LocalPlayerJobResults { get; private set; } = new();
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    //internal state
    List<FrontlineMatch> _matches = new();
    int _matchesProcessed = 0;
    int _matchesTotal = 100;

    FLAggregateStats _overallResults = new();
    Dictionary<FrontlineMap, FLAggregateStats> _mapResults = [];
    Dictionary<Job, FLAggregateStats> _localPlayerJobResults = [];
    FLPlayerJobStats _localPlayerStats = new();
    List<FLScoreboardDouble> _localPlayerTeamContributions = [];
    FLPlayerJobStats _shatterLocalPlayerStats = new();
    List<FLScoreboardDouble> _shatterLocalPlayerTeamContributions = [];

    TimeSpan _totalMatchTime;
    TimeSpan _totalShatterTime;

    public FrontlineSummary(Plugin plugin) {
        Plugin = plugin;
        Reset();
    }

    private void Reset() {
        _overallResults = new();
        _mapResults = [];
        _localPlayerJobResults = [];
        _localPlayerStats = new();
        _localPlayerTeamContributions = [];
        _shatterLocalPlayerStats = new();
        _shatterLocalPlayerTeamContributions = [];

        _totalMatchTime = TimeSpan.Zero;
        _totalShatterTime = TimeSpan.Zero;
    }

    internal Task Refresh(List<FrontlineMatch> matches, List<FrontlineMatch> additions, List<FrontlineMatch> removals) {
        _matchesProcessed = 0;
        Stopwatch s1 = Stopwatch.StartNew();
        try {
            if(removals.Count * 2 >= _matches.Count) {
                //force full build
                Reset();
                _matchesTotal = matches.Count;
                ProcessMatches(matches);
            } else {
                _matchesTotal = removals.Count + additions.Count;
                ProcessMatches(removals, true);
                ProcessMatches(additions);
            }
            FrontlineStatsManager.SetScoreboardStats(_localPlayerStats, _localPlayerTeamContributions, _totalMatchTime);
            FrontlineStatsManager.SetScoreboardStats(_shatterLocalPlayerStats, _shatterLocalPlayerTeamContributions, _totalShatterTime);
            _localPlayerStats.ScoreboardTotal.DamageToOther = _shatterLocalPlayerStats.ScoreboardTotal.DamageToOther;
            _localPlayerStats.ScoreboardPerMatch.DamageToOther = _shatterLocalPlayerStats.ScoreboardPerMatch.DamageToOther;
            _localPlayerStats.ScoreboardPerMin.DamageToOther = _shatterLocalPlayerStats.ScoreboardPerMin.DamageToOther;
            _localPlayerStats.ScoreboardContrib.DamageToOther = _shatterLocalPlayerStats.ScoreboardContrib.DamageToOther;

            OverallResults = _overallResults;
            MapResults = _mapResults.Where(x => x.Value.Matches > 0).ToDictionary();
            LocalPlayerStats = _localPlayerStats;
            LocalPlayerJobResults = _localPlayerJobResults.Where(x => x.Value.Matches > 0).ToDictionary();
            AverageMatchDuration = matches.Count > 0 ? _totalMatchTime / matches.Count : TimeSpan.Zero;
            _matches = matches;
        } finally {
            s1.Stop();
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"FL Summary Refresh", s1.ElapsedMilliseconds.ToString()));
            _matchesProcessed = 0;
        }
        return Task.CompletedTask;
    }

    private void ProcessMatch(FrontlineMatch match, bool remove = false) {
        FrontlineStatsManager.IncrementAggregateStats(_overallResults, match, remove);
        if(remove) {
            _totalMatchTime -= match.MatchDuration ?? TimeSpan.Zero;
        } else {
            _totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;
        }

        if(match.Arena != null) {
            var arena = (FrontlineMap)match.Arena;
            if(!_mapResults.TryGetValue(arena, out FLAggregateStats? val)) {
                _mapResults.Add(arena, new());
            }
            FrontlineStatsManager.IncrementAggregateStats(_mapResults[arena], match, remove);
        }

        if(match.LocalPlayerTeamMember != null && match.LocalPlayerTeamMember.Job != null) {
            var job = (Job)match.LocalPlayerTeamMember.Job;
            if(!_localPlayerJobResults.TryGetValue(job, out FLAggregateStats? val)) {
                _localPlayerJobResults.Add(job, new());
            }
            FrontlineStatsManager.IncrementAggregateStats(_localPlayerJobResults[job], match, remove);
        }

        if(match.PlayerScoreboards != null) {
            var teamScoreboards = match.GetTeamScoreboards();
            if(match.LocalPlayerTeam != null) {
                //scoreboardEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
                FrontlineScoreboard? localPlayerTeamScoreboard = null;
                teamScoreboards?.TryGetValue((FrontlineTeamName)match.LocalPlayerTeam, out localPlayerTeamScoreboard);
                FrontlineStatsManager.AddPlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeamMember!, new FLScoreboardTally(localPlayerTeamScoreboard), remove);
                if(match.Arena == FrontlineMap.FieldsOfGlory) {
                    if(remove) {
                        _totalShatterTime -= match.MatchDuration ?? TimeSpan.Zero;
                    } else {
                        _totalShatterTime += match.MatchDuration ?? TimeSpan.Zero;
                    }
                    teamScoreboards?.TryGetValue((FrontlineTeamName)match.LocalPlayerTeam, out localPlayerTeamScoreboard);
                    FrontlineStatsManager.AddPlayerJobStat(_shatterLocalPlayerStats, _shatterLocalPlayerTeamContributions, match, match.LocalPlayerTeamMember!, new FLScoreboardTally(localPlayerTeamScoreboard), remove);
                }
            }
        }
    }

    private void ProcessMatches(List<FrontlineMatch> matches, bool remove = false) {
        matches.ForEach(x => {
            ProcessMatch(x, remove);
            RefreshProgress = (float)_matchesProcessed++ / _matchesTotal;
        });
    }

    public void Draw() {
        if(OverallResults.Matches > 0) {
            DrawOverallResultsTable();
            if(LocalPlayerJobResults.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Jobs Played:");
                DrawJobTable(LocalPlayerJobResults.OrderByDescending(x => x.Value.Matches).ToDictionary(), 0);
                //DrawMapResultsTable();
            }
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Average Performance:");
            ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: median team contribution per match.\n\n'Damage to Other' only counts Shatter matches.");
            ImGui.Text("KDA: ");
            ImGui.SameLine();
            ImGuiHelper.DrawColorScale((float)LocalPlayerStats.ScoreboardTotal.KDA, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KDARange[0], FrontlineStatsManager.KDARange[1], Plugin.Configuration.ColorScaleStats, "0.00");
            DrawMatchStatsTable();
            if(MapResults.Count > 0) {
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
            ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 148f);
            ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell("Matches: ", -10f);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell($"{OverallResults.Matches:N0}");
            ImGui.TableNextColumn();

            if(OverallResults.Matches > 0) {
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("First places: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{OverallResults.FirstPlaces:N0}");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(OverallResults.FirstRate.ToString("P2"));

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Second places: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{OverallResults.SecondPlaces:N0}");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(OverallResults.SecondRate.ToString("P2"));

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Third places: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{OverallResults.ThirdPlaces:N0}");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(OverallResults.ThirdRate.ToString("P2"));

                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Average place: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)OverallResults.AveragePlace, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 1.5f, 2.5f, Plugin.Configuration.ColorScaleStats, "0.00");
                //ImGui.Text(string.Format("{0:0.00}", OverallResults.AveragePlace));
                ImGui.TableNextColumn();

                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Average match length: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(AverageMatchDuration));
                ImGui.TableNextColumn();
            }
        }
    }

    private void DrawMapResultsTable() {
        using var table = ImRaii.Table($"MapTable", 5, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            float offset = -1f;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Avg.\nPlace", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Name", 0, false, true, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Matches", 2, true, true, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Wins", 2, true, true, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Win Rate", 2, true, true, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Average\nPlace", 2, true, true, offset);
            foreach(var map in MapResults.OrderByDescending(x => x.Value.Matches).ToDictionary()) {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(MatchHelper.GetFrontlineArenaName(map.Key));

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(map.Value.Matches.ToString(), offset);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(map.Value.Wins.ToString(), offset);

                ImGui.TableNextColumn();
                if(map.Value.Matches > 0) {
                    var diffColor = Plugin.Configuration.GetFrontlineWinRateColor(map.Value);
                    ImGuiHelper.DrawNumericCell(map.Value.WinRate.ToString("P2"), offset, diffColor);
                }

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)map.Value.AveragePlace, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 1.5f, 2.5f, Plugin.Configuration.ColorScaleStats, "0.00", offset);
            }
        }
    }

    private void DrawJobTable(Dictionary<Job, FLAggregateStats> jobStats, int id) {
        using var table = ImRaii.Table($"JobTable###{id}", 6, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            float offset = -1f;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn($"Role", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Avg.\nPlace", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

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
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Average\nPlace", 2, true, true, offset);
            foreach(var job in jobStats) {
                ImGui.TableNextColumn();
                ImGui.Text($"{PlayerJobHelper.GetNameFromJob(job.Key)}");

                ImGui.TableNextColumn();
                ImGui.TextColored(Plugin.Configuration.GetJobColor(job.Key), $"{PlayerJobHelper.GetSubRoleFromJob(job.Key)}");

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(job.Value.Matches.ToString(), offset);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(job.Value.Wins.ToString(), offset);

                ImGui.TableNextColumn();
                if(job.Value.Matches > 0) {
                    var diffColor = Plugin.Configuration.GetFrontlineWinRateColor(job.Value);
                    ImGuiHelper.DrawNumericCell(job.Value.WinRate.ToString("P2"), offset, diffColor);
                }

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)job.Value.AveragePlace, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, 1.5f, 2.5f, Plugin.Configuration.ColorScaleStats, "0.00", offset);
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
                ImGui.TableSetupColumn("Damage to PCs");
                ImGui.TableSetupColumn("Damage to Other");
                ImGui.TableSetupColumn($"Damage Taken");
                ImGui.TableSetupColumn($"HP Restored");

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

                //per match
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KillsPerMatchRange[0], FrontlineStatsManager.KillsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, FrontlineStatsManager.DeathsPerMatchRange[0], FrontlineStatsManager.DeathsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.AssistsPerMatchRange[0], FrontlineStatsManager.AssistsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageDealtPerMatchRange[0], FrontlineStatsManager.DamageDealtPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageToOtherPerMatchRange[0], FrontlineStatsManager.DamageToOtherPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageTakenPerMatchRange[0], FrontlineStatsManager.DamageTakenPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.HPRestoredPerMatchRange[0], FrontlineStatsManager.HPRestoredPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);

                //per min
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KillsPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.KillsPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, FrontlineStatsManager.DeathsPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.DeathsPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.AssistsPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.AssistsPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageDealtPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.DamageDealtPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageToOtherPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.DamageToOtherPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageTakenPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.DamageTakenPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.HPRestoredPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.HPRestoredPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, Plugin.Configuration.ColorScaleStats, "#", offset);

                //team contrib
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
            }
        }
    }
}
