using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PvpStats.Helpers;
using PvpStats.Services;
using PvpStats.Types.Match;
using System;

namespace PvpStats.Managers.Game;
internal class RivalWingsMatchManager : MatchManager<RivalWingsMatch> {

    private IntPtr _leaveDutyButton = IntPtr.Zero;
    private bool _matchEnded;
    private bool _resultPayloadReceived;

    //rw director ctor
    private delegate IntPtr RWDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4);
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC ?? 41 8B D9 48 8B F1", DetourName = nameof(RWDirectorCtorDetour))]
    private readonly Hook<RWDirectorCtorDelegate> _rwDirectorCtorHook;

    //rw match end 10 (occurs ~8 seconds after match ends)
    //p1 = director
    //p2 = payloa
    private delegate void RWMatchEnd10Delegate(IntPtr p1, IntPtr p2);
    [Signature("40 55 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B E9", DetourName = nameof(RWMatchEnd10Detour))]
    private readonly Hook<RWMatchEnd10Delegate> _rwMatchEndHook;

    //leave duty
    private delegate void LeaveDutyDelegate(byte p1);
    [Signature("E8 ?? ?? ?? ?? 48 8B 43 28 B1 01", DetourName = nameof(LeaveDutyDetour))]
    private readonly Hook<LeaveDutyDelegate> _leaveDutyHook;

    public RivalWingsMatchManager(Plugin plugin) : base(plugin) {
        plugin.DutyState.DutyCompleted += OnDutyCompleted;
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
                    //Arena = MatchHelper.GetFrontlineMap(dutyId),
                };
                //Plugin.Log.Information($"starting new match on {CurrentMatch.Arena}");
                //Plugin.DataQueue.QueueDataOperation(async () => {
                //    await Plugin.FLCache.AddMatch(CurrentMatch);
                //});
            });
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            Plugin.Log.Error(e, $"Error in FL director .ctor.");
        }
        return _rwDirectorCtorHook.Original(p1, p2, p3, p4);
    }

    private void RWMatchEnd10Detour(IntPtr p1, IntPtr p2) {
        Plugin.Log.Debug("rw match end detour entered.");
        _resultPayloadReceived = true;
        if(_leaveDutyButton != IntPtr.Zero) {
            unsafe {
                ((AtkComponentButton*)_leaveDutyButton)->AtkComponentBase.SetEnabledState(true);
            }
        }
        _rwMatchEndHook.Original(p1, p2);
    }

    private void LeaveDutyDetour(byte p1) {
        Plugin.Log.Debug("leaving duty. neato!");
        _leaveDutyHook.Original(p1);
    }

    protected override void OnDutyCompleted(object? sender, ushort p1) {
        Plugin.Log.Debug("Duty has completed.");
        _matchEnded = true;
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
                Plugin.Log.Debug($"button node: 0x{new IntPtr(buttonNode).ToString("X8")}");
                buttonNode->AtkComponentBase.SetEnabledState(false);
                _leaveDutyButton = (IntPtr)buttonNode;

                //Plugin.Log.Debug($"enabled? {buttonNode->IsEnabled}");
                //buttonNode->AtkComponentBase.OwnerNode->AtkResNode.NodeFlags &= ~NodeFlags.Enabled;
                var textNode = (AtkTextNode*)AtkNodeService.GetNodeByIDChain(addon, 1, 5, 6, 7, 8, 2);
                //textNode->SetText("Testo");
                //addon->Draw();
                //buttonNode->Flags &= ~(uint)NodeFlags.Enabled;
                //Plugin.Log.Debug($"enabled after? {buttonNode->IsEnabled}");
            }
        }
    }

    private void DutyMenuClose (AddonEvent type, AddonArgs args) {
        Plugin.Log.Debug("Duty menu closed");
        _leaveDutyButton = IntPtr.Zero;
    }
}
