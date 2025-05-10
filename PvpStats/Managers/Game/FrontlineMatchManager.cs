using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
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
using System.Reflection;
using System.Threading.Tasks;

namespace PvpStats.Managers.Game;
internal class FrontlineMatchManager : MatchManager<FrontlineMatch> {

    //protected FrontlineMatch? CurrentMatch { get; set; }

    private DateTime _lastUpdate;
    private DateTime _lastPrint = DateTime.MinValue;

    private FrontlineMatchTimeline? _currentMatchTimeline;

    private Dictionary<PlayerAlias, int> _maxObservedBattleHigh = [];

    private static readonly Dictionary<int, int> BattleHighStatuses = new() {
        { 1, 2131 },
        { 2, 2132 },
        { 3, 2133 },
        { 4, 2134 },
        { 5, 2135 },
    };

    //fl director ctor
    private delegate IntPtr FLDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B F9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? BA", DetourName = nameof(FLDirectorCtorDetour))]
    private readonly Hook<FLDirectorCtorDelegate> _flDirectorCtorHook;

    //p1 = director
    //p2 = data packet
    private delegate void FLMatchEnd10Delegate(IntPtr p1, IntPtr p2);
    //4C 8B DC 55 53 56 41 56 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05
    //4C 8B DC 55 56 57 41 55 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 
    [Signature("4C 8B DC 55 56 57 41 55 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ", DetourName = nameof(FLMatchEnd10Detour))]
    private readonly Hook<FLMatchEnd10Delegate> _flMatchEndHook;

    //p1 = data packet
    private delegate void FlPlayerPayload10Delegate(IntPtr p1);
    //40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0 
    //40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0 74 
    [Signature("40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0 74", DetourName = nameof(FLPlayerPayload10Detour))]
    private readonly Hook<FlPlayerPayload10Delegate> _flPlayerPayloadHook;

    public FrontlineMatchManager(Plugin plugin) : base(plugin) {
        //plugin.DutyState.DutyCompleted += OnDutyCompleted;
        //plugin.InteropProvider.InitializeFromAttributes(this);
        plugin.Framework.Update += OnFrameworkUpdate;
        plugin.Log.Debug($"fl director .ctor address: 0x{_flDirectorCtorHook!.Address:X2}");
        plugin.Log.Debug($"fl match end address: 0x{_flMatchEndHook!.Address:X2}");
        plugin.Log.Debug($"fl player payload address: 0x{_flPlayerPayloadHook!.Address:X2}");
        _flDirectorCtorHook.Enable();
        _flMatchEndHook.Enable();
        _flPlayerPayloadHook.Enable();
    }

    public override void Dispose() {
        Plugin.Framework.Update -= OnFrameworkUpdate;
        //Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        _flDirectorCtorHook.Dispose();
        _flMatchEndHook.Dispose();
        _flPlayerPayloadHook.Dispose();
        base.Dispose();
    }

    //private void OnDutyCompleted(object? sender, ushort p1) {
    //    Plugin.Log.Debug("Duty has completed.");
    //}

