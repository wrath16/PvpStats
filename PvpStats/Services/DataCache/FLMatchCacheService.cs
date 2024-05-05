using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Services.DataCache;
internal class FLMatchCacheService : MatchCacheService<FrontlineMatch> {
    public FLMatchCacheService(Plugin plugin) : base(plugin) {
    }

    protected override IEnumerable<FrontlineMatch> GetFromStorage() {
        return _plugin.Storage.GetFLMatches().Query().ToList();
    }

    protected override async Task AddToStorage(FrontlineMatch match) {
        await _plugin.Storage.AddFLMatch(match);
    }

    protected override async Task UpdateToStorage(FrontlineMatch match) {
        await _plugin.Storage.UpdateFLMatch(match);
    }

    protected override async Task UpdateManyToStorage(IEnumerable<FrontlineMatch> matches) {
        await _plugin.Storage.UpdateFLMatches(matches);
    }
}
