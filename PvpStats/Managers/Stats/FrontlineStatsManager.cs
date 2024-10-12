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
    public static float[] BattleHighPerLifeRange = [10.0f, 100.0f];

    internal FrontlineStatsManager(Plugin plugin) : base(plugin, plugin.FLCache) {
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

    public static void AddPlayerJobStat(FLPlayerJobStats statsModel, ConcurrentDictionary<FLScoreboardDouble, byte> teamContributions,
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
                if(remove) {
                    statsModel.ScoreboardTotal -= playerScoreboard;
                    //teamContributions.TryTake(new(playerScoreboard, teamScoreboard));
                    var toRemove = new FLScoreboardDouble(playerScoreboard, teamScoreboard);
                    if(!teamContributions.TryRemove(toRemove, out _)) {
#if DEBUG
                        Plugin.Log2.Warning($"failed to remove teamcontrib!, {match.DutyStartTime} {player.Name}");
#endif
                    }
                } else {
                    statsModel.ScoreboardTotal += playerScoreboard;
                    teamContributions.TryAdd(new(playerScoreboard, teamScoreboard), 0);
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
