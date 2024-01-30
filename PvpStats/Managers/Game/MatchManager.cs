using Dalamud;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Network;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets2;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
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

    Dictionary<ushort, uint> _opCodeCount = new();

    public MatchManager(Plugin plugin) {
        _plugin = plugin;

        _plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        _plugin.DutyState.DutyCompleted += OnDutyCompleted;
        _plugin.DutyState.DutyStarted += OnDutyStarted;
        _plugin.GameNetwork.NetworkMessage += OnNetworkMessage;

        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSIntroduction", OnPvPIntro);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "PvPMKSHeader", OnPvPHeaderUpdate);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSHeader", OnPvPHeaderUpdate);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "MKSRecord", OnPvPResults);
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
        _plugin.AddonLifecycle.UnregisterListener(OnPvPResults);
    }

    private void OnNetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
        if (!IsMatchInProgress()) {
            return;
        }
        if (direction != NetworkMessageDirection.ZoneDown) {
            return;
        }

        _plugin.Log.Verbose($"OPCODE: {opCode} DATAPTR: 0x{dataPtr.ToString("X2")} SOURCEACTORID: {sourceActorId} TARGETACTORID: {targetActorId}");
        if (_opCodeCount.ContainsKey(opCode)) {
            _opCodeCount[opCode]++;
        } else {
            _opCodeCount.Add(opCode, 1);
        }
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
                    _plugin.Log.Debug("Opcodes:");
                    foreach (var opcode in _opCodeCount.OrderByDescending(x => x.Value)) {
                        _plugin.Log.Debug($"opcode {opcode.Key}: {opcode.Value}");
                    }
                    _opCodeCount = new();
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
        var currentTime = DateTime.Now;
        //var currentMatchTemp = _currentMatch;

        //add delay to get last of header updates
        //this could cause issues with players instaleaving
        Task.Delay(100).ContinueWith(t => {
            _plugin.DataQueue.QueueDataOperation(() => {
                _plugin.Log.Debug("Match has ended.");
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
                    _plugin.Log.Verbose("Correcting 99.9% to 100%...");
                    winningTeam.Progress = 100f;
                }

                _plugin.Storage.UpdateCCMatch(_currentMatch);
            });
        });
    }

    //build team data
    private unsafe void OnPvPIntro(AddonEvent type, AddonArgs args) {
        if (!IsMatchInProgress()) {
            return;
        }

        //Log.Debug("Pvp intro post setup!");
        var addon = (AtkUnitBase*)args.Addon;
        CrystallineConflictTeam team = new();

        //PrintAtkValues(addon);
        //AtkNodeHelper.PrintTextNodes(addon->GetNodeById(1), true, false);

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
            string translatedJob = _plugin.Localization.TranslateDataTableEntry<ClassJob>(job, "Name", ClientLanguage.English);
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
                //set ranked as fallback
                //_currentMatch!.MatchType = CrystallineConflictMatchType.Ranked;

                //don't need to translate for Japanese
                if (_plugin.ClientState.ClientLanguage != ClientLanguage.Japanese) {
                    translatedRank = _plugin.Localization.TranslateRankString(rank, ClientLanguage.English);
                } else {
                    translatedRank = rank;
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
        postMatch.MatchWinner = MatchHelper.GetTeamName(Regex.Match(matchWinner, @"^\w*").Value);

        var matchDuration = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[22]);
        postMatch.MatchDuration = TimeSpan.Parse("00:" + matchDuration);

        var leftTeamName = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1526]);
        var leftTeamProgress = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1538]);
        var leftTeamKills = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1542]);
        var leftTeamDeaths = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1544]);
        var leftTeamAssists = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[1546]);

        CrystallineConflictPostMatchTeam leftTeam = new() {
            TeamName = MatchHelper.GetTeamName(leftTeamName),
            Progress = (float)MatchHelper.ConvertProgressStringToFloat(leftTeamProgress),
            TeamStats = new() {
                Team = MatchHelper.GetTeamName(leftTeamName),
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

        CrystallineConflictPostMatchTeam rightTeam = new() {
            TeamName = MatchHelper.GetTeamName(rightTeamName),
            Progress = (float)MatchHelper.ConvertProgressStringToFloat(rightTeamProgress),
            TeamStats = new() {
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
        _plugin.Log.Debug($"BEFORE: TIER:{tierBefore.Value} RISER: {riserBefore.Value} STARS: {starsBefore.Length}");
        _plugin.Log.Debug($"AFTER: TIER:{tierAfter.Value} RISER: {riserAfter.Value} STARS: {starsAfter.Length}");
        _plugin.Log.Debug(string.Format("{4,-6}: progress: {0,-6} kills: {1,-3} deaths: {2,-3} assists: {3,-3}", leftTeamProgress, leftTeamKills, leftTeamDeaths, leftTeamAssists, leftTeamName));
        _plugin.Log.Debug(string.Format("{4,-6}: progress: {0,-6} kills: {1,-3} deaths: {2,-3} assists: {3,-3}", rightTeamProgress, rightTeamKills, rightTeamDeaths, rightTeamAssists, rightTeamName));
        _plugin.Log.Debug(string.Format("{0,-25} {1,-15} {2,-5} {3,-15} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15}", "NAME", "WORLD", "JOB", "TIER", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE TAKEN", "HP RESTORED", "TIME ON CRYSTAL"));

        //set rank change
        PlayerRank beforeRank = new();
        PlayerRank afterRank = new();

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

}
