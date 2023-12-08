using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace PvpStats.Windows.Detail;

enum SortableColumn {
    Name,
    Job,
    Kills,
    Deaths,
    Assists,
    DamageDealt,
    DamageTaken,
    HPRestored,
    TimeOnCrystal,
    DamageDealtPerKill,
    DamageDealtPerDeath,
    DamageTakenPerDeath,
    HPPerTeamDeath,
    KDA,
}

public struct TeamContribution {
    public double Kills, Deaths, Assists, DamageDealt, DamageTaken, HPRestored, TimeOnCrystal;
}

public struct AdvancedStats {
    public double DamageDealtPerKill, DamageDealtPerDeath, DamageTakenPerDeath, HPPerTeamDeath;
}


internal class CrystallineConflictMatchDetail : Window {

    private Plugin _plugin;
    private CrystallineConflictMatch _dataModel;
    private List<CrystallineConflictPostMatchRow> _postMatchRows;
    private ImGuiTableSortSpecsPtr _currentSortSpecs;
    private Dictionary<CrystallineConflictPostMatchRow, TeamContribution> _teamContributionStats;
    private Dictionary<CrystallineConflictPostMatchRow, AdvancedStats> _advancedStats;

    bool _showPercentages;
    bool _showAdvancedStats;

    private SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

