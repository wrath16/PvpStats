using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictSummary {

    private class JobStats {
        internal int Matches, Wins;
    }

    private class PlayerStats {
        internal int Matches, Wins;
        internal Job FavoredJob;
        internal Dictionary<Job, JobStats> JobStats = new();
    }

    private Plugin _plugin;
    private SemaphoreSlim _refreshLock = new SemaphoreSlim(1);

    private int _totalMatches, _totalWins, _totalLosses, _totalOther;
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
    private CrystallineConflictPostMatchRow _averageStats = new();

    public CrystallineConflictSummary(Plugin plugin) {
        _plugin = plugin;
    }

    public void Refresh(List<CrystallineConflictMatch> matches) {
        int totalMatches, totalWins, totalLosses, totalOther;
        Dictionary<Job, JobStats> jobStats = new();
        Dictionary<Job, JobStats> allyJobStats = new();
        Dictionary<Job, JobStats> enemyJobStats = new();
        Dictionary<PlayerAlias, PlayerStats> teammateStats = new();
        Dictionary<PlayerAlias, PlayerStats> enemyStats = new();
        int statsEligibleMatches;
        int statsEligibleWins;
        double averageKills = 0, averageDeaths = 0, averageAssists = 0, averageDamageDealt = 0, averageDamageTaken = 0, averageHPRestored = 0;
        double killsPerMin = 0, deathsPerMin = 0, assistsPerMin = 0, damageDealtPerMin = 0, damageTakenPerMin = 0, hpRestoredPerMin = 0;
        double killContribution = 0, deathContribution = 0, assistContribution = 0, damageDealtContribution = 0, damageTakenContribution = 0, hpRestoredContribution = 0, timeOnCrystalContribution = 0;
        TimeSpan averageTimeOnCrystal = TimeSpan.FromSeconds(0), averageMatchLengthStats = TimeSpan.FromSeconds(0), timeOnCrystalPerMin = TimeSpan.FromSeconds(0);

        totalMatches = matches.Count;
        totalWins = matches.Where(x => x.LocalPlayerTeam != null && x.MatchWinner != null && x.MatchWinner == x.LocalPlayerTeam.TeamName).Count();
        totalLosses = matches.Where(x => x.LocalPlayerTeam != null && x.MatchWinner != null && x.MatchWinner != x.LocalPlayerTeam.TeamName).Count();
        totalOther = totalMatches - totalWins - totalLosses;

        //_statsEligibleMatches = matches.Where(x => x.LocalPlayerTeam != null && x.PostMatch != null).Count();
        //_statsEligibleWins = matches.Where(x => x.LocalPlayerTeam != null && x.PostMatch != null && x.IsWin).Count();
        statsEligibleMatches = 0;
        statsEligibleWins = 0;

        jobStats = new();
        allyJobStats = new();
        enemyJobStats = new();
        teammateStats = new();
        enemyStats = new();
        CrystallineConflictPostMatchRow totalStats = new();
        double killContribTotal = 0, deathContribTotal = 0, assistContribTotal = 0, ddContribTotal = 0, dtContribTotal = 0, hpContribTotal = 0, timeContribTotal = 0;
        List<double> killContribList = new(), deathContribList = new(), assistContribList = new(), ddContribList = new(), dtContribList = new(), hpContribList = new(), timeContribList = new();
        TimeSpan totalStatsMatchLength = new();
        var addJobStat = ((Dictionary<Job, JobStats> jobStats, Job job, bool isWin) => {
            if(jobStats.ContainsKey(job)) {
                jobStats[job].Matches++;
                jobStats[job].Wins += isWin ? 1 : 0;
            } else {
                jobStats.Add(job, new() {
                    Matches = 1,
                    Wins = isWin ? 1 : 0
                });
            }
        });
        var addPlayerStat = ((Dictionary<PlayerAlias, PlayerStats> playerStats, PlayerAlias player, Job job, bool isWin) => {
            if(playerStats.ContainsKey(player)) {
                addJobStat(playerStats[player].JobStats, job, isWin);
                playerStats[player].Matches++;
                playerStats[player].Wins += isWin ? 1 : 0;
            } else {
                playerStats.Add(player, new() {
                    JobStats = new(),
                    Matches = 1,
                    Wins = isWin ? 1 : 0
                });
                addJobStat(playerStats[player].JobStats, job, isWin);
            }
        });
        foreach(var match in matches) {
            if(match.LocalPlayerTeamMember != null) {
                addJobStat(jobStats, match.LocalPlayerTeamMember.Job, match.IsWin);
                foreach(var team in match.Teams) {
                    if(team.Key == match.LocalPlayerTeam.TeamName) {
                        foreach(var player in team.Value.Players) {
                            if(!player.Alias.Equals(match.LocalPlayer)) {
                                addJobStat(allyJobStats, player.Job, match.IsWin);
                                addPlayerStat(teammateStats, player.Alias, player.Job, match.IsWin);
                            }
                        }
                    } else {
                        foreach(var player in team.Value.Players) {
                            addJobStat(enemyJobStats, player.Job, match.IsWin);
                            addPlayerStat(enemyStats, player.Alias, player.Job, match.IsWin);
                        }
                    }
                }
                if(match.PostMatch != null && match.MatchDuration != null) {
                    var playerTeamStats = match.PostMatch.Teams.Where(x => x.Key == match.LocalPlayerTeam!.TeamName).FirstOrDefault().Value;
                    var playerStats = playerTeamStats.PlayerStats.Where(x => x.Player.Equals(match.LocalPlayer)).FirstOrDefault();
                    if(playerStats != null) {
                        statsEligibleMatches++;
                        if(match.IsWin) {
                            statsEligibleWins++;
                        }
                        totalStatsMatchLength += (TimeSpan)match.MatchDuration;
                        totalStats.Kills += playerStats.Kills;
                        totalStats.Deaths += playerStats.Deaths;
                        totalStats.Assists += playerStats.Assists;
                        totalStats.DamageDealt += playerStats.DamageDealt;
                        totalStats.DamageTaken += playerStats.DamageTaken;
                        totalStats.HPRestored += playerStats.HPRestored;
                        totalStats.TimeOnCrystal += playerStats.TimeOnCrystal;

                        //killContribTotal += playerTeamStats.TeamStats.Kills != 0 ? (float)playerStats.Kills / playerTeamStats.TeamStats.Kills : 0;
                        //deathContribTotal += playerTeamStats.TeamStats.Deaths != 0 ? (float)playerStats.Deaths / playerTeamStats.TeamStats.Deaths : 0;
                        //assistContribTotal += playerTeamStats.TeamStats.Assists != 0 ? (float)playerStats.Assists / playerTeamStats.TeamStats.Assists : 0;
                        //ddContribTotal += playerTeamStats.TeamStats.DamageDealt != 0 ? (float)playerStats.DamageDealt / playerTeamStats.TeamStats.DamageDealt : 0;
                        //dtContribTotal += playerTeamStats.TeamStats.DamageTaken != 0 ? (float)playerStats.DamageTaken / playerTeamStats.TeamStats.DamageTaken : 0;
                        //hpContribTotal += playerTeamStats.TeamStats.HPRestored != 0 ? (float)playerStats.HPRestored / playerTeamStats.TeamStats.HPRestored : 0;
                        //timeContribTotal += playerTeamStats.TeamStats.TimeOnCrystal.Ticks != 0 ? playerStats.TimeOnCrystal / playerTeamStats.TeamStats.TimeOnCrystal : 0;

                        killContribList.Add(playerTeamStats.TeamStats.Kills != 0 ? (double)playerStats.Kills / playerTeamStats.TeamStats.Kills : 0);
                        deathContribList.Add(playerTeamStats.TeamStats.Deaths != 0 ? (double)playerStats.Deaths / playerTeamStats.TeamStats.Deaths : 0);
                        assistContribList.Add(playerTeamStats.TeamStats.Assists != 0 ? (double)playerStats.Assists / playerTeamStats.TeamStats.Assists : 0);
                        ddContribList.Add(playerTeamStats.TeamStats.DamageDealt != 0 ? (double)playerStats.DamageDealt / playerTeamStats.TeamStats.DamageDealt : 0);
                        dtContribList.Add(playerTeamStats.TeamStats.DamageTaken != 0 ? (double)playerStats.DamageTaken / playerTeamStats.TeamStats.DamageTaken : 0);
                        hpContribList.Add(playerTeamStats.TeamStats.HPRestored != 0 ? (double)playerStats.HPRestored / playerTeamStats.TeamStats.HPRestored : 0);
                        timeContribList.Add(playerTeamStats.TeamStats.TimeOnCrystal.Ticks != 0 ? playerStats.TimeOnCrystal / playerTeamStats.TeamStats.TimeOnCrystal : 0);
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
        setFavoredJob(teammateStats, true);
        setFavoredJob(enemyStats, false);
        jobStats = jobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        allyJobStats = allyJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        enemyJobStats = enemyJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        teammateStats = teammateStats.OrderBy(x => x.Value.Matches).OrderByDescending(x => 2 * x.Value.Wins - x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        enemyStats = enemyStats.OrderBy(x => x.Value.Matches).OrderByDescending(x => x.Value.Matches - 2 * x.Value.Wins).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        //calculate average stats
        if(statsEligibleMatches > 0) {
            averageKills = (float)totalStats.Kills / statsEligibleMatches;
            averageDeaths = (float)totalStats.Deaths / statsEligibleMatches;
            averageAssists = (float)totalStats.Assists / statsEligibleMatches;
            averageDamageDealt = (float)totalStats.DamageDealt / statsEligibleMatches;
            averageDamageTaken = (float)totalStats.DamageTaken / statsEligibleMatches;
            averageHPRestored = (float)totalStats.HPRestored / statsEligibleMatches;
            averageTimeOnCrystal = totalStats.TimeOnCrystal / statsEligibleMatches;
            averageMatchLengthStats = totalStatsMatchLength / statsEligibleMatches;

            killsPerMin = averageKills / averageMatchLengthStats.TotalMinutes;
            deathsPerMin = averageDeaths / averageMatchLengthStats.TotalMinutes;
            assistsPerMin = averageAssists / averageMatchLengthStats.TotalMinutes;
            damageDealtPerMin = averageDamageDealt / averageMatchLengthStats.TotalMinutes;
            damageTakenPerMin = averageDamageTaken / averageMatchLengthStats.TotalMinutes;
            hpRestoredPerMin = averageHPRestored / averageMatchLengthStats.TotalMinutes;
            timeOnCrystalPerMin = averageTimeOnCrystal / averageMatchLengthStats.TotalMinutes;

            //killContribution = killContribTotal / statsEligibleMatches;
            //deathContribution = deathContribTotal / statsEligibleMatches;
            //assistContribution = assistContribTotal / statsEligibleMatches;
            //damageDealtContribution = ddContribTotal / statsEligibleMatches;
            //damageTakenContribution = dtContribTotal / statsEligibleMatches;
            //hpRestoredContribution = hpContribTotal / statsEligibleMatches;
            //timeOnCrystalContribution = timeContribTotal / statsEligibleMatches;

            killContribution = killContribList.OrderBy(x => x).ElementAt(statsEligibleMatches / 2);
            deathContribution = deathContribList.OrderBy(x => x).ElementAt(statsEligibleMatches / 2); ;
            assistContribution = assistContribList.OrderBy(x => x).ElementAt(statsEligibleMatches / 2); ;
            damageDealtContribution = ddContribList.OrderBy(x => x).ElementAt(statsEligibleMatches / 2); ;
            damageTakenContribution = dtContribList.OrderBy(x => x).ElementAt(statsEligibleMatches / 2); ;
            hpRestoredContribution = hpContribList.OrderBy(x => x).ElementAt(statsEligibleMatches / 2); ;
            timeOnCrystalContribution = timeContribList.OrderBy(x => x).ElementAt(statsEligibleMatches / 2); ;
        }

        try {
            _refreshLock.WaitAsync();

            _totalMatches = totalMatches;
            _totalWins = totalWins;
            _totalLosses = totalLosses;
            _totalOther = totalOther;
            _jobStats = jobStats;
            _allyJobStats = allyJobStats;
            _enemyJobStats = enemyJobStats;
            _teammateStats = teammateStats;
            _enemyStats = enemyStats;
            _statsEligibleMatches = statsEligibleMatches;
            _statsEligibleWins = statsEligibleWins;
            _averageKills = averageKills;
            _averageDeaths = averageDeaths;
            _averageAssists = averageAssists;
            _averageDamageDealt = averageDamageDealt;
            _averageDamageTaken = averageDamageTaken;
            _averageHPRestored = averageHPRestored;
            _averageTimeOnCrystal = averageTimeOnCrystal;
            _killsPerMin = killsPerMin;
            _deathsPerMin = deathsPerMin;
            _assistsPerMin = assistsPerMin;
            _damageDealtPerMin = damageDealtPerMin;
            _damageTakenPerMin = damageTakenPerMin;
            _hpRestoredPerMin = hpRestoredPerMin;
            _timeOnCrystalPerMin = timeOnCrystalPerMin;
            _killContribution = killContribution;
            _deathContribution = deathContribution;
            _assistContribution = assistContribution;
            _damageDealtContribution = damageDealtContribution;
            _damageTakenContribution = damageTakenContribution;
            _hpRestoredContribution = hpRestoredContribution;
            _timeOnCrystalContribution = timeOnCrystalContribution;
        } finally {
            _refreshLock.Release();
        }
    }

    public void Draw() {
        if(!_refreshLock.Wait(0)) {
            return;
        }
        try {
            if(_totalMatches > 0) {
                DrawResultTable();
            }

            if(_jobStats.Count > 0) {
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

            if(_teammateStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Top Teammates:");
                DrawPlayerStatsTable(_teammateStats);
            }

            if(_enemyStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Top Opponents:");
                DrawPlayerStatsTable(_enemyStats);
            }

            if(_statsEligibleMatches > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Average Stats:");
                //ImGui.SameLine();
                //ImGui.Text($"Eligible matches: {_statsEligibleMatches}");
                //ImGui.SameLine();
                //ImGui.Text($"Eligible wins: {_statsEligibleWins}");
                //ImGui.SameLine();
                ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: median team contribution per match.");
                DrawMatchStatsTable();
            }
        } finally {
            _refreshLock.Release();
        }
    }

    private void DrawResultTable() {
        if(ImGui.BeginTable($"StatsSummary", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
            ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
            ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

            ImGui.TableNextColumn();
            ImGui.Text("Matches: ");
            ImGui.TableNextColumn();
            ImGui.Text($"{_totalMatches.ToString("N0")}");
            ImGui.TableNextColumn();

            if(_totalMatches > 0) {
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

                if(_totalOther > 0) {
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
        if(ImGui.BeginTable($"JobTable", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
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
        if(ImGui.BeginTable($"JobTable", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
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
        if(ImGui.BeginTable($"MatchStatsTable", 7, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
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
