using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PvpStats.Helpers;
using PvpStats.Services;
using PvpStats.Types.ClientStruct;
using PvpStats.Types.ClientStruct.Action;
using PvpStats.Types.Event;
using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PvpStats.Managers.Game;
internal class CrystallineConflictMatchManager : IDisposable {

    private Plugin _plugin;
    private CrystallineConflictMatch? _currentMatch;

    private CrystallineConflictMatchTimeline? _currentMatchTimeline;
    private List<ActionEvent>? _casts;
    private float _lastEventTimer;

    private DateTime _lastUpdate;
    private DateTime _lastPrint = DateTime.MinValue;

    //p1 = director
    //p2 = results packet
    //p3 = results packet + offset (ref to specific variable?)
    //p4 = ???
    private delegate void CCMatchEnd101Delegate(IntPtr p1, IntPtr p2, IntPtr p3, uint p4);
    [Signature("40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 0F B6 42", DetourName = nameof(CCMatchEnd101Detour))]
    private readonly Hook<CCMatchEnd101Delegate> _ccMatchEndHook;

    //40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 4C 8B E1 
    [Signature("40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 4C 8B E1", DetourName = nameof(CCMatchEndSpectatorDetour))]
    private readonly Hook<CCMatchEnd101Delegate> _ccMatchEndSpectatorHook;

    private delegate void ProcessPacketActorControlDelegate(uint entityId, uint type, uint statusId, uint amount, uint a5, uint source, uint a7, uint a8, ulong a9, byte flag);
    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private readonly Hook<ProcessPacketActorControlDelegate> _processPacketActorControlHook = null!;

    private unsafe delegate void ProcessPacketActionEffectDelegate(uint entityId, IntPtr sourceCharacter, IntPtr pos, ActionEffectHeader* effectHeader, ActionEffect* effectArray, ulong* effectTrail);
    [Signature("40 55 53 56 41 54 41 55 41 56 41 57 48 8D AC 24 60 FF FF FF 48 81 EC A0 01 00 00", DetourName = nameof(ProcessPacketActionEffectDetour))]
    private readonly Hook<ProcessPacketActionEffectDelegate> _processPacketActionEffectHook = null!;

    private delegate void ProcessKillDelegate(IntPtr agent, IntPtr killerPlayer, uint killStreak, IntPtr victimPlayer, uint localPlayerTeam);
    [Signature("40 55 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 48 8B 01", DetourName = nameof(ProcessKillDetour))]
    private readonly Hook<ProcessKillDelegate> _processKillHook;

    //private delegate void SetAtkValuesDelegate(IntPtr addon, uint count, IntPtr data);
    //[Signature("E8 ?? ?? ?? ?? 48 8B 03 8B D7 4C 8B 83 ", DetourName = nameof(SetAtkValuesDetour))]
    //private readonly Hook<SetAtkValuesDelegate> _setAtkValuesHook;

    //private delegate short OpenAddonDelegate(IntPtr p1, uint p2, uint p3, IntPtr p4, IntPtr p5, IntPtr p6, short p7, int p8);
    //[Signature("E8 ?? ?? ?? ?? 83 67", DetourName = nameof(OpenAddonDetour))]
    //private readonly Hook<OpenAddonDelegate> _openAddonHook;

    private static readonly Regex TierRegex = new(@"\D+", RegexOptions.IgnoreCase);
    private static readonly Regex RiserRegex = new(@"\d+", RegexOptions.IgnoreCase);

