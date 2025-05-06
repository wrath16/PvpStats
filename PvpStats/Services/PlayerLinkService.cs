using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Services;
internal class PlayerLinkService {
    private Plugin _plugin;
    private ICallGateSubscriber<((string, uint), (string, uint)[])[]> GetPreviousAliasesFunction;

    internal List<PlayerAliasLink> AutoPlayerLinksCache { get; private set; } = [];
    internal List<PlayerAliasLink> ManualPlayerLinksCache { get; private set; } = [];
    internal Dictionary<PlayerAlias, PlayerAlias> LinkedAliases { get; private set; } = [];

    internal bool RefreshInProgress { get; private set; }

    internal PlayerLinkService(Plugin plugin) {
        _plugin = plugin;
        _plugin.DataQueue.QueueDataOperation(() => {
            ManualPlayerLinksCache = _plugin.Storage.GetManualLinks().Query().ToList();
            AutoPlayerLinksCache = _plugin.Storage.GetAutoLinks().Query().ToList();
            BuildLinkedAliases();
            _plugin.Log.Debug($"Restored auto link count: {AutoPlayerLinksCache.Count}");
        });
        GetPreviousAliasesFunction = _plugin.PluginInterface.GetIpcSubscriber<((string, uint), (string, uint)[])[]>("PlayerTrack.GetAllPlayerNameWorldHistories");
    }

    internal void BuildLinkedAliases() {
        var unlinks = ManualPlayerLinksCache.Where(x => x.IsUnlink).ToList();
        var manualNoUnlink = ManualPlayerLinksCache.Where(x => !x.IsUnlink && x.CurrentAlias != null).ToList();

        Dictionary<PlayerAlias, PlayerAlias> linkedAliasMap = [];
        Dictionary<PlayerAlias, PlayerAlias> flattenedAliasMap = [];

        //add auto links
        if(_plugin.Configuration.EnableAutoPlayerLinking) {
            foreach(var link in AutoPlayerLinksCache) {
                foreach(var linkedAlias in link.LinkedAliases) {
                    if(linkedAlias != null) {
                        linkedAliasMap.Add(linkedAlias, link.CurrentAlias);
                    }
                }
            }
        }

        //add manual links
        if(_plugin.Configuration.EnableManualPlayerLinking) {
            foreach(var link in manualNoUnlink) {
                linkedAliasMap.TryGetValue(link.CurrentAlias, out var nextAlias);
                foreach(var linkedAlias in link.LinkedAliases) {
                    if(linkedAliasMap.TryGetValue(linkedAlias, out var existingLink)) {
                        //override auto link
                        existingLink = link.CurrentAlias;
                    } else {
                        linkedAliasMap.Add(linkedAlias, link.CurrentAlias);
                    }
                    //delete existing pointer if it loops back on old value
                    if(nextAlias == linkedAlias) {
                        linkedAliasMap.Remove(link.CurrentAlias);
                    }
                }
            }

            //remove unlinks
            foreach(var link in unlinks) {
                foreach(var linkedAlias in link.LinkedAliases) {
                    if(linkedAliasMap.TryGetValue(linkedAlias, out var existingLink)) {
                        if(existingLink == link.CurrentAlias) {
                            linkedAliasMap.Remove(linkedAlias);
                        }
                    }
                    //bidirectional
                    if(linkedAliasMap.TryGetValue(link.CurrentAlias, out var existingLink2)) {
                        if(existingLink2 == linkedAlias) {
                            linkedAliasMap.Remove(link.CurrentAlias);
                        }
                    }
                }
            }
        }

        foreach(var link in linkedAliasMap) {
            flattenedAliasMap.Add(link.Key, FlattenAliasLink(link.Key, linkedAliasMap, new()));
        }
        LinkedAliases = flattenedAliasMap;
    }

    private PlayerAlias FlattenAliasLink(PlayerAlias alias, Dictionary<PlayerAlias, PlayerAlias> map, HashSet<PlayerAlias> prevAliases) {
        //get current value
        //var alias2 = map[alias];
        if(map.TryGetValue(alias, out var alias2)) {
            if(prevAliases.Contains(alias2)) {
                Plugin.Log2.Warning($"Player alias link loop detected! {alias} to {alias2}");
                return alias;
            } else {
                prevAliases.Add(alias);
                return FlattenAliasLink(alias2, map, prevAliases);
            }
        } else {
            //no more links found, this is the one!
            return alias;
        }
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
        BuildLinkedAliases();
        if(consolidatedList.Any()) {
            await _plugin.Storage.SetManualLinks(consolidatedList);
        }
    }

