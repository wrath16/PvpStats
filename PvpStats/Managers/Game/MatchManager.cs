using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Network;
using Dalamud.Hooking;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Component.GUI;
//using Lumina.Excel.GeneratedSheets;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Helpers;
using PvpStats.Types.ClientStruct;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PvpStats.Managers.Game;
internal class MatchManager : IDisposable {

    private Plugin _plugin;
    private CrystallineConflictMatch? _currentMatch;
    private DateTime _lastHeaderUpdateTime;
    private CrystallineConflictTeamName _lastMoved;

    bool _isOvertimePrev = false;
    string _timerMinsPrev = "";
    string _timerSecondsPrev = "";
    string _leftTeamPrev = "";
    string _rightTeamPrev = "";
    string _leftTeamProgressPrev = "";
    string _rightTeamProgressPrev = "";

    internal Dictionary<ushort, uint> _opCodeCount = new();
    internal int _opcodeMatchCount = 0;
    private DateTime _lastSortTime;
    private bool qPopped = false;
    private bool introStarted = false;

    //p1 = data ref?
    //p2 = targetId
    //p3 = dataPtr no 0x10 offset
    private delegate void CCMatchEndDelegate(IntPtr p1, uint p2, IntPtr p3);
    [Signature("48 83 EC ?? 4D 8B C8 48 C7 44 24 20 ?? ?? ?? ?? 41 B8 ?? ?? ?? ?? E8 E5 0C 00 00", DetourName = nameof(CCMatchEndDetour))]
    private readonly Hook<CCMatchEndDelegate> _ccMatchEndHook;


    private delegate IntPtr CCDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    [Signature("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 48 8B F1 E8 84 50 FF FF", DetourName = nameof(CCDirectorCtorDetour))]
    private readonly Hook<CCDirectorCtorDelegate> _ccDirectorCtorHook;

    private delegate void CCDirectorVf6Delegate(IntPtr p1, IntPtr p2, byte p3, IntPtr p4, byte p5);
    [Signature("48 89 5C 24 10 48 89 6C 24 18 56 57 41 54 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 C0 07 D2 00", DetourName = nameof(CCDirectorVf6Detour))]
    private readonly Hook<CCDirectorVf6Delegate> _ccDirectorVf6Hook;

    private delegate void CCDirectorVf308Delegate(IntPtr p1);
    [Signature("48 89 5C 24 08 57 48 83 EC ?? E8 61 58 01 00", DetourName = nameof(CCDirectorVf308Detour))]
    private readonly Hook<CCDirectorVf308Delegate> _ccDirectorVf308Hook;

    //p10 = 16 byte array
    //this is probably le bad!
    private delegate void CCDirectorVf357Delegate(IntPtr p1, IntPtr p2, uint p3, IntPtr p4, IntPtr p5, uint p6, IntPtr p7, IntPtr p8, IntPtr p9, byte[] p10, uint p11, IntPtr p12, uint p13);
    [Signature("48 89 5C 24 08 57 48 83 EC ?? E8 61 58 01 00", DetourName = nameof(CCDirectorVf357Detour))]
    private readonly Hook<CCDirectorVf357Delegate> _ccDirectorVf357Hook;

    private delegate void CCDirectorVf286Delegate(IntPtr p1, IntPtr p2, IntPtr p3);
    [Signature("48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 41 0F B6 F8 0F B6 F2 48 8B E9 80 FA ?? 74 54", DetourName = nameof(CCDirectorVf286Detour))]
    private readonly Hook<CCDirectorVf286Delegate> _ccDirectorVf286Hook;

    private delegate void CCDirectorVf305Delegate(IntPtr p1);
    [Signature("40 53 48 83 EC 20 48 8B 05 DB D9 D3 00", DetourName = nameof(CCDirectorVf305Detour))]
    private readonly Hook<CCDirectorVf305Delegate> _ccDirectorVf305Hook;

    private delegate byte CCDirectorVf271Delegate(IntPtr p1);
    [Signature("48 83 EC ?? F6 81 ?? ?? ?? ?? ?? 74 10 E8 6E AD 3C FF", DetourName = nameof(CCDirectorVf271Detour))]
    private readonly Hook<CCDirectorVf271Delegate> _ccDirectorVf271Hook;

    private delegate byte CCDirectorVf353Delegate(IntPtr p1);
    [Signature("48 83 EC ?? F6 81 ?? ?? ?? ?? ?? 74 10 E8 3E AD 3C FF", DetourName = nameof(CCDirectorVf353Detour))]
    private readonly Hook<CCDirectorVf353Delegate> _ccDirectorVf353Hook;

    private delegate byte CCDirectorVf371Delegate(IntPtr p1);
    [Signature("40 53 48 83 EC ?? 48 8B D9 E8 12 AD 3C FF", DetourName = nameof(CCDirectorVf371Detour))]
    private readonly Hook<CCDirectorVf371Delegate> _ccDirectorVf371Hook;

    private delegate void CCDirectorVf245Delegate(IntPtr p1, IntPtr p2, ushort p3, ushort p4, IntPtr p5);
    [Signature("48 89 5C 24 10 48 89 6C 24 18 48 89 74 24 20 57 48 83 EC ?? 48 8B 02", DetourName = nameof(CCDirectorVf245Detour))]
    private readonly Hook<CCDirectorVf245Delegate> _ccDirectorVf245Hook;

    private delegate void CCDirectorVf378Delegate(IntPtr p1, byte p2);
    [Signature("40 53 48 83 EC ?? 88 15 A8 41 D9 00", DetourName = nameof(CCDirectorVf378Detour))]
    private readonly Hook<CCDirectorVf378Delegate> _ccDirectorVf378Hook;

    private delegate byte CCDirectorVf304Delegate(IntPtr p1);
    [Signature("48 83 EC ?? 44 0F B6 81 ?? ?? ?? ?? BA ?? ?? ?? ??", DetourName = nameof(CCDirectorVf304Detour))]
    private readonly Hook<CCDirectorVf304Delegate> _ccDirectorVf304Hook;

    private delegate IntPtr DDConstructorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    [Signature("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 48 8B F9 E8 84 86 FE FF", DetourName = nameof(DDDirectorDetour))]
    private readonly Hook<DDConstructorDelegate> _ddDirectorHook;


