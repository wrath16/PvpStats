using Dalamud.Utility;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Managers.Stats;
internal class RivalWingsStatsManager : StatsManager<RivalWingsMatch> {

    internal CCAggregateStats OverallResults { get; private set; } = new();
    internal RWPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> LocalPlayerJobResults { get; private set; } = new();
    internal uint LocalPlayerMechMatches;
    internal Dictionary<RivalWingsMech, double> LocalPlayerMechTime { get; private set; } = new();
    internal double LocalPlayerMidWinRate { get; private set; }
    internal double LocalPlayerMercWinRate { get; private set; }
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    public RivalWingsStatsManager(Plugin plugin) : base(plugin, plugin.RWCache) {
    }

    public override async Task Refresh(List<DataFilter> matchFilters, List<DataFilter> jobStatFilters, List<DataFilter> playerStatFilters) {
        var matches = MatchCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        matches = FilterMatches(matchFilters, matches);
        CCAggregateStats overallResults = new();
        Dictionary<Job, CCAggregateStats> localPlayerJobResults = [];
        RWPlayerJobStats localPlayerStats = new();
        List<RWScoreboardDouble> localPlayerTeamContributions = [];
        Dictionary<RivalWingsMech, double> localPlayerMechTime = new() {
            { RivalWingsMech.Chaser, 0},
            { RivalWingsMech.Oppressor, 0},
            { RivalWingsMech.Justice, 0}
        };
        uint localPlayerMechMatches = 0;
        TimeSpan totalMatchTime = TimeSpan.Zero;
        TimeSpan mechEligibleTime = TimeSpan.Zero;
        TimeSpan scoreboardEligibleTime = TimeSpan.Zero;
        int midWins = 0, midLosses = 0;
        int mercWins = 0, mercLosses = 0;

        foreach(var match in matches) {
            var teamScoreboards = match.GetTeamScoreboards();
            IncrementAggregateStats(overallResults, match);
            totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;

            if(match.LocalPlayerTeamMember != null && match.LocalPlayerTeamMember.Job != null) {
                var job = (Job)match.LocalPlayerTeamMember.Job;
                if(localPlayerJobResults.TryGetValue(job, out CCAggregateStats? val)) {
                    IncrementAggregateStats(val, match);
                } else {
                    localPlayerJobResults.Add(job, new());
                    IncrementAggregateStats(localPlayerJobResults[job], match);
                }
            }

            if(match.PlayerScoreboards != null) {
                scoreboardEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
                RivalWingsScoreboard? localPlayerTeamScoreboard = null;
                teamScoreboards?.TryGetValue(match.LocalPlayerTeam ?? RivalWingsTeamName.Unknown, out localPlayerTeamScoreboard);
                AddPlayerJobStat(localPlayerStats, localPlayerTeamContributions, match, match.LocalPlayerTeamMember, localPlayerTeamScoreboard);
                //if(match.LocalPlayerTeamMember!.Job != null) {
                //    var job = (Job)match.LocalPlayerTeamMember!.Job;
                //    if(!localPlayerJobStats.ContainsKey(job)) {
                //        localPlayerJobStats.Add(job, new());
                //    }
                //    IncrementAggregateStats(localPlayerJobStats[job], match);
                //}
            }

            if(match.PlayerMechTime != null && match.LocalPlayer != null) {
                localPlayerMechMatches++;
                mechEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
                if(match.PlayerMechTime.TryGetValue(match.LocalPlayer, out var playerMechTime)) {
                    foreach(var mech in playerMechTime) {
                        localPlayerMechTime[mech.Key] += mech.Value;
                    }
                }
            }

            if(match.Mercs != null) {
                foreach(var team in match.Mercs) {
                    if(team.Key == match.LocalPlayerTeam) {
                        mercWins += team.Value;
                    } else {
                        mercLosses += team.Value;
                    }
                }
            }

            if(match.Supplies != null) {
                foreach(var team in match.Supplies) {
                    if(team.Key == match.LocalPlayerTeam) {
                        foreach(var supply in team.Value) {
                            midWins += supply.Value;
                        }
                    } else {
                        foreach(var supply in team.Value) {
                            midLosses += supply.Value;
                        }
                    }
                }
            }
        }

        //foreach(var mech in localPlayerMechTime) {
        //    mech.Value /= mechEligibleTime;
        //}

        SetScoreboardStats(localPlayerStats, localPlayerTeamContributions, scoreboardEligibleTime);

        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
            OverallResults = overallResults;
            LocalPlayerStats = localPlayerStats;
            LocalPlayerJobResults = localPlayerJobResults;
            LocalPlayerMechTime = localPlayerMechTime.Select(x => (x.Key, x.Value / mechEligibleTime.TotalSeconds)).ToDictionary();
            LocalPlayerMechMatches = localPlayerMechMatches;
            LocalPlayerMercWinRate = (double)mercWins / (mercWins + mercLosses);
            LocalPlayerMidWinRate = (double)midWins / (midWins + midLosses);
            AverageMatchDuration = matches.Count > 0 ? totalMatchTime / matches.Count : TimeSpan.Zero;
        } finally {
            RefreshLock.Release();
        }
    }

    internal void IncrementAggregateStats(CCAggregateStats stats, RivalWingsMatch match) {
        stats.Matches++;
        if(match.IsWin) {
            stats.Wins++;
        } else if(match.IsLoss) {
            stats.Losses++;
        }
    }

    internal void AddPlayerJobStat(RWPlayerJobStats statsModel, List<RWScoreboardDouble> teamContributions,
    RivalWingsMatch match, RivalWingsPlayer player, RivalWingsScoreboard? teamScoreboard) {
        bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
        bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam!;
        bool isOpponent = !isLocalPlayer && !isTeammate;

        statsModel.StatsAll.Matches++;
        if(match.MatchWinner == player.Team) {
            statsModel.StatsAll.Wins++;
        } else if(match.MatchWinner != null) {
            statsModel.StatsAll.Losses++;
        }

        //if(isTeammate) {
        //    IncrementAggregateStats(statsModel.StatsTeammate, match);
        //} else if(isOpponent) {
        //    IncrementAggregateStats(statsModel.StatsOpponent, match);
        //}

        if(match.PlayerScoreboards != null) {
            var playerScoreboard = match.PlayerScoreboards[player.Name];
            if(playerScoreboard != null && teamScoreboard != null) {
                //statsModel.ScoreboardTotal.MatchTime += match.PostMatch.MatchDuration;
                statsModel.ScoreboardTotal += playerScoreboard;
                teamContributions.Add(new(playerScoreboard, teamScoreboard));
            }
        }
    }

    internal void SetScoreboardStats(RWPlayerJobStats stats, List<RWScoreboardDouble> teamContributions, TimeSpan time) {
        var statMatches = teamContributions.Count;
        //set average stats
        if(statMatches > 0) {
            //stats.StatsPersonal.Matches = stats.StatsTeammate.Matches + stats.StatsOpponent.Matches;
            //stats.StatsPersonal.Wins = stats.StatsTeammate.Wins + stats.StatsOpponent.Wins;
            //stats.StatsPersonal.Losses = stats.StatsTeammate.Losses + stats.StatsOpponent.Losses;
            stats.ScoreboardPerMatch = (RWScoreboardDouble)stats.ScoreboardTotal / statMatches;
            stats.ScoreboardPerMin = (RWScoreboardDouble)stats.ScoreboardTotal / (double)time.TotalMinutes;

            stats.ScoreboardContrib.Kills = teamContributions.OrderBy(x => x.Kills).ElementAt(statMatches / 2).Kills;
            stats.ScoreboardContrib.Deaths = teamContributions.OrderBy(x => x.Deaths).ElementAt(statMatches / 2).Deaths;
            stats.ScoreboardContrib.Assists = teamContributions.OrderBy(x => x.Assists).ElementAt(statMatches / 2).Assists;
            stats.ScoreboardContrib.DamageDealt = teamContributions.OrderBy(x => x.DamageDealt).ElementAt(statMatches / 2).DamageDealt;
            stats.ScoreboardContrib.DamageTaken = teamContributions.OrderBy(x => x.DamageTaken).ElementAt(statMatches / 2).DamageTaken;
            stats.ScoreboardContrib.HPRestored = teamContributions.OrderBy(x => x.HPRestored).ElementAt(statMatches / 2).HPRestored;
            stats.ScoreboardContrib.DamageToPCs = teamContributions.OrderBy(x => x.DamageToPCs).ElementAt(statMatches / 2).DamageToPCs;
            stats.ScoreboardContrib.DamageToOther = teamContributions.OrderBy(x => x.DamageToOther).ElementAt(statMatches / 2).DamageToOther;
            stats.ScoreboardContrib.Ceruleum = teamContributions.OrderBy(x => x.Ceruleum).ElementAt(statMatches / 2).Ceruleum;
            stats.ScoreboardContrib.Special1 = teamContributions.OrderBy(x => x.Special1).ElementAt(statMatches / 2).Special1;
        }
    }

    protected List<RivalWingsMatch> ApplyFilter(LocalPlayerJobFilter filter, List<RivalWingsMatch> matches) {
        List<RivalWingsMatch> filteredMatches = new(matches);
        if(!filter.AnyJob) {
            if(filter.JobRole != null) {
                filteredMatches = filteredMatches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && PlayerJobHelper.GetSubRoleFromJob(x.LocalPlayerTeamMember.Job) == filter.JobRole).ToList();
            } else {
                filteredMatches = filteredMatches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && x.LocalPlayerTeamMember.Job == filter.PlayerJob).ToList();
            }
        }
        return filteredMatches;
    }

    protected List<RivalWingsMatch> ApplyFilter(OtherPlayerFilter filter, List<RivalWingsMatch> matches) {
        List<RivalWingsMatch> filteredMatches = new(matches);
        List<PlayerAlias> linkedPlayerAliases = new();
        if(!filter.PlayerNamesRaw.IsNullOrEmpty() && Plugin.Configuration.EnablePlayerLinking) {
            linkedPlayerAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(filter.PlayerNamesRaw);
        }
        filteredMatches = filteredMatches.Where(x => {
            if(x.Players == null) return false;
            foreach(var player in x.Players) {
                if(!filter.AnyJob && player.Job != filter.PlayerJob) {
                    continue;
                }
                if(Plugin.Configuration.EnablePlayerLinking) {
                    if(player.Name.FullName.Contains(filter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase)
                    || linkedPlayerAliases.Any(x => x.Equals(player.Name))) {
                        return true;
                    }
                } else {
                    if(player.Name.FullName.Contains(filter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase)) {
                        return true;
                    }
                }
            }
            return false;
        }).ToList();
        return filteredMatches;
    }

    protected List<RivalWingsMatch> ApplyFilter(ResultFilter filter, List<RivalWingsMatch> matches) {
        List<RivalWingsMatch> filteredMatches = new(matches);
        if(filter.Result == MatchResult.Win) {
            filteredMatches = filteredMatches.Where(x => x.IsWin).ToList();
        } else if(filter.Result == MatchResult.Loss) {
            filteredMatches = filteredMatches.Where(x => !x.IsWin && x.MatchWinner != null).ToList();
        } else if(filter.Result == MatchResult.Other) {
            filteredMatches = filteredMatches.Where(x => x.MatchWinner == null).ToList();
        }
        return filteredMatches;
    }
}
