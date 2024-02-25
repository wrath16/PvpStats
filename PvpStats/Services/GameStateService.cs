using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using System.Linq;

namespace PvpStats.Services;
internal class GameStateService {
    private Plugin _plugin;
    private string _lastCurrentPlayer = "";

    internal GameStateService(Plugin plugin) {
        _plugin = plugin;
    }

    internal unsafe ushort GetCurrentDutyId() {
        return GameMain.Instance()->CurrentContentFinderConditionId;
    }
    internal unsafe InstanceContentType GetContentType() {
        var x = EventFramework.Instance()->GetInstanceContentDirector();
        return x->InstanceContentType;
    }

    public string GetCurrentPlayer() {
        string? currentPlayerName = _plugin.ClientState.LocalPlayer?.Name?.ToString();
        string? currentPlayerWorld = _plugin.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name?.ToString();
        if((currentPlayerName == null || currentPlayerWorld == null) && _lastCurrentPlayer != null) {
            return _lastCurrentPlayer;
        }
        _lastCurrentPlayer = $"{currentPlayerName} {currentPlayerWorld}";
        return _lastCurrentPlayer;
    }

    public void PrintAllPlayerObjects() {
        foreach(PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
            _plugin.Log.Debug($"0x{pc.ObjectId.ToString("X2")} {pc.Name}");
        }
    }
}