    public MatchManager(Plugin plugin) {
        _plugin = plugin;

        _plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        _plugin.DutyState.DutyCompleted += OnDutyCompleted;
        _plugin.DutyState.DutyStarted += OnDutyStarted;
        _plugin.GameNetwork.NetworkMessage += OnNetworkMessage;

        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSIntroduction", OnPvPIntro);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "PvPMKSHeader", OnPvPHeaderUpdate);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSHeader", OnPvPHeaderUpdate);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "PvPMKSHeaderSpec", OnPvPHeaderUpdate);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSHeaderSpec", OnPvPHeaderUpdate);
        //_plugin.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "MKSRecord", OnPvPResults);

        _plugin.InteropProvider.InitializeFromAttributes(this);
        _plugin.Log.Debug($"match end address: 0x{_ccMatchEndHook!.Address.ToString("X2")}");
        _ccMatchEndHook.Enable();
        _ccDirectorCtorHook.Enable();
        _ccDirectorVf6Hook.Enable();
        _ccDirectorVf308Hook.Enable();
        //_ccDirectorVf357Hook.Enable();
        _ccDirectorVf286Hook.Enable();
        _ccDirectorVf305Hook.Enable();
        _ccDirectorVf271Hook.Enable();
        _ccDirectorVf353Hook.Enable();
        //_ccDirectorVf371Hook.Enable();
        _ccDirectorVf245Hook.Enable();
        _ccDirectorVf378Hook.Enable();
        _ccDirectorVf304Hook.Enable();
        //_ddDirectorHook.Enable();
    }

    public void Dispose() {

        //_plugin.Framework.Update -= OnFrameworkUpdate;
        //_plugin.ChatGui.ChatMessage -= OnChatMessage;
        _plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        _plugin.DutyState.DutyStarted -= OnDutyStarted;
        _plugin.GameNetwork.NetworkMessage -= OnNetworkMessage;

        _plugin.AddonLifecycle.UnregisterListener(OnPvPIntro);
        _plugin.AddonLifecycle.UnregisterListener(OnPvPHeaderUpdate);
        //_plugin.AddonLifecycle.UnregisterListener(OnPvPResults);

        _ccMatchEndHook.Dispose();
        _ccDirectorCtorHook.Dispose();
        _ccDirectorVf6Hook.Dispose();
        _ccDirectorVf308Hook.Dispose();
        //_ccDirectorVf357Hook.Dispose();
        _ccDirectorVf286Hook.Dispose();
        _ccDirectorVf305Hook.Dispose();
        _ccDirectorVf271Hook.Dispose();
        _ccDirectorVf353Hook.Dispose();
        //_ccDirectorVf371Hook.Dispose();
        _ccDirectorVf245Hook.Dispose();
        _ccDirectorVf378Hook.Dispose();
        _ccDirectorVf304Hook.Dispose();
        //_ddDirectorHook.Dispose();
    }

    private unsafe IntPtr DDDirectorDetour(IntPtr p1, IntPtr p2, IntPtr p3) {
        _plugin.Log.Debug("DD Director .ctor occurred!");
        return _ddDirectorHook.Original(p1, p2, p3);
    }

    private unsafe IntPtr CCDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3) {
        _plugin.Log.Debug("CC Director .ctor occurred!");
        //_plugin.Functions.PrintAllChars(p1, 0x2000);
        //_plugin.Functions.PrintAllChars(p2, 0x2000);
        //_plugin.Functions.PrintAllChars(p3, 0x2000);
        //_plugin.Functions.FindValue<string>("", p1, 0x500, 0, true);
        //_plugin.Functions.FindValue<string>("", p2, 0x500, 0, true);
        //_plugin.Functions.FindValue<string>("", p3, 0x500, 0, true);
        return _ccDirectorCtorHook.Original(p1, p2, p3);
    }

    //this triggers on leave instance
    private unsafe void CCDirectorVf6Detour(IntPtr p1, IntPtr p2, byte p3, IntPtr p4, byte p5) {
        _plugin.Log.Debug("CC Director vf6 occurred!");
        _ccDirectorVf6Hook.Original(p1, p2, p3, p4, p5);
    }

    //this gets triggered twice at beginning
    private unsafe void CCDirectorVf308Detour(IntPtr p1) {
        _plugin.Log.Debug("CC Director vf308 occurred!");
        _ccDirectorVf308Hook.Original(p1);
    }

    private unsafe void CCDirectorVf357Detour(IntPtr p1, IntPtr p2, uint p3, IntPtr p4, IntPtr p5, uint p6, IntPtr p7, IntPtr p8, IntPtr p9, byte[] p10, uint p11, IntPtr p12, uint p13) {
        _plugin.Log.Debug("CC Director vf357 occurred!");
        _ccDirectorVf357Hook.Original(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);
    }

    //this gets triggered regularly
    private unsafe void CCDirectorVf286Detour(IntPtr p1, IntPtr p2, IntPtr p3) {
        _plugin.Log.Debug("CC Director vf286 occurred!");
        _ccDirectorVf286Hook.Original(p1, p2, p3);
    }

    //this gets triggered repeatedly
    private unsafe void CCDirectorVf305Detour(IntPtr p1) {
        //_plugin.Log.Debug("CC Director vf305 occurred!");
        _ccDirectorVf305Hook.Original(p1);
    }

    //unknown trigger
    private unsafe byte CCDirectorVf271Detour(IntPtr p1) {
        _plugin.Log.Debug("CC Director vf271 occurred!");
        return _ccDirectorVf271Hook.Original(p1);
    }

    //unknown trigger
    private unsafe byte CCDirectorVf353Detour(IntPtr p1) {
        _plugin.Log.Debug("CC Director vf353 occurred!");
        return _ccDirectorVf353Hook.Original(p1);
    }

    //unknown trigger - something about being dead lol
    private unsafe byte CCDirectorVf371Detour(IntPtr p1) {
        _plugin.Log.Debug("CC Director vf371 occurred!");
        return _ccDirectorVf371Hook.Original(p1);
    }

    //gets triggered regularly
    private unsafe void CCDirectorVf245Detour(IntPtr p1, IntPtr p2, ushort p3, ushort p4, IntPtr p5) {
        _plugin.Log.Debug("CC Director vf245 occurred!");
        _ccDirectorVf245Hook.Original(p1, p2, p3, p4, p5);
    }

    //called once at beginning
    private unsafe void CCDirectorVf378Detour(IntPtr p1, byte p2) {
        _plugin.Log.Debug("CC Director vf378 occurred!");
        //_plugin.Functions.FindValue<string>("", p1, 0x500, 0, true);
        _ccDirectorVf378Hook.Original(p1, p2);
    }

    //called once at beginning
    private unsafe byte CCDirectorVf304Detour(IntPtr p1) {
        _plugin.Log.Debug("CC Director vf304 occurred!");
        //_plugin.Functions.FindValue<string>("", p1, 0x500, 0, true);
        return _ccDirectorVf304Hook.Original(p1);
    }

    private unsafe void CCMatchEndDetour(IntPtr p1, uint p2, IntPtr p3) {
        _plugin.Log.Information("Match end detour occurred.");

        //_plugin.Functions.FindValue<int>(0, p3 + 0x10, 0x310, 0, true);
        //_plugin.Functions.FindValue<short>(0, p3 + 0x10, 0x310, 0, true);
        //_plugin.Functions.FindValue<byte>(0, p3 + 0x10, 0x310, 0, true);

        var resultsPacket = *(CrystallineConflictResultsPacket*)(p3 + 0x10);

        //string result = "";
        //switch (resultsPacket.Result) {
        //    case 1:
        //        result = "victory";
        //        break;
        //    case 2:
        //        result = "defeat";
        //        break;
        //    default:
        //        result = "unknown";
        //        break;
        //}
        //_plugin.Log.Debug($"RESULT: {result}");
        //_plugin.Log.Debug($"MATCH DURATION (s): {resultsPacket.MatchLength}");
        //_plugin.Log.Debug($"ASTRA PROGRESS: {resultsPacket.AstraProgress} UMBRA PROGRESS: {resultsPacket.UmbraProgress}");
        //_plugin.Log.Debug(string.Format("{0,-25} {1,-15} {2,-6} {3,-5} {4,-15} {5,-8} {6,-8} {7,-8} {8,-15} {9,-15} {10,-15} {11,-15}", "NAME", "WORLD", "TEAM", "JOB", "TIER", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE TAKEN", "HP RESTORED", "TIME ON CRYSTAL"));

        //for (int i = 0; i < 10; i++) {
        //    //var player = (CrystallineConflictResultsPacket.CrystallineConflictPlayer*)clientStruct->Player[i];
        //    var player = resultsPacket.PlayerSpan[i];

        //    //missing player
        //    if (player.ClassJobId == 0) {
        //        continue;
        //    }
        //    _plugin.Log.Debug(string.Format("{0,-25} {1,-15} {2,-6} {3,-5} {4,-15} {5,-8} {6,-8} {7,-8} {8,-15} {9,-15} {10,-15} {11,-15}",
        //        AtkNodeHelper.ReadString(player.PlayerName, 32), _plugin.DataManager.GetExcelSheet<World>().GetRow(player.WorldId).Name, player.Team == 0 ? "ASTRA" : "UMBRA",
        //        _plugin.DataManager.GetExcelSheet<ClassJob>().GetRow(player.ClassJobId).Abbreviation,
        //        _plugin.DataManager.GetExcelSheet<ColosseumMatchRank>().GetRow(player.ColosseumMatchRankId).Unknown0, player.Kills, player.Deaths, player.Assists, player.DamageDealt, player.DamageTaken, player.HPRestored, player.TimeOnCrystal));
        //}

        _plugin.DataQueue.QueueDataOperation(() => {
            ProcessMatchResults(resultsPacket);
        });
        //_plugin.Functions.PrintAllChars(p3 + 0x10, 0x310);
        //_plugin.Functions.CreateByteDump(p3 + 0x10, 0x310, "MatchResults");
        _ccMatchEndHook.Original(p1, p2, p3);

    }

    private unsafe void OnNetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
        //if (!IsMatchInProgress()) {
        //    return;
        //}
        if (direction != NetworkMessageDirection.ZoneDown) {
            return;
        }

        if (_opCodeCount.ContainsKey(opCode)) {
            _opCodeCount[opCode]++;
        } else {
            _opCodeCount.Add(opCode, 1);
        }

        if (opCode != 845 && opCode != 813 && opCode != 649 && opCode != 717 && opCode != 920 && opCode != 898 && opCode != 316 && opCode != 769 && opCode != 810 
            && opCode != 507 && opCode != 973 && opCode != 234 && opCode != 702 && opCode != 421 && opCode != 244 && opCode != 116 && opCode != 297 && opCode != 493
            && opCode != 857 && opCode != 444 && opCode != 550 && opCode != 658 && opCode != 636 && opCode != 132 && opCode != 230 && opCode != 660
            && opCode != 565 && opCode != 258 && opCode != 390 && opCode != 221 && opCode != 167 && opCode != 849) {
            _plugin.Log.Verbose($"OPCODE: {opCode} DATAPTR: 0x{dataPtr.ToString("X2")} SOURCEACTORID: {sourceActorId} TARGETACTORID: {targetActorId}");
            //_plugin.Functions.PrintAllStrings(dataPtr, 0x500);

            if(qPopped) {
                _plugin.Functions.CreateByteDump(dataPtr, 0x1000, opCode.ToString());
                _plugin.Functions.PrintAllPlayerObjects();
                _plugin.Functions.PrintAllChars(dataPtr, 0x2000);
                foreach(var player in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
                    for (int i = 0; i < sizeof(uint); i++) {
                        _plugin.Functions.FindValue<uint>(player.ObjectId, dataPtr, 0x2000, i);
                    }
                }
            }

            //IntPtr myName = dataPtr + 0x4C;
            //if (AtkNodeHelper.ReadString((byte*)myName).Equals("Sarah Montcroix", StringComparison.OrdinalIgnoreCase)) {
            //    _plugin.Log.Verbose("name found a 0x4C!");
            //}
        }

        if(opCode == 556) {
            _plugin.Log.Debug("q popped");
            qPopped = true;
        }



        ////643 has promise...
        //if (opCode == 945 || opCode == 560) {
        //    _plugin.Functions.FindValue<string>("", dataPtr, 0x500, 0, true);
        //}

        if (DateTime.Now - _lastSortTime > TimeSpan.FromSeconds(60)) {
            _lastSortTime = DateTime.Now;
            _opCodeCount = _opCodeCount.OrderBy(x => x.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }


        //start duty
        //if (opCode == 593) {
        //    _plugin.Log.Debug("duty...started?");
        //}

        //end duty
        //if (opCode == 939) {
        //    //_plugin.Functions.FindValue<int>(0, dataPtr + 0x10, 0x310, 0, true);
        //    //_plugin.Functions.FindValue<short>(0, dataPtr + 0x10, 0x310, 0, true);
        //    //_plugin.Functions.FindValue<long>(0, dataPtr, 0x310, 0, true);
        //    //_plugin.Functions.FindValue<byte>(0, dataPtr + 0x10, 0x310, 0, true);
        //    //_plugin.Functions.FindValue<float>(0, dataPtr, 0x300, 0, true);
        //    //_plugin.Functions.FindValue<double>(0, dataPtr, 0x300, 0, true);
        //    //_plugin.Functions.FindValue<string>("", dataPtr, 0x310, 0, true);
        //    //_plugin.Functions.ReadBytes(dataPtr, typeof(byte), 0x2000);
        //    //_plugin.Functions.ReadBytes(dataPtr, typeof(short), 0x2000);
        //    //_plugin.Functions.ReadBytes(dataPtr, typeof(int), 0x2000);

        //    var clientStruct = (CrystallineConflictResultsPacket*)(dataPtr + 0x10);
        //    string result = "";
        //    switch(clientStruct->Result) {
        //        case 1:
        //            result = "victory";
        //            break;
        //        case 2:
        //            result = "defeat";
        //            break;
        //        default:
        //            result = "unknown";
        //            break;
        //    }
        //    _plugin.Log.Debug($"RESULT: {result}");
        //    _plugin.Log.Debug($"MATCH DURATION (s): {clientStruct->MatchLength}");
        //    _plugin.Log.Debug($"ASTRA PROGRESS: {clientStruct->AstraProgress} UMBRA PROGRESS: {clientStruct->UmbraProgress}");
        //    _plugin.Log.Debug(string.Format("{0,-25} {1,-15} {2,-6} {3,-5} {4,-15} {5,-8} {6,-8} {7,-8} {8,-15} {9,-15} {10,-15} {11,-15}", "NAME", "WORLD", "TEAM", "JOB", "TIER", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE TAKEN", "HP RESTORED", "TIME ON CRYSTAL"));

        //    for (int i = 0; i < 10; i++) {
        //        //var player = (CrystallineConflictResultsPacket.CrystallineConflictPlayer*)clientStruct->Player[i];
        //        var player = clientStruct->PlayerSpan[i];

        //        //missing player
        //        if(player.ClassJobId == 0) {
        //            continue;
        //        }

        //        //_plugin.Log.Debug($"{AtkNodeHelper.ReadString(player.PlayerName, 32)}");
        //        //_plugin.Log.Debug($"WORLD: {player.WorldId}");
        //        //_plugin.Log.Debug($"TEAM: {player.Team}");
        //        //_plugin.Log.Debug($"JOB: {player.ClassJobId}");
        //        //_plugin.Log.Debug($"KILLS: {player.Kills}");
        //        //_plugin.Log.Debug($"DEATHS: {player.Deaths}");
        //        //_plugin.Log.Debug($"ASSISTS: {player.Assists}");
        //        //_plugin.Log.Debug($"DAMAGE DEALT: {player.DamageDealt}");
        //        //_plugin.Log.Debug($"DAMAGE TAKEN: {player.DamageTaken}");
        //        //_plugin.Log.Debug($"HP RESTORED: {player.HPRestored}");
        //        //_plugin.Log.Debug($"TIME ON CRYSTAL: {player.TimeOnCrystal}");

        //        _plugin.Log.Debug(string.Format("{0,-25} {1,-15} {2,-6} {3,-5} {4,-15} {5,-8} {6,-8} {7,-8} {8,-15} {9,-15} {10,-15} {11,-15}",
        //            AtkNodeHelper.ReadString(player.PlayerName, 32), _plugin.DataManager.GetExcelSheet<World>().GetRow(player.WorldId).Name, player.Team == 0 ? "ASTRA" : "UMBRA",
        //            _plugin.DataManager.GetExcelSheet<ClassJob>().GetRow(player.ClassJobId).Abbreviation,
        //            _plugin.DataManager.GetExcelSheet<ColosseumMatchRank>().GetRow(player.ColosseumMatchRankId).Unknown0, player.Kills, player.Deaths, player.Assists, player.DamageDealt, player.DamageTaken, player.HPRestored, player.TimeOnCrystal));



        //        //_plugin.Log.Debug($"PLAYER: {AtkNodeHelper.ReadString(player->PlayerName, 32)} JOB:{_plugin.DataManager.GetExcelSheet<ClassJob>().GetRow(player->ClassJobId).Abbreviation} " +
        //        //    $"TEAM: {(player->Team == 0 ? "ASTRA" : "UMBRA")}");
        //    }
        //}
    }

    private void OnTerritoryChanged(ushort territoryId) {
        var dutyId = _plugin.GameState.GetCurrentDutyId();
        //var duty = _plugin.DataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow(dutyId);
        _plugin.Log.Debug($"Territory changed: {territoryId}, Current duty: {dutyId}");
        if (MatchHelper.IsCrystallineConflictTerritory(territoryId)) {
            _plugin.DataQueue.QueueDataOperation(() => {
                //sometimes client state is unavailable at this time
                //start or pickup match!
                _currentMatch = new() {
                    DutyId = dutyId,
                    TerritoryId = territoryId,
                    Arena = MatchHelper.CrystallineConflictMapLookup[territoryId],
                    MatchType = MatchHelper.GetMatchType(dutyId),
                };
                _plugin.Storage.AddCCMatch(_currentMatch);
            });
        } else {
            if (IsMatchInProgress()) {
                _plugin.DataQueue.QueueDataOperation(() => {
                    //_plugin.Log.Debug("Opcodes:");
                    //foreach (var opcode in _opCodeCount.OrderByDescending(x => x.Value)) {
                    //    _plugin.Log.Debug($"opcode {opcode.Key}: {opcode.Value}");
                    //}
                    //_opCodeCount = new();
                    _opCodeCount = _opCodeCount.OrderBy(x => x.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    _opcodeMatchCount++;
                    _currentMatch = null;
                    _plugin.WindowManager.Refresh();
                });
            }
        }
    }

    private void OnDutyStarted(object? sender, ushort param1) {
        if (!IsMatchInProgress()) {
            return;
        }
        var currentTime = DateTime.Now;
        _plugin.DataQueue.QueueDataOperation(() => {
            _plugin.Log.Debug("Match has started.");
            _currentMatch!.MatchStartTime = currentTime;

            if (_currentMatch.NeedsPlayerNameValidation) {
                _currentMatch.NeedsPlayerNameValidation = !ValidatePlayerAliases() ?? true;
            }
            _plugin.Storage.UpdateCCMatch(_currentMatch);
        });
    }

    private void OnDutyCompleted(object? sender, ushort param1) {
        if (!IsMatchInProgress()) {
            return;
        }
        _plugin.Log.Debug("Match has ended.");
        var currentTime = DateTime.Now;
        //var currentMatchTemp = _currentMatch;
        //add delay to get last of header updates
        //this could cause issues with players instaleaving
        Task.Delay(100).ContinueWith(t => {
            _plugin.DataQueue.QueueDataOperation(() => {

                _currentMatch!.MatchEndTime = currentTime;
                _currentMatch!.IsCompleted = true;

                //set winner todo: check for draws!
                if (_currentMatch.Teams.ElementAt(0).Value.Progress > _currentMatch.Teams.ElementAt(1).Value.Progress) {
                    _currentMatch.MatchWinner = _currentMatch.Teams.ElementAt(0).Key;
                } else if (_currentMatch.Teams.ElementAt(0).Value.Progress < _currentMatch.Teams.ElementAt(1).Value.Progress) {
                    _currentMatch.MatchWinner = _currentMatch.Teams.ElementAt(1).Key;
                } else {
                    //overtime winner at same prog
                    _plugin.Log.Debug("Overtime winner is advantaged team.");
                    _currentMatch.MatchWinner = _currentMatch.OvertimeAdvantage;
                }

                var winningTeam = _currentMatch.Teams[(CrystallineConflictTeamName)_currentMatch.MatchWinner];
                //correct 99.9% on non-overtime wins
                _plugin.Log.Debug($"winner prog: {winningTeam.Progress} match seconds: {_currentMatch.MatchDuration.Value.TotalSeconds} isovertime : {_currentMatch.IsOvertime}");
                _plugin.Log.Debug($"{winningTeam.Progress > 99f} {winningTeam.Progress < 100f} {_currentMatch.MatchDuration.Value.TotalSeconds < 5 * 60} {!_currentMatch.IsOvertime}");
                if (winningTeam.Progress > 99f && winningTeam.Progress < 100f && _currentMatch.MatchDuration.Value.TotalSeconds < 5 * 60 && !_currentMatch.IsOvertime) {
                    _plugin.Log.Debug("Correcting 99.9% to 100%...");
                    winningTeam.Progress = 100f;
                }

                _plugin.Storage.UpdateCCMatch(_currentMatch);
            });
        });
    }

    //build team data
    private unsafe void OnPvPIntro(AddonEvent type, AddonArgs args) {
        if (!IsMatchInProgress()) {
            _plugin.Log.Warning("no match in progress on pvp intro!");
            return;
        }
        qPopped = false;
        _plugin.Log.Debug("Pvp intro post setup!");
        var addon = (AtkUnitBase*)args.Addon;
        CrystallineConflictTeam team = new();


        if (_plugin.ClientState.ClientLanguage != ClientLanguage.English) {
            AtkNodeHelper.PrintAtkValues(addon);
            //AtkNodeHelper.PrintTextNodes(addon->GetNodeById(1), true, false);
        }

        //team name
        string teamName = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[4]);
        string translatedTeamName = _plugin.Localization.TranslateDataTableEntry<Addon>(teamName, "Text", ClientLanguage.English);
        team.TeamName = MatchHelper.GetTeamName(translatedTeamName);

        _plugin.Log.Debug(teamName);
        for (int i = 0; i < 5; i++) {
            int offset = i * 16 + 6;
            uint[] rankIdChain = new uint[] { 1, (uint)(13 + i), 2, 9 };
            if (offset >= addon->AtkValuesCount) {
                break;
            }
            //TODO account for abbreviated name settings...
            string player = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset]);
            string world = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 6]);
            string job = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 5]);
            //JP uses English names...
            string translatedJob = _plugin.Localization.TranslateDataTableEntry<ClassJob>(job, "Name", ClientLanguage.English, 
                _plugin.ClientState.ClientLanguage == ClientLanguage.Japanese ? ClientLanguage.English : _plugin.ClientState.ClientLanguage);
            string rank = "";
            string? translatedRank = null;

            //have to read rank from nodes -_-
            var rankNode = AtkNodeHelper.GetNodeByIDChain(addon, rankIdChain);
            if (rankNode == null || rankNode->Type != NodeType.Text || rankNode->GetAsAtkTextNode()->NodeText.ToString().IsNullOrEmpty()) {
                rankIdChain[3] = 10; //non-crystal
                rankNode = AtkNodeHelper.GetNodeByIDChain(addon, rankIdChain);
            }
            if (rankNode != null && rankNode->Type == NodeType.Text) {
                rank = rankNode->GetAsAtkTextNode()->NodeText.ToString();
                if(!rank.IsNullOrEmpty()) {
                    //set ranked as fallback
                    //_currentMatch!.MatchType = CrystallineConflictMatchType.Ranked;

                    //don't need to translate for Japanese
                    if (_plugin.ClientState.ClientLanguage != ClientLanguage.Japanese) {
                        translatedRank = _plugin.Localization.TranslateRankString(rank, ClientLanguage.English);
                    } else {
                        translatedRank = rank;
                    }
                }
            }

            _plugin.Log.Debug(string.Format("player: {0,-25} {1,-15} job: {2,-15} rank: {3,-10}", player, world, job, rank));

            //abbreviated names
            if (player.Contains(".")) {
                _currentMatch!.NeedsPlayerNameValidation = true;
            }

            team.Players.Add(new() {
                Alias = (PlayerAlias)$"{player} {world}",
                Job = (Job)PlayerJobHelper.GetJobFromName(translatedJob)!,
                Rank = translatedRank != null ? (PlayerRank)translatedRank : null
            });
        }

        _plugin.DataQueue.QueueDataOperation(() => {
            if (!_currentMatch!.Teams.ContainsKey(team.TeamName)) {
                _currentMatch!.Teams.Add(team.TeamName, team);
            } else {
                _plugin.Log.Warning($"Duplicate team found: {team.TeamName}");
            }

            //set local player and data center
            _currentMatch.LocalPlayer ??= (PlayerAlias)_plugin.GameState.GetCurrentPlayer();
            _currentMatch.DataCenter ??= _plugin.ClientState.LocalPlayer?.CurrentWorld.GameData?.DataCenter.Value?.Name.ToString();

            _plugin.Storage.UpdateCCMatch(_currentMatch);

            _plugin.Log.Debug("");
        });
    }

    private unsafe void OnPvPHeaderUpdate(AddonEvent type, AddonArgs args) {
        if (!IsMatchInProgress() || _currentMatch!.IsCompleted) {
            return;
        }

        var addon = (AtkUnitBase*)args.Addon;
        //PrintAtkValues(addon);
        var leftTeamNode = addon->GetNodeById(45)->GetAsAtkTextNode();
        var rightTeamNode = addon->GetNodeById(46)->GetAsAtkTextNode();
        var leftProgressNode = addon->GetNodeById(47)->GetAsAtkTextNode();
        var rightProgressNode = addon->GetNodeById(48)->GetAsAtkTextNode();
        var timerMinsNode = addon->GetNodeById(25)->GetAsAtkTextNode();
        var timerSecondsNode = addon->GetNodeById(27)->GetAsAtkTextNode();

        bool isOvertime = addon->GetNodeById(23) != null ? addon->GetNodeById(23)->IsVisible : false;
        string timerMins = timerMinsNode->NodeText.ToString();
        string timerSeconds = timerSecondsNode->NodeText.ToString();
        string leftTeam = leftTeamNode->NodeText.ToString();
        string rightTeam = rightTeamNode->NodeText.ToString();
        string leftTeamProgress = leftProgressNode->NodeText.ToString();
        string rightTeamProgress = rightProgressNode->NodeText.ToString();

        //limit number of tasks queued by checking for changes
        if (isOvertime != _isOvertimePrev || timerMins != _timerMinsPrev || timerSeconds != _timerSecondsPrev
            || leftTeamProgress != _leftTeamProgressPrev || rightTeamProgress != _rightTeamProgressPrev) {
            _isOvertimePrev = isOvertime;
            _timerMinsPrev = timerMins;
            _timerSecondsPrev = timerSeconds;
            _leftTeamProgressPrev = leftTeamProgress;
            _rightTeamProgressPrev = rightTeamProgress;
            _plugin.DataQueue.QueueDataOperation(() => {
                //check for parse results? this is causing error!
                try {
                    _currentMatch!.MatchTimer = new TimeSpan(0, int.Parse(timerMins), int.Parse(timerSeconds));
                } catch {
                    //hehe
                }

                if (_currentMatch.Teams.Count == 2) {
                    var leftTeamName = MatchHelper.GetTeamName(_plugin.Localization.TranslateDataTableEntry<Addon>(leftTeam, "Text", ClientLanguage.English));
                    var rightTeamName = MatchHelper.GetTeamName(_plugin.Localization.TranslateDataTableEntry<Addon>(rightTeam, "Text", ClientLanguage.English));
                    _currentMatch.Teams[leftTeamName].Progress = float.Parse(leftTeamProgress.Replace("%", "").Replace(",", "."));
                    _currentMatch.Teams[rightTeamName].Progress = float.Parse(rightTeamProgress.Replace("%", "").Replace(",", "."));

                    if (!_currentMatch!.IsOvertime && isOvertime) {
                        _currentMatch.IsOvertime = isOvertime;
                        if (_currentMatch.Teams[leftTeamName].Progress > _currentMatch.Teams[rightTeamName].Progress) {
                            _currentMatch.OvertimeAdvantage = leftTeamName;
                        } else if (_currentMatch.Teams[leftTeamName].Progress < _currentMatch.Teams[rightTeamName].Progress) {
                            _currentMatch.OvertimeAdvantage = rightTeamName;
                        }
                        _plugin.Log.Debug($"Entering overtime...Advantage: {_currentMatch.OvertimeAdvantage}");
                    }
                }

                //don't refresh because this gets triggered too often!
                _plugin.Storage.UpdateCCMatch(_currentMatch, false);

                if ((DateTime.Now - _lastHeaderUpdateTime).TotalSeconds > 60) {
                    _lastHeaderUpdateTime = DateTime.Now;
                    _plugin.Log.Debug($"MATCH TIMER: {timerMins}:{timerSeconds}");
                    _plugin.Log.Debug($"OVERTIME: {isOvertime}");
                    _plugin.Log.Debug($"{leftTeam}: {leftTeamProgress}");
                    _plugin.Log.Debug($"{rightTeam}: {rightTeamProgress}");
                    _plugin.Log.Debug("--------");
                }
            });
        }
    }

    private unsafe void OnPvPResults(AddonEvent type, AddonArgs args) {
        _plugin.Log.Debug("pvp record pre-setup.");

        if (!IsMatchInProgress()) {
            return;
        }
        CrystallineConflictPostMatch postMatch = new();

        var addon = (AtkUnitBase*)args.Addon;
        //AtkNodeHelper.PrintAtkValues(addon);

        var matchWinner = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[3]);
        //need to fix this for JP
        postMatch.MatchWinner = MatchHelper.GetTeamName(Regex.Match(matchWinner, @"(Astra|Umbra)", RegexOptions.IgnoreCase).Value);

        var matchDuration = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[22]);
        postMatch.MatchDuration = TimeSpan.Parse("00:" + matchDuration);

        var leftTeamName = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1526]);
        var leftTeamProgress = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1538]);
        var leftTeamKills = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1542]);
        var leftTeamDeaths = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1544]);
        var leftTeamAssists = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1546]);

        var leftTeamNameTranslated = _plugin.Localization.TranslateDataTableEntry<Addon>(leftTeamName, "Text", ClientLanguage.English);
        CrystallineConflictPostMatchTeam leftTeam = new() {
            TeamName = MatchHelper.GetTeamName(leftTeamNameTranslated),
            Progress = (float)MatchHelper.ConvertProgressStringToFloat(leftTeamProgress),
            TeamStats = new() {
                Team = MatchHelper.GetTeamName(leftTeamNameTranslated),
                Kills = int.Parse(Regex.Match(leftTeamKills, @"\d*$").Value),
                Deaths = int.Parse(Regex.Match(leftTeamDeaths, @"\d*$").Value),
                Assists = int.Parse(Regex.Match(leftTeamAssists, @"\d*$").Value),
            }
        };

        var rightTeamName = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1527]);
        var rightTeamProgress = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1539]);
        var rightTeamKills = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1543]);
        var rightTeamDeaths = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1545]);
        var rightTeamAssists = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1547]);

        var rightTeamNameTranslated = _plugin.Localization.TranslateDataTableEntry<Addon>(rightTeamName, "Text", ClientLanguage.English);
        CrystallineConflictPostMatchTeam rightTeam = new() {
            TeamName = MatchHelper.GetTeamName(rightTeamNameTranslated),
            Progress = (float)MatchHelper.ConvertProgressStringToFloat(rightTeamProgress),
            TeamStats = new() {
                Team = MatchHelper.GetTeamName(rightTeamNameTranslated),
                Kills = int.Parse(Regex.Match(rightTeamKills, @"\d*$").Value),
                Deaths = int.Parse(Regex.Match(rightTeamDeaths, @"\d*$").Value),
                Assists = int.Parse(Regex.Match(rightTeamAssists, @"\d*$").Value),
            }
        };

        var rankChange = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1536]);
        var tierBefore = MatchHelper.TierBeforeRegex.Match(rankChange);
        var riserBefore = MatchHelper.RiserBeforeRegex.Match(rankChange);
        var starsBefore = MatchHelper.StarBeforeRegex.Match(rankChange);
        var creditBefore = MatchHelper.CreditBeforeRegex.Match(rankChange);
        var tierAfter = MatchHelper.TierAfterRegex.Match(rankChange);
        var riserAfter = MatchHelper.RiserAfterRegex.Match(rankChange);
        var starsAfter = MatchHelper.StarAfterRegex.Match(rankChange);
        var creditAfter = MatchHelper.CreditAfterRegex.Match(rankChange);

        _plugin.Log.Debug($"{matchWinner}");
        _plugin.Log.Debug($"match duration: {matchDuration}");
        _plugin.Log.Debug($"rank change: {rankChange}");
        _plugin.Log.Debug($"BEFORE: TIER:{tierBefore.Value} RISER: {riserBefore.Value} STARS: {starsBefore.Length} CREDIT: {creditBefore.Value}");
        _plugin.Log.Debug($"AFTER: TIER:{tierAfter.Value} RISER: {riserAfter.Value} STARS: {starsAfter.Length} CREDIT: {creditAfter.Value}");
        _plugin.Log.Debug(string.Format("{4,-6}: progress: {0,-6} kills: {1,-3} deaths: {2,-3} assists: {3,-3}", leftTeamProgress, leftTeamKills, leftTeamDeaths, leftTeamAssists, leftTeamName));
        _plugin.Log.Debug(string.Format("{4,-6}: progress: {0,-6} kills: {1,-3} deaths: {2,-3} assists: {3,-3}", rightTeamProgress, rightTeamKills, rightTeamDeaths, rightTeamAssists, rightTeamName));
        _plugin.Log.Debug(string.Format("{0,-25} {1,-15} {2,-5} {3,-15} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15}", "NAME", "WORLD", "JOB", "TIER", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE TAKEN", "HP RESTORED", "TIME ON CRYSTAL"));

        //set rank change
        PlayerRank beforeRank = new();
        PlayerRank afterRank = new();
        try {
            if (tierBefore.Success) {
                beforeRank.Tier = MatchHelper.GetTier(_plugin.Localization.TranslateRankString(tierBefore.Value, ClientLanguage.English));
            } else if (creditBefore.Success) {
                beforeRank.Tier = ArenaTier.Crystal;
                if (int.TryParse(creditBefore.Value, out int parseResult)) {
                    beforeRank.Credit = parseResult;
                }
            } else {
                beforeRank.Tier = ArenaTier.None;
            }
            if (tierAfter.Success) {
                afterRank.Tier = MatchHelper.GetTier(_plugin.Localization.TranslateRankString(tierAfter.Value, ClientLanguage.English));
            } else if (creditAfter.Success) {
                afterRank.Tier = ArenaTier.Crystal;
                if (int.TryParse(creditAfter.Value, out int parseResult)) {
                    afterRank.Credit = parseResult;
                }
            } else {
                afterRank.Tier = ArenaTier.None;
            }
            if (riserBefore.Success && beforeRank.Tier != ArenaTier.Crystal) {
                if (int.TryParse(riserBefore.Value, out int parseResult)) {
                    beforeRank.Riser = parseResult;
                }
            }
            if (riserAfter.Success && afterRank.Tier != ArenaTier.Crystal) {
                if (int.TryParse(riserAfter.Value, out int parseResult)) {
                    afterRank.Riser = parseResult;
                }
            }
            if (starsBefore.Success && beforeRank.Tier != ArenaTier.Crystal) {
                beforeRank.Stars = starsBefore.Length;
            }
            if (starsAfter.Success && afterRank.Tier != ArenaTier.Crystal) {
                afterRank.Stars = starsAfter.Length;
            }

            postMatch.RankBefore = beforeRank;
            postMatch.RankAfter = afterRank;
        } catch(ArgumentException e) {
            _plugin.Log.Error($"Unable to add rank:{e.Message}\n{e.StackTrace}");
        }

        postMatch.Teams.Add(leftTeam.TeamName, leftTeam);
        postMatch.Teams.Add(rightTeam.TeamName, rightTeam);

        for (int i = 0; i < 10; i++) {
            int offset = i * 20 + 25;
            var playerName = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset]);
            //missing player
            if (playerName.IsNullOrEmpty()) {
                continue;
            }
            var playerJobIconId = addon->AtkValues[offset + 1].UInt;
            var playerJob = PlayerJobHelper.GetJobFromIcon(playerJobIconId);
            var playerWorld = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 2]);
            var playerKills = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 3]);
            var playerDeaths = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 4]);
            var playerAssists = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 15]);
            var playerDamageDealt = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 6]);
            var playerDamageTaken = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 7]);
            var playerHPRestored = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 8]);
            var playerTimeOnCrystal = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 10]);
            var playerTier = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset + 9]);

            _plugin.Log.Debug(string.Format("{0,-25} {1,-15} {2,-5} {3,-15} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15}", playerName, playerWorld, playerJob, playerTier, playerKills, playerDeaths, playerAssists, playerDamageDealt, playerDamageTaken, playerHPRestored, playerTimeOnCrystal));

            CrystallineConflictPostMatchRow playerRow = new() {
                Job = playerJob,
                Kills = int.Parse(playerKills),
                Deaths = int.Parse(playerDeaths),
                Assists = int.Parse(playerAssists),
                DamageDealt = int.Parse(playerDamageDealt),
                DamageTaken = int.Parse(playerDamageTaken),
                HPRestored = int.Parse(playerHPRestored),
                TimeOnCrystal = TimeSpan.Parse("00:" + playerTimeOnCrystal),
                PlayerRank = new PlayerRank() {
                    Tier = MatchHelper.GetTier(playerTier)
                }
            };

            //validate player name and add to team stats
            foreach (var team in _currentMatch.Teams) {
                foreach (var teamPlayer in team.Value.Players) {
                    bool homeWorldMatch = playerWorld.Equals(teamPlayer.Alias.HomeWorld, StringComparison.OrdinalIgnoreCase);
                    bool jobMatch = playerJob == teamPlayer.Job;
                    if (PlayerJobHelper.IsAbbreviatedAliasMatch(playerName, teamPlayer.Alias.Name) && homeWorldMatch && jobMatch) {
                        playerRow.Player = teamPlayer.Alias;
                        playerRow.Team = team.Key;
                        postMatch.Teams[team.Key].PlayerStats.Add(playerRow);
                        postMatch.Teams[team.Key].TeamStats.DamageDealt += playerRow.DamageDealt;
                        postMatch.Teams[team.Key].TeamStats.DamageTaken += playerRow.DamageTaken;
                        postMatch.Teams[team.Key].TeamStats.HPRestored += playerRow.HPRestored;
                        postMatch.Teams[team.Key].TeamStats.TimeOnCrystal += playerRow.TimeOnCrystal;
                    }
                }
            }
        }

        if (_currentMatch!.PostMatch is null) {
            _currentMatch.PostMatch = postMatch;
            _plugin.Storage.UpdateCCMatch(_currentMatch);
        }
    }

    public bool IsMatchInProgress() {
        return _currentMatch != null;
    }

    //returns true if all names successfully validated
    private bool? ValidatePlayerAliases() {
        if (!IsMatchInProgress()) {
            return null;
        }
        bool allValidated = true;

        foreach (var team in _currentMatch!.Teams) {
            //if can't find player's team ignore team condition
            bool? isPlayerTeam = _currentMatch!.LocalPlayerTeam?.TeamName is null ? null : team.Key == _currentMatch!.LocalPlayerTeam?.TeamName;
            foreach (var player in team.Value.Players) {
                //abbreviated name found
                if (player.Alias.Name.Contains(".")) {
                    //_plugin.Log.Debug($"Checking... {player.Alias.Name}");
                    allValidated = allValidated && ValidatePlayerAgainstObjectTable(player, isPlayerTeam, true);
                }
            }
        }
        return allValidated;
    }

    //returns true if match found
    private bool ValidatePlayerAgainstObjectTable(CrystallineConflictPlayer player, bool? isPartyMember = null, bool updateAlias = false) {
        foreach (PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
            bool homeWorldMatch = player.Alias.HomeWorld.Equals(pc.HomeWorld.GameData.Name.ToString());
            string translatedJobName = _plugin.Localization.TranslateDataTableEntry<ClassJob>(pc.ClassJob.GameData.Name.ToString(), "Name", ClientLanguage.English);
            bool jobMatch = player.Job.Equals(PlayerJobHelper.GetJobFromName(translatedJobName));
            bool isSelf = _plugin.ClientState.LocalPlayer.ObjectId == pc.ObjectId;
            bool teamMatch = isPartyMember is null || (bool)isPartyMember && pc.StatusFlags.HasFlag(StatusFlags.PartyMember) || !(bool)isPartyMember && !pc.StatusFlags.HasFlag(StatusFlags.PartyMember);
            //_plugin.Log.Debug($"Checking against... {pc.Name.ToString()} worldmatch: {homeWorldMatch} jobmatch: {jobMatch} teamMatch:{teamMatch}");
            //_plugin.Log.Debug($"team null? {isPlayerTeam is null} player team? {isPlayerTeam} is p member? {pc.StatusFlags.HasFlag(StatusFlags.PartyMember)} isSelf? {isSelf}");
            if (homeWorldMatch && jobMatch && (isSelf || teamMatch) && PlayerJobHelper.IsAbbreviatedAliasMatch(player.Alias, pc.Name.ToString())) {
                _plugin.Log.Debug($"validated player: {player.Alias.Name} is {pc.Name.ToString()}");
                if (updateAlias) {
                    player.Alias.Name = pc.Name.ToString();
                }
                return true;
            }
        }
        return false;
    }

    private unsafe void ProcessMatchResults(CrystallineConflictResultsPacket resultsPacket) {
        if(!IsMatchInProgress()) {
            _plugin.Log.Warning("trying to process match results on no match!");
            return;
        }

        //set teams
        CrystallineConflictPostMatch postMatch = new();
        CrystallineConflictPostMatchTeam teamAstra = new() {
            TeamName = CrystallineConflictTeamName.Astra,
            TeamStats = new(),
            Progress = resultsPacket.AstraProgress / 10f
        };
        CrystallineConflictPostMatchTeam teamUmbra = new() {
            TeamName = CrystallineConflictTeamName.Umbra,
            TeamStats = new(),
            Progress = resultsPacket.UmbraProgress / 10f
        };
        postMatch.Teams.Add(teamAstra.TeamName, teamAstra);
        postMatch.Teams.Add(teamUmbra.TeamName, teamUmbra);

        //set result
        if (resultsPacket.Result != 1 && resultsPacket.Result != 2) {
            postMatch.MatchWinner = CrystallineConflictTeamName.Unknown;
        }
        if(_currentMatch!.IsSpectated) {
            postMatch.MatchWinner = resultsPacket.Result == 1 ? CrystallineConflictTeamName.Astra : CrystallineConflictTeamName.Umbra;
        } else {
            postMatch.MatchWinner = resultsPacket.Result == 1 ? _currentMatch.LocalPlayerTeam!.TeamName : _currentMatch.Teams.First(x => x.Value.TeamName != _currentMatch.LocalPlayerTeam!.TeamName).Value.TeamName;
        }

        //set duration
        postMatch.MatchDuration = TimeSpan.FromSeconds(resultsPacket.MatchLength);

        //set rank change
        postMatch.RankBefore = new() {
            Tier = (ArenaTier)resultsPacket.ColosseumMatchRankIdBefore,
            Riser = resultsPacket.RiserBefore,
            Stars = resultsPacket.StarsBefore,
            Credit = resultsPacket.CreditBefore
        };
        postMatch.RankAfter = new() {
            Tier = (ArenaTier)resultsPacket.ColosseumMatchRankIdAfter,
            Riser = resultsPacket.RiserAfter,
            Stars = resultsPacket.StarsAfter,
            Credit = resultsPacket.CreditAfter
        };

        //set player stats
        foreach(var player in resultsPacket.PlayerSpan) {
            //missing player?
            if(player.ClassJobId == 0) {
                _plugin.Log.Warning("invalid/missing player result.");
                continue;
            }

            CrystallineConflictPostMatchRow playerStats = new() {
                Player = (PlayerAlias)$"{AtkNodeHelper.ReadString(player.PlayerName, 32)} {_plugin.DataManager.GetExcelSheet<World>().GetRow(player.WorldId).Name}",
                Team = player.Team == 0 ? CrystallineConflictTeamName.Astra : CrystallineConflictTeamName.Umbra,
                Job = PlayerJobHelper.GetJobFromName(_plugin.DataManager.GetExcelSheet<ClassJob>().GetRow(player.ClassJobId).NameEnglish),
                PlayerRank = new PlayerRank() {
                    Tier = (ArenaTier)player.ColosseumMatchRankId
                },
                Kills = player.Kills,
                Deaths = player.Deaths,
                Assists = player.Assists,
                DamageDealt = (int)player.DamageDealt,
                DamageTaken = (int)player.DamageTaken,
                HPRestored = (int)player.HPRestored,
                TimeOnCrystal = TimeSpan.FromSeconds(player.TimeOnCrystal)
            };

            //add to team
            var playerTeam = playerStats.Team == CrystallineConflictTeamName.Astra ? teamAstra : teamUmbra;
            playerTeam.PlayerStats.Add(playerStats);
            playerTeam.TeamStats.Kills += playerStats.Kills;
            playerTeam.TeamStats.Deaths += playerStats.Deaths;
            playerTeam.TeamStats.Assists += playerStats.Assists;
            playerTeam.TeamStats.DamageDealt += playerStats.DamageDealt;
            playerTeam.TeamStats.DamageTaken += playerStats.DamageTaken;
            playerTeam.TeamStats.HPRestored += playerStats.HPRestored;
            playerTeam.TeamStats.TimeOnCrystal += playerStats.TimeOnCrystal;
        }

        _currentMatch.PostMatch = postMatch;
        _plugin.Storage.UpdateCCMatch(_currentMatch);
    }
}
