using Dalamud.Utility;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Managers.Stats;
internal class CrystallineConflictStatsManager {

    private readonly Plugin _plugin;
    internal protected SemaphoreSlim RefreshLock { get; private set; } = new SemaphoreSlim(1);

    //internal List<DataFilter> Filters { get; private set; } = new();

    //matches
    internal List<CrystallineConflictMatch> Matches { get; private set; } = new();

    //summary
    internal CCPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> LocalPlayerJobStats { get; private set; } = new();
    internal Dictionary<CrystallineConflictMap, CCAggregateStats> ArenaStats { get; private set; } = new();
    internal Dictionary<PlayerAlias, CCAggregateStats> TeammateStats { get; private set; } = new();
    internal Dictionary<PlayerAlias, CCAggregateStats> OpponentStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> TeammateJobStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> OpponentJobStats { get; private set; } = new();
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    //records
    internal Dictionary<CrystallineConflictMatch, List<(string, string)>> Superlatives = new();
    internal int LongestWinStreak { get; private set; }
    internal int LongestLossStreak { get; private set; }

    //jobs
    internal List<Job> Jobs { get; private set; } = new();
    internal Dictionary<Job, CCPlayerJobStats> JobStats { get; private set; } = new();

    //players
    internal List<PlayerAlias> Players { get; private set; } = new();
    internal Dictionary<PlayerAlias, CCPlayerJobStats> PlayerStats { get; private set; } = new();
    internal Dictionary<PlayerAlias, List<PlayerAlias>> ActiveLinks { get; private set; } = new();

    internal CrystallineConflictStatsManager(Plugin plugin) {
        _plugin = plugin;
    }

