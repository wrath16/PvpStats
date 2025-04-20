using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Managers;
internal class ValidationManager {
    private Plugin _plugin;

    public ValidationManager(Plugin plugin) {
        _plugin = plugin;
    }

    internal async Task BulkUpdateCCMatchTypes() {
        var matches = _plugin.CCCache.Matches.Where(x => x.MatchType == CrystallineConflictMatchType.Unknown);
        if(!matches.Any()) {
            return;
        }
        _plugin.Log.Information("Attempting to update unknown match types...");
        foreach(var match in matches) {
            match.MatchType = MatchHelper.GetMatchType(match.DutyId);
        }
        await _plugin.CCCache.UpdateMatches(matches);
    }

    internal async Task BulkCCUpdateValidatePlayerCount() {
        var matches = _plugin.CCCache.Matches
            .Where(x => x.PostMatch != null && x.Teams.Count == 2 && (x.Teams.ElementAt(0).Value.Players.Count > 5 || x.Teams.ElementAt(1).Value.Players.Count > 5)).ToList();
        if(matches.Count == 0) {
            return;
        }
        _plugin.Log.Information("Removing erroneously added players...");
        foreach(var match in matches) {
            foreach(var team in match.Teams) {
                if(team.Value.Players.Count > 5) {
                    List<CrystallineConflictPlayer> toRemove = new();
                    //remove ones that are not included in post match...
                    foreach(var player in team.Value.Players) {
                        bool isValid = false;
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
        await _plugin.CCCache.UpdateMatches(matches);
    }

    internal async Task SetRivalWingsMatchFlags() {
        var matches = _plugin.RWCache.Matches.Where(x => x.IsCompleted).ToList();
        if(matches.Count == 0) {
            return;
        }

        Plugin.Log2.Information("Setting Rival Wings match flags...");

        foreach(var match in matches) {
            match.Flags = RWValidationFlag.None;

            //plugin and game version added v2.1.11.0, 2024-10-16

            Version? pluginVersion = null;
            if(match.PluginVersion != null) {
                pluginVersion = new Version(match.PluginVersion);
            }

            DateTime gameVersionDate = DateTime.MinValue;
            if(match.GameVersion != null) {
                var splitString = match.GameVersion.Split(".");
                if(splitString.Length >= 3) {
                    var year = int.Parse(splitString[0]);
                    gameVersionDate = new DateTime(int.Parse(splitString[0]), int.Parse(splitString[1]), int.Parse(splitString[2]));
                }
            }

            //Match flag 0: Possibly invalid ceruleum due to overflow
            //start: beginning
            //fixed: v2.1.2.0, 2024-07-28
            //game and plugin version unavailable at this time
            if(match.DutyStartTime < new DateTime(2024, 07, 28)) {
                match.Flags |= RWValidationFlag.InvalidCeruleum;
            }

            //Match flag 1: mercs double counted
            //start: beginning
            //fixed: v2.3.0.0, 2025-02-08
            if(pluginVersion < new Version(2, 3, 0, 0)) {
                match.Flags |= RWValidationFlag.DoubleMerc;
            }

            //Match flag 2: invalid soaring stacks
            //start: game version 2025-03-27
            //fixed: v2.3.4.1, 2025-02-08
            if(pluginVersion < new Version(2, 3, 4, 1) && gameVersionDate >= new DateTime(2025, 03, 27)) {
                match.Flags |= RWValidationFlag.InvalidSoaring;
            }
        }
        await _plugin.RWCache.UpdateMatches(matches);
    }
}
