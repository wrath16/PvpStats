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
        TimeSpan totalMatchTime = TimeSpan.Zero;

        foreach(var match in matches) {
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
        }
        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
            OverallResults = overallResults;
            MapResults = mapResults;
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
}
