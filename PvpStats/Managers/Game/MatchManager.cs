using Dalamud;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Network;
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

namespace PvpStats.Managers.Game;
internal class MatchManager : IDisposable {

    private Plugin _plugin;
    private CrystallineConflictMatch? _currentMatch;

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

    public MatchManager(Plugin plugin) {
        _plugin = plugin;

        _plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        _plugin.GameNetwork.NetworkMessage += OnNetworkMessage;

        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSIntroduction", OnPvPIntro);

        _plugin.InteropProvider.InitializeFromAttributes(this);
        _plugin.Log.Debug($"match end address: 0x{_ccMatchEndHook!.Address.ToString("X2")}");
        _ccMatchEndHook.Enable();
    }

    public void Dispose() {
        _plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _plugin.GameNetwork.NetworkMessage -= OnNetworkMessage;
        _plugin.AddonLifecycle.UnregisterListener(OnPvPIntro);
        _ccMatchEndHook.Dispose();
    }

    private unsafe void CCMatchEndDetour(IntPtr p1, uint p2, IntPtr p3) {
        _plugin.Log.Information("Match end detour occurred.");
        var resultsPacket = *(CrystallineConflictResultsPacket*)(p3 + 0x10);
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
        if(direction != NetworkMessageDirection.ZoneDown) {
            return;
        }

        if(_opCodeCount.ContainsKey(opCode)) {
            _opCodeCount[opCode]++;
        } else {
            _opCodeCount.Add(opCode, 1);
        }

        if(opCode != 845 && opCode != 813 && opCode != 649 && opCode != 717 && opCode != 920 && opCode != 898 && opCode != 316 && opCode != 769 && opCode != 810
            && opCode != 507 && opCode != 973 && opCode != 234 && opCode != 702 && opCode != 421 && opCode != 244 && opCode != 116 && opCode != 297 && opCode != 493
            && opCode != 857 && opCode != 444 && opCode != 550 && opCode != 658 && opCode != 636 && opCode != 132 && opCode != 230 && opCode != 660
            && opCode != 565 && opCode != 258 && opCode != 390 && opCode != 221 && opCode != 167 && opCode != 849) {
            _plugin.Log.Verbose($"OPCODE: {opCode} DATAPTR: 0x{dataPtr.ToString("X2")} SOURCEACTORID: {sourceActorId} TARGETACTORID: {targetActorId}");
            _plugin.Functions.PrintAllChars(dataPtr, 0x2000, 8);
            //_plugin.Functions.PrintAllStrings(dataPtr, 0x500);

            if(qPopped) {
                //_plugin.Functions.CreateByteDump(dataPtr, 0x1000, opCode.ToString());
                //_plugin.Functions.PrintAllChars(dataPtr, 0x2000);
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

        if(DateTime.Now - _lastSortTime > TimeSpan.FromSeconds(60)) {
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
        if(MatchHelper.IsCrystallineConflictTerritory(territoryId)) {
            _plugin.DataQueue.QueueDataOperation(() => {
                //pickup last match
                var lastMatch = _plugin.Storage.GetCCMatches().Query().ToList().LastOrDefault();
                if(lastMatch != null && !lastMatch.IsCompleted && (DateTime.Now - lastMatch.DutyStartTime).TotalMinutes <= 10) {
                    _plugin.Log.Information($"restoring last match...");
                    _currentMatch = lastMatch;
                } else {
                    //sometimes client state is unavailable at this time
                    _currentMatch = new() {
                        DutyId = dutyId,
                        TerritoryId = territoryId,
                        Arena = MatchHelper.CrystallineConflictMapLookup[territoryId],
                        MatchType = MatchHelper.GetMatchType(dutyId),
                    };
                    _plugin.Log.Information($"starting new match on {_currentMatch.Arena}");
                    _plugin.Storage.AddCCMatch(_currentMatch);
                }

            });
        } else {
            if(IsMatchInProgress()) {
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

    //build team data
    private unsafe void OnPvPIntro(AddonEvent type, AddonArgs args) {
        if(!IsMatchInProgress()) {
            _plugin.Log.Warning("no match in progress on pvp intro!");
            return;
        }
        qPopped = false;
        _plugin.Log.Debug("Pvp intro post setup!");
        var addon = (AtkUnitBase*)args.Addon;
        CrystallineConflictTeam team = new();

        //team name
        string teamName = AtkNodeService.ConvertAtkValueToString(addon->AtkValues[4]);
        string translatedTeamName = _plugin.Localization.TranslateDataTableEntry<Addon>(teamName, "Text", ClientLanguage.English);
        team.TeamName = MatchHelper.GetTeamName(translatedTeamName);

        //_plugin.GameState.PrintAllPlayerObjects();

        _plugin.Log.Debug(teamName);
        for(int i = 0; i < 5; i++) {
            int offset = i * 16 + 6;
            uint[] rankIdChain = new uint[] { 1, (uint)(13 + i), 2, 9 };
            if(offset >= addon->AtkValuesCount) {
                break;
            }
            //TODO account for abbreviated name settings...
            string player = AtkNodeService.ConvertAtkValueToString(addon->AtkValues[offset]);
            string world = AtkNodeService.ConvertAtkValueToString(addon->AtkValues[offset + 6]);
            string job = AtkNodeService.ConvertAtkValueToString(addon->AtkValues[offset + 5]);
            //JP uses English names...
            string translatedJob = _plugin.Localization.TranslateDataTableEntry<ClassJob>(job, "Name", ClientLanguage.English,
                _plugin.ClientState.ClientLanguage == ClientLanguage.Japanese ? ClientLanguage.English : _plugin.ClientState.ClientLanguage);
            string rank = "";
            string? translatedRank = null;

            //have to read rank from nodes -_-
            var rankNode = AtkNodeService.GetNodeByIDChain(addon, rankIdChain);
            if(rankNode == null || rankNode->Type != NodeType.Text || rankNode->GetAsAtkTextNode()->NodeText.ToString().IsNullOrEmpty()) {
                rankIdChain[3] = 10; //non-crystal
                rankNode = AtkNodeService.GetNodeByIDChain(addon, rankIdChain);
            }
            if(rankNode != null && rankNode->Type == NodeType.Text) {
                rank = rankNode->GetAsAtkTextNode()->NodeText.ToString();
                if(!rank.IsNullOrEmpty()) {
                    //set ranked as fallback
                    //_currentMatch!.MatchType = CrystallineConflictMatchType.Ranked;

                    //don't need to translate for Japanese
                    if(_plugin.ClientState.ClientLanguage != ClientLanguage.Japanese) {
                        translatedRank = _plugin.Localization.TranslateRankString(rank, ClientLanguage.English);
                    } else {
                        translatedRank = rank;
                    }
                }
            }

            //abbreviated names
            if(player.Contains(".")) {
                _currentMatch!.NeedsPlayerNameValidation = true;
                foreach(PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
                    //_plugin.Log.Debug($"name: {pc.Name} homeworld {pc.HomeWorld.GameData.Name.ToString()} job: {pc.ClassJob.GameData.NameEnglish}");
                    bool homeWorldMatch = world.Equals(pc.HomeWorld.GameData.Name.ToString(), StringComparison.OrdinalIgnoreCase);
                    bool jobMatch = pc.ClassJob.GameData.NameEnglish.ToString().Equals(translatedJob, StringComparison.OrdinalIgnoreCase);
                    bool nameMatch = PlayerJobHelper.IsAbbreviatedAliasMatch(player, pc.Name.ToString());
                    //_plugin.Log.Debug($"homeworld match:{homeWorldMatch} jobMatch:{jobMatch} nameMatch: {nameMatch}");
                    if(homeWorldMatch && jobMatch && nameMatch) {
                        _plugin.Log.Debug($"validated player: {player} is {pc.Name.ToString()}");
                        player = pc.Name.ToString();
                        break;
                    }
                }
            }

            _plugin.Log.Debug(string.Format("player: {0,-25} {1,-15} job: {2,-15} rank: {3,-10}", player, world, job, rank));

            team.Players.Add(new() {
                Alias = (PlayerAlias)$"{player} {world}",
                Job = (Job)PlayerJobHelper.GetJobFromName(translatedJob)!,
                Rank = translatedRank != null ? (PlayerRank)translatedRank : null,
                Team = team.TeamName
            });
        }

        _plugin.DataQueue.QueueDataOperation(() => {
            foreach(var player in team.Players) {
                _currentMatch.IntroPlayerInfo.Add(player.Alias, player);
            }

            _plugin.Storage.UpdateCCMatch(_currentMatch);

            _plugin.Log.Debug("");
        });
    }

    //returns true if all names successfully validated
    private bool? ValidatePlayerAliases() {
        if(!IsMatchInProgress()) {
            return null;
        }
        bool allValidated = true;

        foreach(var team in _currentMatch!.Teams) {
            //if can't find player's team ignore team condition
            bool? isPlayerTeam = _currentMatch!.LocalPlayerTeam?.TeamName is null ? null : team.Key == _currentMatch!.LocalPlayerTeam?.TeamName;
            foreach(var player in team.Value.Players) {
                //abbreviated name found
                if(player.Alias.Name.Contains(".")) {
                    //_plugin.Log.Debug($"Checking... {player.Alias.Name}");
                    allValidated = allValidated && ValidatePlayerAgainstObjectTable(player, isPlayerTeam, true);
                }
            }
        }
        return allValidated;
    }

    //returns true if match found
    private bool ValidatePlayerAgainstObjectTable(CrystallineConflictPlayer player, bool? isPartyMember = null, bool updateAlias = false) {
        foreach(PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
            bool homeWorldMatch = player.Alias.HomeWorld.Equals(pc.HomeWorld.GameData.Name.ToString());
            string translatedJobName = _plugin.Localization.TranslateDataTableEntry<ClassJob>(pc.ClassJob.GameData.Name.ToString(), "Name", ClientLanguage.English);
            bool jobMatch = player.Job.Equals(PlayerJobHelper.GetJobFromName(translatedJobName));
            bool isSelf = _plugin.ClientState.LocalPlayer.ObjectId == pc.ObjectId;
            bool teamMatch = isPartyMember is null || (bool)isPartyMember && pc.StatusFlags.HasFlag(StatusFlags.PartyMember) || !(bool)isPartyMember && !pc.StatusFlags.HasFlag(StatusFlags.PartyMember);
            //_plugin.Log.Debug($"Checking against... {pc.Name.ToString()} worldmatch: {homeWorldMatch} jobmatch: {jobMatch} teamMatch:{teamMatch}");
            //_plugin.Log.Debug($"team null? {isPlayerTeam is null} player team? {isPlayerTeam} is p member? {pc.StatusFlags.HasFlag(StatusFlags.PartyMember)} isSelf? {isSelf}");
            if(homeWorldMatch && jobMatch && (isSelf || teamMatch) && PlayerJobHelper.IsAbbreviatedAliasMatch(player.Alias, pc.Name.ToString())) {
                _plugin.Log.Debug($"validated player: {player.Alias.Name} is {pc.Name.ToString()}");
                if(updateAlias) {
                    player.Alias.Name = pc.Name.ToString();
                }
                return true;
            }
        }
        return false;
    }

    public bool IsMatchInProgress() {
        return _currentMatch != null;
    }

    private unsafe void ProcessMatchResults(CrystallineConflictResultsPacket resultsPacket) {
        if(!IsMatchInProgress()) {
            _plugin.Log.Warning("trying to process match results on no match!");
            return;
            //fallback for case where you load into a game after the match has completed creating a new match
        } else if((DateTime.Now - _currentMatch!.DutyStartTime).TotalSeconds < 10) {
            _plugin.Log.Warning("double match detected.");
            return;
        }

        CrystallineConflictPostMatch postMatch = new();
        _currentMatch.LocalPlayer ??= (PlayerAlias)_plugin.GameState.GetCurrentPlayer();
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
        foreach(var player in resultsPacket.PlayerSpan) {
            //missing player?
            if(player.ClassJobId == 0) {
                _plugin.Log.Warning("invalid/missing player result.");
                continue;
            }

            CrystallineConflictPostMatchRow playerStats = new() {
                Player = (PlayerAlias)$"{MemoryService.ReadString(player.PlayerName, 32)} {_plugin.DataManager.GetExcelSheet<World>()?.GetRow(player.WorldId)?.Name}",
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

            //add to team
            var playerTeam = playerStats.Team == CrystallineConflictTeamName.Astra ? teamAstra : teamUmbra;
            var newPlayer = new CrystallineConflictPlayer() {
                Alias = playerStats.Player,
                Job = (Job)playerStats.Job!,
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
        foreach(var introPlayer in _currentMatch.IntroPlayerInfo.Where(x => !x.Value.Alias.FullName.Contains("."))) {
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
            postMatch.MatchWinner = resultsPacket.Result == 1 ? CrystallineConflictTeamName.Astra : CrystallineConflictTeamName.Umbra;
        } else {
            postMatch.MatchWinner = resultsPacket.Result == 1 ? _currentMatch.LocalPlayerTeam!.TeamName : _currentMatch.Teams.First(x => x.Value.TeamName != _currentMatch.LocalPlayerTeam!.TeamName).Value.TeamName;
        }
        _currentMatch.MatchWinner = postMatch.MatchWinner;

        _currentMatch.PostMatch = postMatch;
        _currentMatch!.IsCompleted = true;
        _plugin.Storage.UpdateCCMatch(_currentMatch);
    }
}
