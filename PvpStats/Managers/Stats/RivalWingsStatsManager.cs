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

    public static float[] KillsPerMatchRange = [1.0f, 7.0f];
    public static float[] DeathsPerMatchRange = [1.0f, 7.0f];
    public static float[] AssistsPerMatchRange = [7f, 25f];
    public static float[] DamageDealtToPCsPerMatchRange = [300000f, 2000000f];
    public static float[] DamageDealtToOtherPerMatchRange = [100000f, 3000000f];
    public static float[] DamageTakenPerMatchRange = [400000f, 1500000f];
    public static float[] HPRestoredPerMatchRange = [100000f, 1200000f];
    public static float[] CeruleumPerMatchRange = [20f, 140f];
    public static float AverageMatchLength = 10f;
    public static float[] KillsPerMinRange = [KillsPerMatchRange[0] / AverageMatchLength, KillsPerMatchRange[1] / AverageMatchLength];
    public static float[] DeathsPerMinRange = [DeathsPerMatchRange[0] / AverageMatchLength, DeathsPerMatchRange[1] / AverageMatchLength];
    public static float[] AssistsPerMinRange = [AssistsPerMatchRange[0] / AverageMatchLength, AssistsPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageDealtToPCsPerMinRange = [DamageDealtToPCsPerMatchRange[0] / AverageMatchLength, DamageDealtToPCsPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageDealtToOtherPerMinRange = [DamageDealtToOtherPerMatchRange[0] / AverageMatchLength, DamageDealtToOtherPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageTakenPerMinRange = [DamageTakenPerMatchRange[0] / AverageMatchLength, DamageTakenPerMatchRange[1] / AverageMatchLength];
    public static float[] HPRestoredPerMinRange = [HPRestoredPerMatchRange[0] / AverageMatchLength, HPRestoredPerMatchRange[1] / AverageMatchLength];
    public static float[] CeruleumPerMinRange = [CeruleumPerMatchRange[0] / AverageMatchLength, CeruleumPerMatchRange[1] / AverageMatchLength];

    public static float[] ContribRange = [0 / 24f, 2 / 24f];
    public static float[] DamagePerKARange = [40000f, 150000f];
    public static float[] DamagePerLifeRange = [190000f, 400000f];
    public static float[] DamageTakenPerLifeRange = [100000f, 300000f];
    public static float[] HPRestoredPerLifeRange = [120000f, 600000f];
    public static float[] KDARange = [2.0f, 15.0f];

    //for external use
    internal CCAggregateStats OverallResults { get; private set; } = new();
    internal RWPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> LocalPlayerJobResults { get; private set; } = new();
    internal uint LocalPlayerMechMatches;
    internal Dictionary<RivalWingsMech, double> LocalPlayerMechTime { get; private set; } = new();
    internal double LocalPlayerMidWinRate { get; private set; }
    internal double LocalPlayerMercWinRate { get; private set; }
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    //internal state
    CCAggregateStats _overallResults = new();
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

    public RivalWingsStatsManager(Plugin plugin) : base(plugin, plugin.RWCache) {
        Reset();
    }

    protected override async Task RefreshInner(List<DataFilter> matchFilters, List<DataFilter> jobStatFilters, List<DataFilter> playerStatFilters) {
        var matches = MatchCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        matches = FilterMatches(matchFilters, matches);

        var toAdd = matches.Except(Matches).ToList();
        var toSubtract = Matches.Except(matches).ToList();

        int matchesProcessed = 0;
        if(toSubtract.Count * 2 >= Matches.Count) {
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

        SetScoreboardStats(_localPlayerStats, _localPlayerTeamContributions, _scoreboardEligibleTime);

        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
            OverallResults = _overallResults;
            LocalPlayerStats = _localPlayerStats;
            LocalPlayerJobResults = _localPlayerJobResults;
            LocalPlayerMechTime = _localPlayerMechTime.Select(x => (x.Key, x.Value / _mechEligibleTime.TotalSeconds)).ToDictionary();
            LocalPlayerMechMatches = _localPlayerMechMatches;
            LocalPlayerMercWinRate = (double)_mercWins / (_mercWins + _mercLosses);
            LocalPlayerMidWinRate = (double)_midWins / (_midWins + _midLosses);
            AverageMatchDuration = matches.Count > 0 ? _totalMatchTime / matches.Count : TimeSpan.Zero;
        } finally {
            RefreshLock.Release();
        }
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

    private void AddMatch(RivalWingsMatch match) {
        var teamScoreboards = match.GetTeamScoreboards();
        IncrementAggregateStats(_overallResults, match);
        _totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;

        if(match.LocalPlayerTeamMember != null && match.LocalPlayerTeamMember.Job != null) {
            var job = (Job)match.LocalPlayerTeamMember.Job;
            if(_localPlayerJobResults.TryGetValue(job, out CCAggregateStats? val)) {
                IncrementAggregateStats(val, match);
            } else {
                _localPlayerJobResults.Add(job, new());
                IncrementAggregateStats(_localPlayerJobResults[job], match);
            }
        }

        if(match.PlayerScoreboards != null) {
            _scoreboardEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
            RivalWingsScoreboard? localPlayerTeamScoreboard = null;
            teamScoreboards?.TryGetValue(match.LocalPlayerTeam ?? RivalWingsTeamName.Unknown, out localPlayerTeamScoreboard);
            AddPlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeamMember, localPlayerTeamScoreboard);
        }

        if(match.PlayerMechTime != null && match.LocalPlayer != null) {
            _localPlayerMechMatches++;
            _mechEligibleTime += match.MatchDuration ?? TimeSpan.Zero;
            if(match.PlayerMechTime.TryGetValue(match.LocalPlayer, out var playerMechTime)) {
                foreach(var mech in playerMechTime) {
                    _localPlayerMechTime[mech.Key] += mech.Value;
                }
            }
        }

        if(match.Mercs != null) {
            foreach(var team in match.Mercs) {
                if(team.Key == match.LocalPlayerTeam) {
                    _mercWins += team.Value;
                } else {
                    _mercLosses += team.Value;
                }
            }
        }

        if(match.Supplies != null) {
            foreach(var team in match.Supplies) {
                if(team.Key == match.LocalPlayerTeam) {
                    foreach(var supply in team.Value) {
                        _midWins += supply.Value;
                    }
                } else {
                    foreach(var supply in team.Value) {
                        _midLosses += supply.Value;
                    }
                }
            }
        }
    }

    private void RemoveMatch(RivalWingsMatch match) {
        var teamScoreboards = match.GetTeamScoreboards();
        DecrementAggregateStats(_overallResults, match);
        _totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;

        if(match.LocalPlayerTeamMember != null && match.LocalPlayerTeamMember.Job != null) {
            var job = (Job)match.LocalPlayerTeamMember.Job;
            if(_localPlayerJobResults.TryGetValue(job, out CCAggregateStats? val)) {
                DecrementAggregateStats(val, match);
            } else {
                _localPlayerJobResults.Add(job, new());
                DecrementAggregateStats(_localPlayerJobResults[job], match);
            }
        }

        if(match.PlayerScoreboards != null) {
            _scoreboardEligibleTime -= match.MatchDuration ?? TimeSpan.Zero;
            RivalWingsScoreboard? localPlayerTeamScoreboard = null;
            teamScoreboards?.TryGetValue(match.LocalPlayerTeam ?? RivalWingsTeamName.Unknown, out localPlayerTeamScoreboard);
            RemovePlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeamMember, localPlayerTeamScoreboard);
        }

        if(match.PlayerMechTime != null && match.LocalPlayer != null) {
            _localPlayerMechMatches--;
            _mechEligibleTime -= match.MatchDuration ?? TimeSpan.Zero;
            if(match.PlayerMechTime.TryGetValue(match.LocalPlayer, out var playerMechTime)) {
                foreach(var mech in playerMechTime) {
                    _localPlayerMechTime[mech.Key] -= mech.Value;
                }
            }
        }

        if(match.Mercs != null) {
            foreach(var team in match.Mercs) {
                if(team.Key == match.LocalPlayerTeam) {
                    _mercWins -= team.Value;
                } else {
                    _mercLosses -= team.Value;
                }
            }
        }

        if(match.Supplies != null) {
            foreach(var team in match.Supplies) {
                if(team.Key == match.LocalPlayerTeam) {
                    foreach(var supply in team.Value) {
                        _midWins -= supply.Value;
                    }
                } else {
                    foreach(var supply in team.Value) {
                        _midLosses -= supply.Value;
                    }
                }
            }
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

    internal void DecrementAggregateStats(CCAggregateStats stats, RivalWingsMatch match) {
        stats.Matches--;
        if(match.IsWin) {
            stats.Wins--;
        } else if(match.IsLoss) {
            stats.Losses--;
        }
    }

    internal void AddPlayerJobStat(RWPlayerJobStats statsModel, List<RWScoreboardDouble> teamContributions,
    RivalWingsMatch match, RivalWingsPlayer player, RivalWingsScoreboard? teamScoreboard) {
        //bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
        //bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam!;
        //bool isOpponent = !isLocalPlayer && !isTeammate;

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

    internal void RemovePlayerJobStat(RWPlayerJobStats statsModel, List<RWScoreboardDouble> teamContributions,
    RivalWingsMatch match, RivalWingsPlayer player, RivalWingsScoreboard? teamScoreboard) {
        //bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
        //bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam!;
        //bool isOpponent = !isLocalPlayer && !isTeammate;

        statsModel.StatsAll.Matches--;
        if(match.MatchWinner == player.Team) {
            statsModel.StatsAll.Wins--;
        } else if(match.MatchWinner != null) {
            statsModel.StatsAll.Losses--;
        }

        if(match.PlayerScoreboards != null) {
            var playerScoreboard = match.PlayerScoreboards[player.Name];
            if(playerScoreboard != null && teamScoreboard != null) {
                statsModel.ScoreboardTotal -= playerScoreboard;
                teamContributions.Remove(new(playerScoreboard, teamScoreboard));
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
