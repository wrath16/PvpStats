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
internal class CrystallineConflictStatsManager : StatsManager<CrystallineConflictMatch> {

    public static float[] KillsPerMatchRange = [1.0f, 4.0f];
    public static float[] DeathsPerMatchRange = [1.5f, 3.5f];
    public static float[] AssistsPerMatchRange = [4.0f, 8.0f];
    public static float[] DamageDealtPerMatchRange = [400000f, 800000f];
    public static float[] DamageTakenPerMatchRange = [400000f, 800000f];
    public static float[] HPRestoredPerMatchRange = [350000f, 1000000f];
    public static float[] TimeOnCrystalPerMatchRange = [35f, 110f];
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
    public static float[] DamageTakenPerLifeRange = [180000f, 380000f];
    public static float[] HPRestoredPerLifeRange = [120000f, 600000f];
    public static float[] KDARange = [(KillsPerMatchRange[0] + AssistsPerMatchRange[0]) / DeathsPerMatchRange[1], (KillsPerMatchRange[1] + AssistsPerMatchRange[1]) / DeathsPerMatchRange[0]];

    internal CrystallineConflictStatsManager(Plugin plugin) : base(plugin, plugin.CCCache) {
    }

    public static void IncrementAggregateStats(CCAggregateStats stats, CrystallineConflictMatch match, bool decrement = false) {
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

    public static void IncrementAggregateStats(CCAggregateStats stats, CrystallineConflictMatch match, CrystallineConflictTeam team, CrystallineConflictPlayer player, bool decrement = false) {
        if(decrement) {
            stats.Matches--;
            if(match.MatchWinner == team.TeamName) {
                stats.Wins--;
            } else if(match.MatchWinner != null) {
                stats.Losses--;
            }
        } else {
            stats.Matches++;
            if(match.MatchWinner == team.TeamName) {
                stats.Wins++;
            } else if(match.MatchWinner != null) {
                stats.Losses++;
            }
        }
    }

    public static void AddPlayerJobStat(CCPlayerJobStats statsModel, List<CCScoreboardDouble> teamContributions,
        CrystallineConflictMatch match, CrystallineConflictTeam team, CrystallineConflictPlayer player, bool remove = false) {
        bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
        bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.TeamName == match.LocalPlayerTeam!.TeamName;
        bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;

        IncrementAggregateStats(statsModel.StatsAll, match, team, player, remove);
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

    public static void AddPlayerJobStat(CCPlayerJobStats statsModel, ConcurrentDictionary<int, CCScoreboardDouble> teamContributions,
    CrystallineConflictMatch match, CrystallineConflictTeam team, CrystallineConflictPlayer player, bool remove = false) {
        bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
        bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.TeamName == match.LocalPlayerTeam!.TeamName;
        bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;

        IncrementAggregateStats(statsModel.StatsAll, match, team, player, remove);
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
                var hashCode = HashCode.Combine(match.GetHashCode(), player.Alias);
                if(remove) {
                    statsModel.ScoreboardTotal -= playerScoreboard;
                    if(!teamContributions.TryRemove(hashCode, out _)) {
#if DEBUG
                        Plugin.Log2.Warning($"failed to remove team contrib!, {match.DutyStartTime} {player.Alias}");
#endif
                    }
                } else {
                    statsModel.ScoreboardTotal += playerScoreboard;
                    teamContributions.TryAdd(hashCode, new(playerScoreboard, teamScoreboard));
                }
            }
        }
    }

    public static void SetScoreboardStats(CCPlayerJobStats stats, List<CCScoreboardDouble> teamContributions, TimeSpan time) {
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
