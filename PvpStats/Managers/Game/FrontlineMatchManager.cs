using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Helpers;
using PvpStats.Services;
using PvpStats.Types.ClientStruct;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Threading.Tasks;

namespace PvpStats.Managers.Game;
internal class FrontlineMatchManager : MatchManager<FrontlineMatch> {

    //protected FrontlineMatch? CurrentMatch { get; set; }

    //fl director ctor
    private delegate IntPtr FLDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    [Signature("E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 07 48 8D 8F ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 87 ?? ?? ?? ?? 48 8D 05", DetourName = nameof(FLDirectorCtorDetour))]
    private readonly Hook<FLDirectorCtorDelegate> _flDirectorCtorHook;

    //p1 = director
    //p2 = data packet
    private delegate void FLMatchEnd100Delegate(IntPtr p1, IntPtr p2);
    [Signature("4C 8B DC 55 53 56 41 56 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05", DetourName = nameof(FLMatchEnd100Detour))]
    private readonly Hook<FLMatchEnd100Delegate> _flMatchEndHook;

    //p1 = data packet
    private delegate void FlPlayerPayload10Delegate(IntPtr p1);
    //40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0
    [Signature("40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0", DetourName = nameof(FLPlayerPayload10Detour))]
    private readonly Hook<FlPlayerPayload10Delegate> _flPlayerPayloadHook;

    public FrontlineMatchManager(Plugin plugin) : base(plugin) {
        //plugin.DutyState.DutyCompleted += OnDutyCompleted;
        //plugin.InteropProvider.InitializeFromAttributes(this);
        plugin.Log.Debug($"fl director .ctor address: 0x{_flDirectorCtorHook!.Address:X2}");
        plugin.Log.Debug($"fl match end address: 0x{_flMatchEndHook!.Address:X2}");
        plugin.Log.Debug($"fl player payload address: 0x{_flPlayerPayloadHook!.Address:X2}");
        _flDirectorCtorHook.Enable();
        _flMatchEndHook.Enable();
        _flPlayerPayloadHook.Enable();
    }

    public override void Dispose() {
        //Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        _flDirectorCtorHook.Dispose();
        _flMatchEndHook.Dispose();
        _flPlayerPayloadHook.Dispose();
        base.Dispose();
    }

    private void OnDutyCompleted(object? sender, ushort p1) {
        Plugin.Log.Debug("Duty has completed.");
    }

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
                };
                Plugin.Log.Information($"starting new match on {CurrentMatch.Arena}");
                Plugin.DataQueue.QueueDataOperation(async () => {
                    await Plugin.FLCache.AddMatch(CurrentMatch);
                });
            });
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            Plugin.Log.Error(e, $"Error in FL director .ctor.");
        }
        return _flDirectorCtorHook.Original(p1, p2, p3);
    }

    private void FLMatchEnd100Detour(IntPtr p1, IntPtr p2) {
        Plugin.Log.Debug("Fl match end 100 detour entered.");
        try {

            FrontlineResultsPacket resultsPacket;
            unsafe {
                resultsPacket = *(FrontlineResultsPacket*)p2;
            }
            Plugin.DataQueue.QueueDataOperation(async () => {
                if(ProcessMatchResults(resultsPacket)) {
                    await Plugin.FLCache.UpdateMatch(CurrentMatch!);

                    //add delay to refresh to ensure all player payloads are received
                    _ = Task.Delay(1000).ContinueWith((t) => {
                        Plugin.DataQueue.QueueDataOperation(async () => {
                            await Plugin.WindowManager.RefreshFLWindow();
                        });
                    });
                }
            });

            //var printTeamStats = (FrontlineResultsPacket.TeamStat team, string name) => {
            //    Plugin.Log.Debug($"{name}\nPlace {team.Placement}\nOvooPoints {team.OccupationPoints}\nKillPoints {team.EnemyKillPoints}\nDeathLosses {team.KOPointLosses}\nUnknown1 {team.Unknown1}\nIcePoints {team.IcePoints}\nTotalRating {team.TotalPoints}");
            //};
            //printTeamStats(resultsPacket.MaelStats, "Maelstrom");
            //printTeamStats(resultsPacket.AdderStats, "Adders");
            //printTeamStats(resultsPacket.FlameStats, "Flames");
        } catch(Exception e) {
            Plugin.Log.Error(e, $"Error in FLMatchEnd100Detour");
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
        CurrentMatch.DataCenter ??= Plugin.ClientState.LocalPlayer?.CurrentWorld.GameData?.DataCenter.Value?.Name.ToString();
        CurrentMatch.PlayerCount = results.PlayerCount;

        var addTeamStats = (FrontlineResultsPacket.TeamStat teamStat, FrontlineTeamName name) => {
            CurrentMatch.Teams.Add(name, new() {
                Placement = teamStat.Placement,
                OccupationPoints = teamStat.OccupationPoints,
                TargetablePoints = teamStat.IcePoints,
                KillPoints = teamStat.EnemyKillPoints,
                DeathPointLosses = teamStat.KOPointLosses,
                TotalPoints = teamStat.TotalPoints
            });
        };
        addTeamStats(results.MaelStats, FrontlineTeamName.Maelstrom);
        addTeamStats(results.AdderStats, FrontlineTeamName.Adders);
        addTeamStats(results.FlameStats, FrontlineTeamName.Flames);

        CurrentMatch.IsCompleted = true;
        return true;
    }

    private void FLPlayerPayload10Detour(IntPtr p1) {
        Plugin.Log.Debug("Fl player payload 10 detour entered.");
        try {
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
            //Plugin.Log.Error("trying to process FL player results on no match!");
            return false;
            //fallback for case where you load into a game after the match has completed creating a new match
        } else if((DateTime.Now - CurrentMatch!.DutyStartTime).TotalSeconds < 10) {
            //Plugin.Log.Error("double match detected.");
            return false;
        }

        Plugin.Log.Debug("Adding FL player payload.");

        PlayerAlias playerName;
        unsafe {
            playerName = (PlayerAlias)$"{MemoryService.ReadString(results.PlayerName, 32)} {Plugin.DataManager.GetExcelSheet<World>()?.GetRow(results.WorldId)?.Name}";
        }
        //this should probably use id instead of name string
        var job = PlayerJobHelper.GetJobFromName(Plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(results.ClassJobId)?.NameEnglish ?? "");

        FrontlinePlayer newPlayer = new(playerName, job, (FrontlineTeamName)results.Team) {
            ClassJobId = results.ClassJobId,
            Alliance = results.Alliance % 3,
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
        return true;
    }

}