    private IntPtr FLDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3) {
        Plugin.Log.Debug("Fl director .ctor detour entered.");
        try {
            var dutyId = Plugin.GameState.GetCurrentDutyId();
            var territoryId = Plugin.ClientState.TerritoryType;
            Plugin.Log.Debug($"Current duty: {dutyId} Current territory: {territoryId}");
            Plugin.DataQueue.QueueDataOperation(() => {
                CurrentMatch = new() {
                    DutyId = dutyId,
                    TerritoryId = territoryId,
                    Arena = MatchHelper.GetFrontlineMap(dutyId),
                    MaxBattleHigh = new(),
                    PluginVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                };
                unsafe {
                    if(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance() != null) {
                        CurrentMatch.GameVersion = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GameVersionString;
                    }
                }
                _maxObservedBattleHigh = [];
                _currentMatchTimeline = new() {
                    TeamPoints = new() {
                        {FrontlineTeamName.Maelstrom, new()},
                        {FrontlineTeamName.Adders, new()},
                        {FrontlineTeamName.Flames, new()},
                    },
                    SelfBattleHigh = new(),
                };
                Plugin.Log.Information($"starting new match on {CurrentMatch.Arena}");
                Plugin.DataQueue.QueueDataOperation(async () => {
                    await Plugin.FLCache.AddMatch(CurrentMatch);
                    await Plugin.Storage.AddFLTimeline(_currentMatchTimeline);
                });
            });
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            Plugin.Log.Error(e, $"Error in FL director .ctor.");
        }
        return _flDirectorCtorHook.Original(p1, p2, p3);
    }

    private void FLMatchEnd10Detour(IntPtr p1, IntPtr p2) {
        Plugin.Log.Debug("Fl match end 10 detour entered.");
        try {
#if DEBUG
            //Plugin.Functions.CreateByteDump(p2, 0x400, "fl_match_results");
#endif
            FrontlineResultsPacket resultsPacket;
            unsafe {
                resultsPacket = *(FrontlineResultsPacket*)p2;
            }
            var matchEndTask = Plugin.DataQueue.QueueDataOperation(async () => {
                if(ProcessMatchResults(resultsPacket)) {
                    await Plugin.FLCache.UpdateMatch(CurrentMatch!);
                    if(_currentMatchTimeline != null) {
                        await Plugin.Storage.UpdateFLTimeline(_currentMatchTimeline);
                    }

                    //add delay to refresh to ensure all player payloads are received
                    _ = Task.Delay(1000).ContinueWith((t) => {
                        _ = Plugin.WindowManager.RefreshFLWindow();
                    });
                }
            });

            Plugin.Log.Debug(string.Format("{0,-32} {1,-2}", "Player", "MAX BH"));
            foreach(var x in _maxObservedBattleHigh) {
                Plugin.Log.Debug(string.Format("{0,-32} {1,-2}", x.Key, x.Value));
            }

            //var printTeamStats = (FrontlineResultsPacket.TeamStat team, string name) => {
            //    Plugin.Log.Debug($"{name}\nPlace {team.Placement}\nOvooPoints {team.OccupationPoints}\nKillPoints {team.EnemyKillPoints}\nDeathLosses {team.KOPointLosses}\nUnknown1 {team.Unknown1}\nIcePoints {team.IcePoints}\nTotalRating {team.TotalPoints}");
            //};
            //printTeamStats(resultsPacket.MaelStats, "Maelstrom");
            //printTeamStats(resultsPacket.AdderStats, "Adders");
            //printTeamStats(resultsPacket.FlameStats, "Flames");
        } catch(Exception e) {
            Plugin.Log.Error(e, $"Error in FLMatchEnd10Detour");
        }
        _flMatchEndHook.Original(p1, p2);
    }

    private bool ProcessMatchResults(FrontlineResultsPacket results) {
        if(!IsMatchInProgress()) {
            Plugin.Log.Error("trying to process match results on no match!");
            return false;
            //fallback for case where you load into a game after the match has completed creating a new match
        } else if((DateTime.Now - CurrentMatch!.DutyStartTime).TotalSeconds < 10) {
            Plugin.Log.Error("double match detected.");
            return false;
        }

        Plugin.Log.Information("FL match has ended.");

        CurrentMatch.MatchEndTime = DateTime.Now;
        CurrentMatch.MatchStartTime = CurrentMatch.MatchEndTime - TimeSpan.FromSeconds(results.MatchLength);
        CurrentMatch.LocalPlayer ??= Plugin.GameState.CurrentPlayer;
        CurrentMatch.DataCenter ??= Plugin.GameState.DataCenterName;
        CurrentMatch.PlayerCount = results.PlayerCount;

        var addTeamStats = (FrontlineResultsPacket.TeamStat teamStat, FrontlineTeamName name) => {
            CurrentMatch.Teams.Add(name, new() {
                Placement = teamStat.Placement,
                OccupationPoints = teamStat.OccupationPoints,
                TargetablePoints = teamStat.IcePoints,
                DronePoints = teamStat.DronePoints,
                KillPoints = teamStat.EnemyKillPoints,
                DeathPointLosses = teamStat.KOPointLosses,
                TotalPoints = teamStat.TotalPoints
            });
        };
        addTeamStats(results.MaelStats, FrontlineTeamName.Maelstrom);
        addTeamStats(results.AdderStats, FrontlineTeamName.Adders);
        addTeamStats(results.FlameStats, FrontlineTeamName.Flames);

        CurrentMatch.IsCompleted = true;
        CurrentMatch.TimelineId = _currentMatchTimeline?.Id;
        return true;
    }

    private void FLPlayerPayload10Detour(IntPtr p1) {
        //Plugin.Log.Debug("Fl player payload 10 detour entered.");
        try {
#if DEBUG
            //Plugin.Functions.CreateByteDump(p1, 0x400, "fl_player_payload");
#endif
            FrontlinePlayerResultsPacket resultsPacket;
            unsafe {
                resultsPacket = *(FrontlinePlayerResultsPacket*)p1;
            }
            Plugin.DataQueue.QueueDataOperation(async () => {
                if(ProcessPlayerResults(resultsPacket)) {
                    await Plugin.FLCache.UpdateMatch(CurrentMatch!);
                }
            });
            //unsafe {
            //    var playerName = (PlayerAlias)$"{MemoryService.ReadString(resultsPacket.PlayerName, 32)} {Plugin.DataManager.GetExcelSheet<World>()?.GetRow(resultsPacket.WorldId)?.Name}";
            //    var team = resultsPacket.Team == 0 ? "Maelstrom" : resultsPacket.Team == 1 ? "Adders" : "Flames";
            //    var job = PlayerJobHelper.GetJobFromName(Plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(resultsPacket.ClassJobId)?.NameEnglish ?? "");
            //    Plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-15} {13,-15}", "NAME", "TEAM", "ALLIANCE", "JOB", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE OTHER", "DAMAGE TAKEN", "HP RESTORED", "HP RECEIVED", "??? 2", "OCCUPATIONS"));
            //    Plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-15} {13,-15}", playerName, team, resultsPacket.Alliance, job, resultsPacket.Kills, resultsPacket.Deaths, resultsPacket.Assists, resultsPacket.DamageDealt, resultsPacket.DamageToOther, resultsPacket.DamageTaken, resultsPacket.HPRestored, resultsPacket.Unknown1, resultsPacket.Unknown2, resultsPacket.Occupations));
            //}
        } catch(Exception e) {
            Plugin.Log.Error(e, $"Error in FLPlayerPayload10Detour");
        }
        _flPlayerPayloadHook.Original(p1);
    }

    private bool ProcessPlayerResults(FrontlinePlayerResultsPacket results) {
        if(!IsMatchInProgress()) {
            Plugin.Log.Error("trying to process FL player results on no match!");
            return false;
            //fallback for case where you load into a game after the match has completed creating a new match
        } else if((DateTime.Now - CurrentMatch!.DutyStartTime).TotalSeconds < 10) {
            //Plugin.Log.Error("double match detected.");
            return false;
        }

        Plugin.Log.Debug("Adding FL player payload.");

        PlayerAlias playerName;
        unsafe {
            playerName = (PlayerAlias)$"{MemoryService.ReadString(results.PlayerName, 32)} {Plugin.DataManager.GetExcelSheet<World>()?.GetRow(results.WorldId).Name}";
        }
        //this should probably use id instead of name string
        var job = PlayerJobHelper.GetJobFromName(Plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(results.ClassJobId).NameEnglish.ToString() ?? "");

        FrontlinePlayer newPlayer = new(playerName, job, (FrontlineTeamName)results.Team) {
            ClassJobId = results.ClassJobId,
            Alliance = results.Alliance % 3,
            //AccountId = results.AccountId,
            //ContentId = results.ContentId,
        };
        FrontlineScoreboard newScoreboard = new() {
            Kills = results.Kills,
            Deaths = results.Deaths,
            Assists = results.Assists,
            DamageDealt = results.DamageDealt,
            DamageTaken = results.DamageTaken,
            HPRestored = results.HPRestored,
            Special1 = results.Unknown1,
            Occupations = results.Occupations,
            DamageToOther = results.DamageToOther
        };

        //if(CurrentMatch.Players.ContainsKey(newPlayer)) {
        //    Plugin.Log.Warning("Player already exists in match!");
        //    return false;
        //}
        //if(CurrentMatch.Players.Select(x => x.Player).Contains(newPlayer)) {
        //    Plugin.Log.Warning("Player already exists in match!");
        //}
        CurrentMatch.Players.Add(newPlayer);
        CurrentMatch.PlayerScoreboards.Add(newPlayer.Name, newScoreboard);
        if(CurrentMatch.MaxBattleHigh != null) {

            int maxBattleHigh = 0;
            _maxObservedBattleHigh.TryGetValue(playerName, out maxBattleHigh);
            CurrentMatch.MaxBattleHigh.TryAdd(playerName, maxBattleHigh);
        }
        return true;
    }

    private unsafe void OnFrameworkUpdate(IFramework framework) {
        if(!IsMatchInProgress()) {
            return;
        }
        var director = (FrontlineContentDirector*)((IntPtr)EventFramework.Instance()->GetInstanceContentDirector() + FrontlineContentDirector.Offset);
        if(director == null) {
            return;
        }
        if(CurrentMatch?.IsCompleted ?? true && (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51])) {
            return;
        }

        var now = DateTime.Now;

