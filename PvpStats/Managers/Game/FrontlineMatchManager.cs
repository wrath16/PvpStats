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

    //p1 = director
    //p2 = data packet
    private delegate void FLMatchEnd10Delegate(IntPtr p1, IntPtr p2);
    //4C 8B DC 55 53 56 41 56 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05
    //4C 8B DC 55 56 57 41 55 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 
    //4C 8B DC 55 56 57 41 55 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45
    [Signature("4C 8B DC 55 56 57 41 56 49 8D AB", DetourName = nameof(FLMatchEnd10Detour))]
    private readonly Hook<FLMatchEnd10Delegate> _flMatchEndHook;

    public FrontlineMatchManager(Plugin plugin) : base(plugin) {
        //plugin.DutyState.DutyCompleted += OnDutyCompleted;
        plugin.Framework.Update += OnFrameworkUpdate;
        //plugin.Log.Debug($"fl director .ctor address: 0x{_flDirectorCtorHook!.Address:X2}");
        plugin.Log.Debug($"fl match end address: 0x{_flMatchEndHook!.Address:X2}");
        //plugin.Log.Debug($"fl player payload address: 0x{_flPlayerPayloadHook!.Address:X2}");
        //_flDirectorCtorHook.Enable();
        _flMatchEndHook.Enable();
    }

    public override void Dispose() {
        Plugin.Framework.Update -= OnFrameworkUpdate;
        //Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        //_flDirectorCtorHook.Dispose();
        _flMatchEndHook.Dispose();
        base.Dispose();
    }

    //private void OnDutyCompleted(object? sender, ushort p1) {
    //    Plugin.Log.Debug("Duty has completed.");
    //}

    protected override void OnTerritoryChanged(ushort territoryId) {
        var dutyId = Plugin.GameState.GetCurrentDutyId();
        if(MatchHelper.GetFrontlineMap(dutyId) != null) {
            StartMatch();
        } else if(IsMatchInProgress()) {
            Plugin.DataQueue.QueueDataOperation(() => {
                Plugin.Functions._opcodeMatchCount++;
                CurrentMatch = null;
                CurrentTimeline = null;
                //Plugin.WindowManager.Refresh();
            });
        }
    }

    private void StartMatch() {
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
            Plugin.Log.Information($"Starting new match on {CurrentMatch.Arena}");
            Plugin.DataQueue.QueueDataOperation(async () => {
                await Plugin.FLCache.AddMatch(CurrentMatch);
                await Plugin.Storage.AddFLTimeline(_currentMatchTimeline);
            });
        });
    }

    private void FLMatchEnd10Detour(IntPtr p1, IntPtr p2) {
        Plugin.Log.Debug("Fl match end 10 detour entered.");
        try {
#if DEBUG
            Plugin.Functions.CreateByteDump(p2, 0x2000, "fl_match_results");
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

#if DEBUG
            //Plugin.Log.Debug(string.Format("{0,-32} {1,-2}", "Player", "MAX BH"));
            //foreach(var x in _maxObservedBattleHigh) {
            //    Plugin.Log.Debug(string.Format("{0,-32} {1,-2}", x.Key, x.Value));
            //}
            //var printTeamStats = (FrontlineResultsPacket.TeamStat team, string name) => {
            //    Plugin.Log.Debug($"{name}\nPlace {team.Placement}\nOvooPoints {team.OccupationPoints}\nKillPoints {team.EnemyKillPoints}\nDeathLosses {team.KOPointLosses}\nUnknown1 {team.Unknown1}\nIcePoints {team.IcePoints}\nTotalRating {team.TotalPoints}");
            //};
            //printTeamStats(resultsPacket.MaelStats, "Maelstrom");
            //printTeamStats(resultsPacket.AdderStats, "Adders");
            //printTeamStats(resultsPacket.FlameStats, "Flames");
#endif
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
        } else if((DateTime.UtcNow - CurrentMatch!.DutyStartTime).TotalSeconds < 10) {
            Plugin.Log.Error("double match detected.");
            return false;
        }

        Plugin.Log.Information("FL match has ended.");

        CurrentMatch.MatchEndTime = DateTime.UtcNow;
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

        //set player stats
        for(int i = 0; i < results.PlayerSpan.Length; i++) {
            var player = results.PlayerSpan[i];
            //missing player?
            if(player.ClassJobId == 0) {
                Plugin.Log.Warning("invalid/missing player result.");
                continue;
            }
            PlayerAlias playerName;
            unsafe {
                playerName = (PlayerAlias)$"{MemoryService.ReadString(player.PlayerName, 32)} {Plugin.DataManager.GetExcelSheet<World>()?.GetRow(player.WorldId).Name}";
            }
            //this should probably use id instead of name string
            var job = PlayerJobHelper.GetJobFromName(Plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(player.ClassJobId).NameEnglish.ToString() ?? "");

            FrontlinePlayer newPlayer = new(playerName, job, (FrontlineTeamName)player.Team) {
                ClassJobId = player.ClassJobId,
                Alliance = player.Alliance % 3,
                //AccountId = results.AccountId,
                //ContentId = results.ContentId,
            };
            FrontlineScoreboard newScoreboard = new() {
                Kills = player.Kills,
                Deaths = player.Deaths,
                Assists = player.Assists,
                DamageDealt = player.DamageDealt,
                DamageTaken = player.DamageTaken,
                HPRestored = player.HPRestored,
                Special1 = player.Unknown1,
                Occupations = player.Occupations,
                DamageToOther = player.DamageToOther
            };

            CurrentMatch.Players.Add(newPlayer);
            CurrentMatch.PlayerScoreboards.Add(newPlayer.Name, newScoreboard);
            if(CurrentMatch.MaxBattleHigh != null) {

                int maxBattleHigh = 0;
                _maxObservedBattleHigh.TryGetValue(playerName, out maxBattleHigh);
                CurrentMatch.MaxBattleHigh.TryAdd(playerName, maxBattleHigh);
            }
        }

        CurrentMatch.IsCompleted = true;
        CurrentMatch.TimelineId = _currentMatchTimeline?.Id;
        return true;
    }

    private unsafe void OnFrameworkUpdate(IFramework framework) {
        if(!IsMatchInProgress()) {
            return;
        }
        var director = (FrontlineContentDirector*)(IntPtr)EventFramework.Instance()->GetInstanceContentDirector();
        if(director == null) {
            return;
        }
        if(CurrentMatch?.IsCompleted ?? true && (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51])) {
            return;
        }

        var now = DateTime.UtcNow;