    internal Task BuildAutoLinksCache() {
        //if(!IsInitialized() && !Initialize()) return;
        _plugin.Log.Information("Building player alias links cache from PlayerTrack IPC data...");
        return Task.Run(async () => {
            //get players
            var ccMatches = _plugin.CCCache.Matches.ToList();
            var flMatches = _plugin.FLCache.Matches.ToList();
            var rwMatches = _plugin.RWCache.Matches.ToList();
            HashSet<PlayerAlias> allPlayers = new();
            foreach(var match in ccMatches) {
                foreach(var player in match.Players) {
                    allPlayers.Add(player.Alias);
                }
            }
            foreach(var match in flMatches) {
                foreach(var player in match.Players) {
                    allPlayers.Add(player.Name);
                }
            }
            foreach(var match in rwMatches) {
                if(match.Players is null) continue;
                foreach(var player in match.Players) {
                    allPlayers.Add(player.Name);
                }
            }

            if(!allPlayers.Any()) {
                return;
            }

            //get auto links
            List<PlayerAliasLink> autoLinks = [];
            try {
                autoLinks = GetPlayerNameHistory(allPlayers.ToList());
                _plugin.Log.Information($"players with previous aliases: {autoLinks.Count}");
            } catch(Exception e) {
                if(e is IpcNotReadyError) {
                    if(_plugin.Configuration.EnableAutoPlayerLinking) {
                        _plugin.Log.Error("Unable to query PlayerTrack IPC: check whether plugin is installed and up to date.");
                    } else {
                        _plugin.Log.Information("PlayerTrack IPC unavailable.");
                    }
                } else {
                    _plugin.Log.Error(e, "PlayerTrack IPC error");
                }
            }
            if(autoLinks is null || autoLinks.Count <= 0) {
                return;
            }

            //save
            await _plugin.DataQueue.QueueDataOperation(async () => {
                AutoPlayerLinksCache = autoLinks;
                BuildLinkedAliases();
                await _plugin.Storage.SetAutoLinks(AutoPlayerLinksCache);
            });
        });
    }

    private List<PlayerAliasLink> GetPlayerNameHistory(List<PlayerAlias> players) {
        var worlds = _plugin.DataManager.GetExcelSheet<World>();
        var playersWithWorldId = players.Select(x => (x.Name, worlds.Where(y => y.Name.ToString().Equals(x.HomeWorld, StringComparison.OrdinalIgnoreCase)).Select(y => y.RowId).First()));
        var results = GetPreviousAliasesFunction.InvokeFunc();
        List<PlayerAliasLink> playersLinks = new();
        foreach(var result in results) {
            PlayerAlias sourceAlias = (PlayerAlias)$"{result.Item1.Item1} {worlds.GetRow(result.Item1.Item2).Name}";
            List<PlayerAlias> prevAliases = new();
            foreach(var prevResult in result.Item2) {
                //ignore partial results, self results and duplicates
                if(!prevResult.Item1.IsNullOrEmpty() && prevResult.Item2 != 0
                     && !(prevResult.Item1 == result.Item1.Item1 && prevResult.Item2 == result.Item1.Item2)) {
                    //string aliasString = $"{prevResult.Item1} {worlds.GetRow(prevResult.Item2).Name}";
                    try {
                        var alias = (PlayerAlias)$"{prevResult.Item1} {worlds.GetRow(prevResult.Item2).Name}";
                        if(!prevAliases.Contains(alias) && players.Contains(alias)) {
                            prevAliases.Add(alias);
                        }
                    } catch(ArgumentException) {
                        continue;
                    }
                }
            }
            if(prevAliases.Count > 0) {
                var existingLink = playersLinks.FirstOrDefault(x => x.CurrentAlias?.Equals(sourceAlias) ?? false);
                if(existingLink != null) {
                    existingLink.LinkedAliases = existingLink.LinkedAliases.Concat(prevAliases).ToList();
                } else {
                    playersLinks.Add(new() {
                        CurrentAlias = sourceAlias,
                        LinkedAliases = prevAliases,
                    });
                }
            }
        }
        return playersLinks;
    }

    internal List<PlayerAlias> GetAllLinkedAliases(PlayerAlias player) {
        return GetAllLinkedAliases(player.FullName);
    }

    //for a partial string match
    internal List<PlayerAlias> GetAllLinkedAliases(string playerNameFragment) {
        HashSet<PlayerAlias> linkedAliases = new();
        foreach(var link in LinkedAliases) {
            if(link.Value.ToString().Contains(playerNameFragment, StringComparison.OrdinalIgnoreCase)) {
                linkedAliases.Add(link.Key);
            }
        }
        return linkedAliases.ToList();
    }

    internal PlayerAlias GetMainAlias(PlayerAlias alias) {
        if(!_plugin.Configuration.EnablePlayerLinking) {
            return alias;
        }
        LinkedAliases.TryGetValue(alias, out var linkedAlias);
        linkedAlias ??= alias;
        return linkedAlias;
    }
}
