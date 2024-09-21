using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Services;
internal class PlayerLinkService {
    private Plugin _plugin;
    private ICallGateSubscriber<(string, uint)[], ((string, uint), (string, uint)[])[]> GetPreviousAliasesFunction;

    internal List<PlayerAliasLink> AutoPlayerLinksCache { get; private set; }
    internal List<PlayerAliasLink> ManualPlayerLinksCache { get; private set; }

    internal PlayerLinkService(Plugin plugin) {
        _plugin = plugin;
        AutoPlayerLinksCache = new();
        ManualPlayerLinksCache = new();
        _plugin.DataQueue.QueueDataOperation(() => {
            ManualPlayerLinksCache = _plugin.Storage.GetManualLinks().Query().ToList();
            AutoPlayerLinksCache = _plugin.Storage.GetAutoLinks().Query().ToList();
            _plugin.Log.Debug($"Restored auto link count: {AutoPlayerLinksCache.Count}");
        });
        GetPreviousAliasesFunction = _plugin.PluginInterface.GetIpcSubscriber<(string, uint)[], ((string, uint), (string, uint)[])[]>("PlayerTrack.GetPlayerNameWorldHistories");
    }

    internal async Task SaveManualLinksCache(List<PlayerAliasLink> playerLinks) {
        _plugin.Log.Debug("Saving manual player links...");
        List<PlayerAliasLink> consolidatedList = new();
        //consolidate list
        foreach(var playerLink in playerLinks.Where(x => x.CurrentAlias != null && x.LinkedAliases.Count > 0)) {
            var existingItem = consolidatedList.Where(x => x.CurrentAlias!.Equals(playerLink.CurrentAlias) && x.IsUnlink == playerLink.IsUnlink).FirstOrDefault();
            if(existingItem != null) {
                playerLink.LinkedAliases.ForEach(x => {
                    if(!existingItem.LinkedAliases.Contains(x)) {
                        //_plugin.Log.Debug($"Adding {x} to {existingItem.CurrentAlias} unlink: {existingItem.IsUnlink}");
                        existingItem.LinkedAliases.Add(x);
                    }
                });
            } else {
                consolidatedList.Add(playerLink);
            }
        }
        ManualPlayerLinksCache = consolidatedList;
        if(consolidatedList.Any()) {
            await _plugin.Storage.SetManualLinks(consolidatedList);
        }
    }

    internal async Task BuildAutoLinksCache() {
        //if(!IsInitialized() && !Initialize()) return;
        _plugin.Log.Information("Building player alias links cache from PlayerTrack IPC data...");
        var matches = _plugin.Storage.GetCCMatches().Query().ToList();
        List<PlayerAlias> allPlayers = new();
        foreach(var match in matches) {
            foreach(var player in match.Players) {
                if(!allPlayers.Contains(player.Alias)) {
                    allPlayers.Add(player.Alias);
                }
            }
        }

        if(allPlayers.Any()) {
            try {
                AutoPlayerLinksCache = GetPlayerNameHistory(allPlayers);
            } catch(IpcNotReadyError) {
                if(_plugin.Configuration.EnableAutoPlayerLinking) {
                    _plugin.Log.Error("Unable to query PlayerTrack IPC: check whether plugin is installed and up to date.");
                } else {
                    _plugin.Log.Information("PlayerTrack IPC unavailable.");
                }
            }
            _plugin.Log.Information($"Players with previous aliases: {AutoPlayerLinksCache.Count}");
            //_plugin.DataQueue.QueueDataOperation(async () => {
            //    await _plugin.Storage.SetAutoLinks(AutoPlayerLinksCache);
            //});
            await _plugin.Storage.SetAutoLinks(AutoPlayerLinksCache);
        }
    }

