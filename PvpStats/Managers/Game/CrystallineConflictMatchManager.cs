using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.Sheets;
using PvpStats.Helpers;
using PvpStats.Services;
using PvpStats.Types.Action;
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
    private bool _scoreboardPayloadReceived;
    private float _lastEventTimer;
    public Dictionary<uint, Dictionary<ushort, HashSet<uint>>>? _castTargets;
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
    [Signature("40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 4C 8B E1", DetourName = nameof(CCMatchEndSpectatorDetour))]
    private readonly Hook<CCMatchEnd101Delegate> _ccMatchEndSpectatorHook;

    private delegate void ProcessPacketActorControlDelegate(uint entityId, uint type, uint statusId, uint amount, uint a5, uint source, uint a7, uint a8, uint a10, uint a9, ulong targetEntityId, byte flag);
    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private readonly Hook<ProcessPacketActorControlDelegate> _processPacketActorControlHook = null!;

    private unsafe delegate void ProcessPacketActionEffectDelegate(uint entityId, IntPtr sourceCharacter, IntPtr pos, ActionEffectHeader* effectHeader, ActionEffect* effectArray, ulong* effectTrail);
    [Signature("40 55 56 57 41 54 41 55 41 56 41 57 48 8D AC 24", DetourName = nameof(ProcessPacketActionEffectDetour))]
    private readonly Hook<ProcessPacketActionEffectDelegate> _processPacketActionEffectHook = null!;

    private delegate void ProcessKillDelegate(IntPtr agent, IntPtr killerPlayer, uint killStreak, IntPtr victimPlayer, uint localPlayerTeam);
    [Signature("40 55 53 41 54 41 55 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 48 8B 01", DetourName = nameof(ProcessKillDetour))]
    private readonly Hook<ProcessKillDelegate> _processKillHook;

    private delegate void ProcessHoTDoTDelegate(IntPtr p1, IntPtr p2, uint p3, uint p4, int p5, uint p6, int p7);
    [Signature("48 8B C4 48 89 58 ?? 48 89 68 ?? 48 89 70 ?? 57 41 54 41 56 48 83 EC ?? 4C 89 78", DetourName = nameof(ProcessHoTDoTDetour))]
    private readonly Hook<ProcessHoTDoTDelegate> _processHoTDoTHook;

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
        _processHoTDoTHook.Enable();
    }

    public void Dispose() {
        _plugin.Framework.Update -= OnFrameworkUpdate;
        _plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _ccMatchEndHook.Dispose();
        _ccMatchEndSpectatorHook.Dispose();
        _processKillHook.Dispose();
        _processPacketActorControlHook.Dispose();
        _processPacketActionEffectHook.Dispose();
        _processHoTDoTHook.Dispose();
    }

    public bool IsMatchInProgress() {
        return _currentMatch != null;
    }

    private void StartMatch() {
        var dutyId = _plugin.GameState.GetCurrentDutyId();
        var territoryId = _plugin.ClientState.TerritoryType;

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
                NameIdTargetedActionAnalytics = new(),
                PlayerTargetedActionAnalytics = new(),
                PlayerMedkitAnalytics = new(),
                PlayerTargetedStatusAnalytics = new(),
                NameIdTargetedStatusAnalytics = new(),
#endif
            };
            _castTargets = [];
            _lastEventTimer = -1f;
            _scoreboardPayloadReceived = false;
            _plugin.Log.Information($"starting new match on {_currentMatch.Arena}");
            _plugin.DataQueue.QueueDataOperation(async () => {
                await _plugin.CCCache.AddMatch(_currentMatch);
                await _plugin.Storage.AddCCTimeline(_currentMatchTimeline);
            });
        });
    }

    private unsafe void ProcessKillDetour(IntPtr agent, IntPtr killerPlayer, uint killStreak, IntPtr victimPlayer, uint localPlayerTeam) {
        try {
            var now = DateTime.UtcNow;
            Plugin.Log2.Debug($"kill feed detour occurred. killer: 0x{((CCPlayer*)killerPlayer)->EntityId:X2} kills: {killStreak} victim: 0x{((CCPlayer*)victimPlayer)->EntityId:X2} localPlayerTeam: {localPlayerTeam}");

            if(IsMatchInProgress() && _currentMatchTimeline != null && _currentMatchTimeline.Kills != null) {
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

    private void ProcessPacketActorControlDetour(uint sourceEntityId, uint type, uint statusId, uint amount, uint effectSourceEntityId, uint source, uint a7, uint a8, uint a9, uint a10, ulong targetEntityId, byte flag) {
        try {
            if(!_plugin.DebugMode && (!IsMatchInProgress() || _currentMatchTimeline == null)) {
                return;
            }
            if(_plugin.DebugMode) {
                Plugin.Log2.Debug($"actor control: 0x{type:X2} {(ActorControlCategory)type} 0x{sourceEntityId:X2} StatusId: 0x{statusId:X2} Source: {source} Amount: {amount} a5: 0x{effectSourceEntityId:X2} a7: {a7} a8: {a8} a9: 0x{a9:X2} a10: 0x{a10:X2} targetEntityId: 0x{targetEntityId:X2} flag: {flag}");
            }
            //Plugin.Log2.Debug($"actor control: 0x{type:X2} {(ActorControlCategory)type} 0x{sourceEntityId:X2} StatusId: 0x{statusId:X2} Source: {source} Amount: {amount} a5: {a5} a7: {a7} a8: {a8} a9: {targetEntityId} flag: {flag}");

            var now = DateTime.UtcNow;
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
                }
            } else if(type == (uint)ActorControlCategory.CCPot) {
                var player = _plugin.ObjectTable.SearchByEntityId(sourceEntityId);

                Plugin.Log2.Debug($"Potion detected: 0x{sourceEntityId:X2} {player?.Name ?? ""} 0x{type:X2} {(ActorControlCategory)type} StatusId: 0x{statusId:X2} Source: {source} Amount: {amount} a5: {effectSourceEntityId} a7: {a7} a8: {a8} a9: {targetEntityId} flag: {flag}");
                if(amount != 30000) {
                    Plugin.Log2.Warning($"Non-standard potion amount: {amount}");
                }
                var world = _plugin.DataManager.GetExcelSheet<World>().GetRow((player as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
                var alias = (PlayerAlias)$"{player.Name} {world}";

                ActionAnalytics actionAnalytics = new() {
                    Impacts = 1,
                    Heal = amount
                };

                if(_currentMatchTimeline?.PlayerMedkitAnalytics != null && !_scoreboardPayloadReceived) {
                    if(!_currentMatchTimeline.PlayerMedkitAnalytics.TryAdd(alias, actionAnalytics)) {
                        _currentMatchTimeline.PlayerMedkitAnalytics[alias] += actionAnalytics;
                    }
                }
            }
        } finally {
            _processPacketActorControlHook.Original(sourceEntityId, type, statusId, amount, effectSourceEntityId, source, a7, a8, a9, a10, targetEntityId, flag);
        }
    }

    private unsafe void ProcessPacketActionEffectDetour(uint entityId, IntPtr sourceCharacter, IntPtr pos, ActionEffectHeader* effectHeader, ActionEffect* effectArray, ulong* effectTrail) {
        try {
            if(!_plugin.DebugMode && !IsMatchInProgress()) {
                return;
            }
            var director = (CrystallineConflictContentDirector*)EventFramework.Instance()->GetInstanceContentDirector();
            var now = DateTime.UtcNow;
            var actionId = effectHeader->ActionAnimationId;
            uint targets = effectHeader->EffectCount;
            uint? actorNameId = null;
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
                actorNameId = (actor as IBattleNpc)!.NameId;
                if(owner is IPlayerCharacter) {
                    actor = owner;
                    isPet = true;
                }
            }

            string? actorWorld = null;
            PlayerAlias? actorAlias = null;
            ActionEvent? actionEvent = null;
            if(actor is IPlayerCharacter) {
                actorWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((actor as IPlayerCharacter)!.HomeWorld.RowId).Name.ToString();
                actorAlias = (PlayerAlias)$"{(actor as IPlayerCharacter)!.Name} {actorWorld}";
                actionEvent = new(now, actionId, actorAlias) {
                    Variation = effectHeader->Variation,
                    NameIdActor = actorNameId,
                    Snapshots = new() {
                        {actorAlias, new(actor as IBattleChara) }
                    }
                };
            }

            TargetedActionAnalytics targetedActionAnalytics = new() {
                Casts = effectHeader->Variation == 0 ? 1 : 0,
            };
            Dictionary<string, ActionAnalytics> playerActionAnalytics = new();
            Dictionary<uint, ActionAnalytics> nameIdActionAnalytics = new();
            HashSet<uint>? targetEntityIds = null;
            if(_castTargets != null) {
                if(!_castTargets.TryGetValue(actor.EntityId, out var actionMap)) {
                    actionMap = new Dictionary<ushort, HashSet<uint>>();
                    _castTargets[actor.EntityId] = actionMap;
                }

                if(!actionMap.TryGetValue(actionId, out targetEntityIds)) {
                    targetEntityIds = new HashSet<uint>();
                    actionMap[actionId] = targetEntityIds;
                } else if(effectHeader->Variation == 0) {
                    targetEntityIds.Clear();
                }
            }

            for(var i = 0; i < targets; i++) {
                var actionTargetId = (uint)(effectTrail[i] & uint.MaxValue);
                var target = _plugin.ObjectTable.SearchByEntityId(actionTargetId);
                if(target is not IBattleChara || (target as IBattleChara) is null) continue;
                string? targetWorld = null;
                PlayerAlias? targetAlias = null;
                var targetSnapshot = new BattleCharaSnapshot(target as IBattleChara);

                //setup action analytics
                ActionAnalytics? actionAnalytics = null;
                if(target is IPlayerCharacter) {
                    targetWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((target as IPlayerCharacter)!.HomeWorld.RowId).Name.ToString();
                    targetAlias = (PlayerAlias)$"{(target as IPlayerCharacter)!.Name} {targetWorld}";
                    if(!playerActionAnalytics.TryGetValue(targetAlias, out actionAnalytics)) {
                        actionAnalytics = new();
                        playerActionAnalytics.TryAdd(targetAlias, actionAnalytics);
                    }
                } else if(target is IBattleNpc) {
                    if(!nameIdActionAnalytics.TryGetValue((target as IBattleNpc)!.NameId, out actionAnalytics)) {
                        actionAnalytics = new();
                        nameIdActionAnalytics.TryAdd((target as IBattleNpc)!.NameId, actionAnalytics);
                    }
                }

                //add unique target
                if(targetEntityIds?.Add(target.EntityId) ?? false) {
                    targetedActionAnalytics.Targets++;
                    actionAnalytics.Impacts = 1;
                }

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
                    //kek!
                    uint amount = actionEffect.Value;
                    if((actionEffect.Flags2 & 0x40) == 0x40) {
                        amount += (uint)actionEffect.Flags1 << 16;
                    }
                    //check for reflected effects onto the caster: chiten, lifesteal, etc.
                    bool isReflected = (actionEffect.Flags2 & 0x80) == 0x80;
                    ActionAnalytics? reflectedActionAnalytics = null;
                    if(isReflected) {
                        if(actorAlias != null) {
                            if(!playerActionAnalytics.TryGetValue(actorAlias, out reflectedActionAnalytics)) {
                                reflectedActionAnalytics = new() {
                                    Impacts = 1
                                };
                                playerActionAnalytics.TryAdd(actorAlias, reflectedActionAnalytics);
                            }
                        } else if(actorNameId != null) {
                            if(!nameIdActionAnalytics.TryGetValue((uint)actorNameId, out reflectedActionAnalytics)) {
                                reflectedActionAnalytics = new() {
                                    Impacts = 1
                                };
                                nameIdActionAnalytics.TryAdd((uint)actorNameId, reflectedActionAnalytics);
                            }
                        }
                    }
                    if(_plugin.DebugMode) {
                        Plugin.Log2.Debug($"{target?.Name} was hit by {actionEffect.EffectType} p0: {actionEffect.Param0} p1: {actionEffect.Param1} p2: {actionEffect.Param2} " +
                        $"f1: {actionEffect.Flags1} f2: {actionEffect.Flags2} value: {actionEffect.Value} amount: {amount}");
                    }
                    if(actionEffect.EffectType == ActionEffectType.Damage
                        || actionEffect.EffectType == ActionEffectType.BlockedDamage
                        || actionEffect.EffectType == ActionEffectType.ParriedDamage) {
                        if(isReflected) {
                            reflectedActionAnalytics!.Damage += amount;
                        } else {
                            actionAnalytics.Damage += amount;
                        }
                    } else if(actionEffect.EffectType == ActionEffectType.Heal) {
                        if(isReflected) {
                            reflectedActionAnalytics!.Heal += amount;
                            ////sacred claim exempt damage
                            //if((target as IBattleChara).StatusList.Any(x => x.StatusId == 3025) && amount == 3000) {

                            //}
                        } else {
                            actionAnalytics.Heal += amount;
                        }
                    } else if(actionEffect.EffectType == ActionEffectType.MpGain) {
                        if(isReflected) {
                            reflectedActionAnalytics!.MPGain += amount;
                        } else {
                            actionAnalytics.MPGain += amount;
                        }
                    } else if(actionEffect.EffectType == ActionEffectType.MpLoss) {
                        if(isReflected) {
                            reflectedActionAnalytics!.MPDrain += amount;
                        } else {
                            actionAnalytics.MPDrain += amount;
                        }
                    } else if(actionEffect.EffectType == ActionEffectType.ApplyStatusEffectTarget) {
                        if(!isReflected) {
                            actionAnalytics.StatusHits++;
                        }
                    } else if(actionEffect.EffectType == ActionEffectType.StatusNoEffect
                        || actionEffect.EffectType == ActionEffectType.FullResistStatus) {
                        if(!isReflected) {
                            actionAnalytics.StatusMisses++;
                        }
                    }
                }

                //snapshots for limit breaks
                if(actionEvent != null) {
                    if(targetAlias != null) {
                        actionEvent.Snapshots?.TryAdd(targetAlias, targetSnapshot);
                        actionEvent.PlayerTargets.Add(targetAlias);
                    } else if(target is IBattleNpc) {
                        var npcTarget = target as IBattleNpc;
                        //var bnpcName = _plugin.DataManager.GetExcelSheet<BNpcName>(ClientLanguage.English).GetRow(npcTarget.NameId);
                        actionEvent.NameIdTargets.Add(npcTarget.NameId);
                    } else {
                        Plugin.Log2.Warning($"{spell.Name} cast on unknown entity {target?.Name}");
                        continue;
                    }
                }
            }
            targetedActionAnalytics.PlayerAnalytics = playerActionAnalytics;
            targetedActionAnalytics.NameIdAnalytics = nameIdActionAnalytics;

            if(_currentMatchTimeline != null && !_scoreboardPayloadReceived) {
                if(actorAlias != null && _currentMatchTimeline.PlayerTargetedActionAnalytics != null) {
                    _currentMatchTimeline.PlayerTargetedActionAnalytics?.TryAdd(actorAlias, new());
                    _currentMatchTimeline.PlayerTargetedActionAnalytics?[actorAlias].TryAdd(actionId, new());
                    _currentMatchTimeline.PlayerTargetedActionAnalytics![actorAlias][actionId] += targetedActionAnalytics;
                } else if(actorNameId != null && _currentMatchTimeline.NameIdTargetedActionAnalytics != null) {
                    _currentMatchTimeline.NameIdTargetedActionAnalytics?.TryAdd((uint)actorNameId, new());
                    _currentMatchTimeline.NameIdTargetedActionAnalytics?[(uint)actorNameId].TryAdd(actionId, new());
                    _currentMatchTimeline.NameIdTargetedActionAnalytics![(uint)actorNameId][actionId] += targetedActionAnalytics;
                }
            }

            if(CombatHelper.IsLimitBreak(actionId) && actionEvent != null) {
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

    private unsafe void ProcessHoTDoTDetour(IntPtr targetStatusManager, IntPtr targetObj, uint statusId, uint enum1, int amount, uint sourceEntityId, int enum2) {
        try {
            if(!_plugin.DebugMode && !IsMatchInProgress()) {
                return;
            }
            uint? sourceNameId = null;
            var source = _plugin.ObjectTable.SearchByEntityId(sourceEntityId);
            var target = _plugin.ObjectTable.CreateObjectReference(targetObj);
            var status = _plugin.DataManager.GetExcelSheet<Status>().GetRow(statusId);
            if(source is IBattleNpc) {
                //attempt to retrieve owner in case of pet (summoner)
                var owner = _plugin.ObjectTable.SearchByEntityId(source?.OwnerId ?? 0);
                sourceNameId = (source as IBattleNpc)!.NameId;
                if(owner is IPlayerCharacter) {
                    source = owner;
                }
            }
            string? actorWorld = null;
            PlayerAlias? actorAlias = null;
            ActionEvent? actionEvent = null;
            if(source is IPlayerCharacter) {
                actorWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((source as IPlayerCharacter)!.HomeWorld.RowId).Name.ToString();
                actorAlias = (PlayerAlias)$"{(source as IPlayerCharacter)!.Name} {actorWorld}";
            }

            //p3 = 1306 for sole survivor hp+mp, 3111 for haimatinon, 3104 for microcosmos
            //enum1: 4 = heal, 3 = damage, 11 = mp gain
            //enum2: 0 = beneficial, -1 = detrimental
            if(_plugin.DebugMode) {
                Plugin.Log2.Debug($"hot/dot detected! p1: 0x{targetStatusManager:X2} p2: 0x{targetObj:X2}, p3: {statusId}, p4: {enum1}, p5: {amount}, p6: 0x{sourceEntityId:X2}, p7: {enum2}");
                var effect = enum1 switch {
                    3 => "DoT",
                    4 => "HoT",
                    5 => "MP",
                    _ => "???"
                };
                Plugin.Log2.Debug($"{source?.Name} to {target?.Name} {effect} {status.Name} {amount}");
            }

            if(statusId != 0) {
                TargetedActionAnalytics targetedActionAnalytics = new() {
                    Casts = 1,
                    Targets = 1,
                };
                ActionAnalytics actionAnalytics = new() {
                    Impacts = 1
                };
                switch(enum1) {
                    case 0x3:
                        actionAnalytics.Damage = amount;
                        break;
                    case 0x4:
                        actionAnalytics.Heal = amount;
                        break;
                    case 0xB:
                        actionAnalytics.MPGain = amount;
                        break;
                }

                if(target is IPlayerCharacter) {
                    var targetWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((target as IPlayerCharacter)!.HomeWorld.RowId).Name.ToString();
                    var targetAlias = (PlayerAlias)$"{(target as IPlayerCharacter)!.Name} {targetWorld}";
                    targetedActionAnalytics.PlayerAnalytics.Add(targetAlias, actionAnalytics);
                } else if(target is IBattleNpc) {
                    targetedActionAnalytics.NameIdAnalytics.Add((target as IBattleNpc)!.NameId, actionAnalytics);
                }

                if(_currentMatchTimeline != null && !_scoreboardPayloadReceived) {
                    //if(actorAlias != null && _currentMatchTimeline.PlayerTargetedStatusAnalytics != null) {
                    //    _currentMatchTimeline.PlayerTargetedStatusAnalytics?.TryAdd(actorAlias, new());
                    //    _currentMatchTimeline.PlayerTargetedStatusAnalytics?[actorAlias].TryAdd(statusId, new());
                    //    _currentMatchTimeline.PlayerTargetedStatusAnalytics![actorAlias][statusId] += targetedActionAnalytics;
                    //} else if(sourceNameId != null && _currentMatchTimeline.NameIdTargetedStatusAnalytics != null) {
                    //    _currentMatchTimeline.NameIdTargetedStatusAnalytics?.TryAdd((uint)sourceNameId, new());
                    //    _currentMatchTimeline.NameIdTargetedStatusAnalytics?[(uint)sourceNameId].TryAdd(statusId, new());
                    //    _currentMatchTimeline.NameIdTargetedStatusAnalytics![(uint)sourceNameId][statusId] += targetedActionAnalytics;
                    //}
                    if(actorAlias != null && _currentMatchTimeline.PlayerTargetedStatusAnalytics != null) {
                        var dict = _currentMatchTimeline.PlayerTargetedStatusAnalytics;
                        if(!dict.TryGetValue(actorAlias, out var statusDict)) {
                            statusDict = [];
                            dict[actorAlias] = statusDict;
                        }
                        if(!statusDict.TryGetValue(statusId, out var analytics)) {
                            analytics = new TargetedActionAnalytics();
                            statusDict[statusId] = analytics;
                        }
                        statusDict[statusId] += targetedActionAnalytics;
                    } else if(sourceNameId != null && _currentMatchTimeline.NameIdTargetedStatusAnalytics != null) {
                        var dict = _currentMatchTimeline.NameIdTargetedStatusAnalytics;
                        uint key = (uint)sourceNameId;
                        if(!dict.TryGetValue(key, out var statusDict)) {
                            statusDict = [];
                            dict[key] = statusDict;
                        }
                        if(!statusDict.TryGetValue(statusId, out var analytics)) {
                            analytics = new TargetedActionAnalytics();
                            statusDict[statusId] = analytics;
                        }
                        statusDict[statusId] += targetedActionAnalytics;
                    }
                }
            }

        } finally {
            _processHoTDoTHook.Original(targetStatusManager, targetObj, statusId, enum1, amount, sourceEntityId, enum2);
        }
    }

    private void CCMatchEnd101Detour(IntPtr p1, IntPtr p2, IntPtr p3, uint p4) {
        _plugin.Log.Debug("Match end detour occurred.");
#if DEBUG
        _plugin.Functions.CreateByteDump(p2, 0x400, "cc_match_results");
#endif
        _scoreboardPayloadReceived = true;

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
        _scoreboardPayloadReceived = true;

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
        if(MatchHelper.CrystallineConflictMapLookup.ContainsKey(territoryId)) {
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

    //returns true if successfully processed
    private bool ProcessMatchResults(CrystallineConflictResultsPacket resultsPacket) {
        if(!IsMatchInProgress()) {
            _plugin.Log.Error("trying to process match results on no match!");
            return false;
            //fallback for case where you load into a game after the match has completed creating a new match
            //this will trigger if you load in after a disconnect as the match as ending...
        } else if((DateTime.UtcNow - _currentMatch!.DutyStartTime).TotalSeconds < 10) {
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
        _currentMatch.MatchEndTime = DateTime.UtcNow;
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
                playerStats.Player = (PlayerAlias)$"{MemoryService.ReadString(player.PlayerName, 32)} {_plugin.DataManager.GetExcelSheet<World>()?.GetRow(player.WorldId).Name}";
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
            if(_currentMatch.IntroPlayerInfo?.ContainsKey(newPlayer.Alias) ?? false) {
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
        foreach(var introPlayer in _currentMatch.IntroPlayerInfo?.Where(x => !x.Value.Alias.FullName.Contains('.') && !Regex.IsMatch(x.Value.Alias.Name, @"\d+") && !x.Value.Alias.HomeWorld.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) ?? []) {
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

        _currentMatch.IsCompleted = true;
        _currentMatch.TimelineId = _currentMatchTimeline?.Id;
        return true;
    }

    private unsafe void OnFrameworkUpdate(IFramework framework) {
        if(!IsMatchInProgress()) {
            return;
        }
        var director = (CrystallineConflictContentDirector*)EventFramework.Instance()->GetInstanceContentDirector();
        if(director == null) {
            return;
        }
        if(_currentMatch?.IsCompleted ?? true && (_plugin.Condition[ConditionFlag.BetweenAreas] || _plugin.Condition[ConditionFlag.BetweenAreas51])) {
            return;
        }

        var now = DateTime.UtcNow;

#if DEBUG
        if(now - _lastPrint > TimeSpan.FromSeconds(30)) {
            _lastPrint = now;
            //var x = new IntPtr((nint*)director + 0x408);
            //var y = new IntPtr(*(nint*)x);
            //Plugin.Log2.Debug($"creating cc content director dump director: 0x{new IntPtr(director):X2} 0x{x:X2} 0x{y:X2}");
            _plugin.Functions.CreateByteDump((nint)director, 0x10000, "CCICD");
            //var x = (nint*)((nint)director + 0x408);
            //var y = (nint*)*x;
            //var z = *y;
            ////var a = *(nint*)*(nint*)((nint)director + 0x408);
            //_plugin.Functions.CreateByteDump(y, 0x10000, "CCICD_PLAYERS");
            //_plugin.Functions.CreateByteDump(y, 0x10000, "CCICD_PLAYERS");
        }
        //return;
#endif

        //rematch detection
        //this won't work for matches where the crystal literally never moves
        if(_currentMatch!.IsCompleted && director->AstraProgress == 0 && director->UmbraProgress == 0) {
            Plugin.Log2.Information("Crystalline Conflict rematch detected.");
            _currentMatch = null;
            StartMatch();
            return;
        }

        //get player intro info
        //wait for data to be initialized, with small failsafe period
        if(_currentMatch.IntroPlayerInfo == null && director->Players[0].EntityId != 0 && (now - _currentMatch.DutyStartTime) > TimeSpan.FromSeconds(5)) {
            Plugin.Log2.Information("Setting intro info...");
            _currentMatch.IntroPlayerInfo = new();
            for(int i = 0; i < 10; i++) {
                var player = director->Players[i];
                if(player.EntityId == 0) continue;
                var name = MemoryService.ReadString(player.Name, 64) ?? "";
                var world = _plugin.DataManager.GetExcelSheet<World>()?.GetRow(player.WorldId).Name.ToString() ?? "";
                var alias = new PlayerAlias(name, world);
                var team = (CrystallineConflictTeamName)(player.Team + 1);
                var job = PlayerJobHelper.GetJobFromName(_plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(player.ClassJobId).NameEnglish.ToString() ?? "");
                var rank = new PlayerRank((ArenaTier)player.ColosseumMatchRankId, player.Riser);

                _currentMatch.IntroPlayerInfo.TryAdd(alias, new() {
                    Alias = alias,
                    Job = job,
                    ClassJobId = player.ClassJobId,
                    Team = team,
                    Rank = rank,
                });
            }
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
