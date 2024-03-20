using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using PvpStats.Types.Player;
using System;
using System.Linq;

namespace PvpStats.Services;
internal class GameStateService : IDisposable {
    private Plugin _plugin;
    public PlayerAlias? CurrentPlayer { get; private set; }

    internal GameStateService(Plugin plugin) {
        _plugin = plugin;

        _plugin.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose() {
        _plugin.Framework.Update -= OnFrameworkUpdate;
    }

    internal unsafe ushort GetCurrentDutyId() {
        return GameMain.Instance()->CurrentContentFinderConditionId;
    }
    internal unsafe InstanceContentType GetContentType() {
        var x = EventFramework.Instance()->GetInstanceContentDirector();
        return x->InstanceContentType;
    }

    private void OnFrameworkUpdate(IFramework framework) {
        string? currentPlayerName = _plugin.ClientState.LocalPlayer?.Name?.ToString();
        string? currentPlayerWorld = _plugin.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name?.ToString();
        if(currentPlayerName != null && currentPlayerWorld != null) {
            CurrentPlayer = (PlayerAlias)$"{currentPlayerName} {currentPlayerWorld}";
        }
    }

    public void PrintAllPlayerObjects() {
        foreach(PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
            _plugin.Log.Debug($"0x{pc.ObjectId.ToString("X2")} {pc.Name}");
        }
    }
}
