using Dalamud.Plugin.Ipc;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Services;
internal class LodestoneService {
    private Plugin _plugin;
    private ICallGateSubscriber<string, uint, string> GetPlayerCurrentNameWorldFunction;
    private ICallGateSubscriber<string, uint, uint> GetPlayerLodestoneIdFunction;
    private ICallGateSubscriber<string, uint, string[]> GetPlayerPreviousNamesFunction;
    private ICallGateSubscriber<string, uint, string[]> GetPlayerPreviousWorldsFunction;
    private ICallGateSubscriber<(string, uint)[], ((string, uint), string[], uint[])[]> GetPlayersPreviousNamesWorldsFunction;

    internal LodestoneService(Plugin plugin) {
        _plugin = plugin;
        GetPlayerCurrentNameWorldFunction = _plugin.PluginInterface.GetIpcSubscriber<string, uint, string>("PlayerTrack.GetPlayerCurrentNameWorld");
        GetPlayerLodestoneIdFunction = _plugin.PluginInterface.GetIpcSubscriber<string, uint, uint>("PlayerTrack.GetPlayerLodestoneId");
        GetPlayerPreviousNamesFunction = _plugin.PluginInterface.GetIpcSubscriber<string, uint, string[]>("PlayerTrack.GetPlayerPreviousNames");
        GetPlayerPreviousWorldsFunction = _plugin.PluginInterface.GetIpcSubscriber<string, uint, string[]>("PlayerTrack.GetPlayerPreviousWorlds");
        GetPlayersPreviousNamesWorldsFunction = _plugin.PluginInterface.GetIpcSubscriber<(string, uint)[], ((string, uint), string[], uint[])[]>("PlayerTrack.GetPlayersPreviousNamesWorlds");
    }

    internal string GetPlayerCurrentNameWorld(PlayerAlias player) {
        //get world id
        uint? worldId = _plugin.DataManager.GetExcelSheet<World>()?.Where(x => x.Name.ToString().Equals(player.HomeWorld, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.RowId;
        if(worldId == null) {
            throw new ArgumentException("invalid home world!");
        }
        var x = GetPlayerCurrentNameWorldFunction.InvokeFunc(player.Name, (uint)worldId);
        return x;
    }

    internal uint GetPlayerLodestoneId(PlayerAlias player) {
        //get world id
        uint? worldId = _plugin.DataManager.GetExcelSheet<World>()?.Where(x => x.Name.ToString().Equals(player.HomeWorld, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.RowId;
        if(worldId == null) {
            throw new ArgumentException("invalid home world!");
        }
        var x = GetPlayerLodestoneIdFunction.InvokeFunc(player.Name, (uint)worldId);
        return x;
    }

    internal PlayerAlias[] GetPreviousAliases(PlayerAlias player) {
        //get world id
        var worlds = _plugin.DataManager.GetExcelSheet<World>();
        uint ? worldId = worlds?.Where(x => x.Name.ToString().Equals(player.HomeWorld, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.RowId;
        if(worldId == null) {
            throw new ArgumentException($"Invalid home world: {player.HomeWorld}");
        }
        var pNames = GetPlayerPreviousNamesFunction.InvokeFunc(player.Name, (uint)worldId).ToList();
        //_plugin.Log.Debug(pNames.Count.ToString());
        pNames.ForEach(x => x = x.Trim());
        pNames.Add(player.Name);
        var pWorlds = GetPlayerPreviousWorldsFunction.InvokeFunc(player.Name, (uint)worldId).ToList();
        pWorlds.ForEach(x => x = x.Trim());
        //_plugin.Log.Debug(pWorlds.Count.ToString());
        //pWorlds.Add(player.HomeWorld);

        List<PlayerAlias> aliases = new();
        foreach(var pName in pNames) {
            _plugin.Log.Debug($"name: {pName}");
            foreach(var pWorld in pWorlds) {
                try {
                    var alias = (PlayerAlias)$"{pName.Trim()} {pWorld.Trim()}";
                    if(!alias.Equals(player)) {
                        aliases.Add(alias);
                    }
                } catch(ArgumentException) {
                    continue;
                }

                //_plugin.Log.Debug($"world: {pWorldId}");
                //if(uint.TryParse(pWorldId.Trim(), out uint worldIdUint)) {
                //    var worldName = worlds?.Where(x => x.RowId == worldIdUint).FirstOrDefault()?.Name.ToString();
                //    if(worldName == null) {
                //        continue;
                //    }
                //    var alias = (PlayerAlias)$"{pName.Trim()} {worldName}";
                //    if(!alias.Equals(player)) {
                //        aliases.Add(alias);
                //    }
                //}
            }
        }

        return aliases.ToArray();
    }

    internal void GetAllPreviousAliases(List<PlayerAlias> players) {
        var worlds = _plugin.DataManager.GetExcelSheet<World>();
        //_plugin.Log.Debug(players.Count.ToString());
        //var w = players.Select(x => (x.Name, worlds.Where(y => {
        //    _plugin.Log.Debug("here");
        //    bool match = y.Name.ToString().Equals(x.HomeWorld, StringComparison.OrdinalIgnoreCase);
        //    _plugin.Log.Debug($"comparing {y.Name} to {x.HomeWorld} match: {match}");
        //    return y.Name.ToString().Equals(x.HomeWorld, StringComparison.OrdinalIgnoreCase);
        //}).Select(y => y.RowId).First()));
        var z = players.Select(x => (x.Name, worlds.Where(y => y.Name.ToString().Equals(x.HomeWorld, StringComparison.OrdinalIgnoreCase)).Select(y => y.RowId).First()));

        var results = GetPlayersPreviousNamesWorldsFunction.InvokeFunc(z.ToArray());
        foreach(var x in results) {
            _plugin.Log.Debug("-----------");
            _plugin.Log.Debug($"{x.Item1.Item1} {worlds.GetRow(x.Item1.Item2).Name}");
            foreach(var y in x.Item2) {
                _plugin.Log.Debug(y);
            }
            foreach(var y in x.Item3) {
                _plugin.Log.Debug(worlds.GetRow(y).Name);
            }
        }

    }
}
