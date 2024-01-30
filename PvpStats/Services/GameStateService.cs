using FFXIVClientStructs.FFXIV.Client.Game;

namespace PvpStats.Services;
internal class GameStateService {
    private Plugin _plugin;
    private string _lastCurrentPlayer;

    internal GameStateService(Plugin plugin) {
        _plugin = plugin;
    }

    internal unsafe ushort GetCurrentDutyId() {
        return GameMain.Instance()->CurrentContentFinderConditionId;
    }

    public string GetCurrentPlayer() {
        string? currentPlayerName = _plugin.ClientState.LocalPlayer?.Name?.ToString();
        string? currentPlayerWorld = _plugin.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name?.ToString();
        if ((currentPlayerName == null || currentPlayerWorld == null) && _lastCurrentPlayer != null) {
            return _lastCurrentPlayer;
        }
        _lastCurrentPlayer = $"{currentPlayerName} {currentPlayerWorld}";
        return _lastCurrentPlayer;
    }
}
