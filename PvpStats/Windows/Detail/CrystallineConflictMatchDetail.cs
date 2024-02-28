using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
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

internal class CrystallineConflictMatchDetail : Window {

    private enum SortableColumn {
        Name,
        Job,
        Kills,
        Deaths,
        Assists,
        DamageDealt,
        DamageTaken,
        HPRestored,
        TimeOnCrystal,
        DamageDealtPerKillAssist,
        DamageDealtPerDeath,
        DamageTakenPerDeath,
        HPPerDeath,
        HPPerTeamDeath,
        KDA,
    }

    private struct TeamContribution {
        public double Kills, Deaths, Assists, DamageDealt, DamageTaken, HPRestored, TimeOnCrystal;
    }

    private struct AdvancedStats {
        public double DamageDealtPerKillAssist, DamageDealtPerDeath, DamageTakenPerDeath, HPPerDeath;
    }

    private Plugin _plugin;
    private CrystallineConflictMatch _dataModel;
    private List<CrystallineConflictPostMatchRow> _postMatchRows = new();
    private Dictionary<CrystallineConflictPostMatchRow, TeamContribution> _teamContributionStats = new();
    private Dictionary<CrystallineConflictPostMatchRow, AdvancedStats> _advancedStats = new();

    bool _showPercentages;

    private SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

