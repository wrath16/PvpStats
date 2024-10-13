using Dalamud.Utility;
using PvpStats.Helpers.Comparers;
using PvpStats.Services.DataCache;
using PvpStats.Types;
using PvpStats.Types.Match;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PvpStats.Managers.Stats;
internal abstract class StatsManager<T> where T : PvpMatch {
    protected readonly Plugin Plugin;
    protected readonly MatchCacheService<T> MatchCache;
    internal SemaphoreSlim RefreshLock { get; private set; } = new SemaphoreSlim(1);

    public bool RefreshActive { get; protected set; }
    public float RefreshProgress { get; protected set; }

    public List<T> Matches { get; protected set; } = new();

    //public List<PlayerAlias> Players { get; protected set; } = new();

    internal StatsManager(Plugin plugin, MatchCacheService<T> cache) {
        Plugin = plugin;
        MatchCache = cache;
    }

    public (List<T> Matches, List<T> Additions, List<T> Removals) Refresh(List<DataFilter> matchFilters) {
        try {
            RefreshActive = true;
            Stopwatch matchesTimer = Stopwatch.StartNew();
            var matches = MatchCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).OrderByDescending(x => x.DutyStartTime).ToList();
            matches = FilterMatches(matchFilters, matches);
            var toAdd = matches.Except(Matches, new PvpMatchComparer<T>()).ToList();
            var toSubtract = Matches.Except(matches, new PvpMatchComparer<T>()).ToList();
            Matches = matches;
            matchesTimer.Stop();
            Plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"Matches Refresh", matchesTimer.ElapsedMilliseconds.ToString()));
            Plugin.Log.Debug($"total: {matches.Count} additions: {toAdd.Count} removals: {toSubtract.Count}");
            return (matches, toAdd, toSubtract);
        } finally {
            RefreshActive = false;
        }
    }

    protected virtual List<T> FilterMatches(List<DataFilter> filters, List<T> matches) {
        List<T> filteredMatches = new(matches);
        foreach(var filter in filters) {
            var filterType = filter.GetType();
            var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.GetParameters().Length > 0 && x.GetParameters()[0].ParameterType == filterType).FirstOrDefault();
            if(method is null) {
                Plugin.Log.Error($"No method found for filter type {filterType.Name}");
                continue;
            }
            try {
                filteredMatches = (List<T>)method.Invoke(this, new object[] { filter, filteredMatches })!;
            } catch(Exception e) {
                Plugin.Log.Error(e, $"failed to apply filter: {filter.Name}");
            }
        }
        return filteredMatches;
    }

    protected virtual List<T> ApplyFilter(TimeFilter filter, List<T> matches) {
        List<T> filteredMatches = new(matches);
        switch(filter.StatRange) {
            case TimeRange.PastDay:
                filteredMatches = filteredMatches.Where(x => (DateTime.Now - x.DutyStartTime).TotalHours < 24).ToList();
                break;
            case TimeRange.PastWeek:
                filteredMatches = filteredMatches.Where(x => (DateTime.Now - x.DutyStartTime).TotalDays < 7).ToList();
                break;
            case TimeRange.ThisMonth:
                filteredMatches = filteredMatches.Where(x => x.DutyStartTime.Month == DateTime.Now.Month && x.DutyStartTime.Year == DateTime.Now.Year).ToList();
                break;
            case TimeRange.LastMonth:
                var lastMonth = DateTime.Now.AddMonths(-1);
                filteredMatches = filteredMatches.Where(x => x.DutyStartTime.Month == lastMonth.Month && x.DutyStartTime.Year == lastMonth.Year).ToList();
                break;
            case TimeRange.ThisYear:
                filteredMatches = filteredMatches.Where(x => x.DutyStartTime.Year == DateTime.Now.Year).ToList();
                break;
            case TimeRange.LastYear:
                filteredMatches = filteredMatches.Where(x => x.DutyStartTime.Year == DateTime.Now.AddYears(-1).Year).ToList();
                break;
            case TimeRange.Custom:
                filteredMatches = filteredMatches.Where(x => x.DutyStartTime > filter.StartTime && x.DutyStartTime < filter.EndTime).ToList();
                break;
            case TimeRange.Season:
                filteredMatches = filteredMatches.Where(x => x.DutyStartTime > GamePeriod.Season[filter.Season].StartDate && x.DutyStartTime < GamePeriod.Season[filter.Season].EndDate).ToList();
                break;
            case TimeRange.Expansion:
                filteredMatches = filteredMatches.Where(x => x.DutyStartTime > GamePeriod.Expansion[filter.Expansion].StartDate && x.DutyStartTime < GamePeriod.Expansion[filter.Expansion].EndDate).ToList();
                break;
            case TimeRange.All:
            default:
                break;
        }
        return filteredMatches;
    }

    protected virtual List<T> ApplyFilter(LocalPlayerFilter filter, List<T> matches) {
        List<T> filteredMatches = new(matches);
        if(filter.CurrentPlayerOnly && Plugin.ClientState.IsLoggedIn && Plugin.GameState.CurrentPlayer != null) {
            if(Plugin.Configuration.EnablePlayerLinking) {
                var linkedAliases = Plugin.PlayerLinksService.GetAllLinkedAliases(Plugin.GameState.CurrentPlayer);
                filteredMatches = filteredMatches.Where(x => x.LocalPlayer != null && (x.LocalPlayer.Equals(Plugin.GameState.CurrentPlayer) || linkedAliases.Contains(x.LocalPlayer))).ToList();
            } else {
                filteredMatches = filteredMatches.Where(x => x.LocalPlayer != null && x.LocalPlayer.Equals(Plugin.GameState.CurrentPlayer)).ToList();
            }
        }
        return filteredMatches;
    }

    protected virtual List<T> ApplyFilter(DurationFilter filter, List<T> matches) {
        List<T> filteredMatches = new(matches);
        if(filter.DirectionIndex == 0) {
            filteredMatches = filteredMatches.Where(x => x.MatchDuration is null || x.MatchDuration >= filter.Duration).ToList();
        } else {
            filteredMatches = filteredMatches.Where(x => x.MatchDuration is null || x.MatchDuration < filter.Duration).ToList();
        }
        return filteredMatches;
    }

    protected virtual List<T> ApplyFilter(BookmarkFilter filter, List<T> matches) {
        List<T> filteredMatches = new(matches);
        if(filter.BookmarkedOnly) {
            filteredMatches = filteredMatches.Where(x => x.IsBookmarked).ToList();
        }
        return filteredMatches;
    }

    protected virtual List<T> ApplyFilter(TagFilter filter, List<T> matches) {
        List<T> filteredMatches = new(matches);
        if(filter.TagsRaw.Trim().IsNullOrEmpty()) {
            return filteredMatches;
        }
        var tags = filter.TagsRaw.Split(",");
        filteredMatches = filteredMatches.Where(x => {
            var matchTags = x.Tags.Split(",");
            foreach(var tag in tags) {
                if(matchTags.Any(y => y.Trim().Equals(tag.Trim(), StringComparison.OrdinalIgnoreCase))) {
                    if(filter.OrLogic) {
                        return true;
                    }
                } else if(!filter.OrLogic) {
                    return false;
                }
            }
            return !filter.OrLogic;
        }).ToList();
        return filteredMatches;
    }
}
