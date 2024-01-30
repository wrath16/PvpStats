using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictSummary {

    private class JobStats {
        internal int Matches, Wins;
    }

    private class PlayerStats {
        internal int Matches, Wins;
        internal Job FavoredJob;
        internal Dictionary<Job,  JobStats> JobStats = new();
    }

    private Plugin _plugin;

    private int _totalMatches, _totalWins, _totalLosses, _totalOther;
    private TimeSpan _averageMatchLength;
    private Dictionary<Job, JobStats> _jobStats = new();
    private Dictionary<Job, JobStats> _allyJobStats = new();
    private Dictionary<Job, JobStats> _enemyJobStats = new();
    private Dictionary<PlayerAlias, PlayerStats> _teammateStats = new();
    private Dictionary<PlayerAlias, PlayerStats> _enemyStats = new();
    private int _statsEligibleMatches;
    private int _statsEligibleWins;
    private double _averageKills, _averageDeaths, _averageAssists, _averageDamageDealt, _averageDamageTaken, _averageHPRestored;
    private double _killsPerMin, _deathsPerMin, _assistsPerMin, _damageDealtPerMin, _damageTakenPerMin, _hpRestoredPerMin;
    private double _killContribution, _deathContribution, _assistContribution, _damageDealtContribution, _damageTakenContribution, _hpRestoredContribution, _timeOnCrystalContribution;
    private TimeSpan _averageTimeOnCrystal, _averageMatchLengthStats, _timeOnCrystalPerMin;
    //private CrystallineConflictPostMatchRow _averageStats = new();

    public CrystallineConflictSummary(Plugin plugin) {
        _plugin = plugin;
    }

    public void Refresh(List<CrystallineConflictMatch> matches) {
        _totalMatches = matches.Count;
        _totalWins = matches.Where(x => x.LocalPlayerTeam != null && x.MatchWinner != null && x.MatchWinner == x.LocalPlayerTeam.TeamName).Count();
        _totalLosses = matches.Where(x => x.LocalPlayerTeam != null && x.MatchWinner != null && x.MatchWinner != x.LocalPlayerTeam.TeamName).Count();
        _totalOther = _totalMatches - _totalWins - _totalLosses;

        //_statsEligibleMatches = matches.Where(x => x.LocalPlayerTeam != null && x.PostMatch != null).Count();
        //_statsEligibleWins = matches.Where(x => x.LocalPlayerTeam != null && x.PostMatch != null && x.IsWin).Count();
        _statsEligibleMatches = 0;
        _statsEligibleWins = 0;

        _jobStats = new();
        _allyJobStats = new();
        _enemyJobStats = new();
        _teammateStats = new();
        _enemyStats = new();
        CrystallineConflictPostMatchRow totalStats = new();
        double killContribTotal = 0, deathContribTotal = 0, assistContribTotal = 0, ddContribTotal = 0, dtContribTotal = 0, hpContribTotal = 0, timeContribTotal = 0;
        TimeSpan totalStatsMatchLength = new();
        var addJobStat = ((Dictionary<Job, JobStats> jobStats, Job job, bool isWin) => {
            if (jobStats.ContainsKey(job)) {
                jobStats[job].Matches++;
                jobStats[job].Wins += isWin ? 1 : 0;
            }
            else {
                jobStats.Add(job, new() {
                    Matches = 1,
                    Wins = isWin ? 1 : 0
                });
            }
        });
        var addPlayerStat = ((Dictionary<PlayerAlias, PlayerStats> playerStats, PlayerAlias player, Job job, bool isWin) => {
            if (playerStats.ContainsKey(player)) {
                addJobStat(playerStats[player].JobStats, job, isWin);
                playerStats[player].Matches++;
                playerStats[player].Wins += isWin ? 1 : 0;
            }
            else {
                playerStats.Add(player, new() {
                    JobStats = new(),
                    Matches = 1,
                    Wins = isWin ? 1 : 0
                });
                addJobStat(playerStats[player].JobStats, job, isWin);
            }
        });
        foreach (var match in matches) {
            if (match.LocalPlayerTeamMember != null) {
                addJobStat(_jobStats, match.LocalPlayerTeamMember.Job, match.IsWin);
                foreach (var team in match.Teams) {
                    if (team.Key == match.LocalPlayerTeam.TeamName) {
                        foreach(var player in team.Value.Players) {
                            if(!player.Alias.Equals(match.LocalPlayer)) {
                                addJobStat(_allyJobStats, player.Job, match.IsWin);
                                addPlayerStat(_teammateStats, player.Alias, player.Job, match.IsWin);
                            }
                        }
                    } else {
                        foreach (var player in team.Value.Players) {
                            addJobStat(_enemyJobStats, player.Job, match.IsWin);
                            addPlayerStat(_enemyStats, player.Alias, player.Job, match.IsWin);
                        }
                    }
                }
                if (match.PostMatch != null && match.MatchDuration != null) {
                    var playerTeamStats = match.PostMatch.Teams.Where(x => x.Key == match.LocalPlayerTeam!.TeamName).FirstOrDefault().Value;
                    var playerStats = playerTeamStats.PlayerStats.Where(x => x.Player.Equals(match.LocalPlayer)).FirstOrDefault();
                    if (playerStats != null) {
                        _statsEligibleMatches++;
                        if(match.IsWin) {
                            _statsEligibleWins++;
                        }
                        totalStatsMatchLength += (TimeSpan)match.MatchDuration;
                        totalStats.Kills += playerStats.Kills;
                        totalStats.Deaths += playerStats.Deaths;
                        totalStats.Assists += playerStats.Assists;
                        totalStats.DamageDealt += playerStats.DamageDealt;
                        totalStats.DamageTaken += playerStats.DamageTaken;
                        totalStats.HPRestored += playerStats.HPRestored;
                        totalStats.TimeOnCrystal += playerStats.TimeOnCrystal;

                        killContribTotal += playerTeamStats.TeamStats.Kills != 0 ? (float)playerStats.Kills / playerTeamStats.TeamStats.Kills : 0;
                        deathContribTotal += playerTeamStats.TeamStats.Deaths != 0 ? (float)playerStats.Deaths / playerTeamStats.TeamStats.Deaths : 0;
                        assistContribTotal += playerTeamStats.TeamStats.Assists != 0 ? (float)playerStats.Assists / playerTeamStats.TeamStats.Assists : 0;
                        ddContribTotal += playerTeamStats.TeamStats.DamageDealt != 0 ? (float)playerStats.DamageDealt / playerTeamStats.TeamStats.DamageDealt : 0;
                        dtContribTotal += playerTeamStats.TeamStats.DamageTaken != 0 ? (float)playerStats.DamageTaken / playerTeamStats.TeamStats.DamageTaken : 0;
                        hpContribTotal += playerTeamStats.TeamStats.HPRestored != 0 ? (float)playerStats.HPRestored / playerTeamStats.TeamStats.HPRestored : 0;
                        timeContribTotal += playerTeamStats.TeamStats.TimeOnCrystal.Ticks != 0 ? playerStats.TimeOnCrystal / playerTeamStats.TeamStats.TimeOnCrystal : 0;
                    }
                }
            }
        }
        //set favored job
        var setFavoredJob = ((Dictionary<PlayerAlias, PlayerStats> playerStats, bool byWins) => {
            foreach(var player in playerStats) {
                var list = player.Value.JobStats.OrderByDescending(x => 2 * x.Value.Wins - x.Value.Matches);
                if(byWins) {
                    player.Value.FavoredJob = list.FirstOrDefault().Key;
                } else {
                    player.Value.FavoredJob = list.LastOrDefault().Key;
                }
            }
        });
        setFavoredJob(_teammateStats, true);
        setFavoredJob(_enemyStats, false);
        _jobStats = _jobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _allyJobStats = _allyJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _enemyJobStats = _enemyJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _teammateStats = _teammateStats.OrderByDescending(x => 2 * x.Value.Wins - x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _enemyStats = _enemyStats.OrderByDescending(x => x.Value.Matches - 2 * x.Value.Wins).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        //calculate average stats
        if(_statsEligibleMatches > 0) {
            _averageKills = (float)totalStats.Kills / _statsEligibleMatches;
            _averageDeaths = (float)totalStats.Deaths / _statsEligibleMatches;
            _averageAssists = (float)totalStats.Assists / _statsEligibleMatches;
            _averageDamageDealt = (float)totalStats.DamageDealt / _statsEligibleMatches;
            _averageDamageTaken = (float)totalStats.DamageTaken / _statsEligibleMatches;
            _averageHPRestored = (float)totalStats.HPRestored / _statsEligibleMatches;
            _averageTimeOnCrystal = totalStats.TimeOnCrystal / _statsEligibleMatches;
            _averageMatchLengthStats = totalStatsMatchLength / _statsEligibleMatches;

            _killsPerMin = _averageKills / _averageMatchLengthStats.TotalMinutes;
            _deathsPerMin = _averageDeaths / _averageMatchLengthStats.TotalMinutes;
            _assistsPerMin = _averageAssists / _averageMatchLengthStats.TotalMinutes;
            _damageDealtPerMin = _averageDamageDealt / _averageMatchLengthStats.TotalMinutes;
            _damageTakenPerMin = _averageDamageTaken / _averageMatchLengthStats.TotalMinutes;
            _hpRestoredPerMin = _averageHPRestored / _averageMatchLengthStats.TotalMinutes;
            _timeOnCrystalPerMin = _averageTimeOnCrystal / _averageMatchLengthStats.TotalMinutes;

            _killContribution = killContribTotal / _statsEligibleMatches;
            _deathContribution = deathContribTotal / _statsEligibleMatches;
            _assistContribution = assistContribTotal / _statsEligibleMatches;
            _damageDealtContribution = ddContribTotal / _statsEligibleMatches;
            _damageTakenContribution = dtContribTotal / _statsEligibleMatches;
            _hpRestoredContribution = hpContribTotal / _statsEligibleMatches;
            _timeOnCrystalContribution = timeContribTotal / _statsEligibleMatches;
        }
    }

    public void Draw() {
        if (_totalMatches > 0) {
            DrawResultTable();
        }

        if (_jobStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(ImGuiColors.DalamudYellow, "Jobs Played:");
            DrawJobTable(_jobStats);
            ImGui.Separator();
            ImGui.TextColored(ImGuiColors.DalamudYellow, "Teammates' Jobs Played:");
            DrawJobTable(_allyJobStats);
            ImGui.Separator();
            ImGui.TextColored(ImGuiColors.DalamudYellow, "Opponents' Jobs Played:");
            DrawJobTable(_enemyJobStats);
        }

        if (_teammateStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(ImGuiColors.DalamudYellow, "Top Teammates:");
            DrawPlayerStatsTable(_teammateStats);
        }

        if (_enemyStats.Count > 0) {
            ImGui.Separator();
            ImGui.TextColored(ImGuiColors.DalamudYellow, "Top Opponents:");
            DrawPlayerStatsTable(_enemyStats);
        }

        if(_statsEligibleMatches > 0) {
            ImGui.Separator();
            ImGui.TextColored(ImGuiColors.DalamudYellow, "Average Stats:");
            ImGui.SameLine();
            ImGui.Text($"Eligible matches: {_statsEligibleMatches}");
            ImGui.SameLine();
            ImGui.Text($"Eligible wins: {_statsEligibleWins}");
            ImGui.SameLine();
            ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: average team contribution per match.");
            DrawMatchStatsTable();
        }
    }

    private void DrawResultTable() {
        if (ImGui.BeginTable($"StatsSummary", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
            ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

            ImGui.TableNextColumn();
            ImGui.Text("Matches: ");
            ImGui.TableNextColumn();
            ImGui.Text($"{_totalMatches.ToString("N0")}");
            ImGui.TableNextColumn();

            if (_totalMatches > 0) {
                ImGui.TableNextColumn();
                ImGui.Text("Wins: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{_totalWins.ToString("N0")}");
                ImGui.TableNextColumn();
                ImGui.Text($"{string.Format("{0:P}%", (double)_totalWins / (_totalWins + _totalLosses))}");

                ImGui.TableNextColumn();
                ImGui.Text("Losses: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{_totalLosses.ToString("N0")}");
                ImGui.TableNextColumn();

                if (_totalOther > 0) {
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

    private void DrawJobTable(Dictionary<Job, JobStats> jobStats) {
        if (ImGui.BeginTable($"JobTable", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            ImGui.TableSetupColumn("Job");
            ImGui.TableSetupColumn($"Matches");
            ImGui.TableSetupColumn($"Wins");
            ImGui.TableSetupColumn($"Win Rate");

            ImGui.TableHeadersRow();

            foreach(var job in jobStats) {
                ImGui.TableNextColumn();
                ImGui.Text($"{PlayerJobHelper.GetNameFromJob(job.Key)}");

                ImGui.TableNextColumn();
                ImGui.Text($"{job.Value.Matches}");

                ImGui.TableNextColumn();
                ImGui.Text($"{job.Value.Wins}");

                ImGui.TableNextColumn();
                if(job.Value.Matches > 0) {
                    ImGui.Text($"{string.Format("{0:P}%", (double)job.Value.Wins / job.Value.Matches)}");
                }
            }
            ImGui.EndTable();
        }
    }

    private void DrawPlayerStatsTable(Dictionary<PlayerAlias, PlayerStats> playerStats) {
        if (ImGui.BeginTable($"JobTable", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            ImGui.TableSetupColumn("Player");
            //ImGui.TableSetupColumn($"Home World");
            ImGui.TableSetupColumn($"Favored\nJob", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            //ImGui.TableSetupColumn($"Win Rate");

            ImGui.TableHeadersRow();

            for(int i = 0; i < playerStats.Count && i < 5; i++) {
                var player = playerStats.ElementAt(i);

                ImGui.TableNextColumn();
                ImGui.Text($"{player.Key.Name}");

                //ImGui.TableNextColumn();
                //ImGui.Text($"{player.Key.HomeWorld}");

                ImGui.TableNextColumn();
                ImGui.Text($"{player.Value.FavoredJob}");

                ImGui.TableNextColumn();
                ImGui.Text($"{player.Value.Matches}");

                ImGui.TableNextColumn();
                ImGui.Text($"{player.Value.Wins}");

                //ImGui.TableNextColumn();
                //if (player.Value.Matches > 0) {
                //    ImGui.Text($"{string.Format("{0:P}%", (double)player.Value.Wins / player.Value.Matches)}");
                //}
            }
            ImGui.EndTable();
        }
    }

    private void DrawMatchStatsTable() {
        if (ImGui.BeginTable($"MatchStatsTable", 7, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            ImGui.TableSetupColumn("Kills");
            ImGui.TableSetupColumn($"Deaths");
            ImGui.TableSetupColumn($"Assists");
            ImGui.TableSetupColumn("Damage\nDealt");
            ImGui.TableSetupColumn($"Damage\nTaken");
            ImGui.TableSetupColumn($"HP\nRestored");
            ImGui.TableSetupColumn("Time on\nCrystal");

            ImGui.TableHeadersRow();

            //nominal
            ImGui.TableNextColumn();
            ImGui.Text($"{_averageKills.ToString("0.##")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_averageDeaths.ToString("0.##")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_averageAssists.ToString("0.##")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_averageDamageDealt.ToString("#")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_averageDamageTaken.ToString("#")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_averageHPRestored.ToString("#")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_averageTimeOnCrystal.Minutes}{_averageTimeOnCrystal.ToString(@"\:ss")}");

            //per min
            ImGui.TableNextColumn();
            ImGui.Text($"{_killsPerMin.ToString("0.##")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_deathsPerMin.ToString("0.##")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_assistsPerMin.ToString("0.##")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_damageDealtPerMin.ToString("#")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_damageTakenPerMin.ToString("#")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_hpRestoredPerMin.ToString("#")}");
            ImGui.TableNextColumn();
            ImGui.Text($"{_timeOnCrystalPerMin.Minutes}{_timeOnCrystalPerMin.ToString(@"\:ss")}");

            //team contrib
            ImGui.TableNextColumn();
            ImGui.Text($"{string.Format("{0:P1}%", _killContribution)}");
            ImGui.TableNextColumn();
            ImGui.Text($"{string.Format("{0:P1}%", _deathContribution)}");
            ImGui.TableNextColumn();
            ImGui.Text($"{string.Format("{0:P1}%", _assistContribution)}");
            ImGui.TableNextColumn();
            ImGui.Text($"{string.Format("{0:P1}%", _damageDealtContribution)}");
            ImGui.TableNextColumn();
            ImGui.Text($"{string.Format("{0:P1}%", _damageTakenContribution)}");
            ImGui.TableNextColumn();
            ImGui.Text($"{string.Format("{0:P1}%", _hpRestoredContribution)}");
            ImGui.TableNextColumn();
            ImGui.Text($"{string.Format("{0:P1}%", _timeOnCrystalContribution)}");

            ImGui.EndTable();
        }
    }
}
