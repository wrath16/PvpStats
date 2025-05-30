﻿using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PvpStats.Helpers;
using PvpStats.Services;
using PvpStats.Types.ClientStruct;
using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;

namespace PvpStats.Managers.Game;
internal class RivalWingsMatchManager : MatchManager<RivalWingsMatch> {

    private RivalWingsMatchTimeline? _currentMatchTimeline;
    private DateTime _lastStructurePoll = DateTime.UnixEpoch;
    private uint _pollingThresholdMS = 5000;

    private IntPtr _leaveDutyButton = IntPtr.Zero;
    private IntPtr _leaveDutyButtonOwnerNode = IntPtr.Zero;
    private string? _leaveDutyButtonText;
    private ushort _addonId;
    IAddonEventHandle? _mouseOverEvent;
    IAddonEventHandle? _mouseOutEvent;
    private bool _matchEnded;
    private bool _resultPayloadReceived;

    private Dictionary<ulong, PlayerAlias> _objIdToPlayer = [];
    private Dictionary<RivalWingsTeamName, Dictionary<RivalWingsMech, double>> _mechTime = [];
    private Dictionary<RivalWingsTeamName, Dictionary<RivalWingsSupplies, int>> _midCounts = [];
    private Dictionary<RivalWingsTeamName, int> _mercCounts = [];
    private Dictionary<int, (int CeruleumLast, int CeruleumGenerated, int CeruleumConsumed)> _allianceStats = [];
    private Dictionary<uint, (RivalWingsMech? LastMech, Dictionary<RivalWingsMech, int> MechsDeployed, Dictionary<RivalWingsMech, double> MechTime)> _playerMechStats = [];

    private RivalWingsContentDirector.Team? _lastMercControl;
    private DateTime _lastUpdate;
    private int _lastFalconMidScore;
    private int _lastRavenMidScore;

    private DateTime _lastPrint = DateTime.MinValue;

    //rw director ctor
    private delegate IntPtr RWDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4);
    //48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC ?? 41 8B D9 48 8B F1
    //48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC ?? 41 8B D9 48 8B F9 
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC ?? 41 8B D9 48 8B F9", DetourName = nameof(RWDirectorCtorDetour))]
    private readonly Hook<RWDirectorCtorDelegate> _rwDirectorCtorHook;

    //rw match end 10 (occurs ~8 seconds after match ends)
    //p1 = director
    //p2 = payload
    //40 55 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B E9
    //48 89 6C 24 ?? 56 48 81 EC ?? ?? ?? ?? 48 8B E9 
    private delegate void RWMatchEnd10Delegate(IntPtr p1, IntPtr p2);
    [Signature("48 89 6C 24 ?? 56 48 81 EC ?? ?? ?? ?? 48 8B E9 48 8B F2", DetourName = nameof(RWMatchEnd10Detour))]
    private readonly Hook<RWMatchEnd10Delegate> _rwMatchEndHook;

    //private delegate void MechDeployDelegate(IntPtr p1, IntPtr p2);
    //[Signature("48 89 54 24 ?? 48 89 4C 24 ?? 41 55 41 56 48 81 EC", DetourName = nameof(MechDeployDetour))]
    //private readonly Hook<MechDeployDelegate> _mechDeployHook;

    //leave duty
    private delegate void LeaveDutyDelegate(byte p1);
    //E8 ?? ?? ?? ?? 48 8B 43 28 B1 01
    //E8 ?? ?? ?? ?? 48 8B 43 ?? 41 B2 
    [Signature("E8 ?? ?? ?? ?? 48 8B 43 ?? 41 B2", DetourName = nameof(LeaveDutyDetour))]
    private readonly Hook<LeaveDutyDelegate> _leaveDutyHook;

    public RivalWingsMatchManager(Plugin plugin) : base(plugin) {
        plugin.DutyState.DutyCompleted += OnDutyCompleted;
        plugin.Framework.Update += OnFrameworkUpdate;
        plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinderMenu", DutyMenuSetup);
        plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "ContentsFinderMenu", DutyMenuClose);
        plugin.Log.Debug($"rw director .ctor address: 0x{_rwDirectorCtorHook!.Address:X2}");
        plugin.Log.Debug($"rw match end 10 address: 0x{_rwMatchEndHook!.Address:X2}");
        //plugin.Log.Debug($"rw icd update address: 0x{_mechDeployHook!.Address:X2}");
        plugin.Log.Debug($"leave duty address: 0x{_leaveDutyHook!.Address:X2}");
        _rwDirectorCtorHook.Enable();
        _rwMatchEndHook.Enable();
        _leaveDutyHook.Enable();
