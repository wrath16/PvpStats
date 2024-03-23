using PvpStats.Helpers;
using System.Linq;

namespace PvpStats.Managers;
internal class MigrationManager {
    private Plugin _plugin;

    public MigrationManager(Plugin plugin) {
        _plugin = plugin;
    }

    internal void BulkUpdateMatchTypes() {
        var matches = _plugin.Storage.GetCCMatches().Query().Where(x => x.MatchType == Types.Match.CrystallineConflictMatchType.Unknown).ToList();
        if(!matches.Any()) {
            return;
        }
        _plugin.Log.Information("attempting to update unknown match types...");
        foreach(var match in matches) {
            match.MatchType = MatchHelper.GetMatchType(match.DutyId);
        }
        _plugin.Storage.UpdateCCMatches(matches);
    }
}
