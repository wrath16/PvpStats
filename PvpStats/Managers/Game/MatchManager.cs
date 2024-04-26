﻿using Dalamud;
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

    //debug variables
    internal Dictionary<ushort, uint> _opCodeCount = new();
    internal int _opcodeMatchCount = 0;
    private DateTime _lastSortTime;
    private bool _qPopped = false;

    //p1 = director
    //p2 = results packet
    //p3 = results packet + offset (ref to specific variable?)
    //p4 = ???
    private delegate void CCMatchEnd101Delegate(IntPtr p1, IntPtr p2, IntPtr p3, uint p4);
    [Signature("40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 0F B6 42", DetourName = nameof(CCMatchEnd101Detour))]
    private readonly Hook<CCMatchEnd101Delegate> _ccMatchEndHook;

    private delegate IntPtr CCDirectorCtorDelegate(IntPtr p1, IntPtr p2, IntPtr p3);
    [Signature("E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 8D 05", DetourName = nameof(CCDirectorCtorDetour))]
    private readonly Hook<CCDirectorCtorDelegate> _ccDirectorCtorHook;

    public MatchManager(Plugin plugin) {
        _plugin = plugin;

        _plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
#if DEBUG
        _plugin.GameNetwork.NetworkMessage += OnNetworkMessage;
#endif
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSIntroduction", OnPvPIntro);
        _plugin.InteropProvider.InitializeFromAttributes(this);
        _plugin.Log.Debug($"cc director .ctor address: 0x{_ccDirectorCtorHook!.Address.ToString("X2")}");
        _plugin.Log.Debug($"match end 1 address: 0x{_ccMatchEndHook!.Address.ToString("X2")}");
        _ccDirectorCtorHook.Enable();
        _ccMatchEndHook.Enable();
    }

    public void Dispose() {
        _plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
#if DEBUG
        _plugin.GameNetwork.NetworkMessage -= OnNetworkMessage;
#endif
        _plugin.AddonLifecycle.UnregisterListener(OnPvPIntro);
        _ccMatchEndHook.Dispose();
        _ccDirectorCtorHook.Dispose();
    }

    private unsafe IntPtr CCDirectorCtorDetour(IntPtr p1, IntPtr p2, IntPtr p3) {
        _plugin.Log.Debug("CC Director .ctor occurred!");
        try {
            var dutyId = _plugin.GameState.GetCurrentDutyId();
            var territoryId = _plugin.ClientState.TerritoryType;
            _plugin.Log.Debug($"Current duty: {dutyId}");
            _plugin.Log.Debug($"Current territory: {territoryId}");
            _plugin.DataQueue.QueueDataOperation(() => {
                _currentMatch = new() {
                    DutyId = dutyId,
                    TerritoryId = territoryId,
                    Arena = MatchHelper.GetArena(territoryId),
                    MatchType = MatchHelper.GetMatchType(dutyId),
                };
                _plugin.Log.Information($"starting new match on {_currentMatch.Arena}");
                _plugin.Storage.AddCCMatch(_currentMatch);
            });
        } catch(Exception e) {
            //suppress all exceptions so game doesn't crash if something fails here
            _plugin.Log.Error($"Error in CC director ctor: {e.Message}");
        }
        return _ccDirectorCtorHook.Original(p1, p2, p3);
    }

    private unsafe void CCMatchEnd101Detour(IntPtr p1, IntPtr p2, IntPtr p3, uint p4) {
        _plugin.Log.Debug("Match end detour occurred.");
#if DEBUG
        _plugin.Functions.FindValue<byte>(0, p2, 0x400, 0, true);
#endif
        var resultsPacket = *(CrystallineConflictResultsPacket*)(p2 + 0x10);
        _plugin.DataQueue.QueueDataOperation(() => {
            ProcessMatchResults(resultsPacket);
        });
        _ccMatchEndHook.Original(p1, p2, p3, p4);
    }

    private unsafe void OnNetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
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
            //_plugin.Functions.PrintAllChars(dataPtr, 0x2000, 8);
            //_plugin.Functions.PrintAllStrings(dataPtr, 0x500);

            if(_qPopped) {
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
            _qPopped = true;
        }

        ////643 has promise...
        //if (opCode == 945 || opCode == 560) {
        //    _plugin.Functions.FindValue<string>("", dataPtr, 0x500, 0, true);
        //}

        if(DateTime.Now - _lastSortTime > TimeSpan.FromSeconds(60)) {
            _lastSortTime = DateTime.Now;
            _opCodeCount = _opCodeCount.OrderBy(x => x.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    private void OnTerritoryChanged(ushort territoryId) {
        var dutyId = _plugin.GameState.GetCurrentDutyId();
        _plugin.Log.Debug($"Territory changed: {territoryId}, Current duty: {dutyId}");
        if(IsMatchInProgress()) {
            _plugin.DataQueue.QueueDataOperation(() => {
                _opcodeMatchCount++;
                _currentMatch = null;
                _plugin.WindowManager.Refresh();
            });
        }
    }

    //extract player info from intro screen
    private unsafe void OnPvPIntro(AddonEvent type, AddonArgs args) {
        if(!IsMatchInProgress()) {
            _plugin.Log.Warning("no match in progress on pvp intro!");
            return;
        }
        _qPopped = false;
        _plugin.Log.Debug("Pvp intro post setup");
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
                _currentMatch!.IntroPlayerInfo.Add(player.Alias, player);
            }
            _plugin.Storage.UpdateCCMatch(_currentMatch!);
            _plugin.Log.Debug("");
        });
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

        _plugin.Log.Information("Match has ended.");

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
            switch(resultsPacket.Result) {
                case 1:
                    postMatch.MatchWinner = CrystallineConflictTeamName.Astra;
                    break;
                case 2:
                    postMatch.MatchWinner = CrystallineConflictTeamName.Umbra;
                    break;
                default:
                    _plugin.Log.Warning("Unable to determine winner...draw?");
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
                    _plugin.Log.Warning("Unable to determine winner...draw?");
                    postMatch.MatchWinner = CrystallineConflictTeamName.Unknown;
                    break;
            }
        }
        _currentMatch.MatchWinner = postMatch.MatchWinner;

        _currentMatch.PostMatch = postMatch;
        _currentMatch!.IsCompleted = true;
        _plugin.Storage.UpdateCCMatch(_currentMatch);
    }
}