#if DEBUG
        //_mechDeployHook.Enable();
#endif
    }

    public override void Dispose() {
        Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        Plugin.Framework.Update -= OnFrameworkUpdate;
        Plugin.AddonLifecycle.UnregisterListener(DutyMenuSetup);
        Plugin.AddonLifecycle.UnregisterListener(DutyMenuClose);
        _rwDirectorCtorHook.Dispose();
        _rwMatchEndHook.Dispose();
        _leaveDutyHook.Dispose();
#if DEBUG
        //_mechDeployHook.Dispose();
#endif
        base.Dispose();
    }

    private IntPtr RWDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4) {
        try {
            Plugin.Log.Debug("rw director .ctor detour entered.");
            var dutyId = Plugin.GameState.GetCurrentDutyId();
            var territoryId = Plugin.ClientState.TerritoryType;
            var arena = MatchHelper.GetRivalWingsMap(dutyId);
            Plugin.Log.Debug($"Current duty: {dutyId} Current territory: {territoryId}");
            Plugin.DataQueue.QueueDataOperation(() => {
                //fail safe for new map
                if(arena != RivalWingsMap.HiddenGorge) {
                    return;
                }
                CurrentMatch = new() {
                    DutyId = dutyId,
                    TerritoryId = territoryId,
                    Arena = arena,
                    PluginVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                };
                _currentMatchTimeline = new() {
                    StructureHealths = new() {
                        {RivalWingsTeamName.Falcons, new() {
                            { RivalWingsStructure.Core, new() },
                            { RivalWingsStructure.Tower1, new() },
                            { RivalWingsStructure.Tower2, new() },
                        } },
                        {RivalWingsTeamName.Ravens, new() {
                            { RivalWingsStructure.Core, new() },
                            { RivalWingsStructure.Tower1, new() },
                            { RivalWingsStructure.Tower2, new() },
                        } }
                    },
                    MechCounts = new() {
                        {RivalWingsTeamName.Falcons, new() {
                            { RivalWingsMech.Chaser, new() },
                            { RivalWingsMech.Oppressor, new() },
                            { RivalWingsMech.Justice, new() },
                        } },
                        {RivalWingsTeamName.Ravens, new() {
                            { RivalWingsMech.Chaser, new() },
                            { RivalWingsMech.Oppressor, new() },
                            { RivalWingsMech.Justice, new() },
                        } }
                    },
                    AllianceStacks = new() {
                        { 0, new() },
                        { 1, new() },
                        { 2, new() },
                        { 3, new() },
                        { 4, new() },
                        { 5, new() },
                    },
                    MercClaims = new(),
                    MidClaims = new()
                };
                unsafe {
                    if(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance() != null) {
                        CurrentMatch.GameVersion = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GameVersionString;
                    }
                }
                Plugin.Log.Information($"starting new match on {CurrentMatch.Arena}");
                Plugin.DataQueue.QueueDataOperation(async () => {
                    await Plugin.RWCache.AddMatch(CurrentMatch);
                    await Plugin.Storage.AddRWTimeline(_currentMatchTimeline);
                });
            });
            Reset();

        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            Plugin.Log.Error(e, $"Error in rw director .ctor.");
        }
        return _rwDirectorCtorHook.Original(p1, p2, p3, p4);
    }

    private void RWMatchEnd10Detour(IntPtr p1, IntPtr p2) {
        try {
            Plugin.Log.Debug("rw match end detour entered.");

            RivalWingsResultsPacket resultsPacket;
            RivalWingsContentDirector director;
            unsafe {
                director = *(RivalWingsContentDirector*)(IntPtr)EventFramework.Instance()->GetInstanceContentDirector();
                resultsPacket = *(RivalWingsResultsPacket*)p2;
            }

#if DEBUG
            //Plugin.Functions.CreateByteDump(p2, 0x3000, "rw_match_end");
#endif

            var matchEndTask = Plugin.DataQueue.QueueDataOperation(async () => {
                if(ProcessMatchResults(resultsPacket, director)) {
                    await Plugin.RWCache.UpdateMatch(CurrentMatch!);
                    if(_currentMatchTimeline != null) {
                        await Plugin.Storage.UpdateRWTimeline(_currentMatchTimeline);
                    }
                    _ = Plugin.WindowManager.RefreshRWWindow();
                }
            });
            //matchEndTask.Result.ContinueWith(t => Plugin.WindowManager.RefreshRWWindow());

            unsafe {
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", "TEAM", "CORE", "TOWER1", "TOWER2"));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", RivalWingsTeamName.Falcons, director.FalconCore.Integrity, director.FalconTower1.Integrity, director.FalconTower2.Integrity));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", RivalWingsTeamName.Ravens, director.RavenCore.Integrity, director.RavenTower1.Integrity, director.RavenTower2.Integrity));

                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9} {4,-9} {5,-9}", "TEAM", "MERCS", "TANKS", "CERULEUM", "JUICE", "CRATES"));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9} {4,-9} {5,-9}", RivalWingsTeamName.Falcons, _mercCounts[RivalWingsTeamName.Falcons], _midCounts[RivalWingsTeamName.Falcons][RivalWingsSupplies.Gobtank], _midCounts[RivalWingsTeamName.Falcons][RivalWingsSupplies.Ceruleum], _midCounts[RivalWingsTeamName.Falcons][RivalWingsSupplies.Gobbiejuice], _midCounts[RivalWingsTeamName.Falcons][RivalWingsSupplies.Gobcrate]));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9} {4,-9} {5,-9}", RivalWingsTeamName.Ravens, _mercCounts[RivalWingsTeamName.Ravens], _midCounts[RivalWingsTeamName.Ravens][RivalWingsSupplies.Gobtank], _midCounts[RivalWingsTeamName.Ravens][RivalWingsSupplies.Ceruleum], _midCounts[RivalWingsTeamName.Ravens][RivalWingsSupplies.Gobbiejuice], _midCounts[RivalWingsTeamName.Ravens][RivalWingsSupplies.Gobcrate]));

                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", "TEAM", "CHASER", "OPP", "JUSTICE"));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9:0.00} {2,-9:0.00} {3,-9:0.00}", RivalWingsTeamName.Falcons, _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Chaser], _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Oppressor], _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Justice]));
                Plugin.Log.Debug(string.Format("{0,-9} {1,-9:0.00} {2,-9:0.00} {3,-9:0.00}", RivalWingsTeamName.Ravens, _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Chaser], _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Oppressor], _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Justice]));

                Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", "ALLIANCE", "SOARING", "CERULEUM+", "CERULEUM-"));
                foreach(var alliance in _allianceStats) {
                    Plugin.Log.Debug(string.Format("{0,-9} {1,-9} {2,-9} {3,-9}", alliance.Key, director.AllianceSpan[alliance.Key].SoaringStacks, alliance.Value.CeruleumGenerated, alliance.Value.CeruleumConsumed));
                }

                Plugin.Log.Debug(string.Format("{0,-32} {1,-9} {2,-9} {3,-9}", "PLAYER", "CHASER", "OPP", "JUSTICE"));
                foreach(var player in _playerMechStats) {
                    _objIdToPlayer.TryGetValue(player.Key, out var playerName);
                    Plugin.Log.Debug(string.Format("{0,-32} {1,-9:0.00} {2,-9:0.00} {3,-9:0.00}", playerName?.Name ?? player.Key.ToString(), player.Value.MechTime[RivalWingsMech.Chaser], player.Value.MechTime[RivalWingsMech.Oppressor], player.Value.MechTime[RivalWingsMech.Justice]));
                }

                Plugin.Log.Debug($"Match Length: {resultsPacket.MatchLength}");
                Plugin.Log.Debug($"Result: {resultsPacket.Result}");
                Plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-8}", "NAME", "TEAM", "ALLIANCE", "JOB", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE OTHER", "DAMAGE TAKEN", "HP RESTORED", "???", "CERULEUM"));

                for(int i = 0; i < resultsPacket.PlayerCount; i++) {
                    var player = resultsPacket.PlayerSpan[i];
                    var playerName = (PlayerAlias)$"{MemoryService.ReadString(player.PlayerName, 32)} {Plugin.DataManager.GetExcelSheet<World>()?.GetRow(player.WorldId).Name}";
                    //var playerName = MemoryService.ReadString(player.PlayerName, 32);
                    var job = PlayerJobHelper.GetJobFromName(Plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(player.ClassJobId).NameEnglish.ToString() ?? "");
                    Plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-8}", playerName, player.Team, player.Alliance, job, player.Kills, player.Deaths, player.Assists, player.DamageDealt, player.DamageToOther, player.DamageTaken, player.HPRestored, player.Unknown1, player.Ceruleum));
                }
            }
        } catch(Exception e) {
            Plugin.Log.Error(e, $"Error in rw match end detour.");
        } finally {
            _resultPayloadReceived = true;
            EnableLeaveDutyButton();
            _rwMatchEndHook.Original(p1, p2);
        }
    }

    private bool ProcessMatchResults(RivalWingsResultsPacket results, RivalWingsContentDirector director) {
        if(!IsMatchInProgress()) {
            Plugin.Log.Error("trying to process match results on no match!");
            return false;
            //fallback for case where you load into a game after the match has completed creating a new match
        } else if((DateTime.Now - CurrentMatch!.DutyStartTime).TotalSeconds < 10) {
            Plugin.Log.Error("double match detected.");
            return false;
        }

        CurrentMatch.MatchEndTime = DateTime.Now;
        CurrentMatch.MatchStartTime = CurrentMatch.MatchEndTime - TimeSpan.FromSeconds(results.MatchLength);
        CurrentMatch.LocalPlayer ??= Plugin.GameState.CurrentPlayer;
        CurrentMatch.DataCenter ??= Plugin.GameState.DataCenterName;

        CurrentMatch.StructureHealth = new() {
        { RivalWingsTeamName.Falcons , new() {
            { RivalWingsStructure.Core, director.FalconCore.Integrity },
            { RivalWingsStructure.Tower1, director.FalconTower1.Integrity },
            { RivalWingsStructure.Tower2, director.FalconTower2.Integrity },
        }},
        { RivalWingsTeamName.Ravens , new() {
            { RivalWingsStructure.Core, director.RavenCore.Integrity },
            { RivalWingsStructure.Tower1, director.RavenTower1.Integrity },
            { RivalWingsStructure.Tower2, director.RavenTower2.Integrity },
        }} };

        CurrentMatch.AllianceStats = [];
        foreach(var alliance in _allianceStats) {
            CurrentMatch.AllianceStats.Add(alliance.Key, new() {
                SoaringStacks = director.AllianceSpan[alliance.Key].SoaringStacks
            });
        }

        CurrentMatch.PlayerCount = results.PlayerCount;
        CurrentMatch.Players = [];
        CurrentMatch.PlayerScoreboards = [];
        for(int i = 0; i < results.PlayerCount; i++) {
            var player = results.PlayerSpan[i];
            //check bounds here...
            //if(player.ClassJobId == 0) {
            //    Plugin.Log.Warning("invalid/missing player result.");
            //    continue;
            //}
            PlayerAlias playerName;
            unsafe {
                playerName = (PlayerAlias)$"{MemoryService.ReadString(player.PlayerName, 32)} {Plugin.DataManager.GetExcelSheet<World>()?.GetRow(player.WorldId).Name}";
            }
            var job = PlayerJobHelper.GetJobFromName(Plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(player.ClassJobId).NameEnglish.ToString() ?? "");

            RivalWingsScoreboard playerScoreboard = new() {
                Kills = player.Kills,
                Deaths = player.Deaths,
                Assists = player.Assists,
                DamageDealt = player.DamageDealt,
                DamageToOther = player.DamageToOther,
                DamageTaken = player.DamageTaken,
                HPRestored = player.HPRestored,
                Special1 = player.Unknown1,
                Ceruleum = player.Ceruleum
            };
            CurrentMatch.Players.Add(new() {
                Name = playerName,
                Job = job,
                Team = (RivalWingsTeamName)player.Team,
                ClassJobId = player.ClassJobId,
                Alliance = player.Alliance % 6,
                //AccountId = player.AccountId,
                //ContentId = player.ContentId,
            });
            CurrentMatch.PlayerScoreboards.Add(playerName, playerScoreboard);
        }

        //discard grace period 
        if(CurrentMatch.MatchStartTime > CurrentMatch.DutyStartTime - TimeSpan.FromSeconds(40)) {
            //CurrentMatch.TeamMechTime = _mechTime;
            //do this to decouple from fields
            CurrentMatch.TeamMechTime = [];
            foreach(var team in _mechTime) {
                CurrentMatch.TeamMechTime.Add(team.Key, []);
                foreach(var mechTime in team.Value) {
                    CurrentMatch.TeamMechTime[team.Key].Add(mechTime.Key, mechTime.Value);
                }
            }

            CurrentMatch.PlayerMechTime = [];
            foreach(var playerMechStat in _playerMechStats) {
                if(!_objIdToPlayer.TryGetValue(playerMechStat.Key, out var playerName)) {
                    Plugin.Log.Error($"Unknown objectID: {playerMechStat.Key}");
                    continue;
                }
                if(CurrentMatch.PlayerMechTime.ContainsKey(playerName)) {
                    Plugin.Log.Error($"Double player mech stats: {playerName}");
                    continue;
                }
                CurrentMatch.PlayerMechTime.Add(playerName, playerMechStat.Value.MechTime);
            }

            foreach(var alliance in _allianceStats) {
                if(CurrentMatch.AllianceStats.TryGetValue(alliance.Key, out var allianceStats)) {
                    allianceStats.CeruleumGenerated = alliance.Value.CeruleumGenerated;
                    allianceStats.CeruleumConsumed = alliance.Value.CeruleumConsumed;
                }
            }

            CurrentMatch.Supplies = [];
            foreach(var team in _midCounts) {
                CurrentMatch.Supplies.Add(team.Key, []);
                foreach(var supply in team.Value) {
                    CurrentMatch.Supplies[team.Key].Add(supply.Key, supply.Value);
                }
            }

            CurrentMatch.Mercs = [];
            foreach(var team in _mercCounts) {
                CurrentMatch.Mercs.Add(team.Key, team.Value);
            }
        } else {
            Plugin.Log.Warning("Incomplete match recording...discarding frame data.");
        }

        var playerTeam = CurrentMatch.LocalPlayerTeam;
        var enemyTeam = (RivalWingsTeamName)(((int)playerTeam! + 1) % 2);
        CurrentMatch.MatchWinner = results.Result == 0 ? playerTeam : results.Result == 1 ? enemyTeam : RivalWingsTeamName.Unknown;
        CurrentMatch.IsCompleted = true;
        CurrentMatch.TimelineId = _currentMatchTimeline?.Id;
        return true;
    }

    private void MechDeployDetour(IntPtr p1, IntPtr p2) {
        Plugin.Log.Debug("rw ICD update detour entered.");
        //Plugin.Functions.CreateByteDump(p2, 0x1000, "MECHDEPLOY");
        //Plugin.Functions.FindValue<ushort>(0, p2, 0x200, 0, true);
        //_mechDeployHook.Original(p1, p2);
    }

    private void LeaveDutyDetour(byte p1) {
        if(IsMatchInProgress() && _matchEnded && !_resultPayloadReceived && (!Plugin.Configuration.DisableMatchGuardsRW ?? true)) {
            Plugin.Log.Information("Preventing duty leave!");
            return;
        }
        _leaveDutyHook.Original(p1);
    }

    protected override void OnDutyCompleted(object? sender, ushort p1) {
        Plugin.Log.Debug("Duty has completed.");
        _matchEnded = true;
        //re-enable duty leave button after 20 seconds as a fallback
        Task.Delay(20000).ContinueWith(t => {
            try {
                _resultPayloadReceived = true;
                EnableLeaveDutyButton();
            } catch {
                //suppress
            }
        });
    }

    private void DutyMenuSetup(AddonEvent type, AddonArgs args) {
        if(!IsMatchInProgress() || (Plugin.Configuration.DisableMatchGuardsRW ?? false)) {
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
                    _leaveDutyButtonText ??= MemoryService.ReadString(buttonNode->ButtonTextNode->GetText());
                    buttonNode->ButtonTextNode->SetText("Waiting for scoreboard...");
                    var targetNode = buttonNode->AtkComponentBase.OwnerNode;
                    targetNode->AtkResNode.NodeFlags |= NodeFlags.EmitsEvents | NodeFlags.RespondToMouse | NodeFlags.HasCollision;
                    _mouseOverEvent = Plugin.AddonEventManager.AddEvent((nint)addon, (nint)targetNode, AddonEventType.MouseOver, TooltipHandler);
                    _mouseOutEvent = Plugin.AddonEventManager.AddEvent((nint)addon, (nint)targetNode, AddonEventType.MouseOut, TooltipHandler);
                    _leaveDutyButton = (IntPtr)buttonNode;
                    _leaveDutyButtonOwnerNode = (IntPtr)targetNode;
                    _addonId = addon->Id;
                }
            }
        }
    }

    private unsafe void TooltipHandler(AddonEventType type, IntPtr addon, IntPtr node) {
        var addonId = ((AtkUnitBase*)addon)->Id;
        switch(type) {
            case AddonEventType.MouseOver:
                AtkStage.Instance()->TooltipManager.ShowTooltip(addonId, (AtkResNode*)node, "Disabled by PvP Tracker until scoreboard payload is received by client. This can be disabled in plugin settings.");
                break;
            case AddonEventType.MouseOut:
                AtkStage.Instance()->TooltipManager.HideTooltip(addonId);
                break;
        }
    }

    private void DutyMenuClose(AddonEvent type, AddonArgs args) {
        if(!IsMatchInProgress()) {
            return;
        }
        Plugin.Log.Debug("Duty menu closed");
        ResetButton();
    }

    internal unsafe void EnableLeaveDutyButton() {
        try {
            if(_leaveDutyButton != IntPtr.Zero) {
                ((AtkComponentButton*)_leaveDutyButton)->AtkComponentBase.SetEnabledState(true);
                if(_leaveDutyButtonText != null) {
                    ((AtkComponentButton*)_leaveDutyButton)->ButtonTextNode->SetText(_leaveDutyButtonText);
                }
            }
            if(_mouseOverEvent != null) {
                Plugin.AddonEventManager.RemoveEvent(_mouseOverEvent);
            }
            if(_mouseOutEvent != null) {
                Plugin.AddonEventManager.RemoveEvent(_mouseOutEvent);
            }
            if(_leaveDutyButtonOwnerNode != IntPtr.Zero) {
                ((AtkComponentNode*)_leaveDutyButtonOwnerNode)->AtkResNode.NodeFlags &= ~(NodeFlags.HasCollision | NodeFlags.EmitsEvents | NodeFlags.RespondToMouse);
            }
            if(_addonId != 0) {
                AtkStage.Instance()->TooltipManager.HideTooltip(_addonId);
            }

        } catch(Exception e) {
            Plugin.Log2.Error(e, "Error in enabling leave duty button.");
        }
    }

    private unsafe void OnFrameworkUpdate(IFramework framework) {
        if(!IsMatchInProgress()) {
            return;
        }
        var director = (RivalWingsContentDirector*)(IntPtr)EventFramework.Instance()->GetInstanceContentDirector();
        if(director == null) {
            return;
        }
        if(CurrentMatch?.IsCompleted ?? true && (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51])) {
            return;
        }

        var now = DateTime.Now;