    public CrystallineConflictMatchManager(Plugin plugin) {
        _plugin = plugin;

        _plugin.Framework.Update += OnFrameworkUpdate;
        _plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSIntroduction", OnPvPIntro);
        //_plugin.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "PvPMKSBattleLog", OnBattleLog);
        _plugin.InteropProvider.InitializeFromAttributes(this);
        _plugin.Log.Debug($"cc match end 1 address: 0x{_ccMatchEndHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"cc match end 2 address: 0x{_ccMatchEndSpectatorHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"cc process kill address: 0x{_processKillHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"process actor control address: 0x{_processPacketActorControlHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"process action effect address: 0x{_processPacketActionEffectHook!.Address.ToString("X2")}");

        _ccMatchEndHook.Enable();
#if DEBUG
        _ccMatchEndSpectatorHook.Enable();
#endif
        _processKillHook.Enable();
        _processPacketActorControlHook.Enable();
        _processPacketActionEffectHook.Enable();

        //_setAtkValuesHook.Enable();
        //_openAddonHook.Enable();
    }

    public void Dispose() {
        _plugin.Framework.Update -= OnFrameworkUpdate;
        _plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _plugin.AddonLifecycle.UnregisterListener(OnPvPIntro);
        //_plugin.AddonLifecycle.UnregisterListener(OnBattleLog);
        _ccMatchEndHook.Dispose();
        _ccMatchEndSpectatorHook.Dispose();
        _processKillHook.Dispose();
        _processPacketActorControlHook.Dispose();
        _processPacketActionEffectHook.Dispose();
        //_setAtkValuesHook.Dispose();
        //_openAddonHook.Dispose();
    }

    public bool IsMatchInProgress() {
        return _currentMatch != null;
    }

    private void StartMatch() {
        var dutyId = _plugin.GameState.GetCurrentDutyId();
        var territoryId = _plugin.ClientState.TerritoryType;

        _plugin.Log.Debug($"Current duty: {dutyId} Current territory: {territoryId}");
        _plugin.CCStatsEngine.RefreshQueue.QueueDataOperation(() => {
            _currentMatch = new() {
                DutyId = dutyId,
                TerritoryId = territoryId,
                Arena = MatchHelper.GetArena(territoryId),
                MatchType = MatchHelper.GetMatchType(dutyId),
                PluginVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            };
            unsafe {
                if(Framework.Instance() != null) {
                    _currentMatch.GameVersion = Framework.Instance()->GameVersionString;
                }
            }
            _currentMatchTimeline = new() {
                CrystalPosition = new(),
                TeamProgress = new() {
                    {CrystallineConflictTeamName.Astra, new() },
                    {CrystallineConflictTeamName.Umbra, new() },
                },
                TeamMidProgress = new() {
                    {CrystallineConflictTeamName.Astra, new() },
                    {CrystallineConflictTeamName.Umbra, new() },
                },
                Kills = new(),
                LimitBreakCasts = new(),
                LimitBreakEffects = new(),
                MapEvents = new(),
#if DEBUG
                TotalizedMedkits = new(),
#endif
            };
#if DEBUG
            _casts = new();
#endif
            _lastEventTimer = -1f;
            _plugin.Log.Information($"starting new match on {_currentMatch.Arena}");
            _plugin.DataQueue.QueueDataOperation(async () => {
                await _plugin.CCCache.AddMatch(_currentMatch);
                await _plugin.Storage.AddCCTimeline(_currentMatchTimeline);
            });
        });
    }

    private unsafe void ProcessKillDetour(IntPtr agent, IntPtr killerPlayer, uint killStreak, IntPtr victimPlayer, uint localPlayerTeam) {
        try {
            var now = DateTime.Now;
            Plugin.Log2.Debug($"kill feed detour occurred. killer: 0x{((CCPlayer*)killerPlayer)->EntityId:X2} kills: {killStreak} victim: 0x{((CCPlayer*)victimPlayer)->EntityId:X2} localPlayerTeam: {localPlayerTeam}");

            if(_currentMatchTimeline != null && _currentMatchTimeline.Kills != null) {
                var killerObj = _plugin.ObjectTable.SearchByEntityId(((CCPlayer*)killerPlayer)->EntityId);
                var killerWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((killerObj as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
                var killerAlias = (PlayerAlias)$"{killerObj.Name} {killerWorld}";
                var victimObj = _plugin.ObjectTable.SearchByEntityId(((CCPlayer*)victimPlayer)->EntityId);
                var victimWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((victimObj as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
                var victimAlias = (PlayerAlias)$"{victimObj.Name} {victimWorld}";

                _currentMatchTimeline.Kills.Add(new(now, victimAlias) {
                    CreditedKiller = killerAlias,
                    CreditedKillerSnapshot = new(killerObj as IBattleChara),
                    VictimSnapshot = new(victimObj as IBattleChara),
                });
            }
        } finally {
            _processKillHook.Original(agent, killerPlayer, killStreak, victimPlayer, localPlayerTeam);
        }
    }

    private void ProcessPacketActorControlDetour(uint sourceEntityId, uint type, uint statusId, uint amount, uint a5, uint source, uint a7, uint a8, ulong targetEntityId, byte flag) {
        try {
            if(!_plugin.DebugMode && (!IsMatchInProgress() || _currentMatchTimeline == null)) {
                return;
            }
            //Plugin.Log2.Debug($"actor control: 0x{type:X2} {(ActorControlCategory)type} 0x{sourceEntityId:X2} StatusId: 0x{statusId:X2} Source: {source} Amount: {amount} a5: {a5} a7: {a7} a8: {a8} a9: {targetEntityId} flag: {flag}");

            var now = DateTime.Now;
            if(type == (uint)ActorControlCategory.Death && _currentMatchTimeline?.Kills != null) {
                //Plugin.Log2.Debug($"0x{sourceEntityId:X2} was owned by: StatusId: 0x{statusId:X2} Source: {source} Amount: 0x{amount:X2} a5: {a5} a7: {a7} a8: {a8} a9: {targetEntityId} flag: {flag}");
                var victim = _plugin.ObjectTable.SearchByEntityId(sourceEntityId);
                var killer = _plugin.ObjectTable.SearchByEntityId(amount);
                var owner = _plugin.ObjectTable.SearchByEntityId((killer?.OwnerId ?? 0));
                Plugin.Log2.Debug($"Death detected: 0x{sourceEntityId:X2} {victim?.Name ?? ""} was deleted by: 0x{amount:X2} {killer?.Name ?? ""}, owner: 0x{killer?.OwnerId:X2} {owner?.Name ?? ""}");

                if(victim?.ObjectKind is ObjectKind.Player) {
                    var victimWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((victim as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
                    var victimAlias = (PlayerAlias)$"{victim.Name} {victimWorld}";

                    //find matching event and set
                    var matchingEvent = _currentMatchTimeline?.Kills.LastOrDefault(x => x.Victim.Equals(victimAlias)
                        && (now - x.Timestamp) <= TimeSpan.FromSeconds(8));

                    if(killer?.ObjectKind is ObjectKind.BattleNpc && owner == null) {
                        //add BNpc NameId in case of NPC killer (exclude pets)
                        uint? nameId = (killer as IBattleNpc)?.NameId;
                        if(matchingEvent != null) {
                            matchingEvent.KillerNameId = nameId;
                        } else {
                            Plugin.Log2.Warning($"No credited killer found for npc death for: {victimAlias}");
                            _currentMatchTimeline?.Kills.Add(new(now, victimAlias) {
                                KillerNameId = nameId,
                            });
                        }
                    } else if(killer?.ObjectKind is ObjectKind.Player && killer.EntityId == victim.EntityId) {
                        //add suicide death
                        if(matchingEvent == null) {
                            Plugin.Log2.Warning($"No credited killer found for suicide death for: {victimAlias}");
                            _currentMatchTimeline?.Kills.Add(new(now, victimAlias) {
                            });
                        }
                    }

                    ////add to cache
                    //if(nameId != null && (!_currentMatchTimeline?.BNPCNameLookup?.ContainsKey((uint)nameId) ?? false)) {
                    //    var bnpcName = _plugin.DataManager.GetExcelSheet<BNpcName>(ClientLanguage.English).GetRow((uint)nameId);
                    //    _currentMatchTimeline!.BNPCNameLookup!.Add((uint)nameId, (bnpcName.Singular.ToString(), bnpcName.Article));
                    //}
                }
            } else if(type == (uint)ActorControlCategory.CCPot) {
                var player = _plugin.ObjectTable.SearchByEntityId(sourceEntityId);

                Plugin.Log2.Debug($"Potion detected: 0x{sourceEntityId:X2} {player?.Name ?? ""} 0x{type:X2} {(ActorControlCategory)type} StatusId: 0x{statusId:X2} Source: {source} Amount: {amount} a5: {a5} a7: {a7} a8: {a8} a9: {targetEntityId} flag: {flag}");
                if(amount != 30000) {
                    Plugin.Log2.Warning("Non-standard potion amount!");
                }
                var world = _plugin.DataManager.GetExcelSheet<World>().GetRow((player as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
                var alias = (PlayerAlias)$"{player.Name} {world}";

                if(!_currentMatchTimeline?.TotalizedMedkits?.TryAdd(alias, 1) ?? false) {
                    _currentMatchTimeline!.TotalizedMedkits![alias]++;
                }
            }
        } finally {
            _processPacketActorControlHook.Original(sourceEntityId, type, statusId, amount, a5, source, a7, a8, targetEntityId, flag);
        }
    }

    private unsafe void ProcessPacketActionEffectDetour(uint entityId, IntPtr sourceCharacter, IntPtr pos, ActionEffectHeader* effectHeader, ActionEffect* effectArray, ulong* effectTrail) {
        try {
            if(!_plugin.DebugMode && !IsMatchInProgress()) {
                return;
            }
            var now = DateTime.Now;
            var actionId = effectHeader->ActionAnimationId;
            uint targets = effectHeader->EffectCount;
            uint? nameIdActor = null;
            bool isPet = false;

            var actor = _plugin.ObjectTable.SearchByEntityId(entityId);
            var spell = _plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(ClientLanguage.English).GetRow(actionId);
            if(_plugin.DebugMode) {
                Plugin.Log2.Debug($"{actor?.Name} cast {spell.Name} {actionId} targets: {targets} display: {effectHeader->EffectDisplayType} " +
            $"hidden anim: {effectHeader->HiddenAnimation} counter: {effectHeader->GlobalEffectCounter} rotation: {effectHeader->Rotation} variation: {effectHeader->Variation}");
            }

            if(actor is not IBattleChara) return;
            if(actor is IBattleNpc) {
                //attempt to retrieve owner in case of pet (summoner)
                var owner = _plugin.ObjectTable.SearchByEntityId(actor?.OwnerId ?? 0);
                if(owner is not IPlayerCharacter) {
                    //Plugin.Log2.Warning("Limit break cast by non-player character");
                    return;
                } else {
                    nameIdActor = (actor as IBattleNpc)!.NameId;
                    actor = owner;
                    isPet = true;
                }
            }
            var actorWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((actor as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
            var alias = (PlayerAlias)$"{(actor as IPlayerCharacter).Name} {actorWorld}";

            ActionEvent actionEvent = new(now, actionId, alias) {
                Variation = effectHeader->Variation,
                NameIdActor = nameIdActor,
                Snapshots = new() {
                        {alias, new(actor as IBattleChara) }
                    }
            };

            for(var i = 0; i < targets; i++) {
                var actionTargetId = (uint)(effectTrail[i] & uint.MaxValue);
                var target = _plugin.ObjectTable.SearchByEntityId(actionTargetId);
                if(target is not IBattleChara || (target as IBattleChara) is null) continue;

                var playerSnapshot = new BattleCharaSnapshot((target as IBattleChara));
                //Plugin.Log2.Debug($"{target.Name} HP: {playerSnapshot.CurrentHP}/{playerSnapshot.MaxHP} MP: {playerSnapshot.CurrentMP}/{playerSnapshot.MaxMP} shields: {playerSnapshot.ShieldPercents}");
                //string statuses = "";
                //foreach(var status in playerSnapshot.Statuses) {
                //    var data = _plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRow(status.StatusId);
                //    statuses += $"{data.Name}:{status.StatusId}:{status.Param}:{status.RemainingTime} ";
                //}
                //Plugin.Log2.Debug(statuses);

                for(var j = 0; j < 8; j++) {
                    ref var actionEffect = ref effectArray[i * 8 + j];
                    if(actionEffect.EffectType == 0)
                        continue;
                    if(_plugin.DebugMode) {
                        Plugin.Log2.Debug($"{target?.Name} was hit by {actionEffect.EffectType} p0: {actionEffect.Param0} p1: {actionEffect.Param1} p2: {actionEffect.Param2} " +
                        $"f1: {actionEffect.Flags1} f2: {actionEffect.Flags2} value: {actionEffect.Value}");
                    }
                }

                if(target is IPlayerCharacter) {
                    var targetWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((target as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
                    var targetAlias = (PlayerAlias)$"{(target as IPlayerCharacter).Name} {targetWorld}";
                    actionEvent.Snapshots.TryAdd(targetAlias, playerSnapshot);
                    actionEvent.PlayerTargets.Add(targetAlias);
                } else if(target is IBattleNpc) {
                    var npcTarget = target as IBattleNpc;
                    var bnpcName = _plugin.DataManager.GetExcelSheet<BNpcName>(ClientLanguage.English).GetRow(npcTarget.NameId);
                    actionEvent.NameIdTargets.Add(npcTarget.NameId);
                } else {
                    Plugin.Log2.Warning($"{spell.Name} cast on unknown entity {target?.Name}");
                    continue;
                }
            }

            if(effectHeader->Variation == 0 && !isPet) {
                _casts?.Add(actionEvent);
            }

            if(CombatHelper.IsLimitBreak(actionId)) {
                if(!_plugin.DebugMode) {
                    Plugin.Log2.Debug($"{actor?.Name} cast {spell.Name} {actionId} targets: {targets} display: {effectHeader->EffectDisplayType} " +
                    $"hidden anim: {effectHeader->HiddenAnimation} counter: {effectHeader->GlobalEffectCounter} rotation: {effectHeader->Rotation} variation: {effectHeader->Variation}");
                }
                if(effectHeader->Variation == 0) {
                    _currentMatchTimeline?.LimitBreakCasts?.Add(actionEvent);
                } else if(effectHeader->Variation == 2) {
                    _currentMatchTimeline?.LimitBreakEffects?.Add(actionEvent);
                } else {
                    Plugin.Log2.Warning($"{spell.Name} unknown variation: {effectHeader->Variation}");
                }
            }
        } finally {
            _processPacketActionEffectHook.Original(entityId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
        }
    }

    private void CCMatchEnd101Detour(IntPtr p1, IntPtr p2, IntPtr p3, uint p4) {
        _plugin.Log.Debug("Match end detour occurred.");
#if DEBUG
        _plugin.Functions.CreateByteDump(p2, 0x400, "cc_match_results");
#endif
        CrystallineConflictResultsPacket resultsPacket;
        unsafe {
            resultsPacket = *(CrystallineConflictResultsPacket*)p2;
        }
        var matchEndTask = _plugin.DataQueue.QueueDataOperation(async () => {
            if(ProcessMatchResults(resultsPacket)) {
                await _plugin.CCCache.UpdateMatch(_currentMatch!);
                if(_currentMatchTimeline != null) {
                    await _plugin.Storage.UpdateCCTimeline(_currentMatchTimeline);
                }
                _ = _plugin.WindowManager.RefreshCCWindow();
            }
        });
        //matchEndTask.Result.ContinueWith(t => _plugin.WindowManager.RefreshCCWindow());
        _ccMatchEndHook.Original(p1, p2, p3, p4);
    }

    private void CCMatchEndSpectatorDetour(IntPtr p1, IntPtr p2, IntPtr p3, uint p4) {
        _plugin.Log.Debug("Spectated match end detour occurred.");
#if DEBUG
        _plugin.Functions.CreateByteDump(p2, 0x400, "spectated_cc_match_results");
#endif
        CrystallineConflictResultsPacket resultsPacket;
        unsafe {
            resultsPacket = *(CrystallineConflictResultsPacket*)p2;
        }
        var matchEndTask = _plugin.DataQueue.QueueDataOperation(async () => {
            if(ProcessMatchResults(resultsPacket)) {
                await _plugin.CCCache.UpdateMatch(_currentMatch!);
                if(_currentMatchTimeline != null) {
                    await _plugin.Storage.UpdateCCTimeline(_currentMatchTimeline);
                }
                _ = _plugin.WindowManager.RefreshCCWindow();
            }
        });
        //matchEndTask.Result.ContinueWith(t => _plugin.WindowManager.RefreshCCWindow());
        _ccMatchEndSpectatorHook.Original(p1, p2, p3, p4);
    }

    private void OnTerritoryChanged(ushort territoryId) {
        var dutyId = _plugin.GameState.GetCurrentDutyId();
        _plugin.Log.Debug($"Territory changed: {territoryId}, Current duty: {dutyId}");
        if(MatchHelper.CrystallineConflictMapLookup.ContainsKey(territoryId) && MatchHelper.GetMatchType(dutyId) != CrystallineConflictMatchType.Unknown) {
            StartMatch();
        } else if(IsMatchInProgress()) {
            _plugin.DataQueue.QueueDataOperation(async () => {
                _plugin.Functions._opcodeMatchCount++;
                if(_currentMatchTimeline != null) {
                    await _plugin.Storage.UpdateCCTimeline(_currentMatchTimeline);
                }
                _currentMatch = null;
                _currentMatchTimeline = null;
                //_plugin.WindowManager.Refresh();
            });
        }
    }

    //extract player info from intro screen
    private void OnPvPIntro(AddonEvent type, AddonArgs args) {
        if(!IsMatchInProgress()) {
            _plugin.Log.Warning("no match in progress on pvp intro!");
            return;
        }
        _plugin.Functions._qPopped = false;
        _plugin.Log.Debug("Pvp intro post setup");

        //        unsafe {
        //            var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.PvPMKSIntroduction);
        //#if DEBUG
        //            _plugin.Functions.CreateByteDump(new IntPtr(agent), 0x2000, "PvPMKSIntroduction");
        //#endif
        //        }

        CrystallineConflictTeam team = new();
        unsafe {
            var addon = (AtkUnitBase*)args.Addon;
            //var agent = _plugin.GameGui.FindAgentInterface(args.AddonName);
            var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.PvPMKSIntroduction);
#if DEBUG
            _plugin.Functions.CreateByteDump(new IntPtr(agent), 0x4000, "PvPMKSIntroduction_Agent");
#endif

            //team name
            string teamName = AtkNodeService.ConvertAtkValueToString(addon->AtkValues[4]);
            var teamAddonIds = _plugin.Localization.GetRowId<Addon>(teamName, "Text");
            team.TeamName = teamAddonIds.Contains(14423) ? CrystallineConflictTeamName.Astra : teamAddonIds.Contains(14424) ? CrystallineConflictTeamName.Umbra : CrystallineConflictTeamName.Unknown;

            _plugin.Log.Debug(teamName);
            for(int i = 0; i < 5; i++) {
                int offset = i * 16 + 6;
                uint[] rankIdChain = [1, (uint)(13 + i), 2, 9];
                if(offset >= addon->AtkValuesCount) {
                    break;
                }
                string playerRaw = AtkNodeService.ConvertAtkValueToString(addon->AtkValues[offset]);
                string worldRaw = AtkNodeService.ConvertAtkValueToString(addon->AtkValues[offset + 6]);

                string jobRaw = AtkNodeService.ConvertAtkValueToString(addon->AtkValues[offset + 5]).Trim();
                uint? jobId = _plugin.Localization.GetRowId<ClassJob>(jobRaw, "Name").FirstOrDefault();
                //JP uses English names...
                jobId ??= _plugin.Localization.GetRowId<ClassJob>(jobRaw, "NameEnglish").FirstOrDefault();
                string translatedJob = "";
                if(jobId != null) {
                    translatedJob = _plugin.DataManager.GetExcelSheet<ClassJob>().GetRow((uint)jobId).NameEnglish.ToString();
                }
                Job? job = PlayerJobHelper.GetJobFromName(translatedJob);

                string rankRaw = "";
                PlayerRank? rank = null;
                //have to read rank from nodes -_-
                var rankNode = AtkNodeService.GetNodeByIDChain(addon, rankIdChain);
                if(rankNode == null || rankNode->Type != NodeType.Text || rankNode->GetAsAtkTextNode()->NodeText.ToString().IsNullOrEmpty()) {
                    rankIdChain[3] = 10; //non-crystal
                    rankNode = AtkNodeService.GetNodeByIDChain(addon, rankIdChain);
                }
                if(rankNode != null && rankNode->Type == NodeType.Text) {
                    rankRaw = rankNode->GetAsAtkTextNode()->NodeText.ToString();
                    if(!rankRaw.IsNullOrEmpty()) {
                        rank = new();
                        //set ranked as fallback
                        //_currentMatch!.MatchType = CrystallineConflictMatchType.Ranked;
                        string tierString = TierRegex.Match(rankRaw).Value.Trim();
                        var addonId = _plugin.Localization.GetRowId<Addon>(tierString, "Text").FirstOrDefault(x => x >= 14894 && x <= 14899);
                        if(addonId != null) {
                            rank.Tier = (ArenaTier)addonId - 14893;
                        }

                        string riserString = RiserRegex.Match(rankRaw).Value.Trim();
                        if(int.TryParse(riserString, out int riser)) {
                            rank.Riser = riser;
                        }
                    }
                }

                //abbreviated names
                if(playerRaw.Contains('.')) {
                    foreach(IPlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
                        //_plugin.Log.Debug($"name: {pc.Name} homeworld {pc.HomeWorld.GameData.Name.ToString()} job: {pc.ClassJob.GameData.NameEnglish}");
                        bool homeWorldMatch = worldRaw.Equals(pc.HomeWorld.Value.Name.ToString(), StringComparison.OrdinalIgnoreCase);
                        bool jobMatch = pc.ClassJob.Value.NameEnglish.ToString().Equals(translatedJob, StringComparison.OrdinalIgnoreCase);
                        bool nameMatch = PlayerJobHelper.IsAbbreviatedAliasMatch(playerRaw, pc.Name.ToString());
                        //_plugin.Log.Debug($"homeworld match:{homeWorldMatch} jobMatch:{jobMatch} nameMatch: {nameMatch}");
                        if(homeWorldMatch && jobMatch && nameMatch) {
                            _plugin.Log.Debug($"validated player: {playerRaw} is {pc.Name}");
                            playerRaw = pc.Name.ToString();
                            break;
                        }
                    }
                }

                _plugin.Log.Debug(string.Format("player: {0,-25} {1,-15} job: {2,-15} rank: {3,-10}", playerRaw, worldRaw, jobRaw, rankRaw));

                team.Players.Add(new() {
                    Alias = (PlayerAlias)$"{playerRaw} {worldRaw}",
                    Job = job,
                    Rank = rank,
                    Team = team.TeamName
                });
            }
        }

        _plugin.DataQueue.QueueDataOperation(async () => {
            foreach(var player in team.Players) {
                _currentMatch!.IntroPlayerInfo.Add(player.Alias, player);
            }
            await _plugin.CCCache.UpdateMatch(_currentMatch!);
            _plugin.Log.Debug("");
        });
    }

    //private unsafe void SetAtkValuesDetour(IntPtr addon, uint count, IntPtr data) {
    //    try {
    //        var addonBase = (AtkUnitBase*)addon;
    //        var dataValue = (AtkValue*)data;

    //        //Plugin.Log2.Debug($"Set atk values! Count: {addonBase->NameString} {count}");

    //        //count == 85...
    //        if(count == 85 || addonBase->NameString == "PvPMKSIntroduction") {
    //            Plugin.Log2.Debug($"intro set atk values! Count: {count}");
    //            for(int i = 0; i < count; i++) {
    //                var atkValue = dataValue[i];
    //                string stringVal = "";
    //                switch(atkValue.Type) {
    //                    case 0:
    //                        break;
    //                    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int:
    //                    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt: {
    //                            stringVal = $"{atkValue.Int}";
    //                            break;
    //                        }

    //                    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.ManagedString:
    //                    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String8:
    //                    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String: {
    //                            if(atkValue.String.Value == null) {
    //                                stringVal = $"null";
    //                            } else {
    //                                stringVal = AtkNodeService.ConvertAtkValueToString(atkValue);
    //                            }

    //                            break;
    //                        }

    //                    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool: {
    //                            stringVal = $"{atkValue.Byte != 0}";
    //                            break;
    //                        }

    //                    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Pointer:
    //                        stringVal = $"{(nint)atkValue.Pointer}";
    //                        break;

    //                    default: {
    //                            stringVal = "Unhandled Type";
    //                            //Util.ShowStruct(atkValue);
    //                            break;
    //                        }
    //                }

    //                Plugin.Log2.Debug($" Type: {atkValue.Type} Value: {stringVal}");
    //            }
    //        }

    //    } finally {
    //        _setAtkValuesHook.Original(addon, count, data);
    //    }
    //}

    //private unsafe short OpenAddonDetour(IntPtr p1, uint p2, uint p3, IntPtr p4, IntPtr p5, IntPtr p6, short p7, int p8) {
    //    try {
    //        var agent = (AgentInterface*)p5;
    //        var atkVals = p4;
    //        var introAgent = AgentModule.Instance()->GetAgentByInternalId(AgentId.PvPMKSIntroduction);
    //        Plugin.Log2.Debug($"Opening addon! Agent: 0x{p5:X2} ID: {agent->AddonId}");
    //        if(agent == introAgent) {
    //            var data = new IntPtr(agent + 0x28);
    //            Plugin.Log2.Debug($"Intro opened! Data ptr: 0x{data:X2}");
    //            _plugin.Functions.CreateByteDump(new IntPtr(agent), 0x4000, "PvPMKSIntroduction_OpenAddon");

    //            _plugin.Functions.CreateByteDump(data, 0x4000, "PvPMKSIntroduction_OpenAddon_Data");
    //        }

    //    } catch {

    //    }
    //    return _openAddonHook.Original(p1, p2, p3, p4, p5, p6, p7, p8);
    //}

    //returns true if successfully processed
    private bool ProcessMatchResults(CrystallineConflictResultsPacket resultsPacket) {
        if(!IsMatchInProgress()) {
            _plugin.Log.Error("trying to process match results on no match!");
            return false;
            //fallback for case where you load into a game after the match has completed creating a new match
            //this will trigger if you load in after a disconnect as the match as ending...
        } else if((DateTime.Now - _currentMatch!.DutyStartTime).TotalSeconds < 10) {
            _plugin.Log.Error("double match detected.");
            return false;
        }

        _plugin.Log.Information("CC match has ended.");

        CrystallineConflictPostMatch postMatch = new();
        _currentMatch.LocalPlayer ??= _plugin.GameState.CurrentPlayer;
        _currentMatch.DataCenter ??= _plugin.GameState.DataCenterName;

        //set teams
        CrystallineConflictTeam teamAstra = new() {
            TeamName = CrystallineConflictTeamName.Astra,
            Progress = resultsPacket.AstraProgress / 10f,
        };
        CrystallineConflictPostMatchTeam teamAstraPost = new() {
            TeamName = CrystallineConflictTeamName.Astra,
            TeamStats = new(),
            Progress = resultsPacket.AstraProgress / 10f
        };
        CrystallineConflictPostMatchTeam teamUmbraPost = new() {
            TeamName = CrystallineConflictTeamName.Umbra,
            TeamStats = new(),
            Progress = resultsPacket.UmbraProgress / 10f
        };
        CrystallineConflictTeam teamUmbra = new() {
            TeamName = CrystallineConflictTeamName.Umbra,
            Progress = resultsPacket.UmbraProgress / 10f,
        };
        _currentMatch.Teams.Add(teamAstra.TeamName, teamAstra);
        _currentMatch.Teams.Add(teamUmbra.TeamName, teamUmbra);
        postMatch.Teams.Add(teamAstraPost.TeamName, teamAstraPost);
        postMatch.Teams.Add(teamUmbraPost.TeamName, teamUmbraPost);

        //set duration
        postMatch.MatchDuration = TimeSpan.FromSeconds(resultsPacket.MatchLength);
        _currentMatch.MatchEndTime = DateTime.Now;
        _currentMatch.MatchStartTime = _currentMatch.MatchEndTime - postMatch.MatchDuration;

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
        for(int i = 0; i < resultsPacket.PlayerSpan.Length; i++) {
            var player = resultsPacket.PlayerSpan[i];
            //missing player?
            if(player.ClassJobId == 0) {
                _plugin.Log.Warning("invalid/missing player result.");
                continue;
            }

            CrystallineConflictPostMatchRow playerStats = new() {
                Team = player.Team == 0 ? CrystallineConflictTeamName.Astra : CrystallineConflictTeamName.Umbra,
                Job = PlayerJobHelper.GetJobFromName(_plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(player.ClassJobId).NameEnglish.ToString() ?? ""),
                PlayerRank = new PlayerRank() {
                    Tier = (ArenaTier)player.ColosseumMatchRankId
                },
                Kills = player.Kills,
                Deaths = player.Deaths,
                Assists = player.Assists,
                DamageDealt = player.DamageDealt,
                DamageTaken = player.DamageTaken,
                HPRestored = player.HPRestored,
                TimeOnCrystal = TimeSpan.FromSeconds(player.TimeOnCrystal),
            };
            unsafe {
                playerStats.Player = (PlayerAlias)$"{MemoryService.ReadString(player.PlayerName, 32)} {_plugin.DataManager.GetExcelSheet<World>()?.GetRow((uint)player.WorldId).Name}";
            }

            //add to team
            var playerTeam = playerStats.Team == CrystallineConflictTeamName.Astra ? teamAstra : teamUmbra;
            var newPlayer = new CrystallineConflictPlayer() {
                Alias = playerStats.Player,
                Job = playerStats.Job,
                ClassJobId = player.ClassJobId,
                Rank = playerStats.PlayerRank,
                //AccountId = player.AccountId,
                //ContentId = player.ContentId,
            };
            //set player riser from intro
            if(_currentMatch.IntroPlayerInfo.ContainsKey(newPlayer.Alias)) {
                newPlayer.Rank.Riser = _currentMatch.IntroPlayerInfo[newPlayer.Alias].Rank?.Riser;
            }
            playerTeam.Players.Add(newPlayer);

            //add to team stats
            var playerTeamPost = playerStats.Team == CrystallineConflictTeamName.Astra ? teamAstraPost : teamUmbraPost;
            playerTeamPost.PlayerStats.Add(playerStats);
            playerTeamPost.TeamStats.Kills += playerStats.Kills;
            playerTeamPost.TeamStats.Deaths += playerStats.Deaths;
            playerTeamPost.TeamStats.Assists += playerStats.Assists;
            playerTeamPost.TeamStats.DamageDealt += playerStats.DamageDealt;
            playerTeamPost.TeamStats.DamageTaken += playerStats.DamageTaken;
            playerTeamPost.TeamStats.HPRestored += playerStats.HPRestored;
            playerTeamPost.TeamStats.TimeOnCrystal += playerStats.TimeOnCrystal;
        }

        //add players who left match. omit ones with incomplete name or blacklisted name as a failsafe
        foreach(var introPlayer in _currentMatch.IntroPlayerInfo.Where(x => !x.Value.Alias.FullName.Contains('.') && !Regex.IsMatch(x.Value.Alias.Name, @"\d+") && !x.Value.Alias.HomeWorld.Equals("Unknown", StringComparison.OrdinalIgnoreCase))) {
            bool isFound = false;
            foreach(var team in _currentMatch.Teams) {
                foreach(var player in team.Value.Players) {
                    if(player.Alias.Equals(introPlayer.Value.Alias)) {
                        isFound = true;
                        break;
                    }
                }
                if(isFound) {
                    break;
                }
            }
            if(!isFound) {
                try {
                    _plugin.Log.Information($"Adding missing player {introPlayer.Value.Alias} to team list...");
                    _currentMatch.Teams[(CrystallineConflictTeamName)introPlayer.Value.Team].Players.Add(introPlayer.Value);
                } catch(Exception e) {
                    if(e is NullReferenceException || e is KeyNotFoundException) {
                        _plugin.Log.Error($"Unable to add to a team: {introPlayer.Key}");
                    } else {
                        throw;
                    }
                }
            }
        }

        //set result
        if(resultsPacket.Result != 1 && resultsPacket.Result != 2) {
            postMatch.MatchWinner = CrystallineConflictTeamName.Unknown;
        }
        if(_currentMatch.IsSpectated) {
            switch(resultsPacket.Result) {
                case 1:
                    postMatch.MatchWinner = CrystallineConflictTeamName.Astra;
                    break;
                case 2:
                    postMatch.MatchWinner = CrystallineConflictTeamName.Umbra;
                    break;
                default:
                    _plugin.Log.Warning($"Unable to determine winner...draw? {resultsPacket.Result}");
                    postMatch.MatchWinner = CrystallineConflictTeamName.Unknown;
                    break;
            }
        } else {
            switch(resultsPacket.Result) {
                case 1:
                    postMatch.MatchWinner = _currentMatch.LocalPlayerTeam!.TeamName;
                    break;
                case 2:
                    postMatch.MatchWinner = _currentMatch.Teams.First(x => x.Value.TeamName != _currentMatch.LocalPlayerTeam!.TeamName).Value.TeamName;
                    break;
                default:
                    _plugin.Log.Warning($"Unable to determine winner...draw? {resultsPacket.Result}");
                    postMatch.MatchWinner = CrystallineConflictTeamName.Unknown;
                    break;
            }
        }
        _currentMatch.MatchWinner = postMatch.MatchWinner;
        _currentMatch.PostMatch = postMatch;

        //totalized casts
        try {
            Dictionary<string, Dictionary<uint, uint>> casts = new();
            foreach(var cast in _casts ?? []) {
                casts.TryAdd(cast.Actor, new());
                if(!casts[cast.Actor].TryAdd(cast.ActionId, 1)) {
                    casts[cast.Actor][cast.ActionId]++;
                }
            }
            if(_currentMatchTimeline != null && _casts != null) {
                _currentMatchTimeline.TotalizedCasts = casts;
            }
            //foreach(var playerCasts in casts) {
            //    Plugin.Log2.Debug($"{playerCasts.Key}:");
            //    foreach(var actionCasts in playerCasts.Value) {
            //        var action = _plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(ClientLanguage.English).GetRow(actionCasts.Key);
            //        Plugin.Log2.Debug($"{action.Name}:{actionCasts.Value}");
            //    }
            //}
        } catch(Exception e) {
            Plugin.Log2.Error(e, "Error in totalizing casts");
        }

        _currentMatch.IsCompleted = true;
        _currentMatch.TimelineId = _currentMatchTimeline?.Id;
        return true;
    }

    private unsafe void OnFrameworkUpdate(IFramework framework) {
        if(!IsMatchInProgress()) {
            return;
        }
        var director = (CrystallineConflictContentDirector*)(IntPtr)EventFramework.Instance()->GetInstanceContentDirector();
        if(director == null) {
            return;
        }
        if(_currentMatch?.IsCompleted ?? true && (_plugin.Condition[ConditionFlag.BetweenAreas] || _plugin.Condition[ConditionFlag.BetweenAreas51])) {
            return;
        }

        var now = DateTime.Now;

#if DEBUG
        if(now - _lastPrint > TimeSpan.FromSeconds(30)) {
            _lastPrint = now;
            _plugin.Functions.CreateByteDump((nint)director, 0x10000, "CCICD");
            Plugin.Log2.Debug("creating cc content director dump");
        }
#endif
        //rematch detection
        //this won't work for matches where the crystal literally never moves
        if(_currentMatch!.IsCompleted && director->AstraProgress == 0 && director->UmbraProgress == 0) {
            Plugin.Log2.Information("Crystalline Conflict rematch detected.");
            _currentMatch = null;
            StartMatch();
            return;
        }

        if(_currentMatchTimeline != null) {

            //crystal position
            try {
                if(_currentMatchTimeline.CrystalPosition != null) {
                    var lastEvent = _currentMatchTimeline.CrystalPosition?.LastOrDefault();
                    int currentPosition = director->CrystalPosition;
                    if(lastEvent == null ||
                        (lastEvent.Points != currentPosition && now - lastEvent.Timestamp >= TimeSpan.FromSeconds(1))) {
                        _currentMatchTimeline.CrystalPosition?.Add(new(now, currentPosition));
                    }
                }
            } catch(Exception e) {
                Plugin.Log2.Error(e, $"Error in set crystal position");
            }

            //team progress
            try {
                foreach(var team in _currentMatchTimeline.TeamProgress ?? []) {
                    var lastEvent = team.Value.LastOrDefault();
                    var x = now - lastEvent?.Timestamp;
                    //rate limit to once a second
                    if(now - lastEvent?.Timestamp < TimeSpan.FromSeconds(1)) {
                        continue;
                    }
                    int? currentValue = null;
                    switch(team.Key) {
                        case CrystallineConflictTeamName.Astra:
                            currentValue = director->AstraProgress;
                            break;
                        case CrystallineConflictTeamName.Umbra:
                            currentValue = director->UmbraProgress;
                            break;
                        default:
                            break;
                    }
                    if(currentValue != null && (lastEvent == null || lastEvent.Points != currentValue)) {
                        team.Value.Add(new(now, (int)currentValue));
                    }
                }
            } catch(Exception e) {
                Plugin.Log2.Error(e, $"Error in set team progression");
            }

            //team mid progress
            try {
                foreach(var team in _currentMatchTimeline.TeamMidProgress ?? []) {
                    var lastEvent = team.Value.LastOrDefault();
                    var x = now - lastEvent?.Timestamp;
                    int? currentValue = null;
                    switch(team.Key) {
                        case CrystallineConflictTeamName.Astra:
                            currentValue = director->AstraMidpointProgress;
                            break;
                        case CrystallineConflictTeamName.Umbra:
                            currentValue = director->UmbraMidpointProgress;
                            break;
                        default:
                            break;
                    }
                    if(currentValue != null && (lastEvent == null || lastEvent.Points != currentValue)) {
                        team.Value.Add(new(now, (int)currentValue));
                    }
                }
            } catch(Exception e) {
                Plugin.Log2.Error(e, $"Error in set team mid progression");
            }

            //check for map event
            try {
                if(director->EventTimer != -1f && director->EventTimer <= 0f && _lastEventTimer > 0f) {
                    Plugin.Log2.Debug("Map event detected!");
                    _currentMatchTimeline.MapEvents?.Add(new(now, CrystallineConflictMatchEvent.SpecialEvent));
                }
                _lastEventTimer = director->EventTimer;
            } catch(Exception e) {
                Plugin.Log2.Error(e, $"Error in set map event");
            }
        }
    }
}
