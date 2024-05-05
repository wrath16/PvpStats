using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Managers.Stats;
internal abstract class StatsManager<T> where T: PvpMatch {
    protected readonly Plugin Plugin;
    protected readonly MatchCacheService<T> MatchCache;
    protected SemaphoreSlim RefreshLock { get; private set; } = new SemaphoreSlim(1);

    public List<T> Matches { get; protected set; } = new();
    public List<PlayerAlias> Players { get; protected set; } = new();

    internal StatsManager(Plugin plugin, MatchCacheService<T> cache) {
        Plugin = plugin;
        MatchCache = cache;
    }

    public virtual async Task Refresh(List<DataFilter> matchFilters, List<DataFilter> jobStatFilters, List<DataFilter> playerStatFilters) {
        var matches = MatchCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).ToList();
        matches = FilterMatches(matchFilters, matches);
        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
        } finally {
            RefreshLock.Release();
        }
    }

    protected virtual List<T> FilterMatches(List<DataFilter> filters, List<T> matches) {
        List<T> filteredMatches = new(matches);
        foreach(var filter in filters) {
            var filterType = filter.GetType();
            var method = GetType().GetMethods().Where(x => x.GetParameters().Length > 0 && x.GetParameters()[0].ParameterType == filterType).FirstOrDefault();
            if(method is null) {
                Plugin.Log.Error($"No method found for filter type {filterType}");
                continue;
            }
            filteredMatches = (List<T>)method.Invoke(this, new object[] { filter, matches })!;
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
                filteredMatches = filteredMatches.Where(x => x.DutyStartTime > ArenaSeason.Season[filter.Season].StartDate && x.DutyStartTime < ArenaSeason.Season[filter.Season].EndDate).ToList();
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
                matches = matches.Where(x => x.LocalPlayer != null && (x.LocalPlayer.Equals(Plugin.GameState.CurrentPlayer) || linkedAliases.Contains(x.LocalPlayer))).ToList();
            } else {
                matches = matches.Where(x => x.LocalPlayer != null && x.LocalPlayer.Equals(Plugin.GameState.CurrentPlayer)).ToList();
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
}