#if DEBUG
        //if(now - _lastUpdate > TimeSpan.FromSeconds(30)) {
        //    Plugin.Functions.CreateByteDump((nint)director, 0x3000, "RWICD");
        //}
#endif

        try {
            //structure health
            foreach(var team in _currentMatchTimeline!.StructureHealths!) {
                foreach(var structure in team.Value) {
                    var lastEvent = structure.Value.LastOrDefault();
                    int? currentValue = null;
                    switch(team.Key, structure.Key) {
                        case (RivalWingsTeamName.Falcons, RivalWingsStructure.Core):
                            currentValue = director->FalconCore.Integrity;
                            break;
                        case (RivalWingsTeamName.Falcons, RivalWingsStructure.Tower1):
                            currentValue = director->FalconTower1.Integrity;
                            break;
                        case (RivalWingsTeamName.Falcons, RivalWingsStructure.Tower2):
                            currentValue = director->FalconTower2.Integrity;
                            break;
                        case (RivalWingsTeamName.Ravens, RivalWingsStructure.Core):
                            currentValue = director->RavenCore.Integrity;
                            break;
                        case (RivalWingsTeamName.Ravens, RivalWingsStructure.Tower1):
                            currentValue = director->RavenTower1.Integrity;
                            break;
                        case (RivalWingsTeamName.Ravens, RivalWingsStructure.Tower2):
                            currentValue = director->RavenTower2.Integrity;
                            break;
                        default:
                            break;
                    }
                    if(currentValue != null && (lastEvent == null || lastEvent.Health != currentValue)) {
                        structure.Value.Add(new(now, (int)currentValue));
                    }
                }
            }

            if(!_matchEnded) {
                //mech times and counts
                _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Chaser] += director->FalconChaserCount * (now - _lastUpdate).TotalSeconds;
                _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Oppressor] += director->FalconOppressorCount * (now - _lastUpdate).TotalSeconds;
                _mechTime[RivalWingsTeamName.Falcons][RivalWingsMech.Justice] += director->FalconJusticeCount * (now - _lastUpdate).TotalSeconds;
                _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Chaser] += director->RavenChaserCount * (now - _lastUpdate).TotalSeconds;
                _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Oppressor] += director->RavenOppressorCount * (now - _lastUpdate).TotalSeconds;
                _mechTime[RivalWingsTeamName.Ravens][RivalWingsMech.Justice] += director->RavenJusticeCount * (now - _lastUpdate).TotalSeconds;

                foreach(var team in _currentMatchTimeline.MechCounts) {
                    foreach(var mech in team.Value) {
                        var lastEvent = mech.Value.LastOrDefault();
                        int? currentValue = null;
                        switch(team.Key, mech.Key) {
                            case (RivalWingsTeamName.Falcons, RivalWingsMech.Chaser):
                                currentValue = director->FalconChaserCount;
                                break;
                            case (RivalWingsTeamName.Falcons, RivalWingsMech.Oppressor):
                                currentValue = director->FalconOppressorCount;
                                break;
                            case (RivalWingsTeamName.Falcons, RivalWingsMech.Justice):
                                currentValue = director->FalconJusticeCount;
                                break;
                            case (RivalWingsTeamName.Ravens, RivalWingsMech.Chaser):
                                currentValue = director->RavenChaserCount;
                                break;
                            case (RivalWingsTeamName.Ravens, RivalWingsMech.Oppressor):
                                currentValue = director->RavenOppressorCount;
                                break;
                            case (RivalWingsTeamName.Ravens, RivalWingsMech.Justice):
                                currentValue = director->RavenJusticeCount;
                                break;
                            default:
                                break;
                        }
                        if(currentValue != null && (lastEvent == null || lastEvent.Count != currentValue)) {
                            mech.Value.Add(new(now, (int)currentValue));
                        }
                    }
                }

                //merc win
                if(_lastMercControl == RivalWingsContentDirector.Team.None && director->MercControl != RivalWingsContentDirector.Team.None) {
                    var lastMercClaim = _currentMatchTimeline.MercClaims.LastOrDefault();
                    if(lastMercClaim != null && (now - lastMercClaim.Timestamp).TotalSeconds <= 30) {
                        Plugin.Log2.Warning("Double merc claim event detected.");
                    } else {
                        Plugin.Log2.Debug($"Merc Claim Event: {(RivalWingsTeamName)director->MercControl}");
                        _mercCounts[(RivalWingsTeamName)director->MercControl]++;
                        _currentMatchTimeline.MercClaims.Add(new(now, (RivalWingsTeamName)director->MercControl));
                    }
                }

                //mid win
                if(_lastFalconMidScore != 100 && director->FalconMidScore == 100) {
                    _midCounts[RivalWingsTeamName.Falcons][(RivalWingsSupplies)director->MidType]++;
                    _currentMatchTimeline.MidClaims!.Add(new(now, RivalWingsTeamName.Falcons, (RivalWingsSupplies)director->MidType));
                    Plugin.Log2.Debug($"Mid Claim Event: {RivalWingsTeamName.Falcons} {(RivalWingsSupplies)director->MidType}");
                }

                if(_lastRavenMidScore != 100 && director->RavenMidScore == 100) {
                    _midCounts[RivalWingsTeamName.Ravens][(RivalWingsSupplies)director->MidType]++;
                    _currentMatchTimeline.MidClaims!.Add(new(now, RivalWingsTeamName.Ravens, (RivalWingsSupplies)director->MidType));
                    Plugin.Log2.Debug($"Mid Claim Event: {RivalWingsTeamName.Ravens} {(RivalWingsSupplies)director->MidType}");
                }

                //alliance ceruleum stats and soaring
                for(int i = 0; i < director->AllianceSpan.Length; i++) {
                    var alliance = director->AllianceSpan[i];
                    var allianceStats = _allianceStats[i];
                    var ceruleumChange = alliance.Ceruleum - _allianceStats[i].CeruleumLast;
                    //add input bounds for sanity check in case of missing alliance
                    if(ceruleumChange != 0 && alliance.Ceruleum <= 100 && alliance.Ceruleum >= 0) {
                        if(allianceStats.CeruleumLast > alliance.Ceruleum) {
                            allianceStats.CeruleumConsumed += -ceruleumChange;
                            if(ceruleumChange % 5 != 0) {
                                Plugin.Log.Debug($"ceruleum consumed not factor of 5! {i}: {ceruleumChange}");
                            }
                        } else if(allianceStats.CeruleumLast < alliance.Ceruleum) {
                            allianceStats.CeruleumGenerated += ceruleumChange;
                        }
                        allianceStats.CeruleumLast = alliance.Ceruleum;
                        _allianceStats[i] = allianceStats;
                    }

                    var allianceStackList = _currentMatchTimeline.AllianceStacks[i];
                    var lastEvent = allianceStackList.LastOrDefault();
                    var currentSoaring = alliance.SoaringStacks;
                    if(lastEvent == null || lastEvent.Count != currentSoaring) {
                        allianceStackList.Add(new(now, currentSoaring));
                    }
                }

                //associate player Ids with aliases
                foreach(IPlayerCharacter pc in Plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player).Cast<IPlayerCharacter>()) {
                    if(!_objIdToPlayer.ContainsKey(pc.GameObjectId)) {
                        try {
                            var worldName = Plugin.DataManager.GetExcelSheet<World>()?.GetRow(pc.HomeWorld.RowId).Name;
                            var player = (PlayerAlias)$"{pc.Name} {worldName}";
                            _objIdToPlayer.Add(pc.GameObjectId, player);
                        } catch {
                            //sometime encounter players with no object id...
                        }
                    }
                }

                //set friendly mech stats
                for(int i = 0; i < director->FriendlyMechSpan.Length; i++) {
                    var friendlyMechNative = director->FriendlyMechSpan[i];
                    //var mechStats = _playerMechStats[i];
                    //add input bounds for sanity check in case of missing alliance
                    if(friendlyMechNative.Type != RivalWingsContentDirector.MechType.None) {
                        var mech = (RivalWingsMech)friendlyMechNative.Type;
                        if(!_playerMechStats.TryGetValue(friendlyMechNative.PlayerObjectId, out (RivalWingsMech? LastMech, Dictionary<RivalWingsMech, int> MechsDeployed, Dictionary<RivalWingsMech, double> MechTime) mechStats)) {
                            mechStats = new() {
                                MechTime = []
                            };
                            mechStats.MechTime.Add(RivalWingsMech.Chaser, 0);
                            mechStats.MechTime.Add(RivalWingsMech.Oppressor, 0);
                            mechStats.MechTime.Add(RivalWingsMech.Justice, 0);
                            _playerMechStats.Add(friendlyMechNative.PlayerObjectId, mechStats);
                            //Plugin.Log.Debug($"adding mech stats for: {friendlyMechNative.PlayerObjectId}");
                        }
                        mechStats.MechTime[mech] += (now - _lastUpdate).TotalSeconds;
                        //not sure if this needed
                        _playerMechStats[friendlyMechNative.PlayerObjectId] = mechStats;
                    }
                }
            }
        } catch(Exception e) {
#if DEBUG
            Plugin.Log2.Error(e, "Exception in framework update");
#endif
        }
        try {
            _lastUpdate = now;
            _lastMercControl = director->MercControl;
            _lastFalconMidScore = director->FalconMidScore;
            _lastRavenMidScore = director->RavenMidScore;
        } catch {
            //suppress
        }

    }

    private static Vector2 WorldPosToMapCoords(Vector3 pos) {
        var xInt = (int)(MathF.Round(pos.X, 3, MidpointRounding.AwayFromZero) * 1000);
        var yInt = (int)(MathF.Round(pos.Z, 3, MidpointRounding.AwayFromZero) * 1000);
        return new Vector2((int)(xInt * 0.001f * 1000f), (int)(yInt * 0.001f * 1000f));
    }

    private void Reset() {
        ResetButton();
        _matchEnded = false;
        _resultPayloadReceived = false;
        _objIdToPlayer = [];
        _mechTime = [];
        _midCounts = [];
        _mercCounts = [];
        _allianceStats = [];
        _playerMechStats = [];
        _lastMercControl = null;
        _lastUpdate = DateTime.MinValue;
        _lastFalconMidScore = 0;
        _lastRavenMidScore = 0;
        _lastPrint = DateTime.MinValue;

        RivalWingsTeamName[] allTeams = { RivalWingsTeamName.Falcons, RivalWingsTeamName.Ravens };
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
    }

    private void ResetButton() {
        _leaveDutyButton = IntPtr.Zero;
        _leaveDutyButtonOwnerNode = IntPtr.Zero;
        _leaveDutyButtonText = null;
        _addonId = 0;
        _mouseOverEvent = null;
        _mouseOutEvent = null;
    }
}
