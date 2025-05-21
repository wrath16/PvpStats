using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Managers.Stats;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictSummary : Refreshable<CrystallineConflictMatch> {

    private readonly Plugin Plugin;
    public override string Name => "CC Summary";

    internal protected SemaphoreSlim RefreshLock { get; private set; } = new SemaphoreSlim(1);

    internal CCPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> LocalPlayerJobStats { get; private set; } = [];
    internal Dictionary<CrystallineConflictMap, CCAggregateStats> ArenaStats { get; private set; } = [];
    internal Dictionary<PlayerAlias, CCAggregateStats> TeammateStats { get; private set; } = [];
    internal Dictionary<PlayerAlias, CCAggregateStats> OpponentStats { get; private set; } = [];
    internal Dictionary<Job, CCAggregateStats> TeammateJobStats { get; private set; } = [];
    internal Dictionary<Job, CCAggregateStats> OpponentJobStats { get; private set; } = [];
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    //internal state
    TimeTally _totalMatchTime = new();
    CCPlayerJobStats _localPlayerStats = new();
    ConcurrentDictionary<int, CCScoreboardDouble> _localPlayerTeamContributions = [];
    TimeTally _localPlayerMatchTime = new();
    ConcurrentDictionary<Job, CCAggregateStats> _localPlayerJobStats = [];
    ConcurrentDictionary<CrystallineConflictMap, CCAggregateStats> _arenaStats = [];
    ConcurrentDictionary<PlayerAlias, CCAggregateStats> _teammateStats = [];
    ConcurrentDictionary<PlayerAlias, CCAggregateStats> _opponentStats = [];
    ConcurrentDictionary<Job, CCAggregateStats> _teammateJobStats = [];
    ConcurrentDictionary<Job, CCAggregateStats> _opponentJobStats = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<Job, CCAggregateStats>> _teammateJobStatsLookup = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<Job, CCAggregateStats>> _opponentJobStatsLookup = [];

    public CrystallineConflictSummary(Plugin plugin) {
        Plugin = plugin;
        Reset();
    }

    protected override void Reset() {
        _totalMatchTime = new();
        _localPlayerStats = new();
        _localPlayerTeamContributions = [];
        _localPlayerMatchTime = new();
        _localPlayerJobStats = [];
        _arenaStats = [];
        _teammateStats = [];
        _opponentStats = [];
        _teammateJobStats = [];
        _opponentJobStats = [];
        _teammateJobStatsLookup = [];
        _opponentJobStatsLookup = [];
    }

    protected override void PostRefresh(List<CrystallineConflictMatch> matches, List<CrystallineConflictMatch> additions, List<CrystallineConflictMatch> removals) {
        CrystallineConflictStatsManager.SetScoreboardStats(_localPlayerStats, _localPlayerTeamContributions.Values.ToList(), _localPlayerMatchTime.ToTimeSpan());
        foreach(var teammateStat in _teammateStats) {
            teammateStat.Value.Job = _teammateJobStatsLookup[teammateStat.Key].OrderByDescending(x => x.Value.WinDiff).FirstOrDefault().Key;
        }
        foreach(var opponentStat in _opponentStats) {
            opponentStat.Value.Job = _opponentJobStatsLookup[opponentStat.Key].OrderBy(x => x.Value.WinDiff).FirstOrDefault().Key;
        }

        LocalPlayerStats = _localPlayerStats;
        LocalPlayerJobStats = _localPlayerJobStats.Where(x => x.Value.Matches > 0).OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        ArenaStats = _arenaStats.Where(x => x.Value.Matches > 0).OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        TeammateStats = _teammateStats.Where(x => x.Value.Matches > 0).OrderBy(x => x.Value.Matches).OrderByDescending(x => x.Value.WinDiff).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        TeammateJobStats = _teammateJobStats.Where(x => x.Value.Matches > 0).OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        OpponentStats = _opponentStats.Where(x => x.Value.Matches > 0).OrderBy(x => x.Value.Matches).OrderBy(x => x.Value.WinDiff).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        OpponentJobStats = _opponentJobStats.Where(x => x.Value.Matches > 0).OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        AverageMatchDuration = matches.Count > 0 ? _totalMatchTime.ToTimeSpan() / matches.Count : TimeSpan.Zero;
    }

    protected override void ProcessMatch(CrystallineConflictMatch match, bool remove = false) {
        if(remove) {
            _totalMatchTime.RemoveTime(match.MatchDuration ?? TimeSpan.Zero);
        } else {
            _totalMatchTime.AddTime(match.MatchDuration ?? TimeSpan.Zero);
        }

        //local player stats
        if(!match.IsSpectated && match.PostMatch != null) {
            if(remove) {
                _localPlayerMatchTime.RemoveTime(match.MatchDuration ?? TimeSpan.Zero);
            } else {
                _localPlayerMatchTime.AddTime(match.MatchDuration ?? TimeSpan.Zero);
            }
            CrystallineConflictStatsManager.AddPlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeam!, match.LocalPlayerTeamMember!, remove);

            if(match.LocalPlayerTeamMember!.Job != null) {
                var job = (Job)match.LocalPlayerTeamMember!.Job;
                _localPlayerJobStats.TryAdd(job, new());
                CrystallineConflictStatsManager.IncrementAggregateStats(_localPlayerJobStats[job], match, remove);
            }
        }

        //arena stats
        if(match.Arena != null) {
            var arena = (CrystallineConflictMap)match.Arena;
            _arenaStats.TryAdd(arena, new());
            CrystallineConflictStatsManager.IncrementAggregateStats(_arenaStats[arena], match, remove);
        }

        //process player and job stats
        foreach(var team in match.Teams) {
            foreach(var player in team.Value.Players) {
                bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
                bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.Key == match.LocalPlayerTeam!.TeamName;
                bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;

                var job = (Job)player.Job!;
                var alias = Plugin.PlayerLinksService.GetMainAlias(player.Alias);

                if(isTeammate) {
                    _teammateStats.TryAdd(alias, new());
                    CrystallineConflictStatsManager.IncrementAggregateStats(_teammateStats[alias], match, remove);
                    if(player.Job != null) {
                        _teammateJobStats.TryAdd(job, new());
                        CrystallineConflictStatsManager.IncrementAggregateStats(_teammateJobStats[job], match, remove);
                        _teammateJobStatsLookup.TryAdd(alias, new());
                        _teammateJobStatsLookup[alias].TryAdd(job, new());
                        CrystallineConflictStatsManager.IncrementAggregateStats(_teammateJobStatsLookup[alias][job], match, remove);
                    }
                } else if(isOpponent) {
                    _opponentStats.TryAdd(alias, new());
                    CrystallineConflictStatsManager.IncrementAggregateStats(_opponentStats[alias], match, remove);
                    if(player.Job != null) {
                        _opponentJobStats.TryAdd(job, new());
                        CrystallineConflictStatsManager.IncrementAggregateStats(_opponentJobStats[job], match, remove);
                        _opponentJobStatsLookup.TryAdd(alias, new());
                        _opponentJobStatsLookup[alias].TryAdd(job, new());
                        CrystallineConflictStatsManager.IncrementAggregateStats(_opponentJobStatsLookup[alias][job], match, remove);
                    }
                }
            }
        }
    }

    public void Draw() {
        if(LocalPlayerStats.StatsAll.Matches > 0) {
            DrawResultTable();
        } else {
            ImGui.TextDisabled("No matches for given filters.");
        }

        if(LocalPlayerJobStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Jobs Played:");
            DrawJobTable(LocalPlayerJobStats);
        }

        if(LocalPlayerStats.StatsAll.Matches > 0) {
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Average Performance:");
            ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: median team contribution per match.");
            ImGui.Text("KDA: ");
            ImGui.SameLine();
            ImGuiHelper.DrawColorScale((float)LocalPlayerStats.ScoreboardTotal.KDA, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh,
                CrystallineConflictStatsManager.KDARange[0], CrystallineConflictStatsManager.KDARange[1], Plugin.Configuration.ColorScaleStats, LocalPlayerStats.ScoreboardTotal.KDA.ToString("0.00"));
            ImGui.SameLine();
            ImGui.Text("Kill Participation Rate: ");
            ImGui.SameLine();
            ImGuiHelper.DrawColorScale((float)LocalPlayerStats.ScoreboardTotal.KillParticipationRate, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh,
                CrystallineConflictStatsManager.KillParticipationRange[0], CrystallineConflictStatsManager.KillParticipationRange[1], Plugin.Configuration.ColorScaleStats, LocalPlayerStats.ScoreboardTotal.KillParticipationRate.ToString("P1"));
            DrawMatchStatsTable();
        }

        if(ArenaStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Arenas:");
            DrawArenaTable(ArenaStats);
        }

        if(TeammateJobStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Teammates' Jobs Played:");
            DrawJobTable(TeammateJobStats);
        }

        if(OpponentJobStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Opponents' Jobs Played:");
            DrawJobTable(OpponentJobStats);
        }

        if(TeammateStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Top Teammates:");
            DrawPlayerTable(TeammateStats);
        }

        if(OpponentStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Top Opponents:");
            DrawPlayerTable(OpponentStats);
        }
    }

    private void DrawResultTable() {
        using(var table = ImRaii.Table($"StatsSummary", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 160f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
                ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Matches: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{LocalPlayerStats.StatsAll.Matches:N0}");
                ImGui.TableNextColumn();

                if(LocalPlayerStats.StatsAll.Matches > 0) {
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell("Wins: ", -10f);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{LocalPlayerStats.StatsAll.Wins:N0}");
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(LocalPlayerStats.StatsAll.WinRate.ToString("P2"));

                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell("Losses: ", -10f);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{LocalPlayerStats.StatsAll.Losses:N0}");
                    ImGui.TableNextColumn();

                    if(LocalPlayerStats.StatsAll.OtherResult > 0) {
                        ImGui.TableNextColumn();
                        ImGuiHelper.DrawNumericCell("Other: ", -10f);
                        ImGui.TableNextColumn();
                        ImGuiHelper.DrawNumericCell($"{LocalPlayerStats.StatsAll.OtherResult:N0}");
                        ImGui.TableNextColumn();
                    }
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
    }

    private void DrawArenaTable(Dictionary<CrystallineConflictMap, CCAggregateStats> arenaStats) {
        var numColumns = 4;
        using var table = ImRaii.Table($"ArenaTable", numColumns, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            float offset = -1f;
            var cellPadding = ImGui.GetStyle().CellPadding.X;
            var stretchWidth = ImGui.GetContentRegionAvail().X - 55f * ImGuiHelpers.GlobalScale * (numColumns - 1) - cellPadding * 2 * numColumns;
            var maxWidth = 250f * ImGuiHelpers.GlobalScale - cellPadding * 2 + 2 * (55f * ImGuiHelpers.GlobalScale + cellPadding * 2);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, Math.Min(stretchWidth, maxWidth));
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
            ImGuiHelper.DrawTableHeader(ImGuiHelper.WrappedString("Win Rate", ImGuiHelpers.GlobalScale * 55f), 2, true, true, offset);
            foreach(var arena in arenaStats) {
                ImGui.TableNextColumn();
                ImGui.Text($"{MatchHelper.GetArenaName(arena.Key)}");

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(arena.Value.Matches.ToString(), offset);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(arena.Value.Wins.ToString(), offset);

                ImGui.TableNextColumn();
                if(arena.Value.Matches > 0) {
                    var diffColor = arena.Value.WinDiff > 0 ? Plugin.Configuration.Colors.Win : arena.Value.WinDiff < 0 ? Plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                    ImGuiHelper.DrawNumericCell(arena.Value.WinRate.ToString("P2"), offset, diffColor);
                }
            }
        }
    }

    private void DrawJobTable(Dictionary<Job, CCAggregateStats> jobStats) {
        var numColumns = 5;
        using var table = ImRaii.Table($"JobTable", numColumns, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.SizingStretchProp);
        if(table) {
            float offset = -1f;
            var cellPadding = ImGui.GetStyle().CellPadding.X;
            var stretchWidth = ImGui.GetContentRegionAvail().X - 55f * ImGuiHelpers.GlobalScale * (numColumns - 1) - cellPadding * 2 * numColumns;
            var maxWidth = 250f * ImGuiHelpers.GlobalScale - cellPadding * 2 + (55f * ImGuiHelpers.GlobalScale + cellPadding * 2);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, Math.Min(stretchWidth, maxWidth));
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
            ImGuiHelper.DrawTableHeader(ImGuiHelper.WrappedString("Win Rate", ImGuiHelpers.GlobalScale * 55f), 2, true, true, offset);


            foreach(var job in jobStats) {
                if(ImGui.TableNextColumn()) {
                    ImGui.Text($"{PlayerJobHelper.GetNameFromJob(job.Key)}");
                }
                if(ImGui.TableNextColumn()) {
                    ImGui.TextColored(Plugin.Configuration.GetJobColor(job.Key), $"{PlayerJobHelper.GetSubRoleFromJob(job.Key)}");
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell(job.Value.Matches.ToString(), offset);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell(job.Value.Wins.ToString(), offset);
                }
                if(ImGui.TableNextColumn()) {
                    if(job.Value.Matches > 0) {
                        var diffColor = job.Value.WinDiff > 0 ? Plugin.Configuration.Colors.Win : job.Value.WinDiff < 0 ? Plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                        ImGuiHelper.DrawNumericCell(job.Value.WinRate.ToString("P2"), offset, diffColor);
                    }
                }
            }
        }
    }

    private void DrawPlayerTable(Dictionary<PlayerAlias, CCAggregateStats> playerStats) {
        var numColumns = 5;
        using var table = ImRaii.Table($"PlayerTable", numColumns, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            float offset = -1f;
            var cellPadding = ImGui.GetStyle().CellPadding.X;
            var stretchWidth = ImGui.GetContentRegionAvail().X - 55f * ImGuiHelpers.GlobalScale * (numColumns - 1) - cellPadding * 2 * numColumns;
            var maxWidth = 250f * ImGuiHelpers.GlobalScale - cellPadding * 2 + 1 * (55f * ImGuiHelpers.GlobalScale + cellPadding * 2);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, Math.Min(stretchWidth, maxWidth));
            ImGui.TableSetupColumn($"Favored Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
            ImGui.TableSetupColumn($"Win Diff.", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);

            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Name", 0, false, true, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Favored\nJob", 1, true, true, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Matches", 2, true, true, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader("Wins", 2, true, true, offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawTableHeader(ImGuiHelper.WrappedString("Win Diff.", ImGuiHelpers.GlobalScale * 55f), 2, true, true, offset);

            for(int i = 0; i < playerStats.Count && i < 5; i++) {
                var player = playerStats.ElementAt(i);
                ImGui.TableNextColumn();
                ImGui.Text(player.Key.Name);
                ImGuiHelper.WrappedTooltip(player.Key.HomeWorld);

                ImGui.TableNextColumn();
                var jobString = player.Value.Job.ToString() ?? "";
                ImGuiHelper.CenterAlignCursor(jobString);
                ImGui.TextColored(Plugin.Configuration.GetJobColor(player.Value.Job), player.Value.Job.ToString());

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(player.Value.Matches.ToString(), offset);

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(player.Value.Wins.ToString(), offset);

                ImGui.TableNextColumn();
                var diffColor = player.Value.WinDiff > 0 ? Plugin.Configuration.Colors.Win : player.Value.WinDiff < 0 ? Plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
                ImGuiHelper.DrawNumericCell(player.Value.WinDiff.ToString(), offset, diffColor);
            }
        }
    }

    private void DrawMatchStatsTable() {
        string[] cols = ["Kills", "Deaths", "Assists", "Damage Dealt", "Damage Taken", "HP Restored", "Time on Crystal"];
        using var table = ImRaii.Table($"MatchStatsTable", cols.Length, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings);
        if(table) {
            float offset = -1f;
            var cellPadding = ImGui.GetStyle().CellPadding.X;
            var stretchWidth = (ImGui.GetContentRegionAvail().X - cellPadding * cols.Length * 2) / cols.Length;
            var widthLoss = stretchWidth - (float)Math.Floor(stretchWidth);
            var maxWidth = ((250f + 55f * 5) * ImGuiHelpers.GlobalScale + cellPadding * 2 * 6 - cellPadding * cols.Length * 2) / 7f;

            for(int i = 0; i < cols.Length; i++) {
                float width = stretchWidth;
                if(i % 2 == 0) {
                    width += widthLoss * 2f;
                }
                ImGui.TableSetupColumn(cols[i], ImGuiTableColumnFlags.WidthFixed, (float)Math.Min(width, maxWidth));
            }

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
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.KillsPerMatchRange[0], CrystallineConflictStatsManager.KillsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, CrystallineConflictStatsManager.DeathsPerMatchRange[0], CrystallineConflictStatsManager.DeathsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.AssistsPerMatchRange[0], CrystallineConflictStatsManager.AssistsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.DamageDealt, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageDealtPerMatchRange[0], CrystallineConflictStatsManager.DamageDealtPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageTakenPerMatchRange[0], CrystallineConflictStatsManager.DamageTakenPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.HPRestoredPerMatchRange[0], CrystallineConflictStatsManager.HPRestoredPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
            ImGui.TableNextColumn();
            var tcpa = TimeSpan.FromSeconds(LocalPlayerStats.ScoreboardPerMatch.TimeOnCrystal);
            ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(tcpa), (float)tcpa.TotalSeconds, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.TimeOnCrystalPerMatchRange[0], CrystallineConflictStatsManager.TimeOnCrystalPerMatchRange[1], Plugin.Configuration.ColorScaleStats, offset);

            //per min
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.KillsPerMinRange[0], CrystallineConflictStatsManager.KillsPerMinRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, CrystallineConflictStatsManager.DeathsPerMinRange[0], CrystallineConflictStatsManager.DeathsPerMinRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.AssistsPerMinRange[0], CrystallineConflictStatsManager.AssistsPerMinRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.DamageDealt, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageDealtPerMinRange[0], CrystallineConflictStatsManager.DamageDealtPerMinRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageTakenPerMinRange[0], CrystallineConflictStatsManager.DamageTakenPerMinRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.HPRestoredPerMinRange[0], CrystallineConflictStatsManager.HPRestoredPerMinRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
            ImGui.TableNextColumn();
            var tcpm = TimeSpan.FromSeconds(LocalPlayerStats.ScoreboardPerMin.TimeOnCrystal);
            ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(tcpm), (float)tcpm.TotalSeconds, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.TimeOnCrystalPerMinRange[0], CrystallineConflictStatsManager.TimeOnCrystalPerMinRange[1], Plugin.Configuration.ColorScaleStats, offset);

            //team contrib
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.DamageDealt, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
            ImGui.TableNextColumn();
            ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.TimeOnCrystal, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
        }
    }
}
