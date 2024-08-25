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

    public static float[] KillsPerMatchRange = [0.5f, 8.0f];
    public static float[] DeathsPerMatchRange = [0.5f, 5.0f];
    public static float[] AssistsPerMatchRange = [10.0f, 35.0f];
    public static float[] DamageDealtPerMatchRange = [300000f, 1500000f];
    public static float[] DamageToOtherPerMatchRange = [100000f, 2000000f];
    public static float[] DamageTakenPerMatchRange = [300000f, 1250000f];
    public static float[] HPRestoredPerMatchRange = [300000f, 1800000f];
    public static float AverageMatchLength = 15f;
    public static float[] ContribRange = [0 / 48f, 4 / 48f];
    public static float[] DamagePerKARange = [20000f, 50000f];
    public static float[] DamagePerLifeRange = [100000f, 400000f];
    public static float[] DamageTakenPerLifeRange = [120000f, 300000f];
    public static float[] HPRestoredPerLifeRange = [120000f, 300000f];
    public static float[] KDARange = [4.0f, 20.0f];
    public static float[] BattleHighPerLifeRange = [10.0f, 60.0f];
    internal FLAggregateStats OverallResults { get; private set; } = new();
    internal Dictionary<FrontlineMap, FLAggregateStats> MapResults { get; private set; } = new();
    internal FLPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, FLAggregateStats> LocalPlayerJobResults { get; private set; } = new();
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    //jobs
    internal List<Job> Jobs { get; private set; } = new();
    internal Dictionary<Job, FLPlayerJobStats> JobStats { get; private set; } = new();

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

        List<Job> jobs = new();
        Dictionary<Job, FLPlayerJobStats> jobStats = new();
        Dictionary<Job, List<FLScoreboardDouble>> jobTeamContributions = new();
        Dictionary<Job, TimeSpan> jobTimes = new();

        TimeSpan totalMatchTime = TimeSpan.Zero, totalShatterTime = TimeSpan.Zero;

        //this is kinda shit
        var jobStatSourceFilter = jobStatFilters[0] as FLStatSourceFilter;
        var playerFilter = (OtherPlayerFilter)matchFilters.First(x => x.GetType() == typeof(OtherPlayerFilter));
        var linkedPlayerAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(playerFilter.PlayerNamesRaw);

        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        foreach(var job in allJobs) {
            jobStats.Add(job, new());
            jobTimes.Add(job, TimeSpan.Zero);
            jobTeamContributions.Add(job, new());
        }

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

            if(match.PlayerScoreboards != null) {
                if(match.LocalPlayerTeam != null) {
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
                foreach(var playerScoreboard in match.PlayerScoreboards) {
                    var player = match.Players.FirstOrDefault(x => x.Name.Equals(playerScoreboard.Key));
                    if(player is null) continue;
                    bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
                    bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam;
                    bool isOpponent = !isLocalPlayer && !isTeammate;

                    bool jobStatsEligible = true;
                    bool nameMatch = player.Name.FullName.Contains(playerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                    if(Plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                        nameMatch = linkedPlayerAliases.Contains(player.Name);
                    }
                    bool sideMatch = playerFilter.TeamStatus == TeamStatus.Any
                        || playerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                        || playerFilter.TeamStatus == TeamStatus.Opponent && !isTeammate && !isLocalPlayer;
                    bool jobMatch = playerFilter.AnyJob || playerFilter.PlayerJob == player.Job;
                    if(!nameMatch || !sideMatch || !jobMatch) {
                        if(jobStatSourceFilter.InheritFromPlayerFilter) {
                            jobStatsEligible = false;
                        }
                    }
                    if(!jobStatSourceFilter.FilterState[StatSource.LocalPlayer] && isLocalPlayer) {
                        jobStatsEligible = false;
                    } else if(!jobStatSourceFilter.FilterState[StatSource.Teammate] && isTeammate) {
                        jobStatsEligible = false;
                    } else if(!jobStatSourceFilter.FilterState[StatSource.Opponent] && !isTeammate && !isLocalPlayer) {
                        jobStatsEligible = false;
                    }

                    if(player?.Job != null && player?.Team != null && jobStatsEligible) {
                        //Plugin.Log.Debug($"Adding job stats..{player.Name} {player.Job}");
                        var teamScoreboard = match.GetTeamScoreboards()[player.Team];
                        var job = (Job)player.Job;
                        jobTimes[job] += match.MatchDuration ?? TimeSpan.Zero;
                        AddPlayerJobStat(jobStats[job], jobTeamContributions[job], match, player, teamScoreboard);
                    }
                }
            }
        }

        SetScoreboardStats(localPlayerStats, localPlayerTeamContributions, totalMatchTime);
        SetScoreboardStats(shatterLocalPlayerStats, shatterLocalPlayerTeamContributions, totalShatterTime);
        localPlayerStats.ScoreboardTotal.DamageToOther = shatterLocalPlayerStats.ScoreboardTotal.DamageToOther;
        localPlayerStats.ScoreboardPerMatch.DamageToOther = shatterLocalPlayerStats.ScoreboardPerMatch.DamageToOther;
        localPlayerStats.ScoreboardPerMin.DamageToOther = shatterLocalPlayerStats.ScoreboardPerMin.DamageToOther;
        localPlayerStats.ScoreboardContrib.DamageToOther = shatterLocalPlayerStats.ScoreboardContrib.DamageToOther;
        foreach(var jobStat in jobStats) {
            SetScoreboardStats(jobStat.Value, jobTeamContributions[jobStat.Key], jobTimes[jobStat.Key]);
        }

        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
            OverallResults = overallResults;
            MapResults = mapResults;
            LocalPlayerStats = localPlayerStats;
            LocalPlayerJobResults = localPlayerJobResults;
            AverageMatchDuration = matches.Count > 0 ? totalMatchTime / matches.Count : TimeSpan.Zero;
            Jobs = jobStats.Keys.ToList();
            JobStats = jobStats;
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

        statsModel.StatsAll.Matches++;
        if(match.Teams.ContainsKey(player.Team)) {
            switch(match.Teams[player.Team].Placement) {
                case 0:
                    statsModel.StatsAll.FirstPlaces++;
                    break;
                case 1:
                    statsModel.StatsAll.SecondPlaces++;
                    break;
                case 2:
                    statsModel.StatsAll.ThirdPlaces++;
                    break;
                default:
                    break;
            }
        }

        if(match.PlayerScoreboards != null) {
            var playerScoreboard = match.PlayerScoreboards[player.Name];
            if(playerScoreboard != null && teamScoreboard != null) {
                //statsModel.ScoreboardTotal.MatchTime += match.PostMatch.MatchDuration;
                statsModel.ScoreboardTotal += playerScoreboard;
                statsModel.ScoreboardTotal.Size = statsModel.StatsAll.Matches;
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
            stats.ScoreboardContrib.KillsAndAssists = teamContributions.OrderBy(x => x.KillsAndAssists).ElementAt(statMatches / 2).KillsAndAssists;
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
