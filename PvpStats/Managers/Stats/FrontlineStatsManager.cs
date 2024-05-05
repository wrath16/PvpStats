using PvpStats.Types.Match;
using PvpStats.Windows.Filter;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Managers.Stats;
internal class FrontlineStatsManager : StatsManager<FrontlineMatch> {

    internal FrontlineStatsManager(Plugin plugin) : base(plugin, plugin.FLCache) {
    }

    public override async Task Refresh(List<DataFilter> matchFilters, List<DataFilter> jobStatFilters, List<DataFilter> playerStatFilters) {
        var matches = MatchCache.Matches.Where(x => !x.IsDeleted && x.IsCompleted).ToList();
        matches = FilterMatches(matchFilters, matches);
        try {
            await RefreshLock.WaitAsync();
            Matches = matches;
        } finally {
            RefreshLock.Release();
        }
    }
}
