using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Services.DataCache;
internal class CCMatchCacheService : MatchCacheService<CrystallineConflictMatch> {

    public CCMatchCacheService(Plugin plugin) : base(plugin) {
    }

    protected override IEnumerable<CrystallineConflictMatch> GetFromStorage() {
        return Plugin.Storage.GetCCMatches().Query().ToList();
    }

    protected override async Task AddToStorage(CrystallineConflictMatch match) {
        await Plugin.Storage.AddCCMatch(match);
    }

    protected override async Task UpdateToStorage(CrystallineConflictMatch match) {
        await Plugin.Storage.UpdateCCMatch(match);
    }

    protected override async Task UpdateManyToStorage(IEnumerable<CrystallineConflictMatch> matches) {
        await Plugin.Storage.UpdateCCMatches(matches);
    }

    internal CrystallineConflictMatchTimeline? GetTimeline(CrystallineConflictMatch match) {
        if(match.TimelineId == null) return null;
        return Plugin.Storage.GetCCTimelines().Query().Where(x => x.Id.Equals(match.TimelineId)).FirstOrDefault();
    }
}
