using Dalamud;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets2;
//using Lumina.Excel.GeneratedSheets;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PvpStats.Managers;
internal class MatchManager : IDisposable {

    private Plugin _plugin;
    private CrystallineConflictMatch? _currentMatch;
    private DateTime _lastHeaderUpdateTime;

    public MatchManager(Plugin plugin) {
        _plugin = plugin;

        _plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        _plugin.DutyState.DutyCompleted += OnDutyCompleted;
        _plugin.DutyState.DutyStarted += OnDutyStarted;

        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "PvPMKSIntroduction", OnPvPIntro);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "PvPMKSHeader", OnPvPHeaderUpdate);
        _plugin.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "MKSRecord", OnPvPResults);
    }

    public void Dispose() {

        //_plugin.Framework.Update -= OnFrameworkUpdate;
        //_plugin.ChatGui.ChatMessage -= OnChatMessage;
        _plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        _plugin.DutyState.DutyStarted -= OnDutyStarted;

        _plugin.AddonLifecycle.UnregisterListener(OnPvPIntro);
        _plugin.AddonLifecycle.UnregisterListener(OnPvPHeaderUpdate);
        _plugin.AddonLifecycle.UnregisterListener(OnPvPResults);
    }

    private void OnTerritoryChanged(ushort territoryId) {
        var dutyId = _plugin.GetCurrentDutyId();
        var duty = _plugin.DataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow(dutyId);
        _plugin.Log.Debug($"Territory changed: {territoryId}, Current duty: {_plugin.GetCurrentDutyId()}");

        if (MatchHelper.IsCrystallineConflictTerritory(territoryId)) {
            //sometimes client state is unavailable at this time
            //start or pickup match!
            _currentMatch = new() {
                DutyId = dutyId,
                TerritoryId = territoryId,
                Arena = MatchHelper.CrystallineConflictMapLookup[territoryId],
                MatchType = MatchHelper.GetMatchType(dutyId),
            };
            _plugin.StorageManager.AddCCMatch(_currentMatch);
        }
        else {
            _currentMatch = null;
        }
    }

    private void OnDutyStarted(object? sender, ushort param1) {
        if (!IsMatchInProgress()) {
            return;
        }

        _plugin.Log.Debug("Match has started.");
        _currentMatch!.MatchStartTime = DateTime.Now;

        if (_currentMatch.NeedsPlayerNameValidation) {
            _currentMatch.NeedsPlayerNameValidation = !ValidatePlayerAliases() ?? true;
        }
        _plugin.StorageManager.UpdateCCMatch(_currentMatch);

        //foreach (var obj in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
        //    _plugin.Log.Debug($"player object: {obj.Name}");
        //    var pc = obj as PlayerCharacter;
        //    _plugin.Log.Debug($"homeworld: {pc.HomeWorld.GameData.Name.ToString()} job: {pc.ClassJob.GameData.Name.ToString()} partymember?: {pc.StatusFlags & StatusFlags.PartyMember} statusflags: {pc.StatusFlags}");
        //}

        //foreach (var obj in _plugin.ObjectTable) {
        //    _plugin.Log.Debug($"object kind: {obj.ObjectKind} name: {obj.Name}");
        //}
    }

    private void OnDutyCompleted(object? sender, ushort param1) {
        if (!IsMatchInProgress()) {
            return;
        }

        _plugin.Log.Debug("Match has ended.");
        _currentMatch!.MatchEndTime = DateTime.Now;
        _currentMatch!.IsCompleted = true;
        //set winner todo: check for draws!
        if (_currentMatch.Teams.ElementAt(0).Value.Progress > _currentMatch.Teams.ElementAt(1).Value.Progress) {
            _currentMatch.MatchWinner = _currentMatch.Teams.ElementAt(0).Key;
        }
        else {
            _currentMatch.MatchWinner = _currentMatch.Teams.ElementAt(1).Key;
        }
        var winningTeam = _currentMatch.Teams[(CrystallineConflictTeamName)_currentMatch.MatchWinner];
        //correct 99.9% on non-overtime wins
        if (winningTeam.Progress > 99f && winningTeam.Progress < 100f && _currentMatch.MatchDuration.Value.TotalSeconds < 5 * 60 && !_currentMatch.IsOvertime) {
            _plugin.Log.Verbose("Correcting 99.9% to 100%...");
            winningTeam.Progress = 100f;
        }

        _plugin.StorageManager.UpdateCCMatch(_currentMatch);
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
        string translatedTeamName = _plugin.LocalizationManager.TranslateDataTableEntry<Addon>(teamName, "Text", ClientLanguage.English);
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
            string translatedJob = _plugin.LocalizationManager.TranslateDataTableEntry<ClassJob>(job, "Name", ClientLanguage.English);
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
                    translatedRank = _plugin.LocalizationManager.TranslateRankString(rank, ClientLanguage.English);
                }
                else {
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

        if (!_currentMatch!.Teams.ContainsKey(team.TeamName)) {
            _currentMatch!.Teams.Add(team.TeamName, team);
        }
        else {
            _plugin.Log.Warning($"Duplicate team found: {team.TeamName}");
        }

        //set local player and data center
        _currentMatch.LocalPlayer ??= (PlayerAlias)_plugin.GetCurrentPlayer();
        _currentMatch.DataCenter ??= _plugin.ClientState.LocalPlayer?.CurrentWorld.GameData?.DataCenter.Value?.Name.ToString();

        _plugin.StorageManager.UpdateCCMatch(_currentMatch);

        _plugin.Log.Debug("");
    }

    private unsafe void OnPvPHeaderUpdate(AddonEvent type, AddonArgs args) {
        if (!IsMatchInProgress()) {
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

        //check for parse results? this is causing error!
        try {
            _currentMatch!.MatchTimer = new TimeSpan(0, int.Parse(timerMinsNode->NodeText.ToString()), int.Parse(timerSecondsNode->NodeText.ToString()));
        }
        catch {
            //hehe
        }

        if (!_currentMatch!.IsOvertime) {
            _currentMatch.IsOvertime = isOvertime;
        }

        if (_currentMatch.Teams.Count == 2) {
            var leftTeamName = MatchHelper.GetTeamName(_plugin.LocalizationManager.TranslateDataTableEntry<Addon>(leftTeamNode->NodeText.ToString(), "Text", ClientLanguage.English));
            var rightTeamName = MatchHelper.GetTeamName(_plugin.LocalizationManager.TranslateDataTableEntry<Addon>(rightTeamNode->NodeText.ToString(), "Text", ClientLanguage.English));
            _currentMatch.Teams[leftTeamName].Progress = float.Parse(leftProgressNode->NodeText.ToString().Replace("%", "").Replace(",", "."));
            _currentMatch.Teams[rightTeamName].Progress = float.Parse(rightProgressNode->NodeText.ToString().Replace("%", "").Replace(",", "."));
        }

        _plugin.StorageManager.UpdateCCMatch(_currentMatch);

        if ((DateTime.Now - _lastHeaderUpdateTime).TotalSeconds > 60) {
            _lastHeaderUpdateTime = DateTime.Now;
            _plugin.Log.Debug($"MATCH TIMER: {timerMinsNode->NodeText}:{timerSecondsNode->NodeText}");
            _plugin.Log.Debug($"OVERTIME: {isOvertime}");
            _plugin.Log.Debug($"{leftTeamNode->NodeText}: {leftProgressNode->NodeText}");
            _plugin.Log.Debug($"{rightTeamNode->NodeText}: {rightProgressNode->NodeText}");
            _plugin.Log.Debug("--------");
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

        _plugin.Log.Debug($"{matchWinner}");
        _plugin.Log.Debug($"match duration: {matchDuration}");
        _plugin.Log.Debug($"rank change: {rankChange}");
        _plugin.Log.Debug(string.Format("{4,-6}: progress: {0,-6} kills: {1,-3} deaths: {2,-3} assists: {3,-3}", leftTeamProgress, leftTeamKills, leftTeamDeaths, leftTeamAssists, leftTeamName));
        _plugin.Log.Debug(string.Format("{4,-6}: progress: {0,-6} kills: {1,-3} deaths: {2,-3} assists: {3,-3}", rightTeamProgress, rightTeamKills, rightTeamDeaths, rightTeamAssists, rightTeamName));
        _plugin.Log.Debug(string.Format("{0,-25} {1,-15} {2,-5} {3,-15} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15}", "NAME", "WORLD", "JOB", "TIER", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE TAKEN", "HP RESTORED", "TIME ON CRYSTAL"));

        postMatch.Teams.Add(leftTeam.TeamName, leftTeam);
        postMatch.Teams.Add(rightTeam.TeamName, rightTeam);

        for (int i = 0; i < 10; i++) {
            //todo check for missing players...
            int offset = i * 20 + 25;
            var playerName = AtkNodeHelper.ConvertAtkValueToString(addon->AtkValues[offset]);
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
            _plugin.StorageManager.UpdateCCMatch(_currentMatch);
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
            string translatedJobName = _plugin.LocalizationManager.TranslateDataTableEntry<ClassJob>(pc.ClassJob.GameData.Name.ToString(), "Name", ClientLanguage.English);
            bool jobMatch = player.Job.Equals(PlayerJobHelper.GetJobFromName(translatedJobName));
            bool isSelf = _plugin.ClientState.LocalPlayer.ObjectId == pc.ObjectId;
            bool teamMatch = isPartyMember is null || ((bool)isPartyMember && pc.StatusFlags.HasFlag(StatusFlags.PartyMember) || !(bool)isPartyMember && !pc.StatusFlags.HasFlag(StatusFlags.PartyMember));
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
