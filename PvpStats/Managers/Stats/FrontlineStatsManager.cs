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
    public static float[] ContribRange = [0 / 24f, 2 / 24f];
    public static float[] DamagePerKARange = [20000f, 40000f];
    public static float[] DamagePerLifeRange = [100000f, 400000f];
    public static float[] DamageTakenPerLifeRange = [120000f, 300000f];
    public static float[] HPRestoredPerLifeRange = [120000f, 300000f];
    public static float[] KDARange = [4.0f, 20.0f];
    public static float[] BattleHighPerLifeRange = [10.0f, 60.0f];

    //summary
    internal FLAggregateStats OverallResults { get; private set; } = new();
    internal Dictionary<FrontlineMap, FLAggregateStats> MapResults { get; private set; } = new();
    internal FLPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, FLAggregateStats> LocalPlayerJobResults { get; private set; } = new();
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    //jobs
    internal List<Job> Jobs { get; private set; } = new();
    internal Dictionary<Job, FLPlayerJobStats> JobStats { get; private set; } = new();


    //internal state
    FLAggregateStats _overallResults;
    Dictionary<FrontlineMap, FLAggregateStats> _mapResults;
    Dictionary<Job, FLAggregateStats> _localPlayerJobResults;
    FLPlayerJobStats _localPlayerStats;
    List<FLScoreboardDouble> _localPlayerTeamContributions;
    FLPlayerJobStats _shatterLocalPlayerStats;
    List<FLScoreboardDouble> _shatterLocalPlayerTeamContributions;

    List<Job> _jobs;
    Dictionary<Job, FLPlayerJobStats> _jobStats;
    Dictionary<Job, List<FLScoreboardDouble>> _jobTeamContributions;
    Dictionary<Job, TimeSpan> _jobTimes;

    TimeSpan _totalMatchTime; 
    TimeSpan _totalShatterTime;

    FLStatSourceFilter _lastJobStatSourceFilter;
    FLStatSourceFilter _jobStatSourceFilter = new();
    OtherPlayerFilter _lastPlayerFilter;
    OtherPlayerFilter _playerFilter = new();

    List<PlayerAlias> _linkedPlayerAliases = [];

    internal FrontlineStatsManager(Plugin plugin) : base(plugin, plugin.FLCache) {
        Reset();
    }

    protected override async Task RefreshInner(List<DataFilter> matchFilters, List<DataFilter> jobStatFilters, List<DataFilter> playerStatFilters) {
        //this is kinda shit
        _jobStatSourceFilter = jobStatFilters[0] as FLStatSourceFilter;
        _playerFilter = (OtherPlayerFilter)matchFilters.First(x => x.GetType() == typeof(OtherPlayerFilter));
        _linkedPlayerAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(_playerFilter.PlayerNamesRaw);

        var matches = MatchCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        matches = FilterMatches(matchFilters, matches);

        var toAdd = matches.Except(Matches).ToList();
        var toSubtract = Matches.Except(matches).ToList();

        bool jobStatFilterChange = !_jobStatSourceFilter.Equals(_lastJobStatSourceFilter);
        bool playerFilterChange = _jobStatSourceFilter!.InheritFromPlayerFilter && !_playerFilter.Equals(_lastPlayerFilter);
        bool bigSubtract = toSubtract.Count * 2 >= Matches.Count;
        int matchesProcessed = 0;
        Plugin.Log.Debug($"big subtract: {bigSubtract} jobStatSource change: {jobStatFilterChange} playerFilterInheritChange: {playerFilterChange}");
        if(toSubtract.Count * 2 >= Matches.Count || jobStatFilterChange || playerFilterChange) {
            //force full build
            Reset();
            int totalMatches = matches.Count;
            Plugin.Log.Debug($"Full re-build: {totalMatches}");
            matches.ForEach(x => {
                AddMatch(x);
                RefreshProgress = (float)matchesProcessed++ / totalMatches;
            });
        } else {
            int totalMatches = toAdd.Count + toSubtract.Count;
            Plugin.Log.Debug($"Removing: {toSubtract.Count}");
            toSubtract.ForEach(x => {
                RemoveMatch(x);
                RefreshProgress = (float)matchesProcessed++ / totalMatches;
            });
            Plugin.Log.Debug($"Adding: {toAdd.Count}");
            toAdd.ForEach(x => {
                AddMatch(x);
                RefreshProgress = (float)matchesProcessed++ / totalMatches;
            });
        }

        _lastJobStatSourceFilter = new(_jobStatSourceFilter!);
        _lastPlayerFilter = new(_playerFilter);

        SetScoreboardStats(_localPlayerStats, _localPlayerTeamContributions, _totalMatchTime);
        SetScoreboardStats(_shatterLocalPlayerStats, _shatterLocalPlayerTeamContributions, _totalShatterTime);
        _localPlayerStats.ScoreboardTotal.DamageToOther = _shatterLocalPlayerStats.ScoreboardTotal.DamageToOther;
        _localPlayerStats.ScoreboardPerMatch.DamageToOther = _shatterLocalPlayerStats.ScoreboardPerMatch.DamageToOther;
        _localPlayerStats.ScoreboardPerMin.DamageToOther = _shatterLocalPlayerStats.ScoreboardPerMin.DamageToOther;
        _localPlayerStats.ScoreboardContrib.DamageToOther = _shatterLocalPlayerStats.ScoreboardContrib.DamageToOther;
        foreach(var jobStat in _jobStats) {
            SetScoreboardStats(jobStat.Value, _jobTeamContributions[jobStat.Key], _jobTimes[jobStat.Key]);
        }



        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
            OverallResults = _overallResults;
            MapResults = _mapResults;
            LocalPlayerStats = _localPlayerStats;
            LocalPlayerJobResults = _localPlayerJobResults;
            AverageMatchDuration = matches.Count > 0 ? _totalMatchTime / matches.Count : TimeSpan.Zero;
            Jobs = _jobStats.Keys.ToList();
            JobStats = _jobStats;
        } finally {
            RefreshLock.Release();
        }
    }

    private void Reset() {
        _overallResults = new();
        _mapResults = [];
        _localPlayerJobResults = [];
        _localPlayerStats = new();
        _localPlayerTeamContributions = [];
        _shatterLocalPlayerStats = new();
        _shatterLocalPlayerTeamContributions = [];

        _jobs = [];
        _jobStats = [];
        _jobTeamContributions = [];
        _jobTimes = [];

        _totalMatchTime = TimeSpan.Zero;
        _totalShatterTime = TimeSpan.Zero;

        _lastJobStatSourceFilter = new();
        //_jobStatSourceFilter = new();
        _lastPlayerFilter = new();
        //_playerFilter = new();

        //_linkedPlayerAliases = [];

        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        foreach(var job in allJobs) {
            _jobStats.Add(job, new());
            _jobTimes.Add(job, TimeSpan.Zero);
            _jobTeamContributions.Add(job, new());
        }
    }

    private void AddMatch(FrontlineMatch match) {
        var teamScoreboards = match.GetTeamScoreboards();
        IncrementAggregateStats(_overallResults, match);
        _totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;

        if(match.Arena != null) {
            var arena = (FrontlineMap)match.Arena;
            if(_mapResults.TryGetValue(arena, out FLAggregateStats? val)) {
                IncrementAggregateStats(val, match);
            } else {
                _mapResults.Add(arena, new());
                IncrementAggregateStats(_mapResults[arena], match);
            }
        }

        if(match.LocalPlayerTeamMember != null && match.LocalPlayerTeamMember.Job != null) {
            var job = (Job)match.LocalPlayerTeamMember.Job;
            if(_localPlayerJobResults.TryGetValue(job, out FLAggregateStats? val)) {
                IncrementAggregateStats(val, match);
            } else {
                _localPlayerJobResults.Add(job, new());
                IncrementAggregateStats(_localPlayerJobResults[job], match);
            }
        }

        if(match.PlayerScoreboards != null) {
            if(match.LocalPlayerTeam != null) {
                //scoreboardEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
                FrontlineScoreboard? localPlayerTeamScoreboard = null;
                teamScoreboards?.TryGetValue((FrontlineTeamName)match.LocalPlayerTeam, out localPlayerTeamScoreboard);
                AddPlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeamMember!, localPlayerTeamScoreboard);

                if(match.Arena == FrontlineMap.FieldsOfGlory) {
                    _totalShatterTime += match.MatchDuration ?? TimeSpan.Zero;
                    teamScoreboards?.TryGetValue((FrontlineTeamName)match.LocalPlayerTeam, out localPlayerTeamScoreboard);
                    AddPlayerJobStat(_shatterLocalPlayerStats, _shatterLocalPlayerTeamContributions, match, match.LocalPlayerTeamMember!, localPlayerTeamScoreboard);
                }
            }
            foreach(var playerScoreboard in match.PlayerScoreboards) {
                var player = match.Players.FirstOrDefault(x => x.Name.Equals(playerScoreboard.Key));
                if(player is null) continue;
                bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
                bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam;
                bool isOpponent = !isLocalPlayer && !isTeammate;

                bool jobStatsEligible = true;
                bool nameMatch = player.Name.FullName.Contains(_playerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                if(Plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                    nameMatch = _linkedPlayerAliases.Contains(player.Name);
                }
                bool sideMatch = _playerFilter.TeamStatus == TeamStatus.Any
                    || _playerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                    || _playerFilter.TeamStatus == TeamStatus.Opponent && !isTeammate && !isLocalPlayer;
                bool jobMatch = _playerFilter.AnyJob || _playerFilter.PlayerJob == player.Job;
                if(!nameMatch || !sideMatch || !jobMatch) {
                    if(_jobStatSourceFilter.InheritFromPlayerFilter) {
                        jobStatsEligible = false;
                    }
                }
                if(!_jobStatSourceFilter.FilterState[StatSource.LocalPlayer] && isLocalPlayer) {
                    jobStatsEligible = false;
                } else if(!_jobStatSourceFilter.FilterState[StatSource.Teammate] && isTeammate) {
                    jobStatsEligible = false;
                } else if(!_jobStatSourceFilter.FilterState[StatSource.Opponent] && !isTeammate && !isLocalPlayer) {
                    jobStatsEligible = false;
                }

                if(player?.Job != null && player?.Team != null && jobStatsEligible) {
                    //Plugin.Log.Debug($"Adding job stats..{player.Name} {player.Job}");
                    var teamScoreboard = match.GetTeamScoreboards()[player.Team];
                    var job = (Job)player.Job;
                    _jobTimes[job] += match.MatchDuration ?? TimeSpan.Zero;
                    AddPlayerJobStat(_jobStats[job], _jobTeamContributions[job], match, player, teamScoreboard);
                }
            }
        }
    }

    private void RemoveMatch(FrontlineMatch match) {
        var teamScoreboards = match.GetTeamScoreboards();
        DecrementAggregateStats(_overallResults, match);
        _totalMatchTime -= match.MatchDuration ?? TimeSpan.Zero;

        if(match.Arena != null) {
            var arena = (FrontlineMap)match.Arena;
            if(_mapResults.TryGetValue(arena, out FLAggregateStats? val)) {
                DecrementAggregateStats(val, match);
            } else {
                _mapResults.Add(arena, new());
                DecrementAggregateStats(_mapResults[arena], match);
            }
        }

        if(match.LocalPlayerTeamMember != null && match.LocalPlayerTeamMember.Job != null) {
            var job = (Job)match.LocalPlayerTeamMember.Job;
            if(_localPlayerJobResults.TryGetValue(job, out FLAggregateStats? val)) {
                DecrementAggregateStats(val, match);
            } else {
                _localPlayerJobResults.Add(job, new());
                DecrementAggregateStats(_localPlayerJobResults[job], match);
            }
        }

        if(match.PlayerScoreboards != null) {
            if(match.LocalPlayerTeam != null) {
                //scoreboardEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
                FrontlineScoreboard? localPlayerTeamScoreboard = null;
                teamScoreboards?.TryGetValue((FrontlineTeamName)match.LocalPlayerTeam, out localPlayerTeamScoreboard);
                RemovePlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeamMember!, localPlayerTeamScoreboard);

                if(match.Arena == FrontlineMap.FieldsOfGlory) {
                    _totalShatterTime -= match.MatchDuration ?? TimeSpan.Zero;
                    teamScoreboards?.TryGetValue((FrontlineTeamName)match.LocalPlayerTeam, out localPlayerTeamScoreboard);
                    RemovePlayerJobStat(_shatterLocalPlayerStats, _shatterLocalPlayerTeamContributions, match, match.LocalPlayerTeamMember!, localPlayerTeamScoreboard);
                }
            }
            foreach(var playerScoreboard in match.PlayerScoreboards) {
                var player = match.Players.FirstOrDefault(x => x.Name.Equals(playerScoreboard.Key));
                if(player is null) continue;
                bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
                bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam;
                bool isOpponent = !isLocalPlayer && !isTeammate;

                bool jobStatsEligible = true;
                bool nameMatch = player.Name.FullName.Contains(_playerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                if(Plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                    nameMatch = _linkedPlayerAliases.Contains(player.Name);
                }
                bool sideMatch = _playerFilter.TeamStatus == TeamStatus.Any
                    || _playerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                    || _playerFilter.TeamStatus == TeamStatus.Opponent && !isTeammate && !isLocalPlayer;
                bool jobMatch = _playerFilter.AnyJob || _playerFilter.PlayerJob == player.Job;
                if(!nameMatch || !sideMatch || !jobMatch) {
                    if(_jobStatSourceFilter.InheritFromPlayerFilter) {
                        jobStatsEligible = false;
                    }
                }
                if(!_jobStatSourceFilter.FilterState[StatSource.LocalPlayer] && isLocalPlayer) {
                    jobStatsEligible = false;
                } else if(!_jobStatSourceFilter.FilterState[StatSource.Teammate] && isTeammate) {
                    jobStatsEligible = false;
                } else if(!_jobStatSourceFilter.FilterState[StatSource.Opponent] && !isTeammate && !isLocalPlayer) {
                    jobStatsEligible = false;
                }

                if(player?.Job != null && player?.Team != null && jobStatsEligible) {
                    //Plugin.Log.Debug($"Adding job stats..{player.Name} {player.Job}");
                    var teamScoreboard = match.GetTeamScoreboards()[player.Team];
                    var job = (Job)player.Job;
                    _jobTimes[job] -= match.MatchDuration ?? TimeSpan.Zero;
                    RemovePlayerJobStat(_jobStats[job], _jobTeamContributions[job], match, player, teamScoreboard);
                }
            }
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

    private void DecrementAggregateStats(FLAggregateStats stats, FrontlineMatch match) {
        stats.Matches--;
        if(match.Result == 0) {
            stats.FirstPlaces--;
        } else if(match.Result == 1) {
            stats.SecondPlaces--;
        } else if(match.Result == 2) {
            stats.ThirdPlaces--;
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

    internal void RemovePlayerJobStat(FLPlayerJobStats statsModel, List<FLScoreboardDouble> teamContributions,
    FrontlineMatch match, FrontlinePlayer player, FrontlineScoreboard? teamScoreboard) {
        bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
        bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam!;
        bool isOpponent = !isLocalPlayer && !isTeammate;

        //if(isTeammate) {
        //    IncrementAggregateStats(statsModel.StatsTeammate, match);
        //} else if(isOpponent) {
        //    IncrementAggregateStats(statsModel.StatsOpponent, match);
        //}

        statsModel.StatsAll.Matches--;
        if(match.Teams.ContainsKey(player.Team)) {
            switch(match.Teams[player.Team].Placement) {
                case 0:
                    statsModel.StatsAll.FirstPlaces--;
                    break;
                case 1:
                    statsModel.StatsAll.SecondPlaces--;
                    break;
                case 2:
                    statsModel.StatsAll.ThirdPlaces--;
                    break;
                default:
                    break;
            }
        }

        if(match.PlayerScoreboards != null) {
            var playerScoreboard = match.PlayerScoreboards[player.Name];
            if(playerScoreboard != null && teamScoreboard != null) {
                //statsModel.ScoreboardTotal.MatchTime += match.PostMatch.MatchDuration;
                statsModel.ScoreboardTotal -= playerScoreboard;
                statsModel.ScoreboardTotal.Size = statsModel.StatsAll.Matches;
                teamContributions.Remove(new(playerScoreboard, teamScoreboard));
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
