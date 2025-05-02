using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Services.DataCache;
internal class FLMatchCacheService : MatchCacheService<FrontlineMatch> {
    public FLMatchCacheService(Plugin plugin) : base(plugin) {
    }

    protected override IEnumerable<FrontlineMatch> GetFromStorage() {
        return Plugin.Storage.GetFLMatches().Query().ToList();
    }

    protected override async Task AddToStorage(FrontlineMatch match) {
        await Plugin.Storage.AddFLMatch(match);
    }

    protected override async Task UpdateToStorage(FrontlineMatch match) {
        await Plugin.Storage.UpdateFLMatch(match);
    }

    protected override async Task UpdateManyToStorage(IEnumerable<FrontlineMatch> matches) {
        await Plugin.Storage.UpdateFLMatches(matches);
    }

    internal override FrontlineMatchTimeline? GetTimeline(FrontlineMatch match) {
        if(match.TimelineId == null) return null;
        return Plugin.Storage.GetFLTimelines().Query().Where(x => x.Id.Equals(match.TimelineId)).FirstOrDefault();
    }
}
