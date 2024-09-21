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
using System.Threading.Tasks;

namespace PvpStats.Managers.Stats;
internal class CrystallineConflictStatsManager : StatsManager<CrystallineConflictMatch> {

    public static float[] KillsPerMatchRange = [1.0f, 4.5f];
    public static float[] DeathsPerMatchRange = [1.5f, 3.5f];
    public static float[] AssistsPerMatchRange = [4.0f, 8.0f];
    public static float[] DamageDealtPerMatchRange = [400000f, 850000f];
    public static float[] DamageTakenPerMatchRange = [400000f, 850000f];
    public static float[] HPRestoredPerMatchRange = [350000f, 1000000f];
    public static float[] TimeOnCrystalPerMatchRange = [35f, 120f];
    public static float AverageMatchLength = 5f;
    public static float[] KillsPerMinRange = [KillsPerMatchRange[0] / AverageMatchLength, KillsPerMatchRange[1] / AverageMatchLength];
    public static float[] DeathsPerMinRange = [DeathsPerMatchRange[0] / AverageMatchLength, DeathsPerMatchRange[1] / AverageMatchLength];
    public static float[] AssistsPerMinRange = [AssistsPerMatchRange[0] / AverageMatchLength, AssistsPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageDealtPerMinRange = [DamageDealtPerMatchRange[0] / AverageMatchLength, DamageDealtPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageTakenPerMinRange = [DamageTakenPerMatchRange[0] / AverageMatchLength, DamageTakenPerMatchRange[1] / AverageMatchLength];
    public static float[] HPRestoredPerMinRange = [HPRestoredPerMatchRange[0] / AverageMatchLength, HPRestoredPerMatchRange[1] / AverageMatchLength];
    public static float[] TimeOnCrystalPerMinRange = [TimeOnCrystalPerMatchRange[0] / AverageMatchLength, TimeOnCrystalPerMatchRange[1] / AverageMatchLength];

    public static float[] ContribRange = [0.15f, 0.25f];
    public static float[] DamagePerKARange = [52000f, 100000f];
    public static float[] DamagePerLifeRange = [190000f, 400000f];
    public static float[] DamageTakenPerLifeRange = [100000f, 300000f];
    public static float[] HPRestoredPerLifeRange = [120000f, 600000f];
    public static float[] KDARange = [4.0f, 20.0f];

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
    internal Dictionary<PlayerAlias, CCPlayerJobStats> PlayerStats { get; private set; } = new();
    internal Dictionary<PlayerAlias, List<PlayerAlias>> ActiveLinks { get; private set; } = new();

    //internal state
    TimeSpan _totalMatchTime;

    CCPlayerJobStats _localPlayerStats;
    List<CCScoreboardDouble> _localPlayerTeamContributions;
    TimeSpan _localPlayerMatchTime;
    Dictionary<Job, CCAggregateStats> _localPlayerJobStats;
    Dictionary<CrystallineConflictMap, CCAggregateStats> _arenaStats;
    Dictionary<PlayerAlias, CCAggregateStats> _teammateStats;
    Dictionary<PlayerAlias, CCAggregateStats> _opponentStats;
    Dictionary<Job, CCAggregateStats> _teammateJobStats;
    Dictionary<Job, CCAggregateStats> _opponentJobStats;
    Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> _teammateJobStatsLookup;
    Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> _opponentJobStatsLookup;

    Dictionary<CrystallineConflictMatch, List<(string, string)>> _superlatives;
    CrystallineConflictMatch? _longestMatch, _shortestMatch, _highestLoserProg, _lowestWinnerProg,
        _mostKills, _mostDeaths, _mostAssists, _mostDamageDealt, _mostDamageTaken, _mostHPRestored, _mostTimeOnCrystal,
        _highestKillsPerMin, _highestDeathsPerMin, _highestAssistsPerMin, _highestDamageDealtPerMin, _highestDamageTakenPerMin, _highestHPRestoredPerMin, _highestTimeOnCrystalPerMin;
    int _longestWinStreak, _longestLossStreak, _currentWinStreak, _currentLossStreak;

    Dictionary<Job, CCPlayerJobStats> _jobStats;
    Dictionary<Job, List<CCScoreboardDouble>> _jobTeamContributions;
    Dictionary<Job, TimeSpan> _jobTimes;

    List<PlayerAlias> _players;
    Dictionary<PlayerAlias, CCPlayerJobStats> _playerStats;
    Dictionary<PlayerAlias, List<CCScoreboardDouble>> _playerTeamContributions;
    Dictionary<PlayerAlias, TimeSpan> _playerTimes;
    Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> _playerJobStatsLookup;

    StatSourceFilter _lastJobStatSourceFilter = new();
    StatSourceFilter _jobStatSourceFilter = new();
    StatSourceFilter _lastPlayerStatSourceFilter = new();
    StatSourceFilter _playerStatSourceFilter = new();
    OtherPlayerFilter _lastPlayerFilter = new();
    OtherPlayerFilter _playerFilter = new();
    List<PlayerAlias> _linkedPlayerAliases;

    internal CrystallineConflictStatsManager(Plugin plugin) : base(plugin, plugin.CCCache) {
        Reset();
    }

    private void Reset() {
        _totalMatchTime = TimeSpan.Zero;

        _localPlayerStats = new();
        _localPlayerTeamContributions = [];
        _localPlayerMatchTime = TimeSpan.Zero;
        _localPlayerJobStats = [];
        _arenaStats = [];
        _teammateStats = [];
        _opponentStats = [];
        _teammateJobStats = [];
        _opponentJobStats = [];
        _teammateJobStatsLookup = [];
        _opponentJobStatsLookup = [];

        _superlatives = [];
        _longestMatch = null;
        _shortestMatch = null;
        _highestLoserProg = null;
        _lowestWinnerProg = null;
        _mostKills = null;
        _mostDeaths = null;
        _mostAssists = null;
        _mostDamageDealt = null;
        _mostDamageTaken = null;
        _mostHPRestored = null;
        _mostTimeOnCrystal = null;
        _highestKillsPerMin = null;
        _highestDeathsPerMin = null;
        _highestAssistsPerMin = null;
        _highestDamageDealtPerMin = null;
        _highestDamageTakenPerMin = null;
        _highestHPRestoredPerMin = null;
        _highestTimeOnCrystalPerMin = null;
        _longestWinStreak = 0;
        _longestLossStreak = 0;
        _currentWinStreak = 0;
        _currentLossStreak = 0;

        _jobStats = [];
        _jobTeamContributions = [];
        _jobTimes = [];
        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        foreach(var job in allJobs) {
            _jobStats.Add(job, new());
            _jobTimes.Add(job, TimeSpan.Zero);
            _jobTeamContributions.Add(job, new());
        }

        _players = [];
        _playerStats = [];
        _playerTeamContributions = [];
        _playerTimes = [];
        _playerJobStatsLookup = [];
    }

    protected override async Task RefreshInner(List<DataFilter> matchFilters, List<DataFilter> jobStatFilters, List<DataFilter> playerStatFilters) {
        //remove this later
        Reset();

        Stopwatch s0 = new();
        s0.Start();
        //List<Job> jobs = new();
        //Dictionary<Job, CCPlayerJobStats> jobStats = new();
        //Dictionary<Job, List<CCScoreboardDouble>> jobTeamContributions = new();
        //List<PlayerAlias> players = new();
        //Dictionary<PlayerAlias, CCPlayerJobStats> playerStats = new();
        //Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> playerJobStatsLookup = new();
        //Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> teammateJobStatsLookup = new();
        //Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> opponentJobStatsLookup = new();
        //Dictionary<PlayerAlias, List<CCScoreboardDouble>> playerTeamContributions = new();
        //CCPlayerJobStats localPlayerStats = new();
        //List<CCScoreboardDouble> localPlayerTeamContributions = new();
        //Dictionary<CrystallineConflictMap, CCAggregateStats> arenaStats = new();
        //Dictionary<PlayerAlias, CCAggregateStats> teammateStats = new();
        //Dictionary<PlayerAlias, CCAggregateStats> opponentStats = new();
        //Dictionary<Job, CCAggregateStats> localPlayerJobStats = new();
        //Dictionary<Job, CCAggregateStats> teammateJobStats = new();
        //Dictionary<Job, CCAggregateStats> opponentJobStats = new();
        Dictionary<PlayerAlias, List<PlayerAlias>> activeLinks = new();
        //Dictionary<CrystallineConflictMatch, List<(string, string)>> superlatives = new();
        //CrystallineConflictMatch? longestMatch = null, shortestMatch = null, highestLoserProg = null, lowestWinnerProg = null, closestWin = null, closestLoss = null,
        //    mostKills = null, mostDeaths = null, mostAssists = null, mostDamageDealt = null, mostDamageTaken = null, mostHPRestored = null, mostTimeOnCrystal = null,
        //    highestKillsPerMin = null, highestDeathsPerMin = null, highestAssistsPerMin = null, highestDamageDealtPerMin = null, highestDamageTakenPerMin = null, highestHPRestoredPerMin = null, highestTimeOnCrystalPerMin = null;
        //int longestWinStreak = 0, longestLossStreak = 0, spectatedMatchCount = 0, currentWinStreak = 0, currentLossStreak = 0;
        //TimeSpan totalMatchTime = TimeSpan.Zero;
        _jobStatSourceFilter = jobStatFilters[0] as StatSourceFilter ?? new();
        _playerStatSourceFilter = playerStatFilters[0] as StatSourceFilter ?? new();
        _playerFilter = (OtherPlayerFilter)matchFilters.First(x => x.GetType() == typeof(OtherPlayerFilter));
        _linkedPlayerAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(_playerFilter.PlayerNamesRaw);

        Stopwatch s1 = new();
        s1.Start();
        //var matches = Plugin.Storage.GetCCMatches().Query().Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        var matches = Plugin.CCCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", "CC match retrieval", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();
        matches = FilterMatches(matchFilters, matches);
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"total filters", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();

        //var playerFilter = (OtherPlayerFilter)matchFilters.First(x => x.GetType() == typeof(OtherPlayerFilter));
        //var jobStatSourceFilter = jobStatFilters[0] as StatSourceFilter ?? new();
        //bool playerStatSourceInherit = (playerStatFilters[0] as StatSourceFilter ?? new()).InheritFromPlayerFilter;
        //var linkedPlayerAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(_playerFilter.PlayerNamesRaw);
        //var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        //foreach(var job in allJobs) {
        //    jobStats.Add(job, new());
        //    jobTeamContributions.Add(job, new());
        //}
        s1.Restart();

        Stopwatch recordsWatch = new();
        Stopwatch arenaWatch = new();
        Stopwatch localPlayerWatch = new();
        Stopwatch teamPlayerWatch = new();
        Stopwatch aggregateStatsWatch = new();
        Stopwatch playerJobWatch = new();

        int matchesProcessed = 0;

        foreach(var match in matches) {
            _totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;
            //process records
            recordsWatch.Start();
            //track these for spectated matches as well
            if(_longestMatch == null) {
                _longestMatch = match;
                _shortestMatch = match;
                _highestLoserProg = match;
            }
            if(_longestMatch == null || match.MatchDuration > _longestMatch.MatchDuration) {
                _longestMatch = match;
            }
            if(_shortestMatch == null || match.MatchDuration < _shortestMatch.MatchDuration) {
                _shortestMatch = match;
            }
            if(_highestLoserProg == null || match.LoserProgress > _highestLoserProg.LoserProgress) {
                _highestLoserProg = match;
            }
            if(_lowestWinnerProg == null || match.WinnerProgress < _lowestWinnerProg.WinnerProgress) {
                _lowestWinnerProg = match;
            }

            if(match.IsSpectated) {
                //spectatedMatchCount++;
                //continue;
            } else {
                if(_mostKills == null || match.LocalPlayerStats?.Kills > _mostKills.LocalPlayerStats?.Kills
                    || (match.LocalPlayerStats?.Kills == _mostKills.LocalPlayerStats?.Kills && match.MatchDuration < _mostKills.MatchDuration)) {
                    _mostKills = match;
                }
                if(_mostDeaths == null || match.LocalPlayerStats?.Deaths > _mostDeaths.LocalPlayerStats?.Deaths
                    || (match.LocalPlayerStats?.Deaths == _mostDeaths.LocalPlayerStats?.Deaths && match.MatchDuration < _mostDeaths.MatchDuration)) {
                    _mostDeaths = match;
                }
                if(_mostAssists == null || match.LocalPlayerStats?.Assists > _mostAssists.LocalPlayerStats?.Assists
                    || (match.LocalPlayerStats?.Assists == _mostAssists.LocalPlayerStats?.Assists && match.MatchDuration < _mostAssists.MatchDuration)) {
                    _mostAssists = match;
                }
                if(_mostDamageDealt == null || match.LocalPlayerStats?.DamageDealt > _mostDamageDealt.LocalPlayerStats?.DamageDealt) {
                    _mostDamageDealt = match;
                }
                if(_mostDamageTaken == null || match.LocalPlayerStats?.DamageTaken > _mostDamageTaken.LocalPlayerStats?.DamageTaken) {
                    _mostDamageTaken = match;
                }
                if(_mostHPRestored == null || match.LocalPlayerStats?.HPRestored > _mostHPRestored.LocalPlayerStats?.HPRestored) {
                    _mostHPRestored = match;
                }
                if(_mostTimeOnCrystal == null || match.LocalPlayerStats?.TimeOnCrystal > _mostTimeOnCrystal.LocalPlayerStats?.TimeOnCrystal) {
                    _mostTimeOnCrystal = match;
                }
                if(match.MatchDuration != null && match.LocalPlayerStats != null) {
                    if(_highestKillsPerMin == null || (float)match.LocalPlayerStats?.Kills! / match.MatchDuration.Value.TotalMinutes > (float)_highestKillsPerMin.LocalPlayerStats?.Kills! / _highestKillsPerMin.MatchDuration!.Value.TotalMinutes) {
                        _highestKillsPerMin = match;
                    }
                    if(_highestDeathsPerMin == null || (float)match.LocalPlayerStats?.Deaths! / match.MatchDuration.Value.TotalMinutes > (float)_highestDeathsPerMin.LocalPlayerStats?.Deaths! / _highestDeathsPerMin.MatchDuration!.Value.TotalMinutes) {
                        _highestDeathsPerMin = match;
                    }
                    if(_highestAssistsPerMin == null || (float)match.LocalPlayerStats?.Assists! / match.MatchDuration.Value.TotalMinutes > (float)_highestAssistsPerMin.LocalPlayerStats?.Assists! / _highestAssistsPerMin.MatchDuration!.Value.TotalMinutes) {
                        _highestAssistsPerMin = match;
                    }
                    if(_highestDamageDealtPerMin == null || (float)match.LocalPlayerStats?.DamageDealt! / match.MatchDuration.Value.TotalMinutes > (float)_highestDamageDealtPerMin.LocalPlayerStats?.DamageDealt! / _highestDamageDealtPerMin.MatchDuration!.Value.TotalMinutes) {
                        _highestDamageDealtPerMin = match;
                    }
                    if(_highestDamageTakenPerMin == null || (float)match.LocalPlayerStats?.DamageTaken! / match.MatchDuration.Value.TotalMinutes > (float)_highestDamageTakenPerMin.LocalPlayerStats?.DamageTaken! / _highestDamageTakenPerMin.MatchDuration!.Value.TotalMinutes) {
                        _highestDamageTakenPerMin = match;
                    }
                    if(_highestHPRestoredPerMin == null || (float)match.LocalPlayerStats?.HPRestored! / match.MatchDuration.Value.TotalMinutes > (float)_highestHPRestoredPerMin.LocalPlayerStats?.HPRestored! / _highestHPRestoredPerMin.MatchDuration!.Value.TotalMinutes) {
                        _highestHPRestoredPerMin = match;
                    }
                    if(_highestTimeOnCrystalPerMin == null || match.LocalPlayerStats?.TimeOnCrystal / match.MatchDuration.Value.TotalMinutes > _highestTimeOnCrystalPerMin.LocalPlayerStats?.TimeOnCrystal / _highestTimeOnCrystalPerMin.MatchDuration!.Value.TotalMinutes) {
                        _highestTimeOnCrystalPerMin = match;
                    }
                }

                if(match.IsWin) {
                    _currentWinStreak++;
                    if(_currentWinStreak > _longestWinStreak) {
                        _longestWinStreak = _currentWinStreak;
                    }
                } else {
                    _currentWinStreak = 0;
                }
                if(match.IsLoss) {
                    _currentLossStreak++;
                    if(_currentLossStreak > _longestLossStreak) {
                        _longestLossStreak = _currentLossStreak;
                    }
                } else {
                    _currentLossStreak = 0;
                }
            }
            recordsWatch.Stop();

            //local player stats
            localPlayerWatch.Start();
            if(!match.IsSpectated && match.PostMatch != null) {
                _localPlayerMatchTime += match.MatchDuration ?? TimeSpan.Zero;
                AddPlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeam!, match.LocalPlayerTeamMember!);
                if(match.LocalPlayerTeamMember!.Job != null) {
                    var job = (Job)match.LocalPlayerTeamMember!.Job;
                    if(!_localPlayerJobStats.TryGetValue(job, out CCAggregateStats? jobStat)) {
                        jobStat = new();
                        _localPlayerJobStats.Add(job, jobStat);
                    }
                    IncrementAggregateStats(jobStat, match);
                }
            }
            localPlayerWatch.Stop();

            //arena stats
            arenaWatch.Start();
            if(match.Arena != null) {
                var arena = (CrystallineConflictMap)match.Arena;
                if(!_arenaStats.TryGetValue(arena, out CCAggregateStats? arenaStat)) {
                    arenaStat = new();
                    _arenaStats.Add(arena, arenaStat);
                }
                IncrementAggregateStats(arenaStat, match);
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
                    bool nameMatch = player.Alias.FullName.Contains(_playerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                    if(Plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                        nameMatch = _linkedPlayerAliases.Contains(player.Alias);
                    }
                    bool sideMatch = _playerFilter.TeamStatus == TeamStatus.Any
                        || _playerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                        || _playerFilter.TeamStatus == TeamStatus.Opponent && !isTeammate && !isLocalPlayer;
                    bool jobMatch = _playerFilter.AnyJob || _playerFilter.PlayerJob == player.Job;
                    if(!nameMatch || !sideMatch || !jobMatch) {
                        if(_jobStatSourceFilter.InheritFromPlayerFilter) {
                            jobStatsEligible = false;
                        }
                        if(_playerStatSourceFilter.InheritFromPlayerFilter) {
                            playerStatsEligible = false;
                        }
                    }
                    if(player.Job == null) {
                        jobStatsEligible = false;
                    }

                    if(!_jobStatSourceFilter.FilterState[StatSource.LocalPlayer] && isLocalPlayer) {
                        jobStatsEligible = false;
                    } else if(!_jobStatSourceFilter.FilterState[StatSource.Teammate] && isTeammate) {
                        jobStatsEligible = false;
                    } else if(!_jobStatSourceFilter.FilterState[StatSource.Opponent] && !isTeammate && !isLocalPlayer) {
                        jobStatsEligible = false;
                    } else if(!_jobStatSourceFilter.FilterState[StatSource.Spectated] && match.IsSpectated) {
                        jobStatsEligible = false;
                    }
                    var job = (Job)player.Job!;

                    aggregateStatsWatch.Start();
                    if(isTeammate) {
                        if(!_teammateStats.TryGetValue(player.Alias, out CCAggregateStats? teammateStat)) {
                            teammateStat = new();
                            _teammateStats.Add(player.Alias, teammateStat);
                        }
                        IncrementAggregateStats(teammateStat, match);
                        if(player.Job != null) {
                            if(!_teammateJobStats.TryGetValue(job, out CCAggregateStats? teammateJobStat)) {
                                teammateJobStat = new();
                                _teammateJobStats.Add(job, teammateJobStat);
                            }
                            IncrementAggregateStats(teammateJobStat, match);
                            if(!_teammateJobStatsLookup.TryGetValue(player.Alias, out Dictionary<Job, CCAggregateStats>? teammateJobStatLookup)) {
                                teammateJobStatLookup = new();
                                _teammateJobStatsLookup.Add(player.Alias, teammateJobStatLookup);
                            }
                            if(!teammateJobStatLookup.TryGetValue(job, out CCAggregateStats? teammateJobStatLookupJobStat)) {
                                teammateJobStatLookupJobStat = new();
                                teammateJobStatLookup.Add(job, teammateJobStatLookupJobStat);
                            }
                            IncrementAggregateStats(teammateJobStatLookupJobStat, match);
                        }
                    } else if(isOpponent) {
                        if(!_opponentStats.TryGetValue(player.Alias, out CCAggregateStats? opponentStat)) {
                            opponentStat = new();
                            _opponentStats.Add(player.Alias, opponentStat);
                        }
                        IncrementAggregateStats(opponentStat, match);
                        if(player.Job != null) {
                            if(!_opponentJobStats.TryGetValue(job, out CCAggregateStats? opponentJobStat)) {
                                opponentJobStat = new();
                                _opponentJobStats.Add(job, opponentJobStat);
                            }
                            IncrementAggregateStats(opponentJobStat, match);
                        }
                        if(!_opponentJobStatsLookup.TryGetValue(player.Alias, out Dictionary<Job, CCAggregateStats>? opponentJobStatLookup)) {
                            opponentJobStatLookup = new();
                            _opponentJobStatsLookup.Add(player.Alias, opponentJobStatLookup);
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
                        _jobTimes[job] += match.MatchDuration ?? TimeSpan.Zero;
                        AddPlayerJobStat(_jobStats[job], _jobTeamContributions[job], match, team.Value, player);
                    }

                    if(playerStatsEligible) {
                        if(!_playerStats.TryGetValue(player.Alias, out CCPlayerJobStats? playerStat)) {
                            playerStat = new();
                            _playerStats.Add(player.Alias, playerStat);
                            _playerTeamContributions.Add(player.Alias, new());
                            _playerJobStatsLookup.Add(player.Alias, new());
                            _playerTimes.Add(player.Alias, TimeSpan.Zero);
                        }
                        _playerTimes[player.Alias] += match.MatchDuration ?? TimeSpan.Zero;
                        AddPlayerJobStat(playerStat, _playerTeamContributions[player.Alias], match, team.Value, player);
                        if(player.Job != null) {
                            if(!_playerJobStatsLookup[player.Alias].ContainsKey((Job)player.Job)) {
                                _playerJobStatsLookup[player.Alias].Add((Job)player.Job, new());
                            }
                            IncrementAggregateStats(_playerJobStatsLookup[player.Alias][(Job)player.Job], match);
                        }
                    }
                    playerJobWatch.Stop();
                }
            }
            teamPlayerWatch.Stop();
            RefreshProgress = (float)matchesProcessed++ / matches.Count;
        }
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"records", recordsWatch.ElapsedMilliseconds.ToString()));
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"arena stats", arenaWatch.ElapsedMilliseconds.ToString()));
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"local player stats", localPlayerWatch.ElapsedMilliseconds.ToString()));
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"summary stats loop", aggregateStatsWatch.ElapsedMilliseconds.ToString()));
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"player/job stats loop", playerJobWatch.ElapsedMilliseconds.ToString()));
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"team player loop total", teamPlayerWatch.ElapsedMilliseconds.ToString()));
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"cc match loop total", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();

        //player linking
        if(Plugin.Configuration.EnablePlayerLinking) {
            //var manualLinks = Plugin.Storage.GetManualLinks().Query().ToList();
            var unLinks = Plugin.PlayerLinksService.ManualPlayerLinksCache.Where(x => x.IsUnlink).ToList();
            var checkPlayerLink = (PlayerAliasLink playerLink) => {
                if(playerLink.IsUnlink) return;
                if(playerLink.CurrentAlias is null) return;
                foreach(var linkedAlias in playerLink.LinkedAliases) {
                    bool blocked = unLinks.Where(x => x.CurrentAlias?.Equals(playerLink.CurrentAlias) ?? false && x.LinkedAliases.Contains(linkedAlias)).Any();
                    if(!blocked) {
                        bool anyMatch = false;
                        if(_playerStats.ContainsKey(linkedAlias)) {
                            anyMatch = true;
                            if(_playerStats.ContainsKey(playerLink.CurrentAlias)) {
                                _playerStats[playerLink.CurrentAlias] += _playerStats[linkedAlias];
                                _playerTeamContributions[playerLink.CurrentAlias] = _playerTeamContributions[playerLink.CurrentAlias].Concat(_playerTeamContributions[linkedAlias]).ToList();
                                foreach(var jobStat in _playerJobStatsLookup[linkedAlias]) {
                                    if(!_playerJobStatsLookup[playerLink.CurrentAlias].ContainsKey(jobStat.Key)) {
                                        _playerJobStatsLookup[playerLink.CurrentAlias].Add(jobStat.Key, new() {
                                            Matches = jobStat.Value.Matches,
                                        });
                                    } else {
                                        _playerJobStatsLookup[playerLink.CurrentAlias][jobStat.Key].Matches += jobStat.Value.Matches;
                                    }
                                }
                                _playerTimes[playerLink.CurrentAlias] += _playerTimes[linkedAlias];
                            } else {
                                _playerStats.Add(playerLink.CurrentAlias, _playerStats[linkedAlias]);
                                _playerTeamContributions.Add(playerLink.CurrentAlias, _playerTeamContributions[linkedAlias]);
                                _playerJobStatsLookup.Add(playerLink.CurrentAlias, _playerJobStatsLookup[linkedAlias]);
                                _playerTimes.Add(playerLink.CurrentAlias, _playerTimes[linkedAlias]);
                            }
                            _playerStats.Remove(linkedAlias);
                            _playerTeamContributions.Remove(linkedAlias);
                            _playerJobStatsLookup.Remove(linkedAlias);
                            _playerTimes.Remove(linkedAlias);
                        }
                        if(_teammateStats.ContainsKey(linkedAlias)) {
                            anyMatch = true;
                            if(_teammateStats.ContainsKey(playerLink.CurrentAlias)) {
                                _teammateStats[playerLink.CurrentAlias] += _teammateStats[linkedAlias];
                                foreach(var jobStat in _teammateJobStatsLookup[linkedAlias]) {
                                    if(!_teammateJobStatsLookup[playerLink.CurrentAlias].ContainsKey(jobStat.Key)) {
                                        _teammateJobStatsLookup[playerLink.CurrentAlias].Add(jobStat.Key, new() {
                                            Matches = jobStat.Value.Matches,
                                        });
                                    } else {
                                        _teammateJobStatsLookup[playerLink.CurrentAlias][jobStat.Key].Matches += jobStat.Value.Matches;
                                    }
                                }
                            } else {
                                _teammateStats.Add(playerLink.CurrentAlias, _teammateStats[linkedAlias]);
                                _teammateJobStatsLookup.Add(playerLink.CurrentAlias, _teammateJobStatsLookup[linkedAlias]);
                            }
                            _teammateStats.Remove(linkedAlias);
                            _teammateJobStatsLookup.Remove(linkedAlias);
                        }
                        if(_opponentStats.ContainsKey(linkedAlias)) {
                            anyMatch = true;
                            if(_opponentStats.ContainsKey(playerLink.CurrentAlias)) {
                                _opponentStats[playerLink.CurrentAlias] += _opponentStats[linkedAlias];
                                foreach(var jobStat in _opponentJobStatsLookup[linkedAlias]) {
                                    if(!_opponentJobStatsLookup[playerLink.CurrentAlias].ContainsKey(jobStat.Key)) {
                                        _opponentJobStatsLookup[playerLink.CurrentAlias].Add(jobStat.Key, new() {
                                            Matches = jobStat.Value.Matches,
                                        });
                                    } else {
                                        _opponentJobStatsLookup[playerLink.CurrentAlias][jobStat.Key].Matches += jobStat.Value.Matches;
                                    }
                                }
                            } else {
                                _opponentStats.Add(playerLink.CurrentAlias, _opponentStats[linkedAlias]);
                                _opponentJobStatsLookup.Add(playerLink.CurrentAlias, _opponentJobStatsLookup[linkedAlias]);
                            }
                            _opponentStats.Remove(linkedAlias);
                            _opponentJobStatsLookup.Remove(linkedAlias);
                        }
                        if(anyMatch) {
                            Plugin.Log.Debug($"Coalescing {linkedAlias} into {playerLink.CurrentAlias}...");
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
            if(Plugin.Configuration.EnableAutoPlayerLinking) {
                foreach(var playerLink in Plugin.PlayerLinksService.AutoPlayerLinksCache) {
                    try {
                        checkPlayerLink(playerLink);
                    } catch(Exception e) {
                        Plugin.Log.Error($"Unable to add player link: {e.GetType()} {e.Message}\n {e.StackTrace}");
                    }
                }
            }

            //manual links
            if(Plugin.Configuration.EnableManualPlayerLinking) {
                foreach(var playerLink in Plugin.PlayerLinksService.ManualPlayerLinksCache) {
                    try {
                        checkPlayerLink(playerLink);
                    } catch(Exception e) {
                        Plugin.Log.Error($"Unable to add player link: {e.GetType()} {e.Message}\n {e.StackTrace}");
                    }
                }
            }
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"player linking", s1.ElapsedMilliseconds.ToString()));
            s1.Restart();
        }

        foreach(var jobStat in _jobStats) {
            SetScoreboardStats(jobStat.Value, _jobTeamContributions[jobStat.Key], _jobTimes[jobStat.Key]);
        }
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"job scoreboards", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();

        foreach(var playerStat in _playerStats) {
            playerStat.Value.StatsAll.Job = _playerJobStatsLookup[playerStat.Key].OrderByDescending(x => x.Value.Matches).FirstOrDefault().Key;
            SetScoreboardStats(playerStat.Value, _playerTeamContributions[playerStat.Key], _playerTimes[playerStat.Key]);
        }
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"player scoreboards", s1.ElapsedMilliseconds.ToString()));
        s1.Restart();

        SetScoreboardStats(_localPlayerStats, _localPlayerTeamContributions, _localPlayerMatchTime);
        foreach(var teammateStat in _teammateStats) {
            teammateStat.Value.Job = _teammateJobStatsLookup[teammateStat.Key].OrderByDescending(x => x.Value.WinDiff).FirstOrDefault().Key;
        }
        foreach(var opponentStat in _opponentStats) {
            opponentStat.Value.Job = _opponentJobStatsLookup[opponentStat.Key].OrderBy(x => x.Value.WinDiff).FirstOrDefault().Key;
        }

        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
            //Players = _playerStats.Keys.ToList();
            PlayerStats = _playerStats;
            ActiveLinks = activeLinks;
            Jobs = _jobStats.Keys.ToList();
            JobStats = _jobStats;
            LocalPlayerStats = _localPlayerStats;
            LocalPlayerJobStats = _localPlayerJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            TeammateStats = _teammateStats.OrderBy(x => x.Value.Matches).OrderByDescending(x => x.Value.WinDiff).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            TeammateJobStats = _teammateJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            OpponentStats = _opponentStats.OrderBy(x => x.Value.Matches).OrderBy(x => x.Value.WinDiff).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            OpponentJobStats = _opponentJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            ArenaStats = _arenaStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            AverageMatchDuration = matches.Count > 0 ? _totalMatchTime / matches.Count : TimeSpan.Zero;
            Superlatives = new();
            if(_longestMatch != null) {
                AddSuperlative(_longestMatch, "Longest match", ImGuiHelper.GetTimeSpanString((TimeSpan)_longestMatch.MatchDuration!));
                AddSuperlative(_shortestMatch, "Shortest match", ImGuiHelper.GetTimeSpanString((TimeSpan)_shortestMatch!.MatchDuration!));
                AddSuperlative(_highestLoserProg, "Highest loser progress", _highestLoserProg!.LoserProgress!.ToString()!);
                AddSuperlative(_lowestWinnerProg, "Lowest winner progress", _lowestWinnerProg!.WinnerProgress!.ToString()!);
                if(_mostKills != null) {
                    AddSuperlative(_mostKills, "Most kills", _mostKills!.LocalPlayerStats!.Kills.ToString());
                    AddSuperlative(_mostDeaths, "Most deaths", _mostDeaths!.LocalPlayerStats!.Deaths.ToString());
                    AddSuperlative(_mostAssists, "Most assists", _mostAssists!.LocalPlayerStats!.Assists.ToString());
                    AddSuperlative(_mostDamageDealt, "Most damage dealt", _mostDamageDealt!.LocalPlayerStats!.DamageDealt.ToString());
                    AddSuperlative(_mostDamageTaken, "Most damage taken", _mostDamageTaken!.LocalPlayerStats!.DamageTaken.ToString());
                    AddSuperlative(_mostHPRestored, "Most HP restored", _mostHPRestored!.LocalPlayerStats!.HPRestored.ToString());
                    AddSuperlative(_mostTimeOnCrystal, "Longest time on crystal", ImGuiHelper.GetTimeSpanString(_mostTimeOnCrystal!.LocalPlayerStats!.TimeOnCrystal));
                    AddSuperlative(_highestKillsPerMin, "Highest kills per min", (_highestKillsPerMin!.LocalPlayerStats!.Kills / _highestKillsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                    AddSuperlative(_highestDeathsPerMin, "Highest deaths per min", (_highestDeathsPerMin!.LocalPlayerStats!.Deaths / _highestDeathsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                    AddSuperlative(_highestAssistsPerMin, "Highest assists per min", (_highestAssistsPerMin!.LocalPlayerStats!.Assists / _highestAssistsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                    AddSuperlative(_highestDamageDealtPerMin, "Highest damage dealt per min", (_highestDamageDealtPerMin!.LocalPlayerStats!.DamageDealt / _highestDamageDealtPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                    AddSuperlative(_highestDamageTakenPerMin, "Highest damage taken per min", (_highestDamageTakenPerMin!.LocalPlayerStats!.DamageTaken / _highestDamageTakenPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                    AddSuperlative(_highestHPRestoredPerMin, "Highest HP restored per min", (_highestHPRestoredPerMin!.LocalPlayerStats!.HPRestored / _highestHPRestoredPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                    AddSuperlative(_highestTimeOnCrystalPerMin, "Longest time on crystal per min", ImGuiHelper.GetTimeSpanString(_highestTimeOnCrystalPerMin!.LocalPlayerStats!.TimeOnCrystal / _highestTimeOnCrystalPerMin!.MatchDuration!.Value.TotalMinutes));
                }
            }
            LongestWinStreak = _longestWinStreak;
            LongestLossStreak = _longestLossStreak;
        } finally {
            RefreshLock.Release();
        }
        Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"total stats refresh", s0.ElapsedMilliseconds.ToString()));
    }

    private void SummaryProcessMatch(CrystallineConflictMatch match, bool remove = false) {
        _totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;

        //local player stats
        if(!match.IsSpectated && match.PostMatch != null) {
            if(remove) {
                _localPlayerMatchTime -= match.MatchDuration ?? TimeSpan.Zero;
                RemovePlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeam!, match.LocalPlayerTeamMember!);
            } else {
                _localPlayerMatchTime += match.MatchDuration ?? TimeSpan.Zero;
                AddPlayerJobStat(_localPlayerStats, _localPlayerTeamContributions, match, match.LocalPlayerTeam!, match.LocalPlayerTeamMember!);
            }

            if(match.LocalPlayerTeamMember!.Job != null) {
                var job = (Job)match.LocalPlayerTeamMember!.Job;
                if(!_localPlayerJobStats.TryGetValue(job, out CCAggregateStats? jobStat)) {
                    jobStat = new();
                    _localPlayerJobStats.Add(job, jobStat);
                }
                if(remove) {
                    DecrementAggregateStats(jobStat, match);
                } else {
                    IncrementAggregateStats(jobStat, match);
                }
            }
        }

        //arena stats
        if(match.Arena != null) {
            var arena = (CrystallineConflictMap)match.Arena;
            if(!_arenaStats.TryGetValue(arena, out CCAggregateStats? arenaStat)) {
                arenaStat = new();
                _arenaStats.Add(arena, arenaStat);
            }
            if(remove) {
                DecrementAggregateStats(arenaStat, match);
            } else {
                IncrementAggregateStats(arenaStat, match);
            }
        }

        //process player and job stats
        foreach(var team in match.Teams) {
            foreach(var player in team.Value.Players) {
                bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
                bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.Key == match.LocalPlayerTeam!.TeamName;
                bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;

                var job = (Job)player.Job!;

                if(isTeammate) {
                    if(!_teammateStats.TryGetValue(player.Alias, out CCAggregateStats? teammateStat)) {
                        teammateStat = new();
                        _teammateStats.Add(player.Alias, teammateStat);
                    }
                    if(remove) {
                        DecrementAggregateStats(teammateStat, match);
                    } else {
                        IncrementAggregateStats(teammateStat, match);
                    }
                    if(player.Job != null) {
                        if(!_teammateJobStats.TryGetValue(job, out CCAggregateStats? teammateJobStat)) {
                            teammateJobStat = new();
                            _teammateJobStats.Add(job, teammateJobStat);
                        }
                        if(remove) {
                            DecrementAggregateStats(teammateJobStat, match);
                        } else {
                            IncrementAggregateStats(teammateJobStat, match);
                        }
                        if(!_teammateJobStatsLookup.TryGetValue(player.Alias, out Dictionary<Job, CCAggregateStats>? teammateJobStatLookup)) {
                            teammateJobStatLookup = new();
                            _teammateJobStatsLookup.Add(player.Alias, teammateJobStatLookup);
                        }
                        if(!teammateJobStatLookup!.TryGetValue(job, out CCAggregateStats? teammateJobStatLookupJobStat)) {
                            teammateJobStatLookupJobStat = new();
                            teammateJobStatLookup.Add(job, teammateJobStatLookupJobStat);
                        }
                        if(remove) {
                            DecrementAggregateStats(teammateJobStatLookupJobStat, match);
                        } else {
                            IncrementAggregateStats(teammateJobStatLookupJobStat, match);
                        }
                    }
                } else if(isOpponent) {
                    if(!_opponentStats.TryGetValue(player.Alias, out CCAggregateStats? opponentStat)) {
                        opponentStat = new();
                        _opponentStats.Add(player.Alias, opponentStat);
                    }
                    if(remove) {
                        DecrementAggregateStats(opponentStat, match);
                    } else {
                        IncrementAggregateStats(opponentStat, match);
                    }
                    if(player.Job != null) {
                        if(!_opponentJobStats.TryGetValue(job, out CCAggregateStats? opponentJobStat)) {
                            opponentJobStat = new();
                            _opponentJobStats.Add(job, opponentJobStat);
                        }
                        if(remove) {
                            DecrementAggregateStats(opponentJobStat, match);
                        } else {
                            IncrementAggregateStats(opponentJobStat, match);
                        }
                    }
                    if(!_opponentJobStatsLookup.TryGetValue(player.Alias, out Dictionary<Job, CCAggregateStats>? opponentJobStatLookup)) {
                        opponentJobStatLookup = new();
                        _opponentJobStatsLookup.Add(player.Alias, opponentJobStatLookup);
                    }
                    if(!opponentJobStatLookup!.TryGetValue(job, out CCAggregateStats? opponentJobStatLookupJobStat)) {
                        opponentJobStatLookupJobStat = new();
                        opponentJobStatLookup.Add(job, opponentJobStatLookupJobStat);
                    }
                    if(remove) {
                        DecrementAggregateStats(opponentJobStatLookupJobStat, match);
                    } else {
                        IncrementAggregateStats(opponentJobStatLookupJobStat, match);
                    }
                }
            }
        }
    }

    private void JobsProcessMatch(CrystallineConflictMatch match, bool remove = false) {
        foreach(var team in match.Teams) {
            foreach(var player in team.Value.Players) {
                bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
                bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.Key == match.LocalPlayerTeam!.TeamName;
                bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;
                bool jobStatsEligible = true;
                bool nameMatch = player.Alias.FullName.Contains(_playerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                if(Plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                    nameMatch = _linkedPlayerAliases.Contains(player.Alias);
                }
                bool sideMatch = _playerFilter.TeamStatus == TeamStatus.Any
                    || _playerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                    || _playerFilter.TeamStatus == TeamStatus.Opponent && isOpponent;
                bool jobMatch = _playerFilter.AnyJob || _playerFilter.PlayerJob == player.Job;
                if(!nameMatch || !sideMatch || !jobMatch) {
                    if(_jobStatSourceFilter.InheritFromPlayerFilter) {
                        jobStatsEligible = false;
                    }
                }
                if(player.Job == null) {
                    jobStatsEligible = false;
                }

                if(!_jobStatSourceFilter.FilterState[StatSource.LocalPlayer] && isLocalPlayer) {
                    jobStatsEligible = false;
                } else if(!_jobStatSourceFilter.FilterState[StatSource.Teammate] && isTeammate) {
                    jobStatsEligible = false;
                } else if(!_jobStatSourceFilter.FilterState[StatSource.Opponent] && !isTeammate && !isLocalPlayer) {
                    jobStatsEligible = false;
                } else if(!_jobStatSourceFilter.FilterState[StatSource.Spectated] && match.IsSpectated) {
                    jobStatsEligible = false;
                }
                var job = (Job)player.Job!;

                if(jobStatsEligible) {
                    if(remove) {
                        _jobTimes[job] -= match.MatchDuration ?? TimeSpan.Zero;
                        RemovePlayerJobStat(_jobStats[job], _jobTeamContributions[job], match, team.Value, player);
                    } else {
                        _jobTimes[job] += match.MatchDuration ?? TimeSpan.Zero;
                        AddPlayerJobStat(_jobStats[job], _jobTeamContributions[job], match, team.Value, player);
                    }
                }
            }
        }
    }

    private void PlayersProcessMatch(CrystallineConflictMatch match, bool remove = false) {
        foreach(var team in match.Teams) {
            foreach(var player in team.Value.Players) {
                bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
                bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.Key == match.LocalPlayerTeam!.TeamName;
                bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;
                bool playerStatsEligible = true;
                bool nameMatch = player.Alias.FullName.Contains(_playerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                if(Plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                    nameMatch = _linkedPlayerAliases.Contains(player.Alias);
                }
                bool sideMatch = _playerFilter.TeamStatus == TeamStatus.Any
                    || _playerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                    || _playerFilter.TeamStatus == TeamStatus.Opponent && isOpponent;
                bool jobMatch = _playerFilter.AnyJob || _playerFilter.PlayerJob == player.Job;
                if(!nameMatch || !sideMatch || !jobMatch) {
                    if(_playerStatSourceFilter.InheritFromPlayerFilter) {
                        playerStatsEligible = false;
                    }
                }

                if(playerStatsEligible) {
                    if(!_playerStats.TryGetValue(player.Alias, out CCPlayerJobStats? playerStat)) {
                        playerStat = new();
                        _playerStats.Add(player.Alias, playerStat);
                        _playerTeamContributions.Add(player.Alias, new());
                        _playerJobStatsLookup.Add(player.Alias, new());
                        _playerTimes.Add(player.Alias, TimeSpan.Zero);
                    }

                    if(remove) {
                        _playerTimes[player.Alias] -= match.MatchDuration ?? TimeSpan.Zero;
                        RemovePlayerJobStat(playerStat, _playerTeamContributions[player.Alias], match, team.Value, player);
                    } else {
                        _playerTimes[player.Alias] += match.MatchDuration ?? TimeSpan.Zero;
                        AddPlayerJobStat(playerStat, _playerTeamContributions[player.Alias], match, team.Value, player);
                    }
                    if(player.Job != null) {
                        if(!_playerJobStatsLookup[player.Alias].ContainsKey((Job)player.Job)) {
                            _playerJobStatsLookup[player.Alias].Add((Job)player.Job, new());
                        }
                        if(remove) {
                            DecrementAggregateStats(_playerJobStatsLookup[player.Alias][(Job)player.Job], match);
                        } else {
                            IncrementAggregateStats(_playerJobStatsLookup[player.Alias][(Job)player.Job], match);
                        }
                    }
                }
            }
        }
    }

    private void RecordsProcessMatch(CrystallineConflictMatch match, bool remove = false) {

    }

    internal static void AddPlayerJobStat(CCPlayerJobStats statsModel, List<CCScoreboardDouble> teamContributions,
        CrystallineConflictMatch match, CrystallineConflictTeam team, CrystallineConflictPlayer player, bool remove = false) {
        bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
        bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.TeamName == match.LocalPlayerTeam!.TeamName;
        bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;

        if(remove) {
            statsModel.StatsAll.Matches--;
            if(match.MatchWinner == team.TeamName) {
                statsModel.StatsAll.Wins--;
            } else if(match.MatchWinner != null) {
                statsModel.StatsAll.Losses--;
            }
        } else {
            statsModel.StatsAll.Matches++;
            if(match.MatchWinner == team.TeamName) {
                statsModel.StatsAll.Wins++;
            } else if(match.MatchWinner != null) {
                statsModel.StatsAll.Losses++;
            }
        }

        if(!match.IsSpectated) {
            if(isTeammate) {
                IncrementAggregateStats(statsModel.StatsTeammate, match, remove);
            } else if(isOpponent) {
                IncrementAggregateStats(statsModel.StatsOpponent, match, remove);
            }
        }

        if(match.PostMatch != null) {
            var teamPostMatch = match.PostMatch.Teams.Where(x => x.Key == team.TeamName).FirstOrDefault().Value;
            var playerPostMatch = teamPostMatch.PlayerStats.Where(x => x.Player?.Equals(player.Alias) ?? false).FirstOrDefault();
            if(playerPostMatch != null) {
                var playerScoreboard = playerPostMatch.ToScoreboard();
                var teamScoreboard = teamPostMatch.TeamStats.ToScoreboard();
                if(remove) {
                    statsModel.ScoreboardTotal -= playerScoreboard;
                    teamContributions.Remove(new(playerScoreboard, teamScoreboard));
                } else {
                    statsModel.ScoreboardTotal += playerScoreboard;
                    teamContributions.Add(new(playerScoreboard, teamScoreboard));
                }
            }
        }
    }

    internal void RemovePlayerJobStat(CCPlayerJobStats statsModel, List<CCScoreboardDouble> teamContributions,
    CrystallineConflictMatch match, CrystallineConflictTeam team, CrystallineConflictPlayer player) {
        bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
        bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.TeamName == match.LocalPlayerTeam!.TeamName;
        bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;

        statsModel.StatsAll.Matches--;
        if(match.MatchWinner == team.TeamName) {
            statsModel.StatsAll.Wins--;
        } else if(match.MatchWinner != null) {
            statsModel.StatsAll.Losses--;
        }

        if(!match.IsSpectated) {
            if(isTeammate) {
                DecrementAggregateStats(statsModel.StatsTeammate, match);
            } else if(isOpponent) {
                DecrementAggregateStats(statsModel.StatsOpponent, match);
            }
        }

        if(match.PostMatch != null) {
            var teamPostMatch = match.PostMatch.Teams.Where(x => x.Key == team.TeamName).FirstOrDefault().Value;
            var playerPostMatch = teamPostMatch.PlayerStats.Where(x => x.Player?.Equals(player.Alias) ?? false).FirstOrDefault();
            if(playerPostMatch != null) {
                var playerScoreboard = playerPostMatch.ToScoreboard();
                var teamScoreboard = teamPostMatch.TeamStats.ToScoreboard();

                statsModel.ScoreboardTotal -= playerScoreboard;
                teamContributions.Remove(new(playerScoreboard, teamScoreboard));
            }
        }
    }

    internal static void SetScoreboardStats(CCPlayerJobStats stats, List<CCScoreboardDouble> teamContributions, TimeSpan time) {
        var statMatches = teamContributions.Count;
        //set average stats
        if(statMatches > 0) {
            stats.StatsPersonal.Matches = stats.StatsTeammate.Matches + stats.StatsOpponent.Matches;
            stats.StatsPersonal.Wins = stats.StatsTeammate.Wins + stats.StatsOpponent.Wins;
            stats.StatsPersonal.Losses = stats.StatsTeammate.Losses + stats.StatsOpponent.Losses;

            stats.ScoreboardPerMatch = (CCScoreboardDouble)stats.ScoreboardTotal / statMatches;
            stats.ScoreboardPerMin = (CCScoreboardDouble)stats.ScoreboardTotal / time.TotalMinutes;

            stats.ScoreboardContrib.Kills = teamContributions.OrderBy(x => x.Kills).ElementAt(statMatches / 2).Kills;
            stats.ScoreboardContrib.Deaths = teamContributions.OrderBy(x => x.Deaths).ElementAt(statMatches / 2).Deaths;
            stats.ScoreboardContrib.Assists = teamContributions.OrderBy(x => x.Assists).ElementAt(statMatches / 2).Assists;
            stats.ScoreboardContrib.DamageDealt = teamContributions.OrderBy(x => x.DamageDealt).ElementAt(statMatches / 2).DamageDealt;
            stats.ScoreboardContrib.DamageTaken = teamContributions.OrderBy(x => x.DamageTaken).ElementAt(statMatches / 2).DamageTaken;
            stats.ScoreboardContrib.HPRestored = teamContributions.OrderBy(x => x.HPRestored).ElementAt(statMatches / 2).HPRestored;
            stats.ScoreboardContrib.TimeOnCrystal = teamContributions.OrderBy(x => x.TimeOnCrystal).ElementAt(statMatches / 2).TimeOnCrystal;
            stats.ScoreboardContrib.KillsAndAssists = teamContributions.OrderBy(x => x.KillsAndAssists).ElementAt(statMatches / 2).KillsAndAssists;
        }
    }

    internal static void IncrementAggregateStats(CCAggregateStats stats, CrystallineConflictMatch match, bool decrement = false) {
        if(decrement) {
            stats.Matches--;
            if(match.IsWin) {
                stats.Wins--;
            } else if(match.IsLoss) {
                stats.Losses--;
            }
        } else {
            stats.Matches++;
            if(match.IsWin) {
                stats.Wins++;
            } else if(match.IsLoss) {
                stats.Losses++;
            }
        }
    }

    internal void DecrementAggregateStats(CCAggregateStats stats, CrystallineConflictMatch match) {
        stats.Matches--;
        if(match.IsWin) {
            stats.Wins--;
        } else if(match.IsLoss) {
            stats.Losses--;
        }
    }

    private void AddSuperlative(CrystallineConflictMatch? match, string sup, string val) {
        if(match == null) return;
        if(Superlatives.TryGetValue(match, out List<(string, string)>? value)) {
            value.Add((sup, val));
        } else {
            //Plugin.Log.Debug($"adding superlative {sup} {val} to {match.Id.ToString()}");
            Superlatives.Add(match, new() { (sup, val) });
        }
    }

    protected List<CrystallineConflictMatch> ApplyFilter(MatchTypeFilter filter, List<CrystallineConflictMatch> matches) {
        List<CrystallineConflictMatch> filteredMatches = new(matches);
        filteredMatches = filteredMatches.Where(x => filter.FilterState[x.MatchType]).ToList();
        return filteredMatches;
    }

    protected List<CrystallineConflictMatch> ApplyFilter(ArenaFilter filter, List<CrystallineConflictMatch> matches) {
        List<CrystallineConflictMatch> filteredMatches = new(matches);
        filteredMatches = filteredMatches.Where(x => (x.Arena == null && filter.AllSelected) || filter.FilterState[(CrystallineConflictMap)x.Arena!]).ToList();
        return filteredMatches;
    }

    protected List<CrystallineConflictMatch> ApplyFilter(LocalPlayerJobFilter filter, List<CrystallineConflictMatch> matches) {
        List<CrystallineConflictMatch> filteredMatches = new(matches);
        if(!filter.AnyJob) {
            if(filter.JobRole != null) {
                filteredMatches = filteredMatches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && PlayerJobHelper.GetSubRoleFromJob(x.LocalPlayerTeamMember.Job) == filter.JobRole).ToList();
            } else {
                filteredMatches = filteredMatches.Where(x => x.LocalPlayer != null && x.LocalPlayerTeamMember != null && x.LocalPlayerTeamMember.Job == filter.PlayerJob).ToList();
            }
        }
        return filteredMatches;
    }

    protected List<CrystallineConflictMatch> ApplyFilter(OtherPlayerFilter filter, List<CrystallineConflictMatch> matches) {
        List<CrystallineConflictMatch> filteredMatches = new(matches);
        List<PlayerAlias> linkedPlayerAliases = new();
        if(!filter.PlayerNamesRaw.IsNullOrEmpty() && Plugin.Configuration.EnablePlayerLinking) {
            linkedPlayerAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(filter.PlayerNamesRaw);
        }
        filteredMatches = filteredMatches.Where(x => {
            foreach(var team in x.Teams) {
                if(filter.TeamStatus == TeamStatus.Teammate && team.Key != x.LocalPlayerTeam?.TeamName) {
                    continue;
                } else if(filter.TeamStatus == TeamStatus.Opponent && team.Key == x.LocalPlayerTeam?.TeamName) {
                    continue;
                }
                foreach(var player in team.Value.Players) {
                    if(!filter.AnyJob && player.Job != filter.PlayerJob) {
                        continue;
                    }
                    if(Plugin.Configuration.EnablePlayerLinking) {
                        if(player.Alias.FullName.Contains(filter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase)
                        || linkedPlayerAliases.Any(x => x.Equals(player.Alias))) {
                            return true;
                        }
                    } else {
                        if(player.Alias.FullName.Contains(filter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase)) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }).ToList();
        return filteredMatches;
    }

    protected List<CrystallineConflictMatch> ApplyFilter(TierFilter filter, List<CrystallineConflictMatch> matches) {
        List<CrystallineConflictMatch> filteredMatches = new(matches);
        filteredMatches = filteredMatches.Where(x => {
            CrystallineConflictPlayer highestPlayer = new();
            try {
                highestPlayer = x.Players.OrderByDescending(y => y.Rank).First();
            } catch {
                //Plugin.Log.Error($"{x.Id} {x.DutyStartTime}");
                return true;
            }
            return x.MatchType != CrystallineConflictMatchType.Ranked || highestPlayer.Rank == null
            || (highestPlayer.Rank >= filter.TierLow && highestPlayer.Rank <= filter.TierHigh) || (highestPlayer.Rank >= filter.TierHigh && highestPlayer.Rank <= filter.TierLow);
        }).ToList();
        return filteredMatches;
    }

    protected List<CrystallineConflictMatch> ApplyFilter(ResultFilter filter, List<CrystallineConflictMatch> matches) {
        List<CrystallineConflictMatch> filteredMatches = new(matches);
        if(filter.Result == MatchResult.Win) {
            filteredMatches = filteredMatches.Where(x => x.IsWin).ToList();
        } else if(filter.Result == MatchResult.Loss) {
            filteredMatches = filteredMatches.Where(x => !x.IsWin && x.MatchWinner != null && !x.IsSpectated).ToList();
        } else if(filter.Result == MatchResult.Other) {
            filteredMatches = filteredMatches.Where(x => x.IsSpectated || x.MatchWinner == null).ToList();
        }
        return filteredMatches;
    }

    protected List<CrystallineConflictMatch> ApplyFilter(MiscFilter filter, List<CrystallineConflictMatch> matches) {
        List<CrystallineConflictMatch> filteredMatches = new(matches);
        if(filter.MustHaveStats) {
            filteredMatches = filteredMatches.Where(x => x.PostMatch is not null).ToList();
        }
        if(!filter.IncludeSpectated) {
            filteredMatches = filteredMatches.Where(x => !x.IsSpectated).ToList();
        }
        return filteredMatches;
    }
}
