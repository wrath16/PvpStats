using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
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
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static PvpStats.Types.ClientStruct.RivalWingsContentDirector;

namespace PvpStats.Managers.Game;
internal class RivalWingsMatchManager : MatchManager<RivalWingsMatch> {

    private IntPtr _leaveDutyButton = IntPtr.Zero;
    private bool _matchEnded;
    private bool _resultPayloadReceived;

    private Dictionary<uint, PlayerAlias> _objIdToPlayer = [];
    private Dictionary<RivalWingsTeamName, Dictionary<RivalWingsMech, double>> _mechTime = [];
    private Dictionary<RivalWingsTeamName, Dictionary<RivalWingsSupplies, int>> _midCounts = [];
    private Dictionary<RivalWingsTeamName, int> _mercCounts = [];
    private Dictionary<int, (int CeruleumLast, int CeruleumGenerated, int CeruleumConsumed)> _allianceStats = [];
    private Dictionary<uint, (RivalWingsMech? LastMech, Dictionary<RivalWingsMech, int> MechsDeployed, Dictionary<RivalWingsMech, double> MechTime)> _playerMechStats = [];

    private RivalWingsContentDirector.Team? _lastMercControl;
    private DateTime _lastUpdate;
    private int _lastFalconMidScore;
    private int _lastRavenMidScore;
    //private Dictionary<int, (int CeruleumGenerated, int CeruleumConsumed)> _lastAllianceStats = [];
    //private Dictionary<uint, (Dictionary<RivalWingsMech, int> MechsDeployed, Dictionary<RivalWingsMech, double> MechTime)> _lastPlayerMechStats = [];

    //private Dictionary<int, (int CeruleumGenerated, int CeruleumConsumed, Dictionary<RivalWingsMech, int> MechsDeployed, Dictionary<RivalWingsMech, double> MechTime)> _allianceStats = [];

    //private int _lastFalconChaserCount;
    //private int _lastFalconOppressorCount;
    //private int _lastFalconJusticeCount;
    //private int _lastRavenChaserCount;
    //private int _lastRavenOppressorCount;
    //private int _lastRavenJusticeCount;

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

