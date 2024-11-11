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
    private ICallGateSubscriber<((string, uint), (string, uint)[])[]> GetPreviousAliasesFunction;

    internal List<PlayerAliasLink> AutoPlayerLinksCache { get; private set; } = [];
    internal List<PlayerAliasLink> ManualPlayerLinksCache { get; private set; } = [];

    private List<PlayerAliasLink> Unlinks { get; set; } = [];
    private List<PlayerAliasLink> FullLinks { get; set; } = [];
    private List<PlayerAliasLink> ManualNoUnlink { get; set; } = [];
    private Dictionary<PlayerAlias, PlayerAlias> LinkedAliases { get; set; } = [];

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
        Unlinks = ManualPlayerLinksCache.Where(x => x.IsUnlink).ToList();
        ManualNoUnlink = ManualPlayerLinksCache.Where(x => !x.IsUnlink && x.CurrentAlias != null).ToList();
        FullLinks = [.. ManualNoUnlink, .. AutoPlayerLinksCache];

        Dictionary<PlayerAlias, PlayerAlias> linkedAliases = [];
        foreach(var link in FullLinks) {
            foreach(var linkedAlias in link.LinkedAliases) {
                if(linkedAlias != null) {
                    linkedAliases.Add(linkedAlias, GetMainAlias(linkedAlias, new()));
                }
            }
        }
        LinkedAliases = linkedAliases;
    }

    private PlayerAlias GetMainAlias(PlayerAlias alias, List<PlayerAlias> prevAliases) {
        if(!_plugin.Configuration.EnablePlayerLinking) {
            return alias;
        }

        List<PlayerAliasLink> allLinks = [];
        if(_plugin.Configuration.EnableAutoPlayerLinking && _plugin.Configuration.EnableManualPlayerLinking) {
            allLinks = FullLinks;
        } else if(_plugin.Configuration.EnableAutoPlayerLinking && !_plugin.Configuration.EnableManualPlayerLinking) {
            allLinks = AutoPlayerLinksCache;
        } else if(!_plugin.Configuration.EnableAutoPlayerLinking && _plugin.Configuration.EnableManualPlayerLinking) {
            allLinks = ManualNoUnlink;
        }

        foreach(var link in allLinks) {
            if(_plugin.Configuration.EnableManualPlayerLinking) {
                bool unlinkFound = false;
                foreach(var unlink in Unlinks) {
                    var unlinkActive1 = unlink.LinkedAliases.Contains(alias) && unlink.CurrentAlias.Equals(link.CurrentAlias);
                    var unlinkActive2 = unlink.LinkedAliases.Contains(link.CurrentAlias) && unlink.CurrentAlias.Equals(alias);
                    if(unlinkActive1 || unlinkActive2) {
                        unlinkFound = true;
                        break;
                    }
                }
                if(unlinkFound) continue;
            }

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
            var ccMatches = _plugin.Storage.GetCCMatches().Query().ToList();
            var flMatches = _plugin.Storage.GetFLMatches().Query().ToList();
            var rwMatches = _plugin.Storage.GetRWMatches().Query().ToList();
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
            PlayerAlias sourceAlias = (PlayerAlias)$"{result.Item1.Item1} {worlds.GetRow(result.Item1.Item2)?.Name}";
            List<PlayerAlias> prevAliases = new();
            foreach(var prevResult in result.Item2) {
                //ignore partial results, self results and duplicates
                if(!prevResult.Item1.IsNullOrEmpty() && prevResult.Item2 != 0
                     && !(prevResult.Item1 == result.Item1.Item1 && prevResult.Item2 == result.Item1.Item2)) {
                    //string aliasString = $"{prevResult.Item1} {worlds.GetRow(prevResult.Item2).Name}";
                    try {
                        var alias = (PlayerAlias)$"{prevResult.Item1} {worlds.GetRow(prevResult.Item2)?.Name}";
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
