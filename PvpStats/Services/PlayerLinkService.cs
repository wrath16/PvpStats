using Dalamud.Plugin.Ipc;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Services;
internal class PlayerLinkService {
    private Plugin _plugin;
    private ICallGateSubscriber<(string, uint)[], ((string, uint), string[], uint[])[]>? GetPlayersPreviousNamesWorldsFunction;

    internal List<PlayerAliasLink> AutoPlayerLinksCache { get; private set; }

    internal PlayerLinkService(Plugin plugin) {
        _plugin = plugin;
        AutoPlayerLinksCache = new();
        _plugin.DataQueue.QueueDataOperation(() => {
            AutoPlayerLinksCache = _plugin.Storage.GetAutoLinks().Query().ToList();
            _plugin.Log.Debug($"Restored link count: {AutoPlayerLinksCache.Count.ToString()}");
        });
        //Initialize();
    }

    private bool Initialize() {
        try {
            GetPlayersPreviousNamesWorldsFunction = _plugin.PluginInterface.GetIpcSubscriber<(string, uint)[], ((string, uint), string[], uint[])[]>("PlayerTrack.GetPlayersPreviousNamesWorlds");
            _plugin.Log.Information("Player Track IPC initialized.");
            return true;
        } catch {
            _plugin.Log.Warning("Unable to initialize PlayerTrack IPC.");
            return false;
        }
    }

    internal bool IsInitialized() {
        return GetPlayersPreviousNamesWorldsFunction != null;
    }

    internal void BuildAutoLinksCache() {
        if(!IsInitialized() && !Initialize()) return;
        _plugin.Log.Information("Building Player Alias links cache from PlayerTrack IPC...");
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
            AutoPlayerLinksCache = GetPlayerNameHistory(allPlayers);
            _plugin.Log.Information($"Players with previous aliases: {AutoPlayerLinksCache.Count}");
            _plugin.DataQueue.QueueDataOperation(() => {
                _plugin.Storage.GetAutoLinks().DeleteAll();
                _plugin.Storage.AddAutoLinks(AutoPlayerLinksCache);
            });
        }
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
}
