using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Helpers;
using PvpStats.Services;
using PvpStats.Types.ClientStruct;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Threading.Tasks;

namespace PvpStats.Managers.Game;
internal class RivalWingsMatchManager : MatchManager<RivalWingsMatch> {

    private IntPtr _leaveDutyButton = IntPtr.Zero;
    private bool _matchEnded;
    private bool _resultPayloadReceived;

    private DateTime _lastPrint = DateTime.MinValue;

    //rw director ctor
    private delegate IntPtr RWDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4);
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC ?? 41 8B D9 48 8B F1", DetourName = nameof(RWDirectorCtorDetour))]
    private readonly Hook<RWDirectorCtorDelegate> _rwDirectorCtorHook;

    //rw match end 10 (occurs ~8 seconds after match ends)
    //p1 = director
    //p2 = payload
    private delegate void RWMatchEnd10Delegate(IntPtr p1, IntPtr p2);
    [Signature("40 55 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B E9", DetourName = nameof(RWMatchEnd10Detour))]
    private readonly Hook<RWMatchEnd10Delegate> _rwMatchEndHook;

    //leave duty
    private delegate void LeaveDutyDelegate(byte p1);
    [Signature("E8 ?? ?? ?? ?? 48 8B 43 28 B1 01", DetourName = nameof(LeaveDutyDetour))]
    private readonly Hook<LeaveDutyDelegate> _leaveDutyHook;

    public RivalWingsMatchManager(Plugin plugin) : base(plugin) {
        plugin.DutyState.DutyCompleted += OnDutyCompleted;
        plugin.Framework.Update += OnFrameworkUpdate;
        plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinderMenu", DutyMenuSetup);
        plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "ContentsFinderMenu", DutyMenuClose);
        plugin.Log.Debug($"rw director .ctor address: 0x{_rwDirectorCtorHook!.Address:X2}");
        plugin.Log.Debug($"rw match end 10 address: 0x{_rwMatchEndHook!.Address:X2}");
        plugin.Log.Debug($"leave duty address: 0x{_leaveDutyHook!.Address:X2}");
        _rwDirectorCtorHook.Enable();
        _rwMatchEndHook.Enable();
        _leaveDutyHook.Enable();
    }

    public override void Dispose() {
        Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        Plugin.Framework.Update -= OnFrameworkUpdate;
        Plugin.AddonLifecycle.UnregisterListener(DutyMenuSetup);
        Plugin.AddonLifecycle.UnregisterListener(DutyMenuClose);
        _rwDirectorCtorHook.Dispose();
        _rwMatchEndHook.Dispose();
        _leaveDutyHook.Dispose();
        base.Dispose();
    }

    private IntPtr RWDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4) {
        Plugin.Log.Debug("rw director .ctor detour entered.");
        try {
            var dutyId = Plugin.GameState.GetCurrentDutyId();
            var territoryId = Plugin.ClientState.TerritoryType;
            Plugin.Log.Debug($"Current duty: {dutyId} Current territory: {territoryId}");
            Plugin.DataQueue.QueueDataOperation(() => {
                CurrentMatch = new() {
                    DutyId = dutyId,
                    TerritoryId = territoryId,
                    Arena = MatchHelper.GetRivalWingsMap(dutyId),
                };
                Plugin.Log.Information($"starting new match on {CurrentMatch.Arena}");
                //Plugin.DataQueue.QueueDataOperation(async () => {
                //    await Plugin.FLCache.AddMatch(CurrentMatch);
                //});
            });
            _resultPayloadReceived = false;
            _matchEnded = false;
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            Plugin.Log.Error(e, $"Error in rw director .ctor.");
        }
        return _rwDirectorCtorHook.Original(p1, p2, p3, p4);
    }

    private void RWMatchEnd10Detour(IntPtr p1, IntPtr p2) {
        Plugin.Log.Debug("rw match end detour entered.");
        _resultPayloadReceived = true;
        EnableLeaveDutyButton();

        RivalWingsResultsPacket resultsPacket;
        unsafe {
            resultsPacket = *(RivalWingsResultsPacket*)p2;
            Plugin.Log.Debug($"Match Length: {resultsPacket.MatchLength}");
            Plugin.Log.Debug($"Result: {resultsPacket.Result}");
            Plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-8}", "NAME", "TEAM", "ALLIANCE", "JOB", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE OTHER", "DAMAGE TAKEN", "HP RESTORED", "???", "CERULEUM"));

            for(int i = 0; i < resultsPacket.PlayerCount; i++) {
                var player = resultsPacket.PlayerSpan[i];
                var playerName = (PlayerAlias)$"{MemoryService.ReadString(player.PlayerName, 32)} {Plugin.DataManager.GetExcelSheet<World>()?.GetRow(player.WorldId)?.Name}";
                //var playerName = MemoryService.ReadString(player.PlayerName, 32);
                var job = PlayerJobHelper.GetJobFromName(Plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(player.ClassJobId)?.NameEnglish ?? "");
                Plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-8}", playerName, player.Team, player.Alliance, job, player.Kills, player.Deaths, player.Assists, player.DamageDealt, player.DamageToOther, player.DamageTaken, player.HPRestored, player.Unknown1, player.Ceruleum));
            }

        }

        _rwMatchEndHook.Original(p1, p2);
    }

    private void LeaveDutyDetour(byte p1) {
        if(IsMatchInProgress() && _matchEnded && !_resultPayloadReceived) {
            Plugin.Log.Debug("Preventing duty leave!");
            return;
        }
        _leaveDutyHook.Original(p1);
    }

    protected override void OnDutyCompleted(object? sender, ushort p1) {
        Plugin.Log.Debug("Duty has completed.");
        _matchEnded = true;
        ////re-enable duty leave button after 15 seconds as a fallback
        //Task.Delay(15000).ContinueWith(t => {
        //    EnableLeaveDutyButton();
        //});
    }

    private void DutyMenuSetup(AddonEvent type, AddonArgs args) {
        if(!IsMatchInProgress()) {
            return;
        }
        Plugin.Log.Debug("Duty menu setup");
        unsafe {
            if(_matchEnded && !_resultPayloadReceived) {
                var addon = (AtkUnitBase*)args.Addon;
                //var buttonNode = (AtkComponentButton*)AtkNodeService.GetNodeByIDChain(addon, 1, 5, 6, 7, 8);
                var buttonNode = addon->GetButtonNodeById(8);
                if(buttonNode != null) {
                    Plugin.Log.Debug($"Disabling button at node: 0x{new IntPtr(buttonNode):X8}");
                    buttonNode->AtkComponentBase.SetEnabledState(false);
                    _leaveDutyButton = (IntPtr)buttonNode;
                }
            }
        }
    }

    private void DutyMenuClose (AddonEvent type, AddonArgs args) {
        if(!IsMatchInProgress()) {
            return;
        }
        Plugin.Log.Debug("Duty menu closed");
        _leaveDutyButton = IntPtr.Zero;
    }

    private unsafe void EnableLeaveDutyButton() {
        if(_leaveDutyButton != IntPtr.Zero) {
            ((AtkComponentButton*)_leaveDutyButton)->AtkComponentBase.SetEnabledState(true);
        }
    }

    private unsafe void OnFrameworkUpdate(IFramework framework) {
        if(!IsMatchInProgress()) {
            return;
        }
        var now = DateTime.Now;
        if((now - _lastPrint).TotalMinutes < 1) {
            return;
        }
        _lastPrint = now;

        var instanceDirector = (RivalWingsContentDirector*)EventFramework.Instance()->GetInstanceContentDirector();
        //var falconCore = *(ushort*)(instanceDirector + 0x1D78);
        //var falconT1 = *(ushort*)(instanceDirector + 0x1EB8);
        //var falconT2 = *(ushort*)(instanceDirector + 0x1F58);
        //var ravenCore = *(ushort*)(instanceDirector + 0x1E18);
        //var ravenT1 = *(ushort*)(instanceDirector + 0x1FF8);
        //var ravenT2 = *(ushort*)(instanceDirector + 0x2098);

        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "ITEM", "INTEGRITY"));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Core", instanceDirector->FalconCore.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Tower 1", instanceDirector->FalconTower1.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Tower 2", instanceDirector->FalconTower2.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Core", instanceDirector->RavenCore.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Tower 1", instanceDirector->RavenTower1.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Tower 2", instanceDirector->RavenTower2.Integrity));

        Plugin.Log.Debug(string.Format("{0,-9} {1,-9}, {1,-9}", "ALLIANCE", "CERULEUM", "SOARING"));
        for(int i = 0; i < 6; i++) {
            var alliance = instanceDirector->AllianceSpan[i];
            Plugin.Log.Debug(string.Format("{0,-9} {1,-9}, {1,-9}", i, alliance.Ceruleum, alliance.SoaringStacks));
        }
    }
}
