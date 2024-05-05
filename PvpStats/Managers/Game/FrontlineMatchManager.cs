using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Helpers;
using PvpStats.Services;
using PvpStats.Types.ClientStruct;
using PvpStats.Types.Player;
using System;

namespace PvpStats.Managers.Game;
internal class FrontlineMatchManager : MatchManager {

    //E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 07 48 8D 8F ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 87 ?? ?? ?? ?? 48 8D 05 
    //fl director ctor
    private delegate IntPtr FLDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    [Signature("E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 07 48 8D 8F ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 87 ?? ?? ?? ?? 48 8D 05", DetourName = nameof(FLDirectorCtorDetour))]
    private readonly Hook<FLDirectorCtorDelegate> _flDirectorCtorHook;

    //p1 = director
    //p2 = data packet
    private delegate void FLMatchEnd100Delegate(IntPtr p1, IntPtr p2);
    [Signature("4C 8B DC 55 53 56 41 56 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05", DetourName = nameof(FLMatchEnd100Detour))]
    private readonly Hook<FLMatchEnd100Delegate> _flMatchEndHook;

    //p1 = data packet
    private delegate void FlPlayerPayload10Delegate(IntPtr p1);
    //40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0
    [Signature("40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0", DetourName = nameof(FLPlayerPayload10Detour))]
    private readonly Hook<FlPlayerPayload10Delegate> _flPlayerPayloadHook;

    public FrontlineMatchManager(Plugin plugin) : base(plugin) {
        plugin.DutyState.DutyCompleted += OnDutyCompleted;
        //plugin.InteropProvider.InitializeFromAttributes(this);
        plugin.Log.Debug($"fl director .ctor address: 0x{_flDirectorCtorHook!.Address:X2}");
        plugin.Log.Debug($"fl match end address: 0x{_flMatchEndHook!.Address:X2}");
        plugin.Log.Debug($"fl player payload address: 0x{_flPlayerPayloadHook!.Address:X2}");
        _flDirectorCtorHook.Enable();
        _flMatchEndHook.Enable();
        _flPlayerPayloadHook.Enable();
    }

    public override void Dispose() {
        Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        _flDirectorCtorHook.Dispose();
        _flMatchEndHook.Dispose();
        _flPlayerPayloadHook.Dispose();
        base.Dispose();
    }

    private void OnDutyCompleted(object? sender, ushort p1) {
        Plugin.Log.Debug("Duty has completed.");
    }

    private IntPtr FLDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3) {
        Plugin.Log.Debug("Fl director .ctor detour entered.");
        return _flDirectorCtorHook.Original(p1, p2, p3);
    }

    private unsafe void FLMatchEnd100Detour(IntPtr p1, IntPtr p2) {
        Plugin.Log.Debug("Fl match end 100 detour entered.");

        FrontlineResultsPacket resultsPacket = *(FrontlineResultsPacket*)(p2);

        var printTeamStats = (FrontlineResultsPacket.TeamStat team, string name) => {
            Plugin.Log.Debug($"{name}\nPlace {team.Place}\nOvooPoints {team.OccupationPoints}\nKillPoints {team.EnemyKillPoints}\nDeathLosses {team.KOPointLosses}\nUnknown1 {team.Unknown1}\nUnknown2 {team.Unknown2}\nTotalRating {team.TotalPoints}");
        };
        printTeamStats(resultsPacket.MaelStats, "Maelstrom");
        printTeamStats(resultsPacket.AdderStats, "Adders");
        printTeamStats(resultsPacket.FlameStats, "Flames");

        _flMatchEndHook.Original(p1, p2);
    }

    private unsafe void FLPlayerPayload10Detour(IntPtr p1) {
        Plugin.Log.Debug("Fl player payload 10 detour entered.");

        //player payload
        FrontlinePlayerResultsPacket resultsPacket = *(FrontlinePlayerResultsPacket*)(p1);
        var playerName = (PlayerAlias)$"{MemoryService.ReadString(resultsPacket.PlayerName, 32)} {Plugin.DataManager.GetExcelSheet<World>()?.GetRow(resultsPacket.WorldId)?.Name}";
        var team = resultsPacket.Team == 0 ? "Maelstrom" : resultsPacket.Team == 1 ? "Adders" : "Flames";
        var job = PlayerJobHelper.GetJobFromName(Plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(resultsPacket.ClassJobId)?.NameEnglish ?? "");
        Plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-15} {13,-15}", "NAME", "TEAM", "ALLIANCE", "JOB", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE OTHER", "DAMAGE TAKEN", "HP RESTORED", "HP RECEIVED", "??? 2", "OCCUPATIONS"));
        Plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-15} {13,-15}", playerName, team, resultsPacket.Alliance, job, resultsPacket.Kills, resultsPacket.Deaths, resultsPacket.Assists, resultsPacket.DamageDealt, resultsPacket.DamageToOther, resultsPacket.DamageTaken, resultsPacket.HPRestored, resultsPacket.Unknown1, resultsPacket.Unknown2, resultsPacket.Occupations));
        _flPlayerPayloadHook.Original(p1);
    }
}