    internal CrystallineConflictMatchDetail(Plugin plugin, CrystallineConflictMatch match) : base($"Match Details: {match.Id}") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Appearing;
        CollapsedCondition = ImGuiCond.Appearing;
        Position = new Vector2(0, 0);
        Collapsed = false;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(800, 1500)
        };
        _plugin = plugin;
        _dataModel = match;

        //sort team players
        foreach (var team in _dataModel.Teams) {
            team.Value.Players = team.Value.Players.OrderBy(p => p.Job).ToList();
        }

        //setup post match data
        if (_dataModel.PostMatch is not null) {
            _postMatchRows = new();
            _teamContributionStats = new();
            foreach (var team in _dataModel.PostMatch.Teams) {
                var teamStats = team.Value.TeamStats;
                if (teamStats.Team is null) {
                    teamStats.Team = team.Key;
                }
                _postMatchRows.Add(teamStats);

                foreach (var player in team.Value.PlayerStats) {
                    if (player.Team is null) {
                        player.Team = team.Key;
                    }
                    _postMatchRows.Add(player);
                    _teamContributionStats.Add(player, new TeamContribution {
                        Kills = (double)player.Kills / teamStats.Kills,
                        Deaths = (double)player.Deaths / teamStats.Deaths,
                        Assists = (double)player.Assists / teamStats.Assists,
                        DamageDealt = (double)player.DamageDealt / teamStats.DamageDealt,
                        DamageTaken = (double)player.DamageTaken / teamStats.DamageTaken,
                        HPRestored = (double)player.HPRestored / teamStats.HPRestored,
                        TimeOnCrystal = player.TimeOnCrystal / teamStats.TimeOnCrystal,
                    });
                }
            }
        }
        _currentSortSpecs = new();
    }

    public void Open(CrystallineConflictMatch match) {
        _dataModel = match;
        IsOpen = true;
    }

    public override void OnClose() {
        _plugin.WindowSystem.RemoveWindow(this);
        //foreach (var window in _plugin.WindowSystem.Windows) {
        //    _plugin.Log.Debug($"window name: {window.WindowName}");
        //}
    }

    public override void PreDraw() {
        base.PreDraw();
    }

    public override void Draw() {
        ImGui.BeginTable("header", 2, ImGuiTableFlags.PadOuterX);
        ImGui.TableSetupColumn("arena", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("time", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        //ImGui.Indent();
        ImGui.Text($"{MatchHelper.GetArenaName(_dataModel.Arena)}");
        ImGui.TableNextColumn();
        var dutyStartTime = _dataModel.DutyStartTime.ToString();
        ImGuiHelper.RightAlignCursor(dutyStartTime);
        ImGui.Text($"{dutyStartTime}");
        ImGui.EndTable();

        ImGui.BeginTable("subheader", 3, ImGuiTableFlags.PadOuterX);
        ImGui.TableSetupColumn("queue", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("result", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("duration", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text($"{_dataModel.MatchType}");
        ImGui.TableNextColumn();
        bool noWinner = _dataModel.MatchWinner is null;
        bool isWin = _dataModel.MatchWinner == _dataModel.LocalPlayerTeam?.TeamName;
        var color = isWin ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
        color = noWinner ? ImGuiColors.DalamudGrey : color;
        string resultText = isWin ? "WIN" : "LOSS";
        resultText = noWinner ? "UNKNOWN" : resultText;
        ImGuiHelpers.CenterCursorForText(resultText);
        ImGui.TextColored(color, resultText);
        ImGui.TableNextColumn();
        string durationText = "";
        if (_dataModel.MatchStartTime != null && _dataModel.MatchEndTime != null) {
            var duration = _dataModel.MatchEndTime - _dataModel.MatchStartTime;
            durationText = $"{duration.Value.Minutes}{duration.Value.ToString(@"\:ss")}";
            ImGuiHelper.RightAlignCursor(durationText);
            ImGui.Text(durationText);
        }
        ImGui.EndTable();

        if (_dataModel.Teams.Count == 2) {
            var firstTeam = _dataModel.Teams.ElementAt(0).Value;
            var secondTeam = _dataModel.Teams.ElementAt(1).Value;

            ImGui.BeginTable("teams", 2, ImGuiTableFlags.PadOuterX);
            ImGui.TableSetupColumn("team1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("team2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var firstTeamName = MatchHelper.GetTeamName(firstTeam.TeamName);
            ImGuiHelper.CenterAlignCursor(firstTeamName);
            ImGui.Text($"{firstTeamName}");
            ImGui.TableNextColumn();
            var secondTeamName = MatchHelper.GetTeamName(secondTeam.TeamName);
            ImGuiHelper.CenterAlignCursor(secondTeamName);
            ImGui.Text($"{secondTeamName}");
            ImGui.TableNextColumn();
            var firstTeamProgress = string.Format("   {0:P1}%", firstTeam.Progress / 100f);
            ImGuiHelper.CenterAlignCursor(firstTeamProgress);
            ImGui.Text($"{firstTeamProgress}");
            ImGui.TableNextColumn();
            var secondTeamProgress = string.Format("   {0:P1}%", secondTeam.Progress / 100f);
            ImGuiHelper.CenterAlignCursor(secondTeamProgress);
            ImGui.Text($"{secondTeamProgress}");
            ImGui.EndTable();

            ImGui.BeginTable("players", 6, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoClip);
            ImGui.TableSetupColumn("rankteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
            ImGui.TableSetupColumn("playerteam1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("jobteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 26f);
            ImGui.TableSetupColumn("jobteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 26f);
            ImGui.TableSetupColumn("playerteam2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("rankteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
            ImGui.TableNextRow();

            int maxSize = int.Max(firstTeam.Players.Count, secondTeam.Players.Count);

            for (int i = 0; i < maxSize; i++) {
                if (i < firstTeam.Players.Count) {
                    ImGui.TableNextColumn();
                    string rank0 = firstTeam.Players[i].Rank?.Tier != ArenaTier.None ? firstTeam.Players[i].Rank!.ToString() : "";
                    ImGuiHelper.RightAlignCursor(rank0);
                    ImGui.Text(rank0);
                    ImGui.TableNextColumn();
                    var playerColor0 = _dataModel.LocalPlayerTeam is not null && firstTeam.TeamName == _dataModel.LocalPlayerTeam.TeamName ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                    playerColor0 = _dataModel.LocalPlayer.Equals(firstTeam.Players[i]) ? ImGuiColors.DalamudYellow : playerColor0;
                    string playerName0 = firstTeam.Players[i].Alias.Name;
                    ImGuiHelper.RightAlignCursor(playerName0);
                    ImGui.TextColored(playerColor0, playerName0);
                    ImGui.TableNextColumn();
                    //string playerJob0 = firstTeam.Players[i].Job.ToString();
                    //ImGui.Text(playerJob0);
                    ImGui.Image(_plugin.JobIcons[firstTeam.Players[i].Job].ImGuiHandle, new Vector2(24, 24));
                }
                else {
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                }

                if (i < secondTeam.Players.Count) {
                    ImGui.TableNextColumn();
                    //string playerJob1 = secondTeam.Players[i].Job.ToString();
                    //ImGui.Text(playerJob1);
                    ImGui.Image(_plugin.JobIcons[secondTeam.Players[i].Job].ImGuiHandle, new Vector2(24, 24));
                    ImGui.TableNextColumn();
                    var playerColor1 = _dataModel.LocalPlayerTeam is not null && secondTeam.TeamName == _dataModel.LocalPlayerTeam.TeamName ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                    playerColor1 = _dataModel.LocalPlayer.Equals(secondTeam.Players[i]) ? ImGuiColors.DalamudYellow : playerColor1;
                    string playerName1 = secondTeam.Players[i].Alias.Name;
                    ImGui.TextColored(playerColor1, $"     {playerName1}");
                    ImGui.TableNextColumn();
                    string rank1 = secondTeam.Players[i].Rank?.Tier != ArenaTier.None ? secondTeam.Players[i].Rank!.ToString() : "";
                    ImGui.Text(rank1);
                }
                else {
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                }

                //ImGui.TableNextColumn();
            }
            ImGui.EndTable();
        }
        ImGui.NewLine();
        if (_dataModel.PostMatch is null) {
            ImGui.Text("Post game statistics unavailable.");
        }
        else {
            ImGui.Checkbox("Show team contribution", ref _showPercentages);
            ImGui.SameLine();
            ImGui.Checkbox("Show advanced stats", ref _showAdvancedStats);
            //if (ImGui.Checkbox("Show percentages", ref _showPercentages)) {

            //}


            DrawStatsTable();
            //ImGui.BeginTable("players", 8, ImGuiTableFlags.Sortable);
            //ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, (uint)SortableColumn.Name);
            //ImGui.TableSetupColumn("Kills", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Kills);
            //ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Deaths);
            //ImGui.TableSetupColumn("Assists", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Assists);
            //ImGui.TableSetupColumn("Damage\nDealt", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.DamageDealt);
            //ImGui.TableSetupColumn("Damage\nTaken", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.DamageTaken);
            //ImGui.TableSetupColumn("HP\nRestored", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.HPRestored);
            //ImGui.TableSetupColumn("Time on\nCrystal", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f, (uint)SortableColumn.TimeOnCrystal);

            ////ImGui.TableHeadersRow();
            //ImGui.TableNextColumn();
            //ImGui.TableHeader("");
            //ImGui.TableNextColumn();
            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
            //ImGui.TableHeader("Kills");
            //ImGui.TableNextColumn();
            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
            //ImGui.TableHeader("Deaths");
            //ImGui.TableNextColumn();
            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
            //ImGui.TableHeader("Assists");
            //ImGui.TableNextColumn();
            //ImGui.TableHeader("Damage\nDealt");
            //ImGui.TableNextColumn();
            //ImGui.TableHeader("Damage\nTaken");
            //ImGui.TableNextColumn();
            //ImGui.TableHeader("HP\nRestored");
            //ImGui.TableNextColumn();
            //ImGui.TableHeader("Time on\nCrystal");

            ////this is horrible
            //ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
            //if(sortSpecs.SpecsDirty) {
            //    switch(sortSpecs.Specs.ColumnIndex) {
            //        default:
            //        case (int)SortableColumn.Name:
            //            break;
            //        case (int)SortableColumn.Kills:
            //            break;
            //        case (int)SortableColumn.Deaths:
            //            break;
            //        case (int)SortableColumn.Assists:
            //            break;
            //        case (int)SortableColumn.DamageDealt:
            //            break;
            //        case (int)SortableColumn.DamageTaken:
            //            break;
            //        case (int)SortableColumn.HPRestored:
            //            break;
            //        case (int)SortableColumn.TimeOnCrystal:
            //            break;
            //    }
            //}

            //ImGui.TableNextRow();

            //foreach (var team in _dataModel.PostMatch.Teams) {
            //    //ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.7058824f, 0f, 0f, 1f));
            //    ImGui.TableNextColumn();
            //    var rowColor = team.Key == _dataModel.LocalPlayerTeam.TeamName ? new Vector4(0f, 0.6f, 1f, 0.5f) : new Vector4(0.7058824f, 0f, 0f, 0.5f);
            //    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            //    ImGui.Text($" {MatchHelper.GetTeamName(team.Value.TeamName)}");
            //    ImGui.TableNextColumn();
            //    ImGui.Text($"{team.Value.TeamStats.Kills}");
            //    ImGui.TableNextColumn();
            //    ImGui.Text($"{team.Value.TeamStats.Deaths}");
            //    ImGui.TableNextColumn();
            //    ImGui.Text($"{team.Value.TeamStats.Assists}");
            //    ImGui.TableNextColumn();
            //    ImGui.Text($"{team.Value.TeamStats.DamageDealt}");
            //    ImGui.TableNextColumn();
            //    ImGui.Text($"{team.Value.TeamStats.DamageTaken}");
            //    ImGui.TableNextColumn();
            //    ImGui.Text($"{team.Value.TeamStats.HPRestored}");
            //    ImGui.TableNextColumn();
            //    ImGui.Text($"{team.Value.TeamStats.TimeOnCrystal.Minutes}{team.Value.TeamStats.TimeOnCrystal.ToString(@"\:ss")}");
            //    //ImGui.PopStyleColor();
            //    foreach (var player in team.Value.PlayerStats) {
            //        ImGui.TableNextColumn();
            //        rowColor = team.Key == _dataModel.LocalPlayerTeam.TeamName ? new Vector4(0f, 0.6f, 1f, 0.2f) : new Vector4(0.7058824f, 0f, 0f, 0.2f);
            //        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            //        //ImGui.Text($" {player.Player.Name}");
            //        ImGui.TextColored(_dataModel.LocalPlayer.Equals(player.Player) ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite, $" {player.Player.Name}");
            //        ImGui.TableNextColumn();
            //        ImGui.Text($"{player.Kills}");
            //        ImGui.TableNextColumn();
            //        ImGui.Text($"{player.Deaths}");
            //        ImGui.TableNextColumn();
            //        ImGui.Text($"{player.Assists}");
            //        ImGui.TableNextColumn();
            //        ImGui.Text($"{player.DamageDealt}");
            //        ImGui.TableNextColumn();
            //        ImGui.Text($"{player.DamageTaken}");
            //        ImGui.TableNextColumn();
            //        ImGui.Text($"{player.HPRestored}");
            //        ImGui.TableNextColumn();
            //        ImGui.Text($"{player.TimeOnCrystal.Minutes}{player.TimeOnCrystal.ToString(@"\:ss")}");
            //    }
            //}
            //ImGui.EndTable();
        }
        //ImGui.Image(_plugin.TextureProvider.GetIcon(62123).ImGuiHandle, new Vector2(25,25));

        //ImGui.Text(_dataModel.Teams.ElementAt(0).Value.Players
    }

    private void DrawStatsTable() {

        ImGui.BeginTable($"players##{_dataModel.Id}", 9, ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, (uint)SortableColumn.Name);
        ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f, (uint)SortableColumn.Job);
        ImGui.TableSetupColumn("Kills", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Kills);
        ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Deaths);
        ImGui.TableSetupColumn("Assists", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Assists);
        ImGui.TableSetupColumn("Damage\nDealt", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.DamageDealt);
        ImGui.TableSetupColumn("Damage\nTaken", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.DamageTaken);
        ImGui.TableSetupColumn("HP\nRestored", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.HPRestored);
        ImGui.TableSetupColumn("Time on\nCrystal", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f, (uint)SortableColumn.TimeOnCrystal);

        //ImGui.TableHeadersRow();
        ImGui.TableNextColumn();
        ImGui.TableHeader("");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
        ImGui.TableHeader("Job");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
        ImGui.TableHeader("Kills");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
        ImGui.TableHeader("Deaths");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
        ImGui.TableHeader("Assists");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage\nDealt");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage\nTaken");
        ImGui.TableNextColumn();
        ImGui.TableHeader("HP\nRestored");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Time on\nCrystal");

        //this is horrible
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs.SpecsDirty) {
            SortByColumn((SortableColumn)sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
            //switch (sortSpecs.Specs.ColumnUserID) {
            //    default:
            //    case (int)SortableColumn.Name:
            //        var nameSort = (CrystallineConflictPostMatchRow r) => {
            //            return r.Player != null ? r.Player.Name : r.Team.ToString();
            //        };
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(nameSort).ToList() : _postMatchRows.OrderByDescending(nameSort).ToList();
            //        break;
            //    case (int)SortableColumn.Job:
            //        var jobSort = (CrystallineConflictPostMatchRow r) => r.Job;
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(jobSort).ToList() : _postMatchRows.OrderByDescending(jobSort).ToList();
            //        break;
            //    case (int)SortableColumn.Kills:
            //        var killSort = (CrystallineConflictPostMatchRow r) => {
            //            if(r.Player is not null && _showPercentages) {
            //                return _teamContributionStats[r].Kills;
            //            }
            //            return r.Kills;
            //        };
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(killSort).ToList() : _postMatchRows.OrderByDescending(killSort).ToList();
            //        break;
            //    case (int)SortableColumn.Deaths:
            //        var deathSort = (CrystallineConflictPostMatchRow r) => {
            //            if (r.Player is not null && _showPercentages) {
            //                return _teamContributionStats[r].Deaths;
            //            }
            //            return r.Deaths;
            //        };
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(deathSort).ToList() : _postMatchRows.OrderByDescending(deathSort).ToList();
            //        break;
            //    case (int)SortableColumn.Assists:
            //        var assistSort = (CrystallineConflictPostMatchRow r) => {
            //            if (r.Player is not null && _showPercentages) {
            //                return _teamContributionStats[r].Assists;
            //            }
            //            return r.Assists;
            //        };
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(assistSort).ToList() : _postMatchRows.OrderByDescending(assistSort).ToList();
            //        break;
            //    case (int)SortableColumn.DamageDealt:
            //        var ddSort = (CrystallineConflictPostMatchRow r) => {
            //            if (r.Player is not null && _showPercentages) {
            //                return _teamContributionStats[r].DamageDealt;
            //            }
            //            return r.DamageDealt;
            //        };
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(ddSort).ToList() : _postMatchRows.OrderByDescending(ddSort).ToList();
            //        break;
            //    case (int)SortableColumn.DamageTaken:
            //        var dtSort = (CrystallineConflictPostMatchRow r) => {
            //            if (r.Player is not null && _showPercentages) {
            //                return _teamContributionStats[r].DamageTaken;
            //            }
            //            return r.DamageTaken;
            //        };
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(dtSort).ToList() : _postMatchRows.OrderByDescending(dtSort).ToList();
            //        break;
            //    case (int)SortableColumn.HPRestored:
            //        var hpSort = (CrystallineConflictPostMatchRow r) => {
            //            if (r.Player is not null && _showPercentages) { 
            //                return _teamContributionStats[r].HPRestored; 
            //            } 
            //            return r.HPRestored; 
            //        };
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(hpSort).ToList() : _postMatchRows.OrderByDescending(hpSort).ToList();
            //        break;
            //    case (int)SortableColumn.TimeOnCrystal:
            //        var tcSort = (CrystallineConflictPostMatchRow r) => {
            //            if (r.Player is not null && _showPercentages) {
            //                return _teamContributionStats[r].TimeOnCrystal;
            //            }
            //            return r.TimeOnCrystal.Ticks;
            //        };
            //        _postMatchRows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(tcSort).ToList() : _postMatchRows.OrderByDescending(tcSort).ToList();
            //        break;
            //}
            sortSpecs.SpecsDirty = false;
        }

        ImGui.TableNextRow();

        foreach (var row in _postMatchRows) {
            ImGui.TableNextColumn();
            bool isPlayer = row.Player != null;
            bool isPlayerTeam = row.Team == _dataModel.LocalPlayerTeam.TeamName;
            var rowColor = new Vector4(0, 0, 0, 0);
            switch ((isPlayer, isPlayerTeam)) {
                case (true, true):
                    rowColor = ImGuiColors.TankBlue - new Vector4(0f, 0f, 0f, 0.7f);
                    break;
                case (true, false):
                    rowColor = ImGuiColors.DPSRed - new Vector4(0f, 0f, 0f, 0.7f);
                    break;
                case (false, true):
                    rowColor = ImGuiColors.TankBlue - new Vector4(0f, 0f, 0f, 0.3f);
                    break;
                case (false, false):
                    rowColor = ImGuiColors.DPSRed - new Vector4(0f, 0f, 0f, 0.3f);
                    break;
            }
            var textColor = _dataModel.LocalPlayer.Equals(row.Player) ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite;
            string rowText = "";
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            if (isPlayer) {
                ImGui.TextColored(textColor, $" {row.Player.Name}");
            }
            else {
                ImGui.TextColored(textColor, $" {MatchHelper.GetTeamName((CrystallineConflictTeamName)row.Team)}");
            }
            //ImGui.Text($" {player.Player.Name}");
            //ImGui.TextColored(_dataModel.LocalPlayer.Equals(player.Player) ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite, $" {player.Player.Name}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer ? row.Job : "")}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].Kills) : row.Kills)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].Deaths) : row.Deaths)}");
            //ImGui.TextColored(textColor, $"{row.Deaths}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].Assists) : row.Assists)}");
            //ImGui.TextColored(textColor, $"{row.Assists}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].DamageDealt) : row.DamageDealt)}");
            //ImGui.TextColored(textColor, $"{row.DamageDealt}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].DamageTaken) : row.DamageTaken)}");
            //ImGui.TextColored(textColor, $"{row.DamageTaken}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].HPRestored) : row.HPRestored)}");
            //ImGui.TextColored(textColor, $"{row.HPRestored}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].TimeOnCrystal) : $"{row.TimeOnCrystal.Minutes}{row.TimeOnCrystal.ToString(@"\:ss")}")}");
            //ImGui.TextColored(textColor, $"{row.TimeOnCrystal.Minutes}{row.TimeOnCrystal.ToString(@"\:ss")}");
        }
        ImGui.EndTable();
    }

    private void SortPostMatchTable(ImGuiTableColumnSortSpecsPtr specs) {

    }

    private void SortByColumn(SortableColumn column, ImGuiSortDirection direction) {
        //Type returnType;
        //switch(column) {
        //    case SortableColumn.Name:
        //        returnType = typeof(string);
        //        break;
        //    default:
        //        returnType = typeof(double);
        //        break;
        //}

        Func<CrystallineConflictPostMatchRow, object>? comparator;


        var property = typeof(CrystallineConflictPostMatchRow).GetProperty(column.ToString());
        if(property is null) {
            switch(column) {
                case SortableColumn.Name:
                    comparator = (r) => r.Player is not null ? r.Player.Name : r.Team.ToString();
                    break;
                default: 
                    comparator = null;
                    break;
            }
        } else {
            comparator = (r) => {
                switch(column) {
                    case SortableColumn.Job:
                        break;
                    default:
                        if (r.Player is not null && _showPercentages) {
                            var percentageProperty = typeof(TeamContribution).GetProperty(column.ToString());
                            if(percentageProperty is not null) {
                                return percentageProperty.GetValue(_teamContributionStats[r]);
                            }
                        }
                        break;
                }
                return property.GetValue(r);
            };
        }

        _postMatchRows = direction == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(comparator).ToList() : _postMatchRows.OrderByDescending(comparator).ToList();


    }

}