    internal async Task Refresh(List<DataFilter> matchFilters, StatSourceFilter jobStatSourceFilter, bool playerStatSourceInherit) {
        Stopwatch s0 = new();
        s0.Start();
        List<Job> jobs = new();
        Dictionary<Job, CCPlayerJobStats> jobStats = new();
        Dictionary<Job, List<CCScoreboardDouble>> jobTeamContributions = new();
        List<PlayerAlias> players = new();
        Dictionary<PlayerAlias, CCPlayerJobStats> playerStats = new();
        Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> playerJobStatsLookup = new();
        Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> teammateJobStatsLookup = new();
        Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> opponentJobStatsLookup = new();
        Dictionary<PlayerAlias, List<CCScoreboardDouble>> playerTeamContributions = new();
        CCPlayerJobStats localPlayerStats = new();
        List<CCScoreboardDouble> localPlayerTeamContributions = new();
        Dictionary<CrystallineConflictMap, CCAggregateStats> arenaStats = new();
        Dictionary<PlayerAlias, CCAggregateStats> teammateStats = new();
        Dictionary<PlayerAlias, CCAggregateStats> opponentStats = new();
        Dictionary<Job, CCAggregateStats> localPlayerJobStats = new();
        Dictionary<Job, CCAggregateStats> teammateJobStats = new();
        Dictionary<Job, CCAggregateStats> opponentJobStats = new();
        Dictionary<PlayerAlias, List<PlayerAlias>> activeLinks = new();
        Dictionary<CrystallineConflictMatch, List<(string, string)>> superlatives = new();
        CrystallineConflictMatch? longestMatch = null, shortestMatch = null, highestLoserProg = null, lowestWinnerProg = null, closestWin = null, closestLoss = null,
            mostKills = null, mostDeaths = null, mostAssists = null, mostDamageDealt = null, mostDamageTaken = null, mostHPRestored = null, mostTimeOnCrystal = null,
            highestKillsPerMin = null, highestDeathsPerMin = null, highestAssistsPerMin = null, highestDamageDealtPerMin = null, highestDamageTakenPerMin = null, highestHPRestoredPerMin = null, highestTimeOnCrystalPerMin = null;
        int longestWinStreak = 0, longestLossStreak = 0, spectatedMatchCount = 0, currentWinStreak = 0, currentLossStreak = 0;
        TimeSpan totalMatchTime = TimeSpan.Zero;

        var matches = FilterMatches(matchFilters);
        var playerFilter = (OtherPlayerFilter)matchFilters.First(x => x.GetType() == typeof(OtherPlayerFilter));
        var linkedPlayerAliases = _plugin.PlayerLinksService.GetAllLinkedAliases(playerFilter.PlayerNamesRaw);
        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        foreach(var job in allJobs) {
            jobStats.Add(job, new());
            jobTeamContributions.Add(job, new());
        }
        Stopwatch s1 = new();
        s1.Start();

        Stopwatch recordsWatch = new();
        Stopwatch arenaWatch = new();
        Stopwatch localPlayerWatch = new();
        Stopwatch teamPlayerWatch = new();
        Stopwatch aggregateStatsWatch = new();
        Stopwatch playerJobWatch = new();

        foreach(var match in matches) {
            totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;
            //process records
            recordsWatch.Start();
            //track these for spectated matches as well
            if(longestMatch == null) {
                longestMatch = match;
                shortestMatch = match;
                highestLoserProg = match;
            }
            if(longestMatch == null || match.MatchDuration > longestMatch.MatchDuration) {
                longestMatch = match;
            }
            if(shortestMatch == null || match.MatchDuration < shortestMatch.MatchDuration) {
                shortestMatch = match;
            }
            if(highestLoserProg == null || match.LoserProgress > highestLoserProg.LoserProgress) {
                highestLoserProg = match;
            }
            if(lowestWinnerProg == null || match.WinnerProgress < lowestWinnerProg.WinnerProgress) {
                lowestWinnerProg = match;
            }

            if(match.IsSpectated) {
                spectatedMatchCount++;
                //continue;
            } else {
                if(mostKills == null || match.LocalPlayerStats?.Kills > mostKills.LocalPlayerStats?.Kills
                    || (match.LocalPlayerStats?.Kills == mostKills.LocalPlayerStats?.Kills && match.MatchDuration < mostKills.MatchDuration)) {
                    mostKills = match;
                }
                if(mostDeaths == null || match.LocalPlayerStats?.Deaths > mostDeaths.LocalPlayerStats?.Deaths
                    || (match.LocalPlayerStats?.Deaths == mostDeaths.LocalPlayerStats?.Deaths && match.MatchDuration < mostDeaths.MatchDuration)) {
                    mostDeaths = match;
                }
                if(mostAssists == null || match.LocalPlayerStats?.Assists > mostAssists.LocalPlayerStats?.Assists
                    || (match.LocalPlayerStats?.Assists == mostAssists.LocalPlayerStats?.Assists && match.MatchDuration < mostAssists.MatchDuration)) {
                    mostAssists = match;
                }
                if(mostDamageDealt == null || match.LocalPlayerStats?.DamageDealt > mostDamageDealt.LocalPlayerStats?.DamageDealt) {
                    mostDamageDealt = match;
                }
                if(mostDamageTaken == null || match.LocalPlayerStats?.DamageTaken > mostDamageTaken.LocalPlayerStats?.DamageTaken) {
                    mostDamageTaken = match;
                }
                if(mostHPRestored == null || match.LocalPlayerStats?.HPRestored > mostHPRestored.LocalPlayerStats?.HPRestored) {
                    mostHPRestored = match;
                }
                if(mostTimeOnCrystal == null || match.LocalPlayerStats?.TimeOnCrystal > mostTimeOnCrystal.LocalPlayerStats?.TimeOnCrystal) {
                    mostTimeOnCrystal = match;
                }
                if(match.MatchDuration != null && match.LocalPlayerStats != null) {
                    if(highestKillsPerMin == null || (float)match.LocalPlayerStats?.Kills! / match.MatchDuration.Value.TotalMinutes > (float)highestKillsPerMin.LocalPlayerStats?.Kills! / highestKillsPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestKillsPerMin = match;
                    }
                    if(highestDeathsPerMin == null || (float)match.LocalPlayerStats?.Deaths! / match.MatchDuration.Value.TotalMinutes > (float)highestDeathsPerMin.LocalPlayerStats?.Deaths! / highestDeathsPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestDeathsPerMin = match;
                    }
                    if(highestAssistsPerMin == null || (float)match.LocalPlayerStats?.Assists! / match.MatchDuration.Value.TotalMinutes > (float)highestAssistsPerMin.LocalPlayerStats?.Assists! / highestAssistsPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestAssistsPerMin = match;
                    }
                    if(highestDamageDealtPerMin == null || (float)match.LocalPlayerStats?.DamageDealt! / match.MatchDuration.Value.TotalMinutes > (float)highestDamageDealtPerMin.LocalPlayerStats?.DamageDealt! / highestDamageDealtPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestDamageDealtPerMin = match;
                    }
                    if(highestDamageTakenPerMin == null || (float)match.LocalPlayerStats?.DamageTaken! / match.MatchDuration.Value.TotalMinutes > (float)highestDamageTakenPerMin.LocalPlayerStats?.DamageTaken! / highestDamageTakenPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestDamageTakenPerMin = match;
                    }
                    if(highestHPRestoredPerMin == null || (float)match.LocalPlayerStats?.HPRestored! / match.MatchDuration.Value.TotalMinutes > (float)highestHPRestoredPerMin.LocalPlayerStats?.HPRestored! / highestHPRestoredPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestHPRestoredPerMin = match;
                    }
                    if(highestTimeOnCrystalPerMin == null || match.LocalPlayerStats?.TimeOnCrystal / match.MatchDuration.Value.TotalMinutes > highestTimeOnCrystalPerMin.LocalPlayerStats?.TimeOnCrystal / highestTimeOnCrystalPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestTimeOnCrystalPerMin = match;
                    }
                }

                if(match.IsWin && (closestWin == null || match.LoserProgress > closestWin.LoserProgress)) {
                    closestWin = match;
                }
                if(match.IsLoss && (closestLoss == null || match.LoserProgress > closestLoss.LoserProgress)) {
                    closestLoss = match;
                }

                if(match.IsWin) {
                    currentWinStreak++;
                    if(currentWinStreak > longestWinStreak) {
                        longestWinStreak = currentWinStreak;
                    }
                } else {
                    currentWinStreak = 0;
                }
                if(match.IsLoss) {
                    currentLossStreak++;
                    if(currentLossStreak > longestLossStreak) {
                        longestLossStreak = currentLossStreak;
                    }
                } else {
                    currentLossStreak = 0;
                }
            }
            recordsWatch.Stop();

            //local player stats
            localPlayerWatch.Start();
            if(!match.IsSpectated && match.PostMatch != null) {
                AddPlayerJobStat(localPlayerStats, localPlayerTeamContributions, match, match.LocalPlayerTeam!, match.LocalPlayerTeamMember!);
                if(match.LocalPlayerTeamMember!.Job != null) {
                    var job = (Job)match.LocalPlayerTeamMember!.Job;
                    if(!localPlayerJobStats.ContainsKey(job)) {
                        localPlayerJobStats.Add(job, new());
                    }
                    IncrementAggregateStats(localPlayerJobStats[job], match);
                }
            }
            localPlayerWatch.Stop();

            //arena stats
            arenaWatch.Start();
            if(match.Arena != null) {
                var arena = (CrystallineConflictMap)match.Arena;
                if(!arenaStats.ContainsKey(arena)) {
                    arenaStats.Add(arena, new());
                }
                IncrementAggregateStats(arenaStats[arena], match);
            }
            arenaWatch.Stop();

            //process player and job stats
            teamPlayerWatch.Start();
            foreach(var team in match.Teams) {
                foreach(var player in team.Value.Players) {
                    bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
                    bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.Key == match.LocalPlayerTeam!.TeamName;
                    bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;
                    bool jobStatsEligible = true;
                    bool playerStatsEligible = true;
                    bool nameMatch = player.Alias.FullName.Contains(playerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                    if(_plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                        nameMatch = linkedPlayerAliases.Contains(player.Alias);
                    }
                    bool sideMatch = playerFilter.TeamStatus == TeamStatus.Any
                        || playerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                        || playerFilter.TeamStatus == TeamStatus.Opponent && !isTeammate && !isLocalPlayer;
                    bool jobMatch = playerFilter.AnyJob || playerFilter.PlayerJob == player.Job;
                    if(!nameMatch || !sideMatch || !jobMatch) {
                        if(jobStatSourceFilter.InheritFromPlayerFilter) {
                            jobStatsEligible = false;
                        }
                        if(playerStatSourceInherit) {
                            playerStatsEligible = false;
                        }
                    }
                    if(player.Job == null) {
                        jobStatsEligible = false;
                    }

                    if(!jobStatSourceFilter.FilterState[StatSource.LocalPlayer] && isLocalPlayer) {
                        jobStatsEligible = false;
                    } else if(!jobStatSourceFilter.FilterState[StatSource.Teammate] && isTeammate) {
                        jobStatsEligible = false;
                    } else if(!jobStatSourceFilter.FilterState[StatSource.Opponent] && !isTeammate && !isLocalPlayer) {
                        jobStatsEligible = false;
                    } else if(!jobStatSourceFilter.FilterState[StatSource.Spectated] && match.IsSpectated) {
                        jobStatsEligible = false;
                    }
                    var job = (Job)player.Job!;

                    aggregateStatsWatch.Start();
                    if(isTeammate) {
                        if(!teammateStats.TryGetValue(player.Alias, out CCAggregateStats? teammateStat)) {
                            teammateStat = new();
                            teammateStats.Add(player.Alias, teammateStat);
                        }
                        IncrementAggregateStats(teammateStat, match);
                        if(player.Job != null) {
                            if(!teammateJobStats.TryGetValue(job, out CCAggregateStats? teammateJobStat)) {
                                teammateJobStat = new();
                                teammateJobStats.Add(job, teammateJobStat);
                            }
                            IncrementAggregateStats(teammateJobStat, match);
                            if(!teammateJobStatsLookup.TryGetValue(player.Alias, out Dictionary<Job, CCAggregateStats>? teammateJobStatLookup)) {
                                teammateJobStatLookup = new();
                                teammateJobStatsLookup.Add(player.Alias, teammateJobStatLookup);
                            }
                            if(!teammateJobStatLookup.TryGetValue(job, out CCAggregateStats? teammateJobStatLookupJobStat)) {
                                teammateJobStatLookupJobStat = new();
                                teammateJobStatLookup.Add(job, teammateJobStatLookupJobStat);
                            }
                            IncrementAggregateStats(teammateJobStatLookupJobStat, match);
                        }
                    } else if(isOpponent) {
                        if(!opponentStats.TryGetValue(player.Alias, out CCAggregateStats? opponentStat)) {
                            opponentStat = new();
                            opponentStats.Add(player.Alias, opponentStat);
                        }
                        IncrementAggregateStats(opponentStat, match);
                        if(player.Job != null) {
                            if(!opponentJobStats.TryGetValue(job, out CCAggregateStats? opponentJobStat)) {
                                opponentJobStat = new();
                                opponentJobStats.Add(job, opponentJobStat);
                            }
                            IncrementAggregateStats(opponentJobStat, match);
                        }
                        if(!opponentJobStatsLookup.TryGetValue(player.Alias, out Dictionary<Job, CCAggregateStats>? opponentJobStatLookup)) {
                            opponentJobStatLookup = new();
                            opponentJobStatsLookup.Add(player.Alias, opponentJobStatLookup);
                        }
                        if(!opponentJobStatLookup.TryGetValue(job, out CCAggregateStats? opponentJobStatLookupJobStat)) {
                            opponentJobStatLookupJobStat = new();
                            opponentJobStatLookup.Add(job, opponentJobStatLookupJobStat);
                        }
                        IncrementAggregateStats(opponentJobStatLookupJobStat, match);
                    }
                    aggregateStatsWatch.Stop();

                    playerJobWatch.Start();
                    if(jobStatsEligible) {
                        AddPlayerJobStat(jobStats[job], jobTeamContributions[job], match, team.Value, player);
                    }

                    if(playerStatsEligible) {
                        if(!playerStats.TryGetValue(player.Alias, out CCPlayerJobStats? playerStat)) {
                            playerStat = new();
                            playerStats.Add(player.Alias, playerStat);
                            playerTeamContributions.Add(player.Alias, new());
                            playerJobStatsLookup.Add(player.Alias, new());
                        }
                        AddPlayerJobStat(playerStat, playerTeamContributions[player.Alias], match, team.Value, player);
                        if(player.Job != null) {
                            if(!playerJobStatsLookup[player.Alias].ContainsKey((Job)player.Job)) {
                                playerJobStatsLookup[player.Alias].Add((Job)player.Job, new());
                            }
                            IncrementAggregateStats(playerJobStatsLookup[player.Alias][(Job)player.Job], match);
                        }
                    }
                    playerJobWatch.Stop();
                }
            }
            teamPlayerWatch.Stop();
        }
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"records", recordsWatch.ElapsedMilliseconds.ToString()));
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"arena stats", arenaWatch.ElapsedMilliseconds.ToString()));
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"local player stats", localPlayerWatch.ElapsedMilliseconds.ToString()));
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"summary stats loop", aggregateStatsWatch.ElapsedMilliseconds.ToString()));
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"player/job stats loop", playerJobWatch.ElapsedMilliseconds.ToString()));
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"team player loop total", teamPlayerWatch.ElapsedMilliseconds.ToString()));
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"cc match loop total", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();

        //player linking
        if(_plugin.Configuration.EnablePlayerLinking) {
            //var manualLinks = _plugin.Storage.GetManualLinks().Query().ToList();
            var unLinks = _plugin.PlayerLinksService.ManualPlayerLinksCache.Where(x => x.IsUnlink).ToList();
            var checkPlayerLink = (PlayerAliasLink playerLink) => {
                if(playerLink.IsUnlink) return;
                if(playerLink.CurrentAlias is null) return;
                foreach(var linkedAlias in playerLink.LinkedAliases) {
                    bool blocked = unLinks.Where(x => x.CurrentAlias?.Equals(playerLink.CurrentAlias) ?? false && x.LinkedAliases.Contains(linkedAlias)).Any();
                    if(!blocked) {
                        bool anyMatch = false;
                        if(playerStats.ContainsKey(linkedAlias)) {
                            anyMatch = true;
                            if(playerStats.ContainsKey(playerLink.CurrentAlias)) {
                                playerStats[playerLink.CurrentAlias] += playerStats[linkedAlias];
                                playerTeamContributions[playerLink.CurrentAlias] = playerTeamContributions[playerLink.CurrentAlias].Concat(playerTeamContributions[linkedAlias]).ToList();
                                foreach(var jobStat in playerJobStatsLookup[linkedAlias]) {
                                    if(!playerJobStatsLookup[playerLink.CurrentAlias].ContainsKey(jobStat.Key)) {
                                        playerJobStatsLookup[playerLink.CurrentAlias].Add(jobStat.Key, new() {
                                            Matches = jobStat.Value.Matches,
                                        });
                                    } else {
                                        playerJobStatsLookup[playerLink.CurrentAlias][jobStat.Key].Matches += jobStat.Value.Matches;
                                    }
                                }
                            } else {
                                playerStats.Add(playerLink.CurrentAlias, playerStats[linkedAlias]);
                                playerTeamContributions.Add(playerLink.CurrentAlias, playerTeamContributions[linkedAlias]);
                                playerJobStatsLookup.Add(playerLink.CurrentAlias, playerJobStatsLookup[linkedAlias]);
                            }
                            playerStats.Remove(linkedAlias);
                            playerTeamContributions.Remove(linkedAlias);
                            playerJobStatsLookup.Remove(linkedAlias);
                        }
                        if(teammateStats.ContainsKey(linkedAlias)) {
                            anyMatch = true;
                            if(teammateStats.ContainsKey(playerLink.CurrentAlias)) {
                                teammateStats[playerLink.CurrentAlias] += teammateStats[linkedAlias];
                                foreach(var jobStat in teammateJobStatsLookup[linkedAlias]) {
                                    if(!teammateJobStatsLookup[playerLink.CurrentAlias].ContainsKey(jobStat.Key)) {
                                        teammateJobStatsLookup[playerLink.CurrentAlias].Add(jobStat.Key, new() {
                                            Matches = jobStat.Value.Matches,
                                        });
                                    } else {
                                        teammateJobStatsLookup[playerLink.CurrentAlias][jobStat.Key].Matches += jobStat.Value.Matches;
                                    }
                                }
                            } else {
                                teammateStats.Add(playerLink.CurrentAlias, teammateStats[linkedAlias]);
                                teammateJobStatsLookup.Add(playerLink.CurrentAlias, teammateJobStatsLookup[linkedAlias]);
                            }
                            teammateStats.Remove(linkedAlias);
                            teammateJobStatsLookup.Remove(linkedAlias);
                        }
                        if(opponentStats.ContainsKey(linkedAlias)) {
                            anyMatch = true;
                            if(opponentStats.ContainsKey(playerLink.CurrentAlias)) {
                                opponentStats[playerLink.CurrentAlias] += opponentStats[linkedAlias];
                                foreach(var jobStat in opponentJobStatsLookup[linkedAlias]) {
                                    if(!opponentJobStatsLookup[playerLink.CurrentAlias].ContainsKey(jobStat.Key)) {
                                        opponentJobStatsLookup[playerLink.CurrentAlias].Add(jobStat.Key, new() {
                                            Matches = jobStat.Value.Matches,
                                        });
                                    } else {
                                        opponentJobStatsLookup[playerLink.CurrentAlias][jobStat.Key].Matches += jobStat.Value.Matches;
                                    }
                                }
                            } else {
                                opponentStats.Add(playerLink.CurrentAlias, opponentStats[linkedAlias]);
                                opponentJobStatsLookup.Add(playerLink.CurrentAlias, opponentJobStatsLookup[linkedAlias]);
                            }
                            opponentStats.Remove(linkedAlias);
                            opponentJobStatsLookup.Remove(linkedAlias);
                        }
                        if(anyMatch) {
                            _plugin.Log.Debug($"Coalescing {linkedAlias} into {playerLink.CurrentAlias}...");
                            if(activeLinks.ContainsKey(playerLink.CurrentAlias)) {
                                activeLinks[playerLink.CurrentAlias].Add(linkedAlias);
                            } else {
                                activeLinks.Add(playerLink.CurrentAlias, new() { linkedAlias });
                            }
                            if(activeLinks.ContainsKey(linkedAlias)) {
                                activeLinks[linkedAlias].Where(x => !x.Equals(playerLink.CurrentAlias)).ToList().ForEach(x => activeLinks[playerLink.CurrentAlias].Add(x));
                            }
                        }
                    }
                }
            };

            //auto links
            if(_plugin.Configuration.EnableAutoPlayerLinking) {
                foreach(var playerLink in _plugin.PlayerLinksService.AutoPlayerLinksCache) {
                    try {
                        checkPlayerLink(playerLink);
                    } catch(Exception e) {
                        _plugin.Log.Error($"Unable to add player link: {e.GetType()} {e.Message}\n {e.StackTrace}");
                    }
                }
            }

            //manual links
            if(_plugin.Configuration.EnableManualPlayerLinking) {
                foreach(var playerLink in _plugin.PlayerLinksService.ManualPlayerLinksCache) {
                    try {
                        checkPlayerLink(playerLink);
                    } catch(Exception e) {
                        _plugin.Log.Error($"Unable to add player link: {e.GetType()} {e.Message}\n {e.StackTrace}");
                    }
                }
            }
            _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"player linking", s1.ElapsedMilliseconds.ToString()));
            s1.Restart();
        }

        foreach(var jobStat in jobStats) {
            SetScoreboardStats(jobStat.Value, jobTeamContributions[jobStat.Key]);
        }
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"job scoreboards", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();

        foreach(var playerStat in playerStats) {
            playerStat.Value.StatsAll.Job = playerJobStatsLookup[playerStat.Key].OrderByDescending(x => x.Value.Matches).FirstOrDefault().Key;
            SetScoreboardStats(playerStat.Value, playerTeamContributions[playerStat.Key]);
        }
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"player scoreboards", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();

        SetScoreboardStats(localPlayerStats, localPlayerTeamContributions);
        foreach(var teammateStat in teammateStats) {
            teammateStat.Value.Job = teammateJobStatsLookup[teammateStat.Key].OrderByDescending(x => x.Value.WinDiff).FirstOrDefault().Key;
        }
        foreach(var opponentStat in opponentStats) {
            opponentStat.Value.Job = opponentJobStatsLookup[opponentStat.Key].OrderBy(x => x.Value.WinDiff).FirstOrDefault().Key;
        }

        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
            Players = playerStats.Keys.ToList();
            PlayerStats = playerStats;
            ActiveLinks = activeLinks;
            Jobs = jobStats.Keys.Where(x => x != Job.VPR && x != Job.PIC).ToList();
            JobStats = jobStats;
            LocalPlayerStats = localPlayerStats;
            LocalPlayerJobStats = localPlayerJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            TeammateStats = teammateStats.OrderBy(x => x.Value.Matches).OrderByDescending(x => x.Value.WinDiff).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            TeammateJobStats = teammateJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            OpponentStats = opponentStats.OrderBy(x => x.Value.Matches).OrderBy(x => x.Value.WinDiff).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            OpponentJobStats = opponentJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            ArenaStats = arenaStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            AverageMatchDuration = matches.Count > 0 ? totalMatchTime / matches.Count : TimeSpan.Zero;
            Superlatives = new();
            if(longestMatch != null) {
                AddSuperlative(longestMatch, "Longest match", ImGuiHelper.GetTimeSpanString((TimeSpan)longestMatch.MatchDuration!));
                AddSuperlative(shortestMatch, "Shortest match", ImGuiHelper.GetTimeSpanString((TimeSpan)shortestMatch!.MatchDuration!));
                AddSuperlative(highestLoserProg, "Highest loser progress", highestLoserProg!.LoserProgress!.ToString()!);
                AddSuperlative(lowestWinnerProg, "Lowest winner progress", lowestWinnerProg!.WinnerProgress!.ToString()!);
                if(mostKills != null) {
                    AddSuperlative(mostKills, "Most kills", mostKills!.LocalPlayerStats!.Kills.ToString());
                    AddSuperlative(mostDeaths, "Most deaths", mostDeaths!.LocalPlayerStats!.Deaths.ToString());
                    AddSuperlative(mostAssists, "Most assists", mostAssists!.LocalPlayerStats!.Assists.ToString());
                    AddSuperlative(mostDamageDealt, "Most damage dealt", mostDamageDealt!.LocalPlayerStats!.DamageDealt.ToString());
                    AddSuperlative(mostDamageTaken, "Most damage taken", mostDamageTaken!.LocalPlayerStats!.DamageTaken.ToString());
                    AddSuperlative(mostHPRestored, "Most HP restored", mostHPRestored!.LocalPlayerStats!.HPRestored.ToString());
                    AddSuperlative(mostTimeOnCrystal, "Longest time on crystal", ImGuiHelper.GetTimeSpanString(mostTimeOnCrystal!.LocalPlayerStats!.TimeOnCrystal));
                    AddSuperlative(highestKillsPerMin, "Highest kills per min", (highestKillsPerMin!.LocalPlayerStats!.Kills / highestKillsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                    AddSuperlative(highestDeathsPerMin, "Highest deaths per min", (highestDeathsPerMin!.LocalPlayerStats!.Deaths / highestDeathsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                    AddSuperlative(highestAssistsPerMin, "Highest assists per min", (highestAssistsPerMin!.LocalPlayerStats!.Assists / highestAssistsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                    AddSuperlative(highestDamageDealtPerMin, "Highest damage dealt per min", (highestDamageDealtPerMin!.LocalPlayerStats!.DamageDealt / highestDamageDealtPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                    AddSuperlative(highestDamageTakenPerMin, "Highest damage taken per min", (highestDamageTakenPerMin!.LocalPlayerStats!.DamageTaken / highestDamageTakenPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                    AddSuperlative(highestHPRestoredPerMin, "Highest HP restored per min", (highestHPRestoredPerMin!.LocalPlayerStats!.HPRestored / highestHPRestoredPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                    AddSuperlative(highestTimeOnCrystalPerMin, "Longest time on crystal per min", ImGuiHelper.GetTimeSpanString(highestTimeOnCrystalPerMin!.LocalPlayerStats!.TimeOnCrystal / highestTimeOnCrystalPerMin!.MatchDuration!.Value.TotalMinutes));
                }
            }
            LongestWinStreak = longestWinStreak;
            LongestLossStreak = longestLossStreak;
        } finally {
            RefreshLock.Release();
        }
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"total stats refresh", s0.ElapsedMilliseconds.ToString()));
    }

    internal void AddPlayerJobStat(CCPlayerJobStats statsModel, List<CCScoreboardDouble> teamContributions,
        CrystallineConflictMatch match, CrystallineConflictTeam team, CrystallineConflictPlayer player) {
        bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
        bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.TeamName == match.LocalPlayerTeam!.TeamName;
        bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;

        statsModel.StatsAll.Matches++;
        if(match.MatchWinner == team.TeamName) {
            statsModel.StatsAll.Wins++;
        } else if(match.MatchWinner != null) {
            statsModel.StatsAll.Losses++;
        }

        if(!match.IsSpectated) {
            if(isTeammate) {
                IncrementAggregateStats(statsModel.StatsTeammate, match);
            } else if(isOpponent) {
                IncrementAggregateStats(statsModel.StatsOpponent, match);
            }
        }

        if(match.PostMatch != null) {
            var playerTeamScoreboard = match.PostMatch.Teams.Where(x => x.Key == team.TeamName).FirstOrDefault().Value;
            var playerScoreboard = playerTeamScoreboard.PlayerStats.Where(x => x.Player?.Equals(player.Alias) ?? false).FirstOrDefault();
            if(playerScoreboard != null) {
                statsModel.ScoreboardTotal.MatchTime += match.PostMatch.MatchDuration;
                statsModel.ScoreboardTotal.Kills += (ulong)playerScoreboard.Kills;
                statsModel.ScoreboardTotal.Deaths += (ulong)playerScoreboard.Deaths;
                statsModel.ScoreboardTotal.Assists += (ulong)playerScoreboard.Assists;
                statsModel.ScoreboardTotal.DamageDealt += (ulong)playerScoreboard.DamageDealt;
                statsModel.ScoreboardTotal.DamageTaken += (ulong)playerScoreboard.DamageTaken;
                statsModel.ScoreboardTotal.HPRestored += (ulong)playerScoreboard.HPRestored;
                statsModel.ScoreboardTotal.TimeOnCrystal += playerScoreboard.TimeOnCrystal;

                teamContributions.Add(new() {
                    Kills = playerTeamScoreboard.TeamStats.Kills != 0 ? (double)playerScoreboard.Kills / playerTeamScoreboard.TeamStats.Kills : 0,
                    Deaths = playerTeamScoreboard.TeamStats.Deaths != 0 ? (double)playerScoreboard.Deaths / playerTeamScoreboard.TeamStats.Deaths : 0,
                    Assists = playerTeamScoreboard.TeamStats.Assists != 0 ? (double)playerScoreboard.Assists / playerTeamScoreboard.TeamStats.Assists : 0,
                    DamageDealt = playerTeamScoreboard.TeamStats.DamageDealt != 0 ? (double)playerScoreboard.DamageDealt / playerTeamScoreboard.TeamStats.DamageDealt : 0,
                    DamageTaken = playerTeamScoreboard.TeamStats.DamageTaken != 0 ? (double)playerScoreboard.DamageTaken / playerTeamScoreboard.TeamStats.DamageTaken : 0,
                    HPRestored = playerTeamScoreboard.TeamStats.HPRestored != 0 ? (double)playerScoreboard.HPRestored / playerTeamScoreboard.TeamStats.HPRestored : 0,
                    TimeOnCrystalDouble = playerTeamScoreboard.TeamStats.TimeOnCrystal.Ticks != 0 ? playerScoreboard.TimeOnCrystal / playerTeamScoreboard.TeamStats.TimeOnCrystal : 0,
                    KillsAndAssists = (playerTeamScoreboard.TeamStats.Kills + playerTeamScoreboard.TeamStats.Assists) != 0
                    ? (double)(playerScoreboard.Kills + playerScoreboard.Assists) / (playerTeamScoreboard.TeamStats.Assists + playerTeamScoreboard.TeamStats.Kills) : 0,
                });
            }
        }
    }

    internal void SetScoreboardStats(CCPlayerJobStats stats, List<CCScoreboardDouble> teamContributions) {
        var statMatches = teamContributions.Count;
        //set average stats
        if(statMatches > 0) {
            stats.StatsPersonal.Matches = stats.StatsTeammate.Matches + stats.StatsOpponent.Matches;
            stats.StatsPersonal.Wins = stats.StatsTeammate.Wins + stats.StatsOpponent.Wins;
            stats.StatsPersonal.Losses = stats.StatsTeammate.Losses + stats.StatsOpponent.Losses;

            stats.ScoreboardPerMatch.Kills = (double)stats.ScoreboardTotal.Kills / statMatches;
            stats.ScoreboardPerMatch.Deaths = (double)stats.ScoreboardTotal.Deaths / statMatches;
            stats.ScoreboardPerMatch.Assists = (double)stats.ScoreboardTotal.Assists / statMatches;
            stats.ScoreboardPerMatch.DamageDealt = (double)stats.ScoreboardTotal.DamageDealt / statMatches;
            stats.ScoreboardPerMatch.DamageTaken = (double)stats.ScoreboardTotal.DamageTaken / statMatches;
            stats.ScoreboardPerMatch.HPRestored = (double)stats.ScoreboardTotal.HPRestored / statMatches;
            stats.ScoreboardPerMatch.TimeOnCrystal = stats.ScoreboardTotal.TimeOnCrystal / statMatches;
            stats.ScoreboardPerMatch.KillsAndAssists = (double)stats.ScoreboardTotal.KillsAndAssists / statMatches;

            var matchTime = stats.ScoreboardTotal.MatchTime;
            stats.ScoreboardPerMin.Kills = stats.ScoreboardTotal.Kills / matchTime.TotalMinutes;
            stats.ScoreboardPerMin.Deaths = stats.ScoreboardTotal.Deaths / matchTime.TotalMinutes;
            stats.ScoreboardPerMin.Assists = stats.ScoreboardTotal.Assists / matchTime.TotalMinutes;
            stats.ScoreboardPerMin.DamageDealt = stats.ScoreboardTotal.DamageDealt / matchTime.TotalMinutes;
            stats.ScoreboardPerMin.DamageTaken = stats.ScoreboardTotal.DamageTaken / matchTime.TotalMinutes;
            stats.ScoreboardPerMin.HPRestored = stats.ScoreboardTotal.HPRestored / matchTime.TotalMinutes;
            stats.ScoreboardPerMin.TimeOnCrystal = stats.ScoreboardTotal.TimeOnCrystal / matchTime.TotalMinutes;
            stats.ScoreboardPerMin.KillsAndAssists = stats.ScoreboardTotal.KillsAndAssists / matchTime.TotalMinutes;

            stats.ScoreboardContrib.Kills = teamContributions.OrderBy(x => x.Kills).ElementAt(statMatches / 2).Kills;
            stats.ScoreboardContrib.Deaths = teamContributions.OrderBy(x => x.Deaths).ElementAt(statMatches / 2).Deaths;
            stats.ScoreboardContrib.Assists = teamContributions.OrderBy(x => x.Assists).ElementAt(statMatches / 2).Assists;
            stats.ScoreboardContrib.DamageDealt = teamContributions.OrderBy(x => x.DamageDealt).ElementAt(statMatches / 2).DamageDealt;
            stats.ScoreboardContrib.DamageTaken = teamContributions.OrderBy(x => x.DamageTaken).ElementAt(statMatches / 2).DamageTaken;
            stats.ScoreboardContrib.HPRestored = teamContributions.OrderBy(x => x.HPRestored).ElementAt(statMatches / 2).HPRestored;
            stats.ScoreboardContrib.TimeOnCrystalDouble = teamContributions.OrderBy(x => x.TimeOnCrystalDouble).ElementAt(statMatches / 2).TimeOnCrystalDouble;
            stats.ScoreboardContrib.KillsAndAssists = teamContributions.OrderBy(x => x.KillsAndAssists).ElementAt(statMatches / 2).KillsAndAssists;
        }
    }

    internal void IncrementAggregateStats(CCAggregateStats stats, CrystallineConflictMatch match) {
        stats.Matches++;
        if(match.IsWin) {
            stats.Wins++;
        } else if(match.IsLoss) {
            stats.Losses++;
        }
    }

    private void AddSuperlative(CrystallineConflictMatch? match, string sup, string val) {
        if(match == null) return;
        if(Superlatives.TryGetValue(match, out List<(string, string)>? value)) {
            value.Add((sup, val));
        } else {
            //_plugin.Log.Debug($"adding superlative {sup} {val} to {match.Id.ToString()}");
            Superlatives.Add(match, new() { (sup, val) });
        }
    }

    private List<CrystallineConflictMatch> FilterMatches(List<DataFilter> filters) {
        Stopwatch s1 = new();
        s1.Start();
        //var matches = _plugin.Storage.GetCCMatches().Query().Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        var matches = _plugin.DataCache.CCMatches.Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", "CC match retrieval", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();
        foreach(var filter in filters) {
            try {
                Stopwatch s2 = new();
                s2.Start();
                switch(filter.GetType()) {
                    case Type _ when filter.GetType() == typeof(MatchTypeFilter):
                        var matchTypeFilter = (MatchTypeFilter)filter;
                        matches = matches.Where(x => matchTypeFilter.FilterState[x.MatchType]).ToList();
                        //_plugin.Configuration.MatchWindowFilters.MatchTypeFilter = matchTypeFilter;
                        break;
                    case Type _ when filter.GetType() == typeof(ArenaFilter):
                        var arenaFilter = (ArenaFilter)filter;
                        //include unknown maps under all
                        matches = matches.Where(x => (x.Arena == null && arenaFilter.AllSelected) || arenaFilter.FilterState[(CrystallineConflictMap)x.Arena!]).ToList();
                        //_plugin.Configuration.MatchWindowFilters.ArenaFilter = arenaFilter;
                        break;
                    case Type _ when filter.GetType() == typeof(TimeFilter):
                        var timeFilter = (TimeFilter)filter;
                        switch(timeFilter.StatRange) {
                            case TimeRange.PastDay:
                                matches = matches.Where(x => (DateTime.Now - x.DutyStartTime).TotalHours < 24).ToList();
                                break;
                            case TimeRange.PastWeek:
                                matches = matches.Where(x => (DateTime.Now - x.DutyStartTime).TotalDays < 7).ToList();
                                break;
                            case TimeRange.ThisMonth:
                                matches = matches.Where(x => x.DutyStartTime.Month == DateTime.Now.Month && x.DutyStartTime.Year == DateTime.Now.Year).ToList();
                                break;
                            case TimeRange.LastMonth:
                                var lastMonth = DateTime.Now.AddMonths(-1);
                                matches = matches.Where(x => x.DutyStartTime.Month == lastMonth.Month && x.DutyStartTime.Year == lastMonth.Year).ToList();
                                break;
                            case TimeRange.ThisYear:
                                matches = matches.Where(x => x.DutyStartTime.Year == DateTime.Now.Year).ToList();
                                break;
                            case TimeRange.LastYear:
                                matches = matches.Where(x => x.DutyStartTime.Year == DateTime.Now.AddYears(-1).Year).ToList();
                                break;
                            case TimeRange.Custom:
                                matches = matches.Where(x => x.DutyStartTime > timeFilter.StartTime && x.DutyStartTime < timeFilter.EndTime).ToList();
                                break;
                            case TimeRange.Season:
                                matches = matches.Where(x => x.DutyStartTime > ArenaSeason.Season[timeFilter.Season].StartDate && x.DutyStartTime < ArenaSeason.Season[timeFilter.Season].EndDate).ToList();
                                break;
                            case TimeRange.All:
                            default:
                                break;
                        }
                        //_plugin.Configuration.MatchWindowFilters.TimeFilter = timeFilter;
                        break;
                    case Type _ when filter.GetType() == typeof(LocalPlayerFilter):
                        var localPlayerFilter = (LocalPlayerFilter)filter;
                        if(localPlayerFilter.CurrentPlayerOnly && _plugin.ClientState.IsLoggedIn && _plugin.GameState.CurrentPlayer != null) {
                            if(_plugin.Configuration.EnablePlayerLinking) {
                                var linkedAliases = _plugin.PlayerLinksService.GetAllLinkedAliases(_plugin.GameState.CurrentPlayer);
                                matches = matches.Where(x => x.LocalPlayer != null && (x.LocalPlayer.Equals(_plugin.GameState.CurrentPlayer) || linkedAliases.Contains(x.LocalPlayer))).ToList();
                            } else {
                                matches = matches.Where(x => x.LocalPlayer != null && x.LocalPlayer.Equals(_plugin.GameState.CurrentPlayer)).ToList();
                            }
                        }
                        //_plugin.Configuration.MatchWindowFilters.LocalPlayerFilter = localPlayerFilter;
                        break;
                    case Type _ when filter.GetType() == typeof(LocalPlayerJobFilter):
                        var localPlayerJobFilter = (LocalPlayerJobFilter)filter;
                        //_plugin.Log.Debug($"anyjob: {localPlayerJobFilter.AnyJob} role: {localPlayerJobFilter.JobRole} job: {localPlayerJobFilter.PlayerJob}");
                        if(!localPlayerJobFilter.AnyJob) {
                            if(localPlayerJobFilter.JobRole != null) {
                                matches = matches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && PlayerJobHelper.GetSubRoleFromJob(x.LocalPlayerTeamMember.Job) == localPlayerJobFilter.JobRole).ToList();
                            } else {
                                matches = matches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && x.LocalPlayerTeamMember.Job == localPlayerJobFilter.PlayerJob).ToList();
                            }
                        }
                        //_plugin.Configuration.MatchWindowFilters.LocalPlayerJobFilter = localPlayerJobFilter;
                        break;
                    case Type _ when filter.GetType() == typeof(OtherPlayerFilter):
                        var otherPlayerFilter = (OtherPlayerFilter)filter;
                        List<PlayerAlias> linkedPlayerAliases = new();
                        if(!otherPlayerFilter.PlayerNamesRaw.IsNullOrEmpty() && _plugin.Configuration.EnablePlayerLinking) {
                            linkedPlayerAliases = _plugin.PlayerLinksService.GetAllLinkedAliases(otherPlayerFilter.PlayerNamesRaw);
                        }
                        matches = matches.Where(x => {
                            foreach(var team in x.Teams) {
                                if(otherPlayerFilter.TeamStatus == TeamStatus.Teammate && team.Key != x.LocalPlayerTeam?.TeamName) {
                                    continue;
                                } else if(otherPlayerFilter.TeamStatus == TeamStatus.Opponent && team.Key == x.LocalPlayerTeam?.TeamName) {
                                    continue;
                                }
                                foreach(var player in team.Value.Players) {
                                    if(!otherPlayerFilter.AnyJob && player.Job != otherPlayerFilter.PlayerJob) {
                                        continue;
                                    }
                                    if(_plugin.Configuration.EnablePlayerLinking) {
                                        if(player.Alias.FullName.Contains(otherPlayerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase)
                                        || linkedPlayerAliases.Any(x => x.Equals(player.Alias))) {
                                            return true;
                                        }
                                    } else {
                                        if(player.Alias.FullName.Contains(otherPlayerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase)) {
                                            return true;
                                        }
                                    }
                                }
                            }
                            return false;
                        }).ToList();
                        //_plugin.Configuration.MatchWindowFilters.OtherPlayerFilter = otherPlayerFilter;
                        break;
                    case Type _ when filter.GetType() == typeof(TierFilter):
                        var tierFilter = (TierFilter)filter;
                        matches = matches.Where(x => {
                            CrystallineConflictPlayer highestPlayer = new();
                            try {
                                highestPlayer = x.Players.OrderByDescending(y => y.Rank).First();
                            } catch {
                                //_plugin.Log.Error($"{x.Id} {x.DutyStartTime}");
                                return true;
                            }
                            return x.MatchType != CrystallineConflictMatchType.Ranked || highestPlayer.Rank == null
                            || (highestPlayer.Rank >= tierFilter.TierLow && highestPlayer.Rank <= tierFilter.TierHigh) || (highestPlayer.Rank >= tierFilter.TierHigh && highestPlayer.Rank <= tierFilter.TierLow);
                        }).ToList();
                        break;
                    case Type _ when filter.GetType() == typeof(ResultFilter):
                        var resultFilter = (ResultFilter)filter;
                        if(resultFilter.Result == MatchResult.Win) {
                            matches = matches.Where(x => x.IsWin).ToList();
                        } else if(resultFilter.Result == MatchResult.Loss) {
                            matches = matches.Where(x => !x.IsWin && x.MatchWinner != null && !x.IsSpectated).ToList();
                        } else if(resultFilter.Result == MatchResult.Other) {
                            matches = matches.Where(x => x.IsSpectated || x.MatchWinner == null).ToList();
                        }
                        break;
                    case Type _ when filter.GetType() == typeof(DurationFilter):
                        var durationFilter = (DurationFilter)filter;
                        if(durationFilter.DirectionIndex == 0) {
                            matches = matches.Where(x => x.MatchDuration is null || x.MatchDuration >= durationFilter.Duration).ToList();
                        } else {
                            matches = matches.Where(x => x.MatchDuration is null || x.MatchDuration < durationFilter.Duration).ToList();
                        }
                        break;
                    case Type _ when filter.GetType() == typeof(BookmarkFilter):
                        var bookmarkFilter = (BookmarkFilter)filter;
                        if(bookmarkFilter.BookmarkedOnly) {
                            matches = matches.Where(x => x.IsBookmarked).ToList();
                        }
                        //_plugin.Configuration.MatchWindowFilters.BookmarkFilter = bookmarkFilter;
                        break;
                    case Type _ when filter.GetType() == typeof(MiscFilter):
                        var miscFilter = (MiscFilter)filter;
                        if(miscFilter.MustHaveStats) {
                            matches = matches.Where(x => x.PostMatch is not null).ToList();
                        }
                        if(!miscFilter.IncludeSpectated) {
                            matches = matches.Where(x => !x.IsSpectated).ToList();
                        }
                        //_plugin.Configuration.MatchWindowFilters.MiscFilter = miscFilter;
                        break;
                }
                _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"filter {filter.Name}", s2.ElapsedMilliseconds.ToString()));
            } catch(Exception e) {
                //catch exceptions to keep filtering going
                _plugin.Log.Error($"failed to apply filter: {filter.Name}\n{e.Message}");
            }
        }
        _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"total filters", s1.ElapsedMilliseconds.ToString()));
        return matches;
    }
}
