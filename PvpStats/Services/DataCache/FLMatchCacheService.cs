using PvpStats.Types.Match;
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
}
