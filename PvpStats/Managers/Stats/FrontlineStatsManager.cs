using Dalamud.Utility;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PvpStats.Managers.Stats;
internal class FrontlineStatsManager : StatsManager<FrontlineMatch> {

    public static float[] KillsPerMatchRange = [0.5f, 8.0f];
    public static float[] DeathsPerMatchRange = [0.5f, 5.0f];
    public static float[] AssistsPerMatchRange = [10.0f, 35.0f];
    public static float[] DamageDealtPerMatchRange = [300000f, 1500000f];
    public static float[] DamageDealtToPCsPerMatchRange = [300000f, 1500000f];
    public static float[] DamageToOtherPerMatchRange = [100000f, 1800000f];
    public static float[] DamageTakenPerMatchRange = [300000f, 1250000f];
    public static float[] HPRestoredPerMatchRange = [300000f, 1600000f];
    public static float AverageMatchLength = 15f;
    public static float[] KillsPerMinRange = [KillsPerMatchRange[0] / AverageMatchLength, KillsPerMatchRange[1] / AverageMatchLength];
    public static float[] DeathsPerMinRange = [DeathsPerMatchRange[0] / AverageMatchLength, DeathsPerMatchRange[1] / AverageMatchLength];
    public static float[] AssistsPerMinRange = [AssistsPerMatchRange[0] / AverageMatchLength, AssistsPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageDealtPerMinRange = [DamageDealtPerMatchRange[0] / AverageMatchLength, DamageDealtPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageDealtToPCsPerMinRange = [DamageDealtToPCsPerMatchRange[0] / AverageMatchLength, DamageDealtToPCsPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageToOtherPerMinRange = [DamageToOtherPerMatchRange[0] / AverageMatchLength, DamageToOtherPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageTakenPerMinRange = [DamageTakenPerMatchRange[0] / AverageMatchLength, DamageTakenPerMatchRange[1] / AverageMatchLength];
    public static float[] HPRestoredPerMinRange = [HPRestoredPerMatchRange[0] / AverageMatchLength, HPRestoredPerMatchRange[1] / AverageMatchLength];
    public static float[] ContribRange = [0 / 24f, 2 / 24f];
    public static float[] DamagePerKARange = [20000f, 40000f];
    public static float[] DamagePerLifeRange = [100000f, 400000f];
    public static float[] DamageTakenPerLifeRange = [120000f, 300000f];
    public static float[] HPRestoredPerLifeRange = [120000f, 300000f];
    public static float[] KDARange = [4.0f, 20.0f];
    public static float[] BattleHighPerLifeRange = [20.0f, 100.0f];
    public static float[] KillParticipationRange = [0.1f, 0.5f];

    internal FrontlineStatsManager(Plugin plugin) : base(plugin, plugin.FLCache) {
    }

    public static void IncrementAggregateStats(FLAggregateStats stats, FrontlineMatch match, bool decrement = false) {
        if(decrement) {
            Interlocked.Decrement(ref stats.Matches);
            if(match.Result == 0) {
                Interlocked.Decrement(ref stats.FirstPlaces);
            } else if(match.Result == 1) {
                Interlocked.Decrement(ref stats.SecondPlaces);
            } else if(match.Result == 2) {
                Interlocked.Decrement(ref stats.ThirdPlaces);
            }
        } else {
            Interlocked.Increment(ref stats.Matches);
            if(match.Result == 0) {
                Interlocked.Increment(ref stats.FirstPlaces);
            } else if(match.Result == 1) {
                Interlocked.Increment(ref stats.SecondPlaces);
            } else if(match.Result == 2) {
                Interlocked.Increment(ref stats.ThirdPlaces);
            }
        }
    }

    public static void IncrementAggregateStats(FLAggregateStats stats, FrontlineMatch match, FrontlinePlayer player, bool decrement = false) {
        if(decrement) {
            Interlocked.Decrement(ref stats.Matches);
        } else {
            Interlocked.Increment(ref stats.Matches);
        }

        if(match.Teams.ContainsKey(player.Team)) {
            switch(match.Teams[player.Team].Placement) {
                case 0:
                    if(decrement) {
                        Interlocked.Decrement(ref stats.FirstPlaces);
                    } else {
                        Interlocked.Increment(ref stats.FirstPlaces);
                    }
                    break;
                case 1:
                    if(decrement) {
                        Interlocked.Decrement(ref stats.SecondPlaces);
                    } else {
                        Interlocked.Increment(ref stats.SecondPlaces);
                    }
                    break;
                case 2:
                    if(decrement) {
                        Interlocked.Decrement(ref stats.ThirdPlaces);
                    } else {
                        Interlocked.Increment(ref stats.ThirdPlaces);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    public static void AddPlayerJobStat(FLPlayerJobStats statsModel, List<FLScoreboardDouble> teamContributions,
    FrontlineMatch match, FrontlinePlayer player, FLScoreboardTally? teamScoreboard, bool remove = false) {
        IncrementAggregateStats(statsModel.StatsAll, match, player, remove);
        if(match.PlayerScoreboards != null) {
            var playerScoreboard = new FLScoreboardTally(match.PlayerScoreboards[player.Name]);
            if(playerScoreboard != null && teamScoreboard != null) {
                playerScoreboard.TeamKills = teamScoreboard.Kills;
                //statsModel.ScoreboardTotal.MatchTime += match.PostMatch.MatchDuration;
                if(remove) {
                    statsModel.ScoreboardTotal.RemoveScoreboard(playerScoreboard);
                    teamContributions.Remove(new(playerScoreboard, teamScoreboard));
                } else {
                    statsModel.ScoreboardTotal.AddScoreboard(playerScoreboard);
                    teamContributions.Add(new(playerScoreboard, teamScoreboard));
                }
            }
        }
    }

    public static void AddPlayerJobStat(FLPlayerJobStats statsModel, ConcurrentDictionary<int, FLScoreboardDouble> teamContributions,
    FrontlineMatch match, FrontlinePlayer player, FLScoreboardTally? teamScoreboard, bool remove = false) {
        IncrementAggregateStats(statsModel.StatsAll, match, player, remove);
        if(match.PlayerScoreboards != null) {
            var playerScoreboard = new FLScoreboardTally(match.PlayerScoreboards[player.Name]);
            if(playerScoreboard != null && teamScoreboard != null) {
                playerScoreboard.TeamKills = teamScoreboard.Kills;
                var hashCode = HashCode.Combine(match.GetHashCode(), player.Name);
                if(remove) {
                    statsModel.ScoreboardTotal.RemoveScoreboard(playerScoreboard);
                    if(!teamContributions.TryRemove(hashCode, out _)) {
#if DEBUG
                        Plugin.Log2.Warning($"failed to remove team contrib!, {match.DutyStartTime} {player.Name}");
#endif
                    }
                } else {
                    statsModel.ScoreboardTotal.AddScoreboard(playerScoreboard);
                    teamContributions.TryAdd(hashCode, new(playerScoreboard, teamScoreboard));
                }
            }
        }
    }

    public static void SetScoreboardStats(FLPlayerJobStats stats, List<FLScoreboardDouble> teamContributions, TimeSpan time) {
        var statMatches = teamContributions.Count;

        //stats.ScoreboardTotal.Size--;
        stats.ScoreboardTotal.Size = statMatches;
        stats.ScoreboardPerMatch = (FLScoreboardDouble)stats.ScoreboardTotal / statMatches;
        stats.ScoreboardPerMin = (FLScoreboardDouble)stats.ScoreboardTotal / (double)time.TotalMinutes;

        if(statMatches > 0) {
            //stats.StatsPersonal.Matches = stats.StatsTeammate.Matches + stats.StatsOpponent.Matches;
            //stats.StatsPersonal.Wins = stats.StatsTeammate.Wins + stats.StatsOpponent.Wins;
            //stats.StatsPersonal.Losses = stats.StatsTeammate.Losses + stats.StatsOpponent.Losses;
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
        } else {
            stats.ScoreboardContrib = new();
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
                if(filter.TeamStatus == TeamStatus.Teammate && player.Team != x.LocalPlayerTeamMember?.Team) {
                    continue;
                } else if(filter.TeamStatus == TeamStatus.Opponent && player.Team == x.LocalPlayerTeamMember?.Team) {
                    continue;
                }
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
