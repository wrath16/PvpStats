using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Services.DataCache;
internal class RWMatchCacheService : MatchCacheService<RivalWingsMatch> {
    public RWMatchCacheService(Plugin plugin) : base(plugin) {
    }

    protected override IEnumerable<RivalWingsMatch> GetFromStorage() {
        return Plugin.Storage.GetRWMatches().Query().ToList();
    }

    protected override async Task AddToStorage(RivalWingsMatch match) {
        await Plugin.Storage.AddRWMatch(match);
    }

    protected override async Task UpdateToStorage(RivalWingsMatch match) {
        await Plugin.Storage.UpdateRWMatch(match);
    }

    protected override async Task UpdateManyToStorage(IEnumerable<RivalWingsMatch> matches) {
        await Plugin.Storage.UpdateRWMatches(matches);
    }
}
