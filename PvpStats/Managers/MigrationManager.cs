using PvpStats.Helpers;
using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Managers;
internal class MigrationManager {
    private Plugin _plugin;

    public MigrationManager(Plugin plugin) {
        _plugin = plugin;
    }

    internal async Task BulkUpdateMatchTypes() {
        var matches = _plugin.DataCache.CCMatches.Where(x => x.MatchType == Types.Match.CrystallineConflictMatchType.Unknown);
        if(!matches.Any()) {
            return;
        }
        _plugin.Log.Information("attempting to update unknown match types...");
        foreach(var match in matches) {
            match.MatchType = MatchHelper.GetMatchType(match.DutyId);
        }
        await _plugin.DataCache.UpdateCCMatches(matches);
    }

    internal async Task BulkUpdateValidatePlayerCount() {
        var matches = _plugin.DataCache.CCMatches
            .Where(x => x.PostMatch != null && x.Teams.Count == 2 && (x.Teams.ElementAt(0).Value.Players.Count > 5 || x.Teams.ElementAt(1).Value.Players.Count > 5)).ToList();
        if(!matches.Any()) {
            return;
        }
        _plugin.Log.Information("removing erroneously added players...");
        foreach(var match in matches) {
            foreach(var team in match.Teams) {
                if(team.Value.Players.Count > 5) {
                    List<CrystallineConflictPlayer> toRemove = new();
                    bool isValid = false;
                    //remove ones that are not included in post match...
                    foreach(var player in team.Value.Players) {
                        foreach(var postMatchPlayer in match.PostMatch!.Teams[team.Key].PlayerStats) {
                            if(postMatchPlayer.Player.Equals(player.Alias)) {
                                isValid = true;
                                break;
                            }
                        }
                        if(!isValid) {
                            toRemove.Add(player);
                        }
                    }
                    toRemove.ForEach(x => team.Value.Players.Remove(x));
                }
            }
        }
        //await _plugin.Storage.UpdateCCMatches(matches);
        await _plugin.DataCache.UpdateCCMatches(matches);
    }
}