    private List<PlayerAliasLink> GetPlayerNameHistory(List<PlayerAlias> players) {
        var worlds = _plugin.DataManager.GetExcelSheet<World>();
        var playersWithWorldId = players.Select(x => (x.Name, worlds.Where(y => y.Name.ToString().Equals(x.HomeWorld, StringComparison.OrdinalIgnoreCase)).Select(y => y.RowId).First()));
        var results = GetPreviousAliasesFunction.InvokeFunc(playersWithWorldId.ToArray());
        List<PlayerAliasLink> playersLinks = new();
        foreach(var result in results) {
            PlayerAlias sourceAlias = (PlayerAlias)$"{result.Item1.Item1} {worlds.GetRow(result.Item1.Item2)?.Name}";
            List<PlayerAlias> prevAliases = new();
            foreach(var prevResult in result.Item2) {
                //ignore partial results, self results and duplicates
                if(!prevResult.Item1.IsNullOrEmpty() && prevResult.Item2 != 0
                     && !(prevResult.Item1 == result.Item1.Item1 && prevResult.Item2 == result.Item1.Item2)) {
                    //string aliasString = $"{prevResult.Item1} {worlds.GetRow(prevResult.Item2).Name}";
                    try {
                        var alias = (PlayerAlias)$"{prevResult.Item1} {worlds.GetRow(prevResult.Item2)?.Name}";
                        if(!prevAliases.Contains(alias)) {
                            prevAliases.Add(alias);
                        }
                    } catch(ArgumentException) {
                        continue;
                    }
                }
            }
            if(prevAliases.Count > 0) {
                playersLinks.Add(new() {
                    CurrentAlias = sourceAlias,
                    LinkedAliases = prevAliases,
                });
            }
        }
        return playersLinks;
    }

    internal List<PlayerAlias> GetAllLinkedAliases(PlayerAlias player) {
        return GetAllLinkedAliases(player.FullName);
    }

    //for a partial string match
    internal List<PlayerAlias> GetAllLinkedAliases(string playerNameFragment) {
        //var manualLinks = _plugin.Storage.GetManualLinks().Query().ToList();
        var unLinks = ManualPlayerLinksCache.Where(x => x.IsUnlink).ToList();
        List<PlayerAlias> linkedAliases = new();
        var addAlias = ((PlayerAlias alias) => {
            if(!linkedAliases.Contains(alias)) {
                linkedAliases.Add(alias);
            }
        });
        var removeAlias = ((PlayerAlias alias) => {
            linkedAliases.Remove(alias);
        });
        var checkPlayerLink = (PlayerAliasLink link) => {
            if(!link.IsUnlink
            && (link.CurrentAlias.FullName.Contains(playerNameFragment, StringComparison.OrdinalIgnoreCase)
            || link.LinkedAliases.Any(x => x.FullName.Contains(playerNameFragment, StringComparison.OrdinalIgnoreCase)))) {
                addAlias(link.CurrentAlias);
                link.LinkedAliases.ForEach(addAlias);
            } else if(link.IsUnlink) {
                removeAlias(link.CurrentAlias);
                link.LinkedAliases.ForEach(removeAlias);
            }
        };
        if(_plugin.Configuration.EnableAutoPlayerLinking) {
            AutoPlayerLinksCache.ForEach(checkPlayerLink);
        }
        if(_plugin.Configuration.EnableManualPlayerLinking) {
            ManualPlayerLinksCache.ForEach(checkPlayerLink);
            unLinks.ForEach(checkPlayerLink);
        }
        return linkedAliases;
    }

    internal PlayerAlias GetMainAlias(PlayerAlias alias) {
        return GetMainAlias(alias, new());
    }

    private PlayerAlias GetMainAlias(PlayerAlias alias, List<PlayerAlias> prevAliases) {
        var unLinks = ManualPlayerLinksCache.Where(x => x.IsUnlink).ToList();
        List<PlayerAliasLink> allLinks = [.. AutoPlayerLinksCache.Where(x => x.CurrentAlias != null), .. ManualPlayerLinksCache.Where(x => !x.IsUnlink && x.CurrentAlias != null)];
        foreach(var link in allLinks) {
            bool unlinkFound = false;
            foreach(var unlink in unLinks) {
                var unlinkActive1 = unlink.LinkedAliases.Contains(alias) && unlink.CurrentAlias.Equals(link.CurrentAlias);
                var unlinkActive2 = unlink.LinkedAliases.Contains(link.CurrentAlias) && unlink.CurrentAlias.Equals(alias);
                if(unlinkActive1 || unlinkActive2) {
                    unlinkFound = true;
                    break;
                }
            }
            if(unlinkFound) continue;

            if(link.LinkedAliases.Contains(alias)) {
                //detect loop
                if(prevAliases.Contains(link.CurrentAlias!)) {
                    //_plugin.Log.Warning($"Player alias link loop detected! {alias} to {link.CurrentAlias}");
                    return alias; 
                }
                //search recursively
                return GetMainAlias(link.CurrentAlias!, [.. prevAliases, alias]);
            }
        }

        //not found
        return alias;
    }
}