    internal CrystallineConflictMatchDetail(Plugin plugin, CrystallineConflictMatch match) : base($"Match Details: {match.Id}") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Appearing;
        CollapsedCondition = ImGuiCond.Appearing;
        Position = new Vector2(0, 0);
        Collapsed = false;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(1200, 1500)
        };
        Flags = Flags | ImGuiWindowFlags.AlwaysAutoResize;
        _plugin = plugin;
        _dataModel = match;

        //sort team players
        foreach(var team in _dataModel.Teams) {
            team.Value.Players = team.Value.Players.OrderBy(p => p.Job).ToList();
        }

        //setup post match data
        if(_dataModel.PostMatch is not null) {
            foreach(var team in _dataModel.PostMatch.Teams) {
                var teamStats = team.Value.TeamStats;
                if(teamStats.Team is null) {
                    teamStats.Team = team.Key;
                }
                _postMatchRows.Add(teamStats);
                _advancedStats.Add(teamStats, new AdvancedStats {
                    DamageDealtPerKillAssist = teamStats.DamageDealt / double.Max(teamStats.Kills, 1),
                    DamageDealtPerDeath = teamStats.DamageDealt / double.Max(teamStats.Deaths + 5, 1),
                    DamageTakenPerDeath = teamStats.DamageTaken / double.Max(teamStats.Deaths + 5, 1),
                    HPPerDeath = teamStats.HPRestored / double.Max(teamStats.Deaths + 5, 1),
                });

                foreach(var player in team.Value.PlayerStats) {
                    if(player.Team is null) {
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
                    _advancedStats.Add(player, new AdvancedStats {
                        DamageDealtPerKillAssist = player.DamageDealt / double.Max(player.Kills + player.Assists, 1),
                        DamageDealtPerDeath = player.DamageDealt / double.Max(player.Deaths + 1, 1),
                        DamageTakenPerDeath = player.DamageTaken / double.Max(player.Deaths + 1, 1),
                        HPPerDeath = player.HPRestored / double.Max(player.Deaths + 1, 1),
                    });
                }
            }
        }
    }

    public void Open(CrystallineConflictMatch match) {
        _dataModel = match;
        IsOpen = true;
    }

    public override void OnClose() {
        _plugin.WindowManager.RemoveWindow(this);
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
        if(_dataModel.Arena != null) {
            ImGui.Text($"{MatchHelper.GetArenaName((CrystallineConflictMap)_dataModel.Arena)}");
        }
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
        bool isSpectated = _dataModel.LocalPlayerTeam is null;
        bool isWin = _dataModel.MatchWinner == _dataModel.LocalPlayerTeam?.TeamName;
        var color = ImGuiColors.DalamudWhite;
        color = noWinner ? ImGuiColors.DalamudGrey : color;
        string resultText = "";
        if(isSpectated && _dataModel.MatchWinner is not null) {
            color = _dataModel.MatchWinner == CrystallineConflictTeamName.Astra ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
            resultText = MatchHelper.GetTeamName((CrystallineConflictTeamName)_dataModel.MatchWinner) + " WINS";
        } else {
            color = isWin ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
            color = noWinner ? ImGuiColors.DalamudGrey : color;
            resultText = isWin ? "WIN" : "LOSS";
            resultText = noWinner ? "UNKNOWN" : resultText;
        }
        ImGuiHelpers.CenterCursorForText(resultText);
        ImGui.TextColored(color, resultText);
        ImGui.TableNextColumn();
        string durationText = "";
        if(_dataModel.MatchStartTime != null && _dataModel.MatchEndTime != null) {
            var duration = _dataModel.MatchEndTime - _dataModel.MatchStartTime;
            durationText = $"{duration.Value.Minutes}{duration.Value.ToString(@"\:ss")}";
            ImGuiHelper.RightAlignCursor(durationText);
            ImGui.Text(durationText);
        }
        ImGui.EndTable();

        if(_dataModel.Teams.Count == 2) {
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

            ImGui.BeginTable($"players##{_dataModel.Id}", 6, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoClip);
            ImGui.TableSetupColumn("rankteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
            ImGui.TableSetupColumn("playerteam1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("jobteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 26f);
            ImGui.TableSetupColumn("jobteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 26f);
            ImGui.TableSetupColumn("playerteam2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("rankteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
            ImGui.TableNextRow();

            int maxSize = int.Max(firstTeam.Players.Count, secondTeam.Players.Count);

            for(int i = 0; i < maxSize; i++) {
                if(i < firstTeam.Players.Count) {
                    var player0 = firstTeam.Players[i];
                    ImGui.TableNextColumn();
                    string rank0 = player0.Rank != null && player0.Rank!.Tier != ArenaTier.None ? player0.Rank!.ToString() : "";
                    ImGuiHelper.RightAlignCursor(rank0);
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(rank0);
                    ImGui.TableNextColumn();
                    var playerColor0 = _dataModel.LocalPlayerTeam is not null && firstTeam.TeamName == _dataModel.LocalPlayerTeam.TeamName ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                    playerColor0 = _dataModel.LocalPlayer is not null && _dataModel.LocalPlayer.Equals(player0) ? ImGuiColors.DalamudYellow : playerColor0;
                    if(isSpectated) {
                        playerColor0 = firstTeam.TeamName == CrystallineConflictTeamName.Astra ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                    }
                    string playerName0 = player0.Alias.Name;
                    ImGuiHelper.RightAlignCursor(playerName0);
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(playerColor0, playerName0);
                    ImGuiHelper.WrappedTooltip(player0.Alias.HomeWorld);
                    ImGui.TableNextColumn();
                    if(player0.Job != null && _plugin.WindowManager.JobIcons.ContainsKey((Job)player0.Job)) {
                        ImGui.Image(_plugin.WindowManager.JobIcons[(Job)player0.Job].ImGuiHandle, new Vector2(24, 24));
                    }
                } else {
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                }

                if(i < secondTeam.Players.Count) {
                    var player1 = secondTeam.Players[i];
                    ImGui.TableNextColumn();
                    if(player1.Job != null && _plugin.WindowManager.JobIcons.ContainsKey((Job)player1.Job)) {
                        ImGui.Image(_plugin.WindowManager.JobIcons[(Job)player1.Job].ImGuiHandle, new Vector2(24, 24));
                    }
                    ImGui.TableNextColumn();
                    var playerColor1 = _dataModel.LocalPlayerTeam is not null && secondTeam.TeamName == _dataModel.LocalPlayerTeam.TeamName ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                    playerColor1 = _dataModel.LocalPlayer is not null && _dataModel.LocalPlayer.Equals(player1) ? ImGuiColors.DalamudYellow : playerColor1;
                    if(isSpectated) {
                        playerColor1 = secondTeam.TeamName == CrystallineConflictTeamName.Astra ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                    }
                    string playerName1 = secondTeam.Players[i].Alias.Name;
                    ImGui.TextColored(playerColor1, $"     {playerName1}");
                    ImGuiHelper.WrappedTooltip(secondTeam.Players[i].Alias.HomeWorld);
                    ImGui.TableNextColumn();
                    string rank1 = player1.Rank != null && player1.Rank?.Tier != ArenaTier.None ? player1.Rank!.ToString() : "";
                    ImGui.Text(rank1);
                } else {
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                }

                //ImGui.TableNextColumn();
            }
            ImGui.EndTable();
        }
        ImGui.NewLine();
        if(_dataModel.PostMatch is null) {
            ImGui.Text("Post game statistics unavailable.");
        } else {
            //#if DEBUG
            //            foreach(var team in _dataModel.PostMatch.Teams) {
            //                ImGui.Text($"{team.Key}: {team.Value.Progress}");
            //                ImGui.SameLine();
            //            }
            //            ImGui.Text($"winner: {_dataModel.PostMatch.MatchWinner}");
            //            ImGui.SameLine();
            //            ImGui.Text($"duration: {_dataModel.PostMatch.MatchDuration.TotalSeconds}");
            //            //ImGui.NewLine();
            //#endif

            if(_dataModel.MatchType == CrystallineConflictMatchType.Ranked && _dataModel.PostMatch.RankBefore is not null && _dataModel.PostMatch.RankAfter is not null) {
                ImGui.Text($"Rank Change: {_dataModel.PostMatch.RankBefore.ToString()} → {_dataModel.PostMatch.RankAfter.ToString()}");
            }
            ImGuiComponents.ToggleButton("##showPercentages", ref _showPercentages);
            ImGui.SameLine();
            ImGui.Text("Show team contributions");
            ImGuiHelper.HelpMarker("Right-click table header to show and hide columns.");
            //if(ImGui.BeginChild("statsTableChild")) {
            DrawStatsTable();
            //    ImGui.EndChild();
            //}
            //ImGui.NewLine();
        }
    }

    private void DrawStatsTable() {
        if(ImGui.BeginTable($"postmatchplayers##{_dataModel.Id}", 13, ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable)) {

            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, (uint)SortableColumn.Name);
            ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f, (uint)SortableColumn.Job);
            ImGui.TableSetupColumn("Kills", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Kills);
            ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Deaths);
            ImGui.TableSetupColumn("Assists", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)SortableColumn.Assists);
            ImGui.TableSetupColumn("Damage Dealt", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.DamageDealt);
            ImGui.TableSetupColumn("Damage Taken", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.DamageTaken);
            ImGui.TableSetupColumn("HP Restored", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)SortableColumn.HPRestored);
            ImGui.TableSetupColumn("Time on Crystal", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f, (uint)SortableColumn.TimeOnCrystal);
            ImGui.TableSetupColumn("Damage Dealt per Kill/Assist", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)SortableColumn.DamageDealtPerKillAssist);
            ImGui.TableSetupColumn("Damage Dealt per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)SortableColumn.DamageDealtPerDeath);
            ImGui.TableSetupColumn("Damage Taken per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)SortableColumn.DamageTakenPerDeath);
            ImGui.TableSetupColumn("HP Restored per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)SortableColumn.HPPerDeath);
            ImGui.TableSetupScrollFreeze(1, 0);
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
            ImGui.TableNextColumn();
            ImGui.TableHeader("Damage Dealt\nper Kill/Assist");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Damage Dealt\nper Life");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Damage Taken\nper Life");
            ImGui.TableNextColumn();
            ImGui.TableHeader("HP Restored\nper Life");

            //column sorting
            ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
            if(sortSpecs.SpecsDirty) {
                SortByColumn((SortableColumn)sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
                sortSpecs.SpecsDirty = false;
            }

            ImGui.TableNextRow();

            foreach(var row in _postMatchRows) {
                ImGui.TableNextColumn();
                bool isPlayer = row.Player != null;
                bool isPlayerTeam = row.Team == _dataModel.LocalPlayerTeam?.TeamName;
                if(_dataModel.IsSpectated) {
                    isPlayerTeam = row.Team == CrystallineConflictTeamName.Astra;
                }
                var rowColor = new Vector4(0, 0, 0, 0);
                switch((isPlayer, isPlayerTeam)) {
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
                var textColor = _dataModel.LocalPlayer is not null && _dataModel.LocalPlayer.Equals(row.Player) ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite;
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
                if(isPlayer) {
                    ImGui.TextColored(textColor, $" {row.Player?.Name} ");
                } else {
                    ImGui.TextColored(textColor, $" {MatchHelper.GetTeamName(row.Team ?? CrystallineConflictTeamName.Unknown)}");
                }
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(isPlayer ? row.Job : "")}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].Kills) : row.Kills)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].Deaths) : row.Deaths)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].Assists) : row.Assists)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].DamageDealt) : row.DamageDealt)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].DamageTaken) : row.DamageTaken)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].HPRestored) : row.HPRestored)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", _teamContributionStats[row].TimeOnCrystal) : $"{row.TimeOnCrystal.Minutes}{row.TimeOnCrystal.ToString(@"\:ss")}")}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{string.Format("{0:f0}", _advancedStats[row].DamageDealtPerKillAssist)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{string.Format("{0:f0}", _advancedStats[row].DamageDealtPerDeath)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{string.Format("{0:f0}", _advancedStats[row].DamageTakenPerDeath)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{string.Format("{0:f0}", _advancedStats[row].HPPerDeath)}");
            }
            ImGui.EndTable();
        }
    }

    private void SortByColumn(SortableColumn column, ImGuiSortDirection direction) {
        Func<CrystallineConflictPostMatchRow, object> comparator = (r) => 0;

        var rowProperty = typeof(CrystallineConflictPostMatchRow).GetProperty(column.ToString());
        if(rowProperty is null) {
            switch(column) {
                case SortableColumn.Name:
                    comparator = (r) => (r.Player is not null ? r.Player.Name : r.Team.ToString()) ?? "";
                    break;
                default:
                    var advancedField = typeof(AdvancedStats).GetField(column.ToString());
                    if(advancedField is not null) {
                        comparator = (r) => advancedField.GetValue(_advancedStats[r]) ?? 0;
                    }
                    break;
            }
        } else if(rowProperty.PropertyType.IsEnum) {
            comparator = (r) => rowProperty.GetValue(r) ?? 0;
        } else {
            comparator = (r) => {
                if(r.Player is not null && _showPercentages) {
                    var percentageField = typeof(TeamContribution).GetField(column.ToString());
                    if(percentageField is not null) {
                        return percentageField.GetValue(_teamContributionStats[r]) ?? 0;
                    }
                }

                switch(column) {
                    case SortableColumn.TimeOnCrystal:
                        return (double)r.TimeOnCrystal.Ticks;
                    default:
                        return Convert.ToDouble(rowProperty.GetValue(r));
                }
            };
        }
        _postMatchRows = direction == ImGuiSortDirection.Ascending ? _postMatchRows.OrderBy(comparator).ToList() : _postMatchRows.OrderByDescending(comparator).ToList();
    }
}
