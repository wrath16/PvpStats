﻿using Dalamud.Interface.Colors;
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
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Summary;
internal class RivalWingsSummary {
    private readonly Plugin Plugin;

    public float RefreshProgress { get; set; } = 0f;

    public CCAggregateStats OverallResults { get; private set; } = new();
    public RWPlayerJobStats LocalPlayerStats { get; private set; } = new();
    public Dictionary<Job, CCAggregateStats> LocalPlayerJobResults { get; private set; } = new();
    public uint LocalPlayerMechMatches { get; private set; } = 0;
    public Dictionary<RivalWingsMech, double> LocalPlayerMechTime { get; private set; } = new();
    public double LocalPlayerMidWinRate { get; private set; }
    public double LocalPlayerMercWinRate { get; private set; }
    public TimeSpan AverageMatchDuration { get; private set; } = new();

    //internal state
    List<RivalWingsMatch> _matches = new();
    int _matchesProcessed = 0;
    int _matchesTotal = 100;

    CCAggregateStats _overallResults;
    Dictionary<Job, CCAggregateStats> _localPlayerJobResults = [];
    RWPlayerJobStats _localPlayerStats = new();
    List<RWScoreboardDouble> _localPlayerTeamContributions = [];
    Dictionary<RivalWingsMech, double> _localPlayerMechTime = new() {
            { RivalWingsMech.Chaser, 0},
            { RivalWingsMech.Oppressor, 0},
            { RivalWingsMech.Justice, 0}
        };
    uint _localPlayerMechMatches = 0;
    TimeSpan _totalMatchTime = TimeSpan.Zero;
    TimeSpan _mechEligibleTime = TimeSpan.Zero;
    TimeSpan _scoreboardEligibleTime = TimeSpan.Zero;
    int _midWins = 0, _midLosses = 0;
    int _mercWins = 0, _mercLosses = 0;

    public RivalWingsSummary(Plugin plugin) {
        Plugin = plugin;
        Reset();
    }

    private void Reset() {
        _overallResults = new();
        _localPlayerJobResults = [];
        _localPlayerStats = new();
        _localPlayerTeamContributions = [];
        _localPlayerMechTime = new() {
            { RivalWingsMech.Chaser, 0},
            { RivalWingsMech.Oppressor, 0},
            { RivalWingsMech.Justice, 0}
        };
        _localPlayerMechMatches = 0;
        _totalMatchTime = TimeSpan.Zero;
        _mechEligibleTime = TimeSpan.Zero;
        _scoreboardEligibleTime = TimeSpan.Zero;
        _midWins = 0;
        _midLosses = 0;
        _mercWins = 0;
        _mercLosses = 0;
    }