#if DEBUG
        //if(now - _lastPrint > TimeSpan.FromSeconds(30)) {
        //    _lastPrint = now;
        //    Plugin.Functions.CreateByteDump((nint)director, 0x10000, "FLICD");
        //    Plugin.Log2.Debug("creating fl content director dump");
        //}
#endif

        var objectTable = Plugin.ObjectTable;
        for(int i = 0; i < objectTable.Length; i++) {
            var obj = objectTable[i];
            if(obj == null || obj.ObjectKind != ObjectKind.Player)
                continue;

            try {
                var pc = (IPlayerCharacter)obj;
                var statusList = pc.StatusList;
                int battleHigh = 0;

                foreach(var kvp in BattleHighStatuses) {
                    foreach(var status in statusList) {
                        if(status.StatusId == kvp.Value) {
                            battleHigh = kvp.Key;
                            goto FoundBattleHigh;
                        }
                    }
                }

            FoundBattleHigh:
                var aliasKey = (PlayerAlias)$"{pc.Name} {pc.HomeWorld.Value.Name}";
                if(_maxObservedBattleHigh.TryGetValue(aliasKey, out int existing)) {
                    if(battleHigh > existing)
                        _maxObservedBattleHigh[aliasKey] = battleHigh;
                } else {
                    _maxObservedBattleHigh[aliasKey] = battleHigh;
                }
            } catch {
                // ignored: continue processing next player
            }
        }

        if(_currentMatchTimeline != null) {
            // Update team scores
            try {
                var teamPoints = _currentMatchTimeline.TeamPoints;
                if(teamPoints != null) {
                    foreach(var team in teamPoints) {
                        int? currentValue = team.Key switch {
                            FrontlineTeamName.Maelstrom => director->MaelstromScore,
                            FrontlineTeamName.Adders => director->AddersScore,
                            FrontlineTeamName.Flames => director->FlamesScore,
                            _ => null
                        };

                        if(currentValue.HasValue) {
                            var points = team.Value;
                            var last = points.Count > 0 ? points[points.Count - 1] : null;
                            if(last == null || last.Points != currentValue.Value)
                                points.Add(new(now, currentValue.Value));
                        }
                    }
                }
            } catch {
                // ignored
            }

            // Update self battle high
            try {
                var selfHigh = _currentMatchTimeline.SelfBattleHigh;
                if(selfHigh != null) {
                    int currentBattleHigh = director->PlayerBattleHigh;
                    var last = selfHigh.Count > 0 ? selfHigh[selfHigh.Count - 1] : null;
                    if(last == null || last.Count != currentBattleHigh)
                        selfHigh.Add(new(now, currentBattleHigh));
                }
            } catch {
                // ignored
            }
        }

        _lastUpdate = now;
    }

    //private void Reset() {
    //    _maxObservedBattleHigh = [];
    //}
}
