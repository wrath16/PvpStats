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
internal class FrontlineStatsManager : StatsManager<FrontlineMatch> {

    internal FLAggregateStats OverallResults { get; private set; } = new();
    internal Dictionary<FrontlineMap, FLAggregateStats> MapResults { get; private set; } = new();
    internal FLPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, FLAggregateStats> LocalPlayerJobResults { get; private set; } = new();
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    internal FrontlineStatsManager(Plugin plugin) : base(plugin, plugin.FLCache) {
    }

    public override async Task Refresh(List<DataFilter> matchFilters, List<DataFilter> jobStatFilters, List<DataFilter> playerStatFilters) {
        var matches = MatchCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        matches = FilterMatches(matchFilters, matches);
        FLAggregateStats overallResults = new();
        Dictionary<FrontlineMap, FLAggregateStats> mapResults = new();
        Dictionary<Job, FLAggregateStats> localPlayerJobResults = new();
        FLPlayerJobStats localPlayerStats = new();
        List<FLScoreboardDouble> localPlayerTeamContributions = [];
        FLPlayerJobStats shatterLocalPlayerStats = new();
        List<FLScoreboardDouble> shatterLocalPlayerTeamContributions = [];
        TimeSpan totalMatchTime = TimeSpan.Zero, totalShatterTime = TimeSpan.Zero;

        foreach(var match in matches) {
            var teamScoreboards = match.GetTeamScoreboards();
            IncrementAggregateStats(overallResults, match);
            totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;

            if(match.Arena != null) {
                var arena = (FrontlineMap)match.Arena;
                if(mapResults.TryGetValue(arena, out FLAggregateStats? val)) {
                    IncrementAggregateStats(val, match);
                } else {
                    mapResults.Add(arena, new());
                    IncrementAggregateStats(mapResults[arena], match);
                }
            }

            if(match.LocalPlayerTeamMember != null && match.LocalPlayerTeamMember.Job != null) {
                var job = (Job)match.LocalPlayerTeamMember.Job;
                if(localPlayerJobResults.TryGetValue(job, out FLAggregateStats? val)) {
                    IncrementAggregateStats(val, match);
                } else {
                    localPlayerJobResults.Add(job, new());
                    IncrementAggregateStats(localPlayerJobResults[job], match);
                }
            }

            if(match.PlayerScoreboards != null && match.LocalPlayerTeam != null) {
                //scoreboardEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
                FrontlineScoreboard? localPlayerTeamScoreboard = null;
                teamScoreboards?.TryGetValue((FrontlineTeamName)match.LocalPlayerTeam, out localPlayerTeamScoreboard);
                AddPlayerJobStat(localPlayerStats, localPlayerTeamContributions, match, match.LocalPlayerTeamMember!, localPlayerTeamScoreboard);

                if(match.Arena == FrontlineMap.FieldsOfGlory) {
                    totalShatterTime += match.MatchDuration ?? TimeSpan.Zero;
                    teamScoreboards?.TryGetValue((FrontlineTeamName)match.LocalPlayerTeam, out localPlayerTeamScoreboard);
                    AddPlayerJobStat(shatterLocalPlayerStats, shatterLocalPlayerTeamContributions, match, match.LocalPlayerTeamMember!, localPlayerTeamScoreboard);
                }
            }
        }

        SetScoreboardStats(localPlayerStats, localPlayerTeamContributions, totalMatchTime);
        SetScoreboardStats(shatterLocalPlayerStats, shatterLocalPlayerTeamContributions, totalShatterTime);
        localPlayerStats.ScoreboardTotal.DamageToOther = shatterLocalPlayerStats.ScoreboardTotal.DamageToOther;
        localPlayerStats.ScoreboardPerMatch.DamageToOther = shatterLocalPlayerStats.ScoreboardPerMatch.DamageToOther;
        localPlayerStats.ScoreboardPerMin.DamageToOther = shatterLocalPlayerStats.ScoreboardPerMin.DamageToOther;
        localPlayerStats.ScoreboardContrib.DamageToOther = shatterLocalPlayerStats.ScoreboardContrib.DamageToOther;

        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
            OverallResults = overallResults;
            MapResults = mapResults;
            LocalPlayerStats = localPlayerStats;
            LocalPlayerJobResults = localPlayerJobResults;
            AverageMatchDuration = matches.Count > 0 ? totalMatchTime / matches.Count : TimeSpan.Zero;
        } finally {
            RefreshLock.Release();
        }
    }

    private void IncrementAggregateStats(FLAggregateStats stats, FrontlineMatch match) {
        stats.Matches++;
        if(match.Result == 0) {
            stats.FirstPlaces++;
        } else if(match.Result == 1) {
            stats.SecondPlaces++;
        } else if(match.Result == 2) {
            stats.ThirdPlaces++;
        }
    }

    internal void AddPlayerJobStat(FLPlayerJobStats statsModel, List<FLScoreboardDouble> teamContributions,
    FrontlineMatch match, FrontlinePlayer player, FrontlineScoreboard? teamScoreboard) {
        bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
        bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam!;
        bool isOpponent = !isLocalPlayer && !isTeammate;

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

    internal void SetScoreboardStats(FLPlayerJobStats stats, List<FLScoreboardDouble> teamContributions, TimeSpan time) {
        var statMatches = teamContributions.Count;
        //set average stats
        if(statMatches > 0) {
            //stats.StatsPersonal.Matches = stats.StatsTeammate.Matches + stats.StatsOpponent.Matches;
            //stats.StatsPersonal.Wins = stats.StatsTeammate.Wins + stats.StatsOpponent.Wins;
            //stats.StatsPersonal.Losses = stats.StatsTeammate.Losses + stats.StatsOpponent.Losses;
            stats.ScoreboardPerMatch = (FLScoreboardDouble)stats.ScoreboardTotal / statMatches;
            stats.ScoreboardPerMin = (FLScoreboardDouble)stats.ScoreboardTotal / (double)time.TotalMinutes;

            stats.ScoreboardContrib.Kills = teamContributions.OrderBy(x => x.Kills).ElementAt(statMatches / 2).Kills;
            stats.ScoreboardContrib.Deaths = teamContributions.OrderBy(x => x.Deaths).ElementAt(statMatches / 2).Deaths;
            stats.ScoreboardContrib.Assists = teamContributions.OrderBy(x => x.Assists).ElementAt(statMatches / 2).Assists;
            stats.ScoreboardContrib.DamageDealt = teamContributions.OrderBy(x => x.DamageDealt).ElementAt(statMatches / 2).DamageDealt;
            stats.ScoreboardContrib.DamageTaken = teamContributions.OrderBy(x => x.DamageTaken).ElementAt(statMatches / 2).DamageTaken;
            stats.ScoreboardContrib.HPRestored = teamContributions.OrderBy(x => x.HPRestored).ElementAt(statMatches / 2).HPRestored;
            stats.ScoreboardContrib.DamageToPCs = teamContributions.OrderBy(x => x.DamageToPCs).ElementAt(statMatches / 2).DamageToPCs;
            stats.ScoreboardContrib.DamageToOther = teamContributions.OrderBy(x => x.DamageToOther).ElementAt(statMatches / 2).DamageToOther;
            stats.ScoreboardContrib.Occupations = teamContributions.OrderBy(x => x.Occupations).ElementAt(statMatches / 2).Occupations;
            stats.ScoreboardContrib.Special1 = teamContributions.OrderBy(x => x.Special1).ElementAt(statMatches / 2).Special1;
        }
    }

    protected List<FrontlineMatch> ApplyFilter(FrontlineArenaFilter filter, List<FrontlineMatch> matches) {
        List<FrontlineMatch> filteredMatches = new(matches);
        filteredMatches = filteredMatches.Where(x => (x.Arena == null && filter.AllSelected) || filter.FilterState[(FrontlineMap)x.Arena!]).ToList();
        return filteredMatches;
    }

    protected List<FrontlineMatch> ApplyFilter(LocalPlayerJobFilter filter, List<FrontlineMatch> matches) {
        List<FrontlineMatch> filteredMatches = new(matches);
        if(!filter.AnyJob) {
            if(filter.JobRole != null) {
                filteredMatches = filteredMatches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && PlayerJobHelper.GetSubRoleFromJob(x.LocalPlayerTeamMember.Job) == filter.JobRole).ToList();
            } else {
                filteredMatches = filteredMatches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && x.LocalPlayerTeamMember.Job == filter.PlayerJob).ToList();
            }
        }
        return filteredMatches;
    }

    protected List<FrontlineMatch> ApplyFilter(OtherPlayerFilter filter, List<FrontlineMatch> matches) {
        List<FrontlineMatch> filteredMatches = new(matches);
        List<PlayerAlias> linkedPlayerAliases = new();
        if(!filter.PlayerNamesRaw.IsNullOrEmpty() && Plugin.Configuration.EnablePlayerLinking) {
            linkedPlayerAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(filter.PlayerNamesRaw);
        }
        filteredMatches = filteredMatches.Where(x => {
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

    protected List<FrontlineMatch> ApplyFilter(FLResultFilter filter, List<FrontlineMatch> matches) {
        List<FrontlineMatch> filteredMatches = new(matches);
        filteredMatches = filteredMatches.Where(x => x.Result == null || filter.FilterState[(int)x.Result]).ToList();
        return filteredMatches;
    }
}
