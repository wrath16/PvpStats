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

    private delegate IntPtr CCDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    //48 89 5C 24 ?? 56 57 41 57 48 83 EC ?? 4C 89 74 24 
    //48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC ?? 4C 8B F1 E8
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC ?? 4C 8B F1 E8", DetourName = nameof(CCDirectorCtorDetour))]
    private readonly Hook<CCDirectorCtorDelegate> _ccDirectorCtorHook;
    [Signature("48 89 5C 24 ?? 56 57 41 57 48 83 EC ?? 4C 89 74 24 ", DetourName = nameof(CCDirectorCtor2Detour))]
    private readonly Hook<CCDirectorCtorDelegate> _ccDirectorCtor2Hook;

    private delegate void ProcessPacketActorControlDelegate(uint entityId, uint type, uint statusId, uint amount, uint a5, uint source, uint a7, uint a8, ulong a9, byte flag);
    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private readonly Hook<ProcessPacketActorControlDelegate> _processPacketActorControlHook = null!;

    private unsafe delegate void ProcessPacketActionEffectDelegate(uint entityId, IntPtr sourceCharacter, IntPtr pos, ActionEffectHeader* effectHeader, ActionEffect* effectArray, ulong* effectTrail);
    [Signature("40 55 53 56 41 54 41 55 41 56 41 57 48 8D AC 24 60 FF FF FF 48 81 EC A0 01 00 00", DetourName = nameof(ProcessPacketActionEffectDetour))]
    private readonly Hook<ProcessPacketActionEffectDelegate> _processPacketActionEffectHook = null!;

    private delegate void ProcessKillDelegate(IntPtr agent, IntPtr killerPlayer, uint killStreak, IntPtr victimPlayer, uint localPlayerTeam);
    [Signature("40 55 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 48 8B 01", DetourName = nameof(ProcessKillDetour))]
    private readonly Hook<ProcessKillDelegate> _processKillHook;

    private static readonly Regex TierRegex = new(@"\D+", RegexOptions.IgnoreCase);
    private static readonly Regex RiserRegex = new(@"\d+", RegexOptions.IgnoreCase);

    public CrystallineConflictMatchManager(Plugin plugin) {
        _plugin = plugin;

        _plugin.Framework.Update += OnFrameworkUpdate;
        _plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSIntroduction", OnPvPIntro);
        //_plugin.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "PvPMKSBattleLog", OnBattleLog);
        _plugin.InteropProvider.InitializeFromAttributes(this);
        _plugin.Log.Debug($"cc director .ctor address: 0x{_ccDirectorCtorHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"cc director .ctor 2 address: 0x{_ccDirectorCtor2Hook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"cc match end 1 address: 0x{_ccMatchEndHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"cc match end 2 address: 0x{_ccMatchEndSpectatorHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"cc process kill address: 0x{_processKillHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"process actor control address: 0x{_processPacketActorControlHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"process action effect address: 0x{_processPacketActionEffectHook!.Address.ToString("X2")}");

        //_ccDirectorCtorHook.Enable();
        //_ccDirectorCtor2Hook.Enable();
        _ccMatchEndHook.Enable();
#if DEBUG
        _ccMatchEndSpectatorHook.Enable();
#endif
        _processKillHook.Enable();
        _processPacketActorControlHook.Enable();
        _processPacketActionEffectHook.Enable();
    }

    public void Dispose() {
        _plugin.Framework.Update -= OnFrameworkUpdate;
        _plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _plugin.AddonLifecycle.UnregisterListener(OnPvPIntro);
        //_plugin.AddonLifecycle.UnregisterListener(OnBattleLog);
        _ccMatchEndHook.Dispose();
        _ccMatchEndSpectatorHook.Dispose();
        _ccDirectorCtorHook.Dispose();
        _ccDirectorCtor2Hook.Dispose();
        _processKillHook.Dispose();
        _processPacketActorControlHook.Dispose();
        _processPacketActionEffectHook.Dispose();
    }

    private IntPtr CCDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3) {
        _plugin.Log.Debug("CC Director .ctor occurred!");
        try {
            StartMatch();
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            _plugin.Log.Error(e, $"Error in CC director ctor");
        }
        return _ccDirectorCtorHook.Original(p1, p2, p3);
    }

    private IntPtr CCDirectorCtor2Detour(IntPtr p1, IntPtr p2, IntPtr p3) {
        _plugin.Log.Debug("CC Director .ctor 2 occurred!");
        try {
            StartMatch();
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            _plugin.Log.Error(e, $"Error in CC director ctor 2");
        }
        return _ccDirectorCtor2Hook.Original(p1, p2, p3);
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
            };
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
            if(!IsMatchInProgress() || _currentMatchTimeline == null) {
                return;
            }
            var now = DateTime.Now;
            if(type == (uint)ActorControlCategory.Death && _currentMatchTimeline.Kills != null) {
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
            }
        } finally {
            _processPacketActorControlHook.Original(sourceEntityId, type, statusId, amount, a5, source, a7, a8, targetEntityId, flag);
        }
    }

    private unsafe void ProcessPacketActionEffectDetour(uint entityId, IntPtr sourceCharacter, IntPtr pos, ActionEffectHeader* effectHeader, ActionEffect* effectArray, ulong* effectTrail) {
        try {
            if(!IsMatchInProgress()) {
                return;
            }
            var now = DateTime.Now;
            var actionId = effectHeader->ActionAnimationId;
            uint targets = effectHeader->EffectCount;
            uint? nameIdActor = null;
            if(CombatHelper.IsLimitBreak(actionId)) {
                var actor = _plugin.ObjectTable.SearchByEntityId(entityId);
                if(actor is not IBattleChara) return;
                if(actor is IBattleNpc) {
                    //attempt to retrieve owner in case of pet (summoner)
                    var owner = _plugin.ObjectTable.SearchByEntityId(actor?.OwnerId ?? 0);
                    if(owner is not IPlayerCharacter) {
                        Plugin.Log2.Warning("Limit break cast by non-player character");
                        return;
                    } else {
                        nameIdActor = (actor as IBattleNpc)!.NameId;
                        ////add lookup
                        //if(!_currentMatchTimeline?.BNPCNameLookup?.ContainsKey((uint)nameIdActor) ?? false) {
                        //    var bnpcName = _plugin.DataManager.GetExcelSheet<BNpcName>(ClientLanguage.English).GetRow((uint)nameIdActor);
                        //    _currentMatchTimeline!.BNPCNameLookup!.Add((uint)nameIdActor, (bnpcName.Singular.ToString(), bnpcName.Article));
                        //}
                        actor = owner;
                    }
                }
                var actorWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((actor as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
                var alias = (PlayerAlias)$"{(actor as IPlayerCharacter).Name} {actorWorld}";
                var spell = _plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(ClientLanguage.English).GetRow(actionId);
                Plugin.Log2.Debug($"{actor?.Name} cast {spell.Name} {actionId} targets: {targets} display: {effectHeader->EffectDisplayType} " +
                $"hidden anim: {effectHeader->HiddenAnimation} counter: {effectHeader->GlobalEffectCounter} rotation: {effectHeader->Rotation} variation: {effectHeader->Variation}");

                ////add lookup
                //if(!_currentMatchTimeline?.ActionIdLookup?.ContainsKey(spell.RowId) ?? false) {
                //    _currentMatchTimeline!.ActionIdLookup!.Add(spell.RowId, spell.Name.ToString());
                //}

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
                    //logging purposes!
                    Plugin.Log2.Debug($"{target.Name} HP: {playerSnapshot.CurrentHP}/{playerSnapshot.MaxHP} MP: {playerSnapshot.CurrentMP}/{playerSnapshot.MaxMP} shields: {playerSnapshot.ShieldPercents}");
                    string statuses = "";
                    foreach(var status in playerSnapshot.Statuses) {
                        var data = _plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRow(status.StatusId);
                        statuses += $"{data.Name}:{status.StatusId}:{status.Param}:{status.RemainingTime} ";
                    }
                    Plugin.Log2.Debug(statuses);

                    if(target is IPlayerCharacter) {
                        var targetWorld = _plugin.DataManager.GetExcelSheet<World>().GetRow((target as IPlayerCharacter).HomeWorld.RowId).Name.ToString();
                        var targetAlias = (PlayerAlias)$"{(target as IPlayerCharacter).Name} {targetWorld}";
                        actionEvent.Snapshots.TryAdd(targetAlias, playerSnapshot);
                        actionEvent.PlayerTargets.Add(targetAlias);
                    } else if(target is IBattleNpc) {
                        var npcTarget = target as IBattleNpc;
                        var bnpcName = _plugin.DataManager.GetExcelSheet<BNpcName>(ClientLanguage.English).GetRow(npcTarget.NameId);
                        actionEvent.NameIdTargets.Add(npcTarget.NameId);
                        ////add lookup
                        //if(!_currentMatchTimeline?.BNPCNameLookup?.ContainsKey(npcTarget.NameId) ?? false) {
                        //    _currentMatchTimeline!.BNPCNameLookup!.Add(npcTarget.NameId, (bnpcName.Singular.ToString(), bnpcName.Article));
                        //}
                    } else {
                        Plugin.Log2.Warning($"{spell.Name} cast on unknown entity {target?.Name}");
                        continue;
                    }
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

        CrystallineConflictTeam team = new();
        unsafe {
            var addon = (AtkUnitBase*)args.Addon;
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
                    _currentMatch!.NeedsPlayerNameValidation = true;
                    foreach(IPlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
                        //_plugin.Log.Debug($"name: {pc.Name} homeworld {pc.HomeWorld.GameData.Name.ToString()} job: {pc.ClassJob.GameData.NameEnglish}");
                        bool homeWorldMatch = worldRaw.Equals(pc.HomeWorld.Value.Name.ToString(), StringComparison.OrdinalIgnoreCase);
                        bool jobMatch = pc.ClassJob.Value.NameEnglish.ToString().Equals(translatedJob, StringComparison.OrdinalIgnoreCase);
                        bool nameMatch = PlayerJobHelper.IsAbbreviatedAliasMatch(playerRaw, pc.Name.ToString());
                        //_plugin.Log.Debug($"homeworld match:{homeWorldMatch} jobMatch:{jobMatch} nameMatch: {nameMatch}");
                        if(homeWorldMatch && jobMatch && nameMatch) {
                            _plugin.Log.Debug($"validated player: {playerRaw} is {pc.Name.ToString()}");
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

    public bool IsMatchInProgress() {
        return _currentMatch != null;
    }

    //returns true if successfully processed
    private bool ProcessMatchResults(CrystallineConflictResultsPacket resultsPacket) {
        if(!IsMatchInProgress()) {
            _plugin.Log.Error("trying to process match results on no match!");
            return false;
            //fallback for case where you load into a game after the match has completed creating a new match
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
