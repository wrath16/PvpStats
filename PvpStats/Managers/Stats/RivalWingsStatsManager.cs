﻿using Dalamud.Utility;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Managers.Stats;
internal class RivalWingsStatsManager : StatsManager<RivalWingsMatch> {

    public static float[] KillsPerMatchRange = [1.0f, 7.0f];
    public static float[] DeathsPerMatchRange = [1.0f, 7.0f];
    public static float[] AssistsPerMatchRange = [7f, 25f];
    public static float[] DamageDealtToPCsPerMatchRange = [300000f, 1800000f];
    public static float[] DamageDealtToOtherPerMatchRange = [100000f, 3000000f];
    public static float[] DamageDealtPerMatchRange = [DamageDealtToPCsPerMatchRange[0] + DamageDealtToOtherPerMatchRange[0], DamageDealtToPCsPerMatchRange[1] + DamageDealtToOtherPerMatchRange[1]];
    public static float[] DamageTakenPerMatchRange = [400000f, 1500000f];
    public static float[] HPRestoredPerMatchRange = [100000f, 1000000f];
    public static float[] CeruleumPerMatchRange = [20f, 120f];
    public static float AverageMatchLength = 10f;
    public static float[] KillsPerMinRange = [KillsPerMatchRange[0] / AverageMatchLength, KillsPerMatchRange[1] / AverageMatchLength];
    public static float[] DeathsPerMinRange = [DeathsPerMatchRange[0] / AverageMatchLength, DeathsPerMatchRange[1] / AverageMatchLength];
    public static float[] AssistsPerMinRange = [AssistsPerMatchRange[0] / AverageMatchLength, AssistsPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageDealtToPCsPerMinRange = [DamageDealtToPCsPerMatchRange[0] / AverageMatchLength, DamageDealtToPCsPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageDealtToOtherPerMinRange = [DamageDealtToOtherPerMatchRange[0] / AverageMatchLength, DamageDealtToOtherPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageDealtPerMinRange = [DamageDealtPerMatchRange[0] / AverageMatchLength, DamageDealtPerMatchRange[1] / AverageMatchLength];
    public static float[] DamageTakenPerMinRange = [DamageTakenPerMatchRange[0] / AverageMatchLength, DamageTakenPerMatchRange[1] / AverageMatchLength];
    public static float[] HPRestoredPerMinRange = [HPRestoredPerMatchRange[0] / AverageMatchLength, HPRestoredPerMatchRange[1] / AverageMatchLength];
    public static float[] CeruleumPerMinRange = [CeruleumPerMatchRange[0] / AverageMatchLength, CeruleumPerMatchRange[1] / AverageMatchLength];

    public static float[] ContribRange = [0 / 24f, 2 / 24f];
    public static float[] DamagePerKARange = [35000f, 100000f];
    public static float[] DamagePerLifeRange = [150000f, 1500000f];
    public static float[] DamageTakenPerLifeRange = [120000f, 500000f];
    public static float[] HPRestoredPerLifeRange = [80000f, 600000f];
    public static float[] KDARange = [2.0f, 15.0f];

    public RivalWingsStatsManager(Plugin plugin) : base(plugin, plugin.RWCache) {
    }

    internal static void IncrementAggregateStats(CCAggregateStats stats, RivalWingsMatch match, bool decrement = false) {
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

    internal static void AddPlayerJobStat(RWPlayerJobStats statsModel, List<RWScoreboardDouble> teamContributions,
    RivalWingsMatch match, RivalWingsPlayer player, RivalWingsScoreboard? teamScoreboard, bool remove = false) {

        if(remove) {
            statsModel.StatsAll.Matches--;
            if(match.MatchWinner == player.Team) {
                statsModel.StatsAll.Wins--;
            } else if(match.MatchWinner != null) {
                statsModel.StatsAll.Losses--;
            }
        } else {
            statsModel.StatsAll.Matches++;
            if(match.MatchWinner == player.Team) {
                statsModel.StatsAll.Wins++;
            } else if(match.MatchWinner != null) {
                statsModel.StatsAll.Losses++;
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
            }
        }
    }

    internal static void SetScoreboardStats(RWPlayerJobStats stats, List<RWScoreboardDouble> teamContributions, TimeSpan time) {
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
            stats.ScoreboardContrib.KillsAndAssists = teamContributions.OrderBy(x => x.KillsAndAssists).ElementAt(statMatches / 2).KillsAndAssists;
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