    private delegate void MechDeployDelegate(IntPtr p1, IntPtr p2);
    [Signature("48 89 54 24 ?? 48 89 4C 24 ?? 41 55 41 56 48 81 EC", DetourName = nameof(MechDeployDetour))]
    private readonly Hook<MechDeployDelegate> _mechDeployHook;

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
        plugin.Log.Debug($"rw mech deploy address: 0x{_mechDeployHook!.Address:X2}");
        plugin.Log.Debug($"leave duty address: 0x{_leaveDutyHook!.Address:X2}");
        _rwDirectorCtorHook.Enable();
        _rwMatchEndHook.Enable();
        _leaveDutyHook.Enable();
        _mechDeployHook.Enable();
    }

    public override void Dispose() {
        Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        Plugin.Framework.Update -= OnFrameworkUpdate;
        Plugin.AddonLifecycle.UnregisterListener(DutyMenuSetup);
        Plugin.AddonLifecycle.UnregisterListener(DutyMenuClose);
        _rwDirectorCtorHook.Dispose();
        _rwMatchEndHook.Dispose();
        _leaveDutyHook.Dispose();
        _mechDeployHook.Dispose();
        base.Dispose();
    }

    private IntPtr RWDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4) {
        try {
            Plugin.Log.Debug("rw director .ctor detour entered.");
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
            _objIdToPlayer = [];
            _mechTime = [];
            _midCounts = [];
            _mercCounts = [];
            _playerMechStats = [];
            _allianceStats = [];
            var allTeams = Enum.GetValues(typeof(RivalWingsTeamName)).Cast<RivalWingsTeamName>();
            var allMechs = Enum.GetValues(typeof(RivalWingsMech)).Cast<RivalWingsMech>();
            var allSupples = Enum.GetValues(typeof(RivalWingsSupplies)).Cast<RivalWingsSupplies>();
            foreach(var team in allTeams) {
                _mechTime.Add(team, new());
                _mercCounts.Add(team, 0);
                foreach(var mech in allMechs) {
                    _mechTime[team].Add(mech, 0f);
                }
                _midCounts.Add(team, new());
                foreach(var supplies in allSupples) {
                    _midCounts[team].Add(supplies, 0);
                }
            }
            for(int i = 0; i < 6; i++) {
                _allianceStats.Add(i, new());
            }
            _lastFalconMidScore = 0;
            _lastRavenMidScore = 0;
            _lastMercControl = RivalWingsContentDirector.Team.None;
            //_lastAllianceStats = [];
            //_lastPlayerMechStats = [];
            //_lastFalconChaserCount = 0;
            //_lastFalconOppressorCount = 0;
            //_lastFalconJusticeCount = 0;
            //_lastRavenChaserCount = 0;
            //_lastRavenOppressorCount = 0;
            //_lastRavenJusticeCount = 0;
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            Plugin.Log.Error(e, $"Error in rw director .ctor.");
        }
        return _rwDirectorCtorHook.Original(p1, p2, p3, p4);
    }

    private void RWMatchEnd10Detour(IntPtr p1, IntPtr p2) {
        try {
            Plugin.Log.Debug("rw match end detour entered.");
            _resultPayloadReceived = true;
            EnableLeaveDutyButton();

            RivalWingsResultsPacket resultsPacket;
            unsafe {
                var director = (RivalWingsContentDirector*)EventFramework.Instance()->GetInstanceContentDirector();
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", "TEAM", "CORE", "TOWER1", "TOWER2"));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", RivalWingsTeamName.Falcons, director->FalconCore.Integrity, director->FalconTower1.Integrity, director->FalconTower2.Integrity));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", RivalWingsTeamName.Ravens, director->RavenCore.Integrity, director->RavenTower1.Integrity, director->RavenTower2.Integrity));

                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9} {4,-9} {5,-9}", "TEAM", "MERCS", "TANKS", "CERULEUM", "JUICE", "CRATES"));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9} {4,-9} {5,-9}", RivalWingsTeamName.Falcons, _mercCounts[RivalWingsTeamName.Falcons], _midCounts[RivalWingsTeamName.Falcons][RivalWingsSupplies.Gobtank], _midCounts[RivalWingsTeamName.Falcons][RivalWingsSupplies.Ceruleum], _midCounts[RivalWingsTeamName.Falcons][RivalWingsSupplies.Gobbiejuice], _midCounts[RivalWingsTeamName.Falcons][RivalWingsSupplies.Gobcrate]));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9} {4,-9} {5,-9}", RivalWingsTeamName.Ravens, _mercCounts[RivalWingsTeamName.Ravens], _midCounts[RivalWingsTeamName.Ravens][RivalWingsSupplies.Gobtank], _midCounts[RivalWingsTeamName.Ravens][RivalWingsSupplies.Ceruleum], _midCounts[RivalWingsTeamName.Ravens][RivalWingsSupplies.Gobbiejuice], _midCounts[RivalWingsTeamName.Ravens][RivalWingsSupplies.Gobcrate]));

                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", "TEAM", "CHASER", "OPP", "JUSTICE"));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9:0.00} {2,-9:0.00} {3,-9:0.00}", RivalWingsTeamName.Falcons, _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Chaser], _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Oppressor], _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Justice]));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9:0.00} {2,-9:0.00} {3,-9:0.00}", RivalWingsTeamName.Ravens, _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Chaser], _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Oppressor], _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Justice]));

                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", "ALLIANCE", "SOARING", "CERULEUM+", "CERULEUM-"));
                foreach(var alliance in _allianceStats) {
                    Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", alliance.Key, director->AllianceSpan[alliance.Key].SoaringStacks, alliance.Value.CeruleumGenerated, alliance.Value.CeruleumConsumed));
                }

                Plugin.Log.Debug(string.Format("{0,-32} {1,-9} {2,-9} {3,-9}", "PLAYER", "CHASER", "OPP", "JUSTICE"));
                foreach(var player in _playerMechStats) {
                    _objIdToPlayer.TryGetValue(player.Key, out var playerName);

                    Plugin.Log.Debug(string.Format("{0,-32} {1,-9:0.00} {2,-9:0.00} {3,-9:0.00}", playerName?.Name ?? player.Key.ToString(), player.Value.MechTime[RivalWingsMech.Chaser], player.Value.MechTime[RivalWingsMech.Oppressor], player.Value.MechTime[RivalWingsMech.Justice]));
                }

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
        } catch(Exception e) {
            Plugin.Log.Error(e, $"Error in rw match end .ctor.");
        }
        _rwMatchEndHook.Original(p1, p2);
    }

    private void MechDeployDetour(IntPtr p1, IntPtr p2) {
        Plugin.Log.Debug("rw ICD update detour entered.");
        //Plugin.Functions.CreateByteDump(p2, 0x1000, "MECHDEPLOY");
        //Plugin.Functions.FindValue<ushort>(0, p2, 0x200, 0, true);
        _mechDeployHook.Original(p1, p2);
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
        var director = (RivalWingsContentDirector*)EventFramework.Instance()->GetInstanceContentDirector();
        var now = DateTime.Now;

        //mech times
        if(!_matchEnded) {
            _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Chaser] += director->FalconChaserCount * (now - _lastUpdate).TotalSeconds;
            _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Oppressor] += director->FalconOppressorCount * (now - _lastUpdate).TotalSeconds;
            _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Justice] += director->FalconJusticeCount * (now - _lastUpdate).TotalSeconds;
            _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Chaser] += director->RavenChaserCount * (now - _lastUpdate).TotalSeconds;
            _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Oppressor] += director->RavenOppressorCount * (now - _lastUpdate).TotalSeconds;
            _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Justice] += director->RavenJusticeCount * (now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;
        }

        //merc win
        if(_lastMercControl == RivalWingsContentDirector.Team.None && director->MercControl != RivalWingsContentDirector.Team.None) {
            _mercCounts[(RivalWingsTeamName)director->MercControl]++;
        }
        _lastMercControl = director->MercControl;

        //mid win
        if(_lastFalconMidScore != 100 && director->FalconMidScore == 100) {
            _midCounts[RivalWingsTeamName.Falcons][(RivalWingsSupplies)director->MidType]++;
        }
        _lastFalconMidScore = director->FalconMidScore;
        if(_lastRavenMidScore != 100 && director->RavenMidScore == 100) {
            _midCounts[RivalWingsTeamName.Ravens][(RivalWingsSupplies)director->MidType]++;
        }
        _lastRavenMidScore = director->RavenMidScore;

        //alliance ceruleum stats
        for(int i = 0; i < director->AllianceSpan.Length; i++) {
            var alliance = director->AllianceSpan[i];
            var allianceStats = _allianceStats[i];
            //add input bounds for sanity check in case of missing alliance
            if(allianceStats.CeruleumLast != alliance.Ceruleum && alliance.Ceruleum <= 100 && alliance.Ceruleum >= 0) {
                if(allianceStats.CeruleumLast > alliance.Ceruleum) {
                    allianceStats.CeruleumConsumed += _allianceStats[i].CeruleumLast - (int)alliance.Ceruleum;
                } else if(allianceStats.CeruleumLast < alliance.Ceruleum) {
                    allianceStats.CeruleumGenerated += (int)alliance.Ceruleum - _allianceStats[i].CeruleumLast;
                }
                allianceStats.CeruleumLast = (int)alliance.Ceruleum;
                _allianceStats[i] = allianceStats;
            }
        }

        //associate player Ids with aliases
        foreach(PlayerCharacter pc in Plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player).Cast<PlayerCharacter>()) {
            if(!_objIdToPlayer.ContainsKey(pc.ObjectId)) {
                var worldName = Plugin.DataManager.GetExcelSheet<World>()?.GetRow(pc.HomeWorld.Id)?.Name;
                var player = (PlayerAlias)$"{pc.Name} {worldName}";
                _objIdToPlayer.Add(pc.ObjectId, player);
            }
        }

        //set friendly mech stats
        for(int i = 0; i < director->FriendlyMechSpan.Length; i++) {
            var friendlyMechNative = director->FriendlyMechSpan[i];
            //var mechStats = _playerMechStats[i];
            //add input bounds for sanity check in case of missing alliance
            if(friendlyMechNative.Type != MechType.None) {
                var mech = (RivalWingsMech)friendlyMechNative.Type;
                if(!_playerMechStats.TryGetValue(friendlyMechNative.PlayerObjectId, out (RivalWingsMech? LastMech, Dictionary<RivalWingsMech, int> MechsDeployed, Dictionary<RivalWingsMech, double> MechTime) mechStats)) {
                    mechStats = new();
                    mechStats.MechTime.Add(RivalWingsMech.Chaser, 0);
                    mechStats.MechTime.Add(RivalWingsMech.Oppressor, 0);
                    mechStats.MechTime.Add(RivalWingsMech.Justice, 0);
                    _playerMechStats.Add(friendlyMechNative.PlayerObjectId, mechStats);
                }
                if(!mechStats.MechTime.TryGetValue(mech, out double mechTime)) {
                    mechStats.MechTime.Add(mech, 0);
                }
                mechStats.MechTime[mech] += (now - _lastUpdate).TotalSeconds;
                //not sure if this needed
                _playerMechStats[friendlyMechNative.PlayerObjectId] = mechStats;
            }
        }



        return;

        if((now - _lastPrint).TotalSeconds < 30) {
            return;
        }
        _lastPrint = now;

        Plugin.Log.Debug("");

        var playerPosition = Plugin.ClientState.LocalPlayer.Position;
        var playerMapPosition = WorldPosToMapCoords(playerPosition);
        Plugin.Log.Debug($"Player Position: {playerPosition} Map Coords: {playerMapPosition}");

        foreach(PlayerCharacter pc in Plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
            //Status x = new();
            //x.RowId = 1420;
            //var y = pc.StatusList.ToList().Where(x => x.StatusId == 1420);

            if(pc.MaxHp ==250000) {
                Plugin.Log.Debug($"{pc.Name} is a CC (health)");
            } else if(pc.MaxHp == 500000) {
                Plugin.Log.Debug($"{pc.Name} is an Opp (health)");
            } else if(pc.MaxHp == 375000) {
                Plugin.Log.Debug($"{pc.Name} is a BJ (health)");
            }

            if(pc.StatusList.Where(x => x.StatusId == 1420).Any()) {
                Plugin.Log.Debug($"ID: {pc.ObjectId} 0x{pc.ObjectId:X2} NAME: {pc.Name} is in a machina! Position: {pc.Position} Coords: {WorldPosToMapCoords(pc.Position)}");
            }
            //_plugin.Log.Debug($"0x{pc.ObjectId.ToString("X2")} {pc.Name}");
            //_plugin.Log.Debug($"team null? {isPlayerTeam is null} player team? {isPlayerTeam} is p member? {pc.StatusFlags.HasFlag(StatusFlags.PartyMember)} isSelf? {isSelf}");
        }


        Plugin.Functions.CreateByteDump((nint)director, 0x3000, "RWICD");
        //var falconCore = *(ushort*)(instanceDirector + 0x1D78);
        //var falconT1 = *(ushort*)(instanceDirector + 0x1EB8);
        //var falconT2 = *(ushort*)(instanceDirector + 0x1F58);
        //var ravenCore = *(ushort*)(instanceDirector + 0x1E18);
        //var ravenT1 = *(ushort*)(instanceDirector + 0x1FF8);
        //var ravenT2 = *(ushort*)(instanceDirector + 0x2098);

        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "ITEM", "INTEGRITY"));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Core", director->FalconCore.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Tower 1", director->FalconTower1.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Tower 2", director->FalconTower2.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Core", director->RavenCore.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Tower 1", director->RavenTower1.Integrity));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Tower 2", director->RavenTower2.Integrity));

        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Chasers:", director->FalconChaserCount));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Opps:", director->FalconOppressorCount));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Falcon Justices:", director->FalconJusticeCount));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Chasers:", director->RavenChaserCount));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Opps:", director->RavenOppressorCount));
        Plugin.Log.Debug(string.Format("{0,-20} {1,-9}", "Raven Justices:", director->RavenJusticeCount));

        Plugin.Log.Debug($"MERC SCORE: {director->MercBalance} CONTROL: {director->MercControl}");


        Plugin.Log.Debug($"MID TYPE: {director->MidType} CONTROL: {director->MidControl} FALCONS: {director->FalconMidScore} RAVENS: {director->RavenMidScore}");

        Plugin.Log.Debug(string.Format("{0,-32} {1,-9}", "PLAYER", "MECH"));
        for(int i = 0; i < 24; i++) {
            var mech = director->FriendlyMechSpan[i];
            if(mech.Type != RivalWingsContentDirector.MechType.None) {
                string? name = null;
                foreach(PlayerCharacter pc in Plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
                    if(pc.ObjectId == mech.PlayerObjectId) {
                        name = pc.Name.ToString(); break;
                    }
                }
                Plugin.Log.Debug(string.Format("{0,-32} {1,-9}", name ?? mech.PlayerObjectId.ToString(), mech.Type));
            }
        }

        //Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", "A", instanceDirector->MechAllianceA, instanceDirector->MechAllianceA_2));
        //Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", "B", instanceDirector->MechAllianceB, instanceDirector->MechAllianceB_2));
        //Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", "C", instanceDirector->MechAllianceC, instanceDirector->MechAllianceC_2));
        //Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", "D", instanceDirector->MechAllianceD, instanceDirector->MechAllianceD_2));
        //Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", "E", instanceDirector->MechAllianceE, instanceDirector->MechAllianceE_2));
        //Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", "F", instanceDirector->MechAllianceF, instanceDirector->MechAllianceF_2));

        Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", "ALLIANCE", "CERULEUM", "SOARING"));
        for(int i = 0; i < 6; i++) {
            var alliance = director->AllianceSpan[i];
            Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9}", i, alliance.Ceruleum, alliance.SoaringStacks));
        }
    }

    private static Vector2 WorldPosToMapCoords(Vector3 pos) {
        var xInt = (int)(MathF.Round(pos.X, 3, MidpointRounding.AwayFromZero) * 1000);
        var yInt = (int)(MathF.Round(pos.Z, 3, MidpointRounding.AwayFromZero) * 1000);
        return new Vector2((int)(xInt * 0.001f * 1000f), (int)(yInt * 0.001f * 1000f));
    }
}
