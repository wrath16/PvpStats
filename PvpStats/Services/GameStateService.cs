using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Services;
internal class GameStateService {
    private Plugin _plugin;

    internal GameStateService(Plugin plugin) {
        _plugin = plugin;
    }

    internal unsafe ushort GetCurrentDutyId() {
        return GameMain.Instance()->CurrentContentFinderConditionId;
    }

    public string GetCurrentPlayer() {
        string? currentPlayerName = _plugin.ClientState.LocalPlayer?.Name?.ToString();
        string? currentPlayerWorld = _plugin.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name?.ToString();
        if (currentPlayerName == null || currentPlayerWorld == null) {
            //throw exception?
            //throw new InvalidOperationException("Cannot retrieve current player");
            return null;
        }
        return $"{currentPlayerName} {currentPlayerWorld}";
    }
}