    internal Task Refresh(List<RivalWingsMatch> matches, List<RivalWingsMatch> additions, List<RivalWingsMatch> removals) {
        _matchesProcessed = 0;
        Stopwatch summaryTimer = Stopwatch.StartNew();
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
            RivalWingsStatsManager.SetScoreboardStats(_localPlayerStats, _localPlayerTeamContributions, _scoreboardEligibleTime);

            OverallResults = _overallResults;
            LocalPlayerStats = _localPlayerStats;
            LocalPlayerJobResults = _localPlayerJobResults.Where(x => x.Value.Matches > 0).ToDictionary();
            LocalPlayerMechTime = _localPlayerMechTime.Select(x => (x.Key, x.Value / _mechEligibleTime.TotalSeconds)).ToDictionary();
            LocalPlayerMechMatches = _localPlayerMechMatches;
            LocalPlayerMercWinRate = (double)_mercWins / (_mercWins + _mercLosses);
            LocalPlayerMidWinRate = (double)_midWins / (_midWins + _midLosses);
            AverageMatchDuration = matches.Count > 0 ? _totalMatchTime / matches.Count : TimeSpan.Zero;

            _matches = matches;

        } finally {
            summaryTimer.Stop();
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"RW Summary Refresh", summaryTimer.ElapsedMilliseconds.ToString()));
            _matchesProcessed = 0;
        }
        return Task.CompletedTask;
    }

    private void ProcessMatch(RivalWingsMatch match, bool remove = false) {
        var teamScoreboards = match.GetTeamScoreboards();
        RivalWingsStatsManager.IncrementAggregateStats(_overallResults, match, remove);
        if(remove) {
            _totalMatchTime -= match.MatchDuration ?? TimeSpan.Zero;

        } else {
            _totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;
        }

        if(match.LocalPlayerTeamMember != null && match.LocalPlayerTeamMember.Job != null) {
            var job = (Job)match.LocalPlayerTeamMember.Job;
            if(!_localPlayerJobResults.TryGetValue(job, out CCAggregateStats? val)) {
                _localPlayerJobResults.Add(job, new());
            }
            RivalWingsStatsManager.IncrementAggregateStats(_localPlayerJobResults[job], match, remove);
        }

        if(match.PlayerScoreboards != null) {
            _scoreboardEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
            RivalWingsScoreboard? localPlayerTeamScoreboard = null;
            teamScoreboards?.TryGetValue(match.LocalPlayerTeam ?? RivalWingsTeamName.Unknown, out localPlayerTeamScoreboard);
            RivalWingsStatsManager.AddPlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeamMember, localPlayerTeamScoreboard, remove);
        }

        if(match.PlayerMechTime != null && match.LocalPlayer != null) {
            if(remove) {
                _localPlayerMechMatches--;
                _mechEligibleTime -= match.MatchDuration ?? TimeSpan.Zero;
            } else {
                _localPlayerMechMatches++;
                _mechEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
            }
            if(match.PlayerMechTime.TryGetValue(match.LocalPlayer, out var playerMechTime)) {
                foreach(var mech in playerMechTime) {
                    if(remove) {
                        _localPlayerMechTime[mech.Key] -= mech.Value;
                    } else {
                        _localPlayerMechTime[mech.Key] += mech.Value;
                    }
                }
            }
        }

        if(match.Mercs != null) {
            foreach(var team in match.Mercs) {
                if(team.Key == match.LocalPlayerTeam) {
                    if(remove) {
                        _mercWins -= team.Value;
                    } else {
                        _mercWins += team.Value;
                    }
                } else {
                    if(remove) {
                        _mercLosses -= team.Value;
                    } else {
                        _mercLosses += team.Value;
                    }
                }
            }
        }

        if(match.Supplies != null) {
            foreach(var team in match.Supplies) {
                if(team.Key == match.LocalPlayerTeam) {
                    foreach(var supply in team.Value) {
                        if(remove) {
                            _midWins -= supply.Value;
                        } else {
                            _midWins += supply.Value;
                        }
                    }
                } else {
                    foreach(var supply in team.Value) {
                        if(remove) {
                            _midLosses -= supply.Value;
                        } else {
                            _midLosses += supply.Value;
                        }
                    }
                }
            }
        }
    }

    private void ProcessMatches(List<RivalWingsMatch> matches, bool remove = false) {
        matches.ForEach(x => {
            ProcessMatch(x, remove);
            RefreshProgress = (float)_matchesProcessed++ / _matchesTotal;
        });
    }

    public void Draw() {
        if(OverallResults.Matches > 0) {
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
            if(LocalPlayerJobResults.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Jobs Played:");
                ImGuiHelper.HelpMarker("Job is determined by the post-match scoreboard.");
                DrawJobTable(LocalPlayerJobResults.OrderByDescending(x => x.Value.Matches).ToDictionary(), 0);
            }
            ImGui.Separator();
            ImGui.TextColored(Plugin.Configuration.Colors.Header, "Average Performance:");
            ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: median team contribution per match.");
            DrawMatchStatsTable();
        } else {
            ImGui.TextDisabled("No matches for given filters.");
        }

        //if(RefreshActive) {
        //    ImGuiHelper.DrawRefreshProgressBar( _matchesProcessed / _matchesTotal);
        //    Plugin.Log.Debug($"{_matchesProcessed}/{_matchesTotal}");
        //}
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
                ImGuiHelper.DrawNumericCell("Wins: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{OverallResults.Wins:N0}");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(OverallResults.WinRate.ToString("P2"));

                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell("Losses: ", -10f);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{OverallResults.Losses:N0}");
                ImGui.TableNextColumn();

                if(OverallResults.OtherResult > 0) {
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell("Other: ", -10f);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{OverallResults.OtherResult:N0}");
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
            ImGuiHelper.DrawNumericCell(LocalPlayerMechTime[RivalWingsMech.Chaser].ToString("P2"), -1f);

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.OppressorIcons[RivalWingsTeamName.Unknown]), size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Oppressor");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGuiHelper.DrawNumericCell(LocalPlayerMechTime[RivalWingsMech.Oppressor].ToString("P2"), -1f);

            ImGui.TableNextColumn();
            ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.JusticeIcons[RivalWingsTeamName.Unknown]), size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Brute Justice");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGuiHelper.DrawNumericCell(LocalPlayerMechTime[RivalWingsMech.Justice].ToString("P2"), -1f);
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
            ImGuiHelper.DrawNumericCell(LocalPlayerMercWinRate.ToString("P2"), -1f);

            ImGui.TableNextColumn();
            uv0 = new Vector2(0.1f);
            uv1 = new Vector2(0.9f);
            ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.TrainIcon), size, uv0, uv1);
            ImGuiHelper.WrappedTooltip("Supplies");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGuiHelper.DrawNumericCell(LocalPlayerMidWinRate.ToString("P2"), -1f);
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
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.KillsPerMatchRange[0], RivalWingsStatsManager.KillsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, RivalWingsStatsManager.DeathsPerMatchRange[0], RivalWingsStatsManager.DeathsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.AssistsPerMatchRange[0], RivalWingsStatsManager.AssistsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtToPCsPerMatchRange[0], RivalWingsStatsManager.DamageDealtToPCsPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtToOtherPerMatchRange[0], RivalWingsStatsManager.DamageDealtToOtherPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageTakenPerMatchRange[0], RivalWingsStatsManager.DamageTakenPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.HPRestoredPerMatchRange[0], RivalWingsStatsManager.HPRestoredPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMatch.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.CeruleumPerMatchRange[0], RivalWingsStatsManager.CeruleumPerMatchRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);

                //per min
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.KillsPerMinRange[0], RivalWingsStatsManager.KillsPerMinRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, RivalWingsStatsManager.DeathsPerMinRange[0], RivalWingsStatsManager.DeathsPerMinRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.AssistsPerMinRange[0], RivalWingsStatsManager.AssistsPerMinRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtToPCsPerMinRange[0], RivalWingsStatsManager.DamageDealtToPCsPerMinRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtToOtherPerMinRange[0], RivalWingsStatsManager.DamageDealtToOtherPerMinRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageTakenPerMinRange[0], RivalWingsStatsManager.DamageTakenPerMinRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.HPRestoredPerMinRange[0], RivalWingsStatsManager.HPRestoredPerMinRange[1], Plugin.Configuration.ColorScaleStats, "#", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardPerMin.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.CeruleumPerMinRange[0], RivalWingsStatsManager.CeruleumPerMinRange[1], Plugin.Configuration.ColorScaleStats, "0.00", offset);

                //team contrib
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Kills, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Assists, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.DamageToPCs, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.DamageToOther, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.DamageTaken, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.HPRestored, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)LocalPlayerStats.ScoreboardContrib.Ceruleum, Plugin.Configuration.Colors.StatLow, Plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], Plugin.Configuration.ColorScaleStats, "P1", offset);
            }
        }
    }
}
