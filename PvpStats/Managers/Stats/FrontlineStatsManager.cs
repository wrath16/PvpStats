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

    ////summary
    //internal FLAggregateStats OverallResults { get; private set; } = new();
    //internal Dictionary<FrontlineMap, FLAggregateStats> MapResults { get; private set; } = new();
    //internal FLPlayerJobStats LocalPlayerStats { get; private set; } = new();
    //internal Dictionary<Job, FLAggregateStats> LocalPlayerJobResults { get; private set; } = new();
    //internal TimeSpan AverageMatchDuration { get; private set; } = new();

    ////jobs
    //internal List<Job> Jobs { get; private set; } = new();
    //internal Dictionary<Job, FLPlayerJobStats> JobStats { get; private set; } = new();

    //internal state
    //FLAggregateStats _overallResults;
    //Dictionary<FrontlineMap, FLAggregateStats> _mapResults;
    //Dictionary<Job, FLAggregateStats> _localPlayerJobResults;
    //FLPlayerJobStats _localPlayerStats;
    //List<FLScoreboardDouble> _localPlayerTeamContributions;
    //FLPlayerJobStats _shatterLocalPlayerStats;
    //List<FLScoreboardDouble> _shatterLocalPlayerTeamContributions;

    //Dictionary<Job, FLPlayerJobStats> _jobStats;
    //Dictionary<Job, List<FLScoreboardDouble>> _jobTeamContributions;
    //Dictionary<Job, TimeSpan> _jobTimes;

    //TimeSpan _totalMatchTime;
    //TimeSpan _totalShatterTime;

    //FLStatSourceFilter _lastJobStatSourceFilter = new();
    //FLStatSourceFilter _jobStatSourceFilter = new();
    //OtherPlayerFilter _lastPlayerFilter = new();
    //OtherPlayerFilter _playerFilter = new();

    List<PlayerAlias> _linkedPlayerAliases = [];

    internal FrontlineStatsManager(Plugin plugin) : base(plugin, plugin.FLCache) {
    }

    protected override async Task RefreshInner(List<DataFilter> matchFilters, List<DataFilter> jobStatFilters, List<DataFilter> playerStatFilters) {
        ////this is kinda shit
        //_jobStatSourceFilter = jobStatFilters[0] as FLStatSourceFilter;
        //_playerFilter = (OtherPlayerFilter)matchFilters.First(x => x.GetType() == typeof(OtherPlayerFilter));
        //_linkedPlayerAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(_playerFilter.PlayerNamesRaw);

        //Stopwatch matchesTimer = Stopwatch.StartNew();
        //var matches = MatchCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        //matches = FilterMatches(matchFilters, matches);
        //var toAdd = matches.Except(Matches).ToList();
        //var toSubtract = Matches.Except(matches).ToList();
        //Matches = matches;
        //MatchRefreshActive = false;
        //matchesTimer.Stop();
        //Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"Matches Refresh", matchesTimer.ElapsedMilliseconds.ToString()));

        //bool jobStatFilterChange = !_jobStatSourceFilter.Equals(_lastJobStatSourceFilter);
        //bool playerFilterChange = _jobStatSourceFilter!.InheritFromPlayerFilter && !_playerFilter.Equals(_lastPlayerFilter);
        //bool bigSubtract = toSubtract.Count * 2 >= Matches.Count;

        //Plugin.Log.Debug($"big subtract: {bigSubtract} jobStatSource change: {jobStatFilterChange} playerFilterInheritChange: {playerFilterChange}");

        //Task summaryTask = Task.CompletedTask;
        //Task jobTask = Task.CompletedTask;
        //Stopwatch summaryTimer = Stopwatch.StartNew();
        //Stopwatch jobTimer = Stopwatch.StartNew();

        ////force full build
        //if(toSubtract.Count * 2 >= Matches.Count) {
        //    Reset();
        //    int totalMatches = matches.Count;
        //    Plugin.Log.Debug($"Full re-build: {totalMatches}");
        //    summaryTask = Task.Run(() => BuildSummaryStats(matches));
        //    jobTask = Task.Run(() => BuildJobStats(matches));
        //} else {
        //    int totalMatches = toAdd.Count + toSubtract.Count;
        //    Plugin.Log.Debug($"Removing: {toSubtract.Count} Adding: {toAdd.Count}");
        //    //force rebuild of job stats
        //    if(jobStatFilterChange || playerFilterChange) {
        //        ResetJobs();
        //        jobTask = Task.Run(() => BuildJobStats(matches));
        //    } else {
        //        jobTask = Task.Run(() => {
        //            BuildJobStats(toSubtract, true);
        //            BuildJobStats(toAdd);
        //        });
        //    }
        //    summaryTask = Task.Run(() => {
        //        BuildSummaryStats(toSubtract, true);
        //        BuildSummaryStats(toAdd);
        //    });
        //}
        //summaryTask = summaryTask.ContinueWith(x => {
        //    CommitSummaryStats();
        //    summaryTimer.Stop();
        //    Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"Summary Refresh", summaryTimer.ElapsedMilliseconds.ToString()));
        //    SummaryRefreshActive = false;
        //});
        //jobTask = jobTask.ContinueWith(x => {
        //    CommitJobStats();
        //    jobTimer.Stop();
        //    Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"Jobs Refresh", jobTimer.ElapsedMilliseconds.ToString()));
        //    JobsRefreshActive = false;
        //});

        //Task.WaitAll([summaryTask, jobTask]);

        //_lastJobStatSourceFilter = new(_jobStatSourceFilter!);
        //_lastPlayerFilter = new(_playerFilter);
    }

    public static void IncrementAggregateStats(FLAggregateStats stats, FrontlineMatch match, bool decrement = false) {
        if(decrement) {
            stats.Matches--;
            if(match.Result == 0) {
                stats.FirstPlaces--;
            } else if(match.Result == 1) {
                stats.SecondPlaces--;
            } else if(match.Result == 2) {
                stats.ThirdPlaces--;
            }
        } else {
            stats.Matches++;
            if(match.Result == 0) {
                stats.FirstPlaces++;
            } else if(match.Result == 1) {
                stats.SecondPlaces++;
            } else if(match.Result == 2) {
                stats.ThirdPlaces++;
            }
        }
    }

    public static void AddPlayerJobStat(FLPlayerJobStats statsModel, List<FLScoreboardDouble> teamContributions,
    FrontlineMatch match, FrontlinePlayer player, FrontlineScoreboard? teamScoreboard, bool remove = false) {
        bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
        bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam!;
        bool isOpponent = !isLocalPlayer && !isTeammate;

        //if(isTeammate) {
        //    IncrementAggregateStats(statsModel.StatsTeammate, match);
        //} else if(isOpponent) {
        //    IncrementAggregateStats(statsModel.StatsOpponent, match);
        //}

        if(remove) {
            statsModel.StatsAll.Matches--;
        } else {
            statsModel.StatsAll.Matches++;
        }

        if(match.Teams.ContainsKey(player.Team)) {
            switch(match.Teams[player.Team].Placement) {
                case 0:
                    if(remove) {
                        statsModel.StatsAll.FirstPlaces--;
                    } else {
                        statsModel.StatsAll.FirstPlaces++;
                    }
                    break;
                case 1:
                    if(remove) {
                        statsModel.StatsAll.SecondPlaces--;
                    } else {
                        statsModel.StatsAll.SecondPlaces++;
                    }
                    break;
                case 2:
                    if(remove) {
                        statsModel.StatsAll.ThirdPlaces--;
                    } else {
                        statsModel.StatsAll.ThirdPlaces++;
                    }
                    break;
                default:
                    break;
            }
        }

        if(match.PlayerScoreboards != null) {
            var playerScoreboard = match.PlayerScoreboards[player.Name];
            if(playerScoreboard != null && teamScoreboard != null) {
                //statsModel.ScoreboardTotal.MatchTime += match.PostMatch.MatchDuration;
                if(remove) {
                    statsModel.ScoreboardTotal -= playerScoreboard;
                    teamContributions.Remove(new(playerScoreboard, teamScoreboard));
                } else {
                    statsModel.ScoreboardTotal += playerScoreboard;
                    teamContributions.Add(new(playerScoreboard, teamScoreboard));
                }
                statsModel.ScoreboardTotal.Size = statsModel.StatsAll.Matches;
            }
        }
    }

    public static void SetScoreboardStats(FLPlayerJobStats stats, List<FLScoreboardDouble> teamContributions, TimeSpan time) {
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