#if DEBUG
        //if(now - _lastPrint > TimeSpan.FromSeconds(30)) {
        //    _lastPrint = now;
        //    Plugin.Functions.CreateByteDump((nint)director, 0x10000, "FLICD");
        //    Plugin.Log2.Debug("creating fl content director dump");
        //}
#endif

        //max BH
        foreach(IPlayerCharacter pc in Plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player).Cast<IPlayerCharacter>()) {
            try {
                var battleHigh = 0;
                foreach(var battleHighLevel in BattleHighStatuses) {
                    if(pc.StatusList.Where(x => x.StatusId == battleHighLevel.Value).Any()) {
                        battleHigh = battleHighLevel.Key; break;
                    }
                }

                var alias = (PlayerAlias)$"{pc.Name} {pc.HomeWorld.Value.Name}";
                _maxObservedBattleHigh.TryAdd(alias, 0);
                if(_maxObservedBattleHigh[alias] < battleHigh) {
                    _maxObservedBattleHigh[alias] = battleHigh;
                }
            } catch {
                //suppress
            }
        }

        if(_currentMatchTimeline != null) {

            //team points
            try {
                foreach(var team in _currentMatchTimeline.TeamPoints ?? []) {
                    var lastEvent = team.Value.LastOrDefault();
                    int? currentValue = null;
                    switch(team.Key) {
                        case FrontlineTeamName.Maelstrom:
                            currentValue = director->MaelstromScore;
                            break;
                        case FrontlineTeamName.Adders:
                            currentValue = director->AddersScore;
                            break;
                        case FrontlineTeamName.Flames:
                            currentValue = director->FlamesScore;
                            break;
                        default:
                            break;
                    }
                    if(currentValue != null && (lastEvent == null || lastEvent.Points != currentValue)) {
                        team.Value.Add(new(now, (int)currentValue));
                    }
                }
            } catch {
                //suppress
            }

            //self battle high
            try {
                if(_currentMatchTimeline.SelfBattleHigh != null) {
                    var lastBattleHighEvent = _currentMatchTimeline.SelfBattleHigh.LastOrDefault();
                    int? currentBattleHigh = director->PlayerBattleHigh;
                    if(currentBattleHigh != null && (lastBattleHighEvent == null || lastBattleHighEvent.Count != currentBattleHigh)) {
                        _currentMatchTimeline.SelfBattleHigh.Add(new(now, (int)currentBattleHigh));
                    }
                }
            } catch {
                //suppress
            }
        }
        _lastUpdate = now;
    }

    //private void Reset() {
    //    _maxObservedBattleHigh = [];
    //}
}
