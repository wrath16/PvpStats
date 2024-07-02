using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
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
using System.Text.RegularExpressions;

namespace PvpStats.Managers.Game;
internal class CrystallineConflictMatchManager : IDisposable {

    private Plugin _plugin;
    private CrystallineConflictMatch? _currentMatch;

    //p1 = director
    //p2 = results packet
    //p3 = results packet + offset (ref to specific variable?)
    //p4 = ???
    private delegate void CCMatchEnd101Delegate(IntPtr p1, IntPtr p2, IntPtr p3, uint p4);
    [Signature("40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 0F B6 42", DetourName = nameof(CCMatchEnd101Detour))]
    private readonly Hook<CCMatchEnd101Delegate> _ccMatchEndHook;

    private delegate IntPtr CCDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    //48 89 5C 24 ?? 56 57 41 57 48 83 EC ?? 4C 89 74 24 
    //48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC ?? 4C 8B F1 E8
    [Signature("48 89 5C 24 ?? 56 57 41 57 48 83 EC ?? 4C 89 74 24", DetourName = nameof(CCDirectorCtorDetour))]
    private readonly Hook<CCDirectorCtorDelegate> _ccDirectorCtorHook;

    //E8 ?? ?? ?? ?? 48 8B F8 EB ?? 33 FF 8B C7 
    //instance content director...
    private delegate IntPtr InstanceContentDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    [Signature("E8 ?? ?? ?? ?? 48 8B F8 EB ?? 33 FF 8B C7 ", DetourName = nameof(ICDCtorDetour))]
    private readonly Hook<CCDirectorCtorDelegate> _icdCtorHook;

    private static readonly Regex TierRegex = new(@"\D+", RegexOptions.IgnoreCase);
    private static readonly Regex RiserRegex = new(@"\d+", RegexOptions.IgnoreCase);

    public CrystallineConflictMatchManager(Plugin plugin) {
        _plugin = plugin;

        _plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSIntroduction", OnPvPIntro);
        _plugin.InteropProvider.InitializeFromAttributes(this);
        _plugin.Log.Debug($"cc director .ctor address: 0x{_ccDirectorCtorHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"match end 1 address: 0x{_ccMatchEndHook!.Address.ToString("X2")}");
        _ccDirectorCtorHook.Enable();
        _ccMatchEndHook.Enable();
        _icdCtorHook.Enable();
    }

    public void Dispose() {
        _plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _plugin.AddonLifecycle.UnregisterListener(OnPvPIntro);
        _ccMatchEndHook.Dispose();
        _ccDirectorCtorHook.Dispose();
        _icdCtorHook.Dispose();
    }

    private IntPtr CCDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3) {
        _plugin.Log.Debug("CC Director .ctor occurred!");
        try {
            var dutyId = _plugin.GameState.GetCurrentDutyId();
            var territoryId = _plugin.ClientState.TerritoryType;
            _plugin.Log.Debug($"Current duty: {dutyId} Current territory: {territoryId}");
            _plugin.DataQueue.QueueDataOperation(() => {
                _currentMatch = new() {
                    DutyId = dutyId,
                    TerritoryId = territoryId,
                    Arena = MatchHelper.GetArena(territoryId),
                    MatchType = MatchHelper.GetMatchType(dutyId),
                };
                _plugin.Log.Information($"starting new match on {_currentMatch.Arena}");
                _plugin.DataQueue.QueueDataOperation(async () => {
                    await _plugin.CCCache.AddMatch(_currentMatch);
                });

#if DEBUG
                //_plugin.Functions.FindValue<byte>(0, p1, 0x500, 0, true);
                //_plugin.Log.Debug("p1");
                //_plugin.Functions.FindValue("", p1, 0x1000, 0, true);
                //_plugin.Log.Debug("p2");
                //_plugin.Functions.FindValue("", p2, 0x1000, 0, true);
                //_plugin.Log.Debug("p3");
                //_plugin.Functions.FindValue("", p3, 0x1000, 0, true);
#endif
            });
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            _plugin.Log.Error($"Error in CC director ctor: {e.Message}");
        }
        return _ccDirectorCtorHook.Original(p1, p2, p3);
    }

    private IntPtr ICDCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3) {
        _plugin.Log.Debug("icd ctor detour occurred!");
        return _icdCtorHook.Original(p1, p2, p3);
    }

    private void CCMatchEnd101Detour(IntPtr p1, IntPtr p2, IntPtr p3, uint p4) {
        _plugin.Log.Debug("Match end detour occurred.");
#if DEBUG
        _plugin.Functions.FindValue<byte>(0, p2, 0x400, 0, true);
        _plugin.Functions.CreateByteDump(p2, 0x400, "cc_match_results");
#endif
        CrystallineConflictResultsPacket resultsPacket;
        unsafe {
            resultsPacket = *(CrystallineConflictResultsPacket*)p2;
        }
        _plugin.DataQueue.QueueDataOperation(async () => {
            if(ProcessMatchResults(resultsPacket)) {
                await _plugin.CCCache.UpdateMatch(_currentMatch!);
                await _plugin.WindowManager.RefreshCCWindow();
            }
        });
        _ccMatchEndHook.Original(p1, p2, p3, p4);
    }

    private void OnTerritoryChanged(ushort territoryId) {
        var dutyId = _plugin.GameState.GetCurrentDutyId();
        _plugin.Log.Debug($"Territory changed: {territoryId}, Current duty: {dutyId}");
        if(IsMatchInProgress()) {
            _plugin.DataQueue.QueueDataOperation(() => {
                _plugin.Functions._opcodeMatchCount++;
                _currentMatch = null;
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
                    translatedJob = _plugin.DataManager.GetExcelSheet<ClassJob>().GetRow((uint)jobId).NameEnglish;
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
                        bool homeWorldMatch = worldRaw.Equals(pc.HomeWorld.GameData?.Name.ToString(), StringComparison.OrdinalIgnoreCase);
                        bool jobMatch = pc.ClassJob.GameData?.NameEnglish.ToString().Equals(translatedJob, StringComparison.OrdinalIgnoreCase) ?? false;
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
        _currentMatch.DataCenter ??= _plugin.ClientState.LocalPlayer?.CurrentWorld.GameData?.DataCenter.Value?.Name.ToString();

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
                Job = PlayerJobHelper.GetJobFromName(_plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(player.ClassJobId)?.NameEnglish ?? ""),
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
            unsafe {
                playerStats.Player = (PlayerAlias)$"{MemoryService.ReadString(player.PlayerName, 32)} {_plugin.DataManager.GetExcelSheet<World>()?.GetRow((uint)player.WorldId)?.Name}";
            }

            //add to team
            var playerTeam = playerStats.Team == CrystallineConflictTeamName.Astra ? teamAstra : teamUmbra;
            var newPlayer = new CrystallineConflictPlayer() {
                Alias = playerStats.Player,
                Job = playerStats.Job,
                ClassJobId = player.ClassJobId,
                Rank = playerStats.PlayerRank
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

        //add players who left match. omit ones with incomplete name as a failsafe
        foreach(var introPlayer in _currentMatch.IntroPlayerInfo.Where(x => !x.Value.Alias.FullName.Contains('.') && !x.Value.Alias.HomeWorld.Equals("Unknown", StringComparison.OrdinalIgnoreCase))) {
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
        _currentMatch!.IsCompleted = true;
        //this should really happen in same Task...
        //_plugin.DataQueue.QueueDataOperation(async () => {
        //    await _plugin.Storage.UpdateCCMatch(_currentMatch);
        //});
        //await _plugin.Storage.UpdateCCMatch(_currentMatch);
        return true;
    }
}
