using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Services;
internal class PlayerLinkService {
    private Plugin _plugin;
    private ICallGateSubscriber<(string, uint)[], ((string, uint), string[], uint[])[]> GetPlayersPreviousNamesWorldsFunction;

    internal List<PlayerAliasLink> AutoPlayerLinksCache { get; private set; }
    //private List<PlayerAliasLink> ManualPlayerLinksCache { get; set; }

    internal PlayerLinkService(Plugin plugin) {
        _plugin = plugin;
        AutoPlayerLinksCache = new();
        _plugin.DataQueue.QueueDataOperation(() => {
            AutoPlayerLinksCache = _plugin.Storage.GetAutoLinks().Query().ToList();
            _plugin.Log.Debug($"Restored auto link count: {AutoPlayerLinksCache.Count}");
        });
        GetPlayersPreviousNamesWorldsFunction = _plugin.PluginInterface.GetIpcSubscriber<(string, uint)[], ((string, uint), string[], uint[])[]>("PlayerTrack.GetPlayersPreviousNamesWorlds");
    }

    //returns whether cache was updated successfully
    internal bool BuildAutoLinksCache() {
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
            } catch(IpcNotReadyError e) {
                if(_plugin.Configuration.EnableAutoPlayerLinking) {
                    _plugin.Log.Error("Unable to query PlayerTrack IPC: check whether plugin is installed and up to date.");
                } else {
                    _plugin.Log.Information("PlayerTrack IPC unavailable.");
                }
                return false;
            }
            _plugin.Log.Information($"Players with previous aliases: {AutoPlayerLinksCache.Count}");
            _plugin.DataQueue.QueueDataOperation(() => {
                _plugin.Storage.GetAutoLinks().DeleteAll();
                _plugin.Storage.AddAutoLinks(AutoPlayerLinksCache);
            });
            return true;
        }
        return false;
    }

    private List<PlayerAliasLink> GetPlayerNameHistory(List<PlayerAlias> players) {
        var worlds = _plugin.DataManager.GetExcelSheet<World>();
        var playersWithWorldId = players.Select(x => (x.Name, worlds.Where(y => y.Name.ToString().Equals(x.HomeWorld, StringComparison.OrdinalIgnoreCase)).Select(y => y.RowId).First()));
        var results = GetPlayersPreviousNamesWorldsFunction.InvokeFunc(playersWithWorldId.ToArray());
        List<PlayerAliasLink> playersLinks = new();

        foreach(var result in results) {
            PlayerAlias sourceAlias = (PlayerAlias)$"{result.Item1.Item1} {worlds.GetRow(result.Item1.Item2).Name.ToString()}";
            List<PlayerAlias> prevAliases = new();
            List<string> names = result.Item2.ToList();
            List<string> worldNames = result.Item3.Select(x => worlds.GetRow(x).Name.ToString()).ToList();
            names.Add(sourceAlias.Name);
            worldNames.Add(sourceAlias.HomeWorld);

            foreach(var playerName in names) {
                foreach(var world in worldNames) {
                    var alias = (PlayerAlias)$"{playerName} {world}";
                    if(!alias.Equals(sourceAlias)) {
                        prevAliases.Add(alias);
                    }
                }
            }
            playersLinks.Add(new() {
                CurrentAlias = sourceAlias,
                LinkedAliases = prevAliases,
            });
        }
        return playersLinks;
    }

    internal List<PlayerAlias> GetAllLinkedAliases(PlayerAlias player) {
        return GetAllLinkedAliases(player.FullName);
    }

    //for a partial string match
    internal List<PlayerAlias> GetAllLinkedAliases(string playerNameFragment) {
        var manualLinks = _plugin.Storage.GetManualLinks().Query().ToList();
        var unLinks = manualLinks.Where(x => x.IsUnlink).ToList();
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
            manualLinks.ForEach(checkPlayerLink);
            unLinks.ForEach(checkPlayerLink);
        }
        return linkedAliases;
    }
}
