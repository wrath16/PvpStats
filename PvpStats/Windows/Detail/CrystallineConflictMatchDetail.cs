using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PvpStats.Windows.Detail;

internal class CrystallineConflictMatchDetail : Window {

    private Plugin _plugin;
    private CrystallineConflictMatch _dataModel;
    private Dictionary<CrystallineConflictPostMatchRow, (CCScoreboard, CCScoreboardDouble)> _scoreboard = new();

    bool _showPercentages;
    private string _csv;

    internal CrystallineConflictMatchDetail(Plugin plugin, CrystallineConflictMatch match) : base($"Match Details: {match.Id}") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Appearing;
        CollapsedCondition = ImGuiCond.Appearing;
        Position = new Vector2(0, 0);
        Collapsed = false;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(650, 400),
            MaximumSize = new Vector2(5000, 5000)
        };
        Flags |= ImGuiWindowFlags.NoSavedSettings;
        if(!plugin.Configuration.ResizeableMatchWindow) {
            Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        }
        _plugin = plugin;
        _dataModel = match;

        //sort team players
        foreach(var team in _dataModel.Teams) {
            team.Value.Players = [.. team.Value.Players.OrderBy(p => p.Job)];
        }

        //setup post match data
        if(_dataModel.PostMatch is not null) {
            foreach(var team in _dataModel.PostMatch.Teams) {
                var teamStats = team.Value.TeamStats;
                if(teamStats.Team is null) {
                    teamStats.Team = team.Key;
                }
                var teamScoreboard = team.Value.TeamStats.ToScoreboard();
                _scoreboard.Add(team.Value.TeamStats, (teamScoreboard, new()));

                foreach(var player in team.Value.PlayerStats) {
                    if(player.Team is null) {
                        player.Team = team.Key;
                    }
                    var playerStatsContrib = new CCScoreboardDouble() {
                        Kills = teamStats.Kills != 0 ? (double)player.Kills / teamStats.Kills : 0,
                        Deaths = teamStats.Deaths != 0 ? (double)player.Deaths / teamStats.Deaths : 0,
                        Assists = teamStats.Assists != 0 ? (double)player.Assists / teamStats.Assists : 0,
                        DamageDealt = teamStats.DamageDealt != 0 ? (double)player.DamageDealt / teamStats.DamageDealt : 0,
                        DamageTaken = teamStats.DamageTaken != 0 ? (double)player.DamageTaken / teamStats.DamageTaken : 0,
                        HPRestored = teamStats.HPRestored != 0 ? (double)player.HPRestored / teamStats.HPRestored : 0,
                        TimeOnCrystalDouble = teamStats.TimeOnCrystal != TimeSpan.Zero ? player.TimeOnCrystal / teamStats.TimeOnCrystal : 0,
                    };
                    _scoreboard.Add(player, (player.ToScoreboard(), playerStatsContrib));
                }
            }
        }
        SortByColumn(0, ImGuiSortDirection.Ascending);
        _csv = BuildCSV();
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
        if(_plugin.Configuration.ShowBackgroundImage) {
            var cursorPosBefore = ImGui.GetCursorPos();
            //ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - (250f + 3f) * ImGuiHelpers.GlobalScale);
            //ImGui.SetCursorPosY((ImGui.GetCursorPos().Y + 50f * ImGuiHelpers.GlobalScale));
            //ImGui.Image(_plugin.WindowManager.CCBannerImage.ImGuiHandle, new Vector2(500, 230) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, new Vector4(1, 1, 1, 0.1f));
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - (243 / 2 + 3f) * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY((ImGui.GetCursorPos().Y + 40f * ImGuiHelpers.GlobalScale));
            ImGui.Image(_plugin.WindowManager.CCBannerImage.ImGuiHandle, new Vector2(243, 275) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, new Vector4(1, 1, 1, 0.1f));
            ImGui.SetCursorPos(cursorPosBefore);
        }

        using(var table = ImRaii.Table("header", 3, ImGuiTableFlags.PadOuterX)) {
            if(table) {
                ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                //ImGui.Indent();
                if(_dataModel.Arena != null) {
                    ImGui.Text($"{MatchHelper.GetArenaName((CrystallineConflictMap)_dataModel.Arena)}");
                }
                ImGui.TableNextColumn();
                DrawFunctions();
                ImGui.TableNextColumn();
                var dutyStartTime = _dataModel.DutyStartTime.ToString();
                ImGuiHelper.RightAlignCursor(dutyStartTime);
                ImGui.Text($"{dutyStartTime}");

                ImGui.TableNextRow(ImGuiTableRowFlags.None, 5f * ImGuiHelpers.GlobalScale);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"{_dataModel.MatchType}");
                ImGui.TableNextColumn();
                bool noWinner = _dataModel.MatchWinner is null;
                bool isWin = _dataModel.MatchWinner == _dataModel.LocalPlayerTeam?.TeamName;
                var color = ImGuiColors.DalamudWhite;
                color = noWinner ? ImGuiColors.DalamudGrey : color;
                string resultText = "";
                if(_dataModel.IsSpectated && _dataModel.MatchWinner is not null) {
                    color = _dataModel.MatchWinner == CrystallineConflictTeamName.Astra ? _plugin.Configuration.Colors.CCPlayerTeam : _plugin.Configuration.Colors.CCEnemyTeam;
                    resultText = MatchHelper.GetTeamName((CrystallineConflictTeamName)_dataModel.MatchWinner) + " WINS";
                } else {
                    color = isWin ? _plugin.Configuration.Colors.Win : _plugin.Configuration.Colors.Loss;
                    color = noWinner ? _plugin.Configuration.Colors.Other : color;
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
            }
        }

        if(_dataModel.Teams.Count == 2) {
            var firstTeam = _dataModel.Teams.ElementAt(0).Value;
            var secondTeam = _dataModel.Teams.ElementAt(1).Value;
            if(_plugin.Configuration.LeftPlayerTeam && !_dataModel.IsSpectated) {
                firstTeam = _dataModel.Teams.Where(x => x.Key == _dataModel.LocalPlayerTeam!.TeamName).FirstOrDefault().Value;
                secondTeam = _dataModel.Teams.Where(x => x.Key != _dataModel.LocalPlayerTeam!.TeamName).FirstOrDefault().Value;
            }

            using(var table = ImRaii.Table("teams", 2, ImGuiTableFlags.PadOuterX)) {
                if(table) {
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
                    string firstTeamProgress = string.Format("{0:P1}", firstTeam.Progress / 100f);
                    ImGuiHelper.CenterAlignCursor(firstTeamProgress);
                    ImGui.TextUnformatted(firstTeamProgress);
                    ImGui.TableNextColumn();
                    var secondTeamProgress = string.Format("{0:P1}", secondTeam.Progress / 100f);
                    ImGuiHelper.CenterAlignCursor(secondTeamProgress);
                    ImGui.TextUnformatted($"{secondTeamProgress}");
                }
            }

            using(var table = ImRaii.Table($"players##{_dataModel.Id}", 6, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoClip)) {

                if(table) {
                    ////ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

                    ImGui.TableSetupColumn("rankteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
                    ImGui.TableSetupColumn("playerteam1", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("jobteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 24f);
                    ImGui.TableSetupColumn("jobteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 24f);
                    ImGui.TableSetupColumn("playerteam2", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("rankteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
                    ImGui.TableNextRow();

                    int maxSize = int.Max(firstTeam.Players.Count, secondTeam.Players.Count);

                    for(int i = 0; i < maxSize; i++) {
                        if(i < firstTeam.Players.Count) {
                            var player0 = firstTeam.Players[i];
                            ImGui.TableNextColumn();
                            using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                string rank0 = player0.Rank != null && player0.Rank!.Tier != ArenaTier.None ? player0.Rank!.ToString() : "";
                                ImGuiHelper.RightAlignCursor(rank0);
                                ImGui.AlignTextToFramePadding();
                                ImGui.Text(rank0);
                            }
                            ImGui.TableNextColumn();
                            var playerColor0 = _dataModel.LocalPlayerTeam is not null && firstTeam.TeamName == _dataModel.LocalPlayerTeam.TeamName ? _plugin.Configuration.Colors.CCPlayerTeam : _plugin.Configuration.Colors.CCEnemyTeam;
                            playerColor0 = _dataModel.LocalPlayer is not null && _dataModel.LocalPlayer.Equals(player0) ? _plugin.Configuration.Colors.CCLocalPlayer : playerColor0;
                            if(_dataModel.IsSpectated) {
                                playerColor0 = firstTeam.TeamName == CrystallineConflictTeamName.Astra ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                            }
                            string playerName0 = player0.Alias.Name;
                            ImGuiHelper.RightAlignCursor(playerName0);
                            ImGui.AlignTextToFramePadding();
                            ImGui.TextColored(playerColor0, playerName0);
                            ImGuiHelper.WrappedTooltip(player0.Alias.HomeWorld);

                            ImGui.TableNextColumn();
                            using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                if(player0.Job != null && _plugin.WindowManager.JobIcons.ContainsKey((Job)player0.Job)) {
                                    ImGui.Image(_plugin.WindowManager.JobIcons[(Job)player0.Job]?.ImGuiHandle ?? _plugin.WindowManager.Icon0.ImGuiHandle, new Vector2(24 * ImGuiHelpers.GlobalScale, 24 * ImGuiHelpers.GlobalScale));
                                }
                            }
                        } else {
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                        }

                        if(i < secondTeam.Players.Count) {
                            var player1 = secondTeam.Players[i];
                            ImGui.TableNextColumn();
                            using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                if(player1.Job != null && _plugin.WindowManager.JobIcons.ContainsKey((Job)player1.Job)) {
                                    ImGui.Image(_plugin.WindowManager.JobIcons[(Job)player1.Job]?.ImGuiHandle ?? _plugin.WindowManager.Icon0.ImGuiHandle, new Vector2(24 * ImGuiHelpers.GlobalScale, 24 * ImGuiHelpers.GlobalScale));
                                }
                            }

                            ImGui.TableNextColumn();
                            var playerColor1 = _dataModel.LocalPlayerTeam is not null && secondTeam.TeamName == _dataModel.LocalPlayerTeam.TeamName ? _plugin.Configuration.Colors.CCPlayerTeam : _plugin.Configuration.Colors.CCEnemyTeam;
                            playerColor1 = _dataModel.LocalPlayer is not null && _dataModel.LocalPlayer.Equals(player1) ? _plugin.Configuration.Colors.CCLocalPlayer : playerColor1;
                            if(_dataModel.IsSpectated) {
                                playerColor1 = secondTeam.TeamName == CrystallineConflictTeamName.Astra ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                            }
                            string playerName1 = secondTeam.Players[i].Alias.Name;
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X * 2);
                            ImGui.TextColored(playerColor1, playerName1);
                            ImGuiHelper.WrappedTooltip(secondTeam.Players[i].Alias.HomeWorld);

                            ImGui.TableNextColumn();
                            using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                string rank1 = player1.Rank != null && player1.Rank?.Tier != ArenaTier.None ? player1.Rank!.ToString() : "";
                                ImGui.AlignTextToFramePadding();
                                ImGui.Text(rank1);
                            }

                        } else {
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                        }
                    }
                }
            }
        }
        ImGui.NewLine();
        if(_dataModel.PostMatch is null) {
            ImGui.Text("Post game statistics unavailable.");
        } else {
            if((_dataModel.MatchType == CrystallineConflictMatchType.Ranked || _dataModel.MatchType == CrystallineConflictMatchType.Unknown)
                && _dataModel.PostMatch.RankBefore is not null && _dataModel.PostMatch.RankAfter is not null) {
                ImGui.Text($"{_dataModel.PostMatch.RankBefore.ToString()} → {_dataModel.PostMatch.RankAfter.ToString()}");
            }
            ImGuiComponents.ToggleButton("##showPercentages", ref _showPercentages);
            ImGui.SameLine();
            ImGui.Text("Show team contributions");
            ImGuiHelper.HelpMarker("Right-click table header to show and hide columns including extra metrics.");
            DrawStatsTable();
        }
    }

    private void DrawFunctions() {
        //need to increment this for each function
        int functionCount = 2;
        //get width of strip
        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
            string text = "";
            for(int i = 0; i < functionCount; i++) {
                text += $"{FontAwesomeIcon.Star.ToIconString()}";
            }
            //ImGuiHelpers.CenterCursorForText(text);
            ImGuiHelper.CenterAlignCursor(text);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ((ImGui.GetStyle().FramePadding.X - 3f) * 2.5f + 9f * (functionCount - 1)));
        }

        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
            var text = $"{FontAwesomeIcon.Star.ToIconString()}{FontAwesomeIcon.Copy.ToIconString()}";
            var color = _dataModel.IsBookmarked ? _plugin.Configuration.Colors.Favorite : ImGuiColors.DalamudWhite;
            using(_ = ImRaii.PushColor(ImGuiCol.Text, color)) {
                if(ImGui.Button($"{FontAwesomeIcon.Star.ToIconString()}##--FavoriteMatch")) {
                    _dataModel.IsBookmarked = !_dataModel.IsBookmarked;
                    _plugin.DataQueue.QueueDataOperation(async () => {
                        await _plugin.CCCache.UpdateMatch(_dataModel);
                    });
                }
            }
        }
        ImGuiHelper.WrappedTooltip("Favorite match");
        ImGui.SameLine();
        ImGuiHelper.CSVButton(_csv);
    }

    private void DrawStatsTable() {
        using var table = ImRaii.Table($"postmatchplayers##{_dataModel.Id}", 14, ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.NoSavedSettings);
        if(!table) return;
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 50f, 0);
        ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f, 1);
        ImGui.TableSetupColumn("Kills", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Kills".GetHashCode());
        ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Deaths".GetHashCode());
        ImGui.TableSetupColumn("Assists", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Assists".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageTaken".GetHashCode());
        ImGui.TableSetupColumn("HP Restored", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"HPRestored".GetHashCode());
        ImGui.TableSetupColumn("Time on Crystal", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f, (uint)"TimeOnCrystal".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Kill/Assist", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerKA".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerLife".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageTakenPerLife".GetHashCode());
        ImGui.TableSetupColumn("HP Restored per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"HPRestoredPerLife".GetHashCode());
        ImGui.TableSetupColumn("KDA Ratio", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"KDA".GetHashCode());

        ImGui.TableNextColumn();
        ImGui.TableHeader("");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeader("Job");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeader("Kills");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeader("Deaths");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
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
        ImGui.TableNextColumn();
        ImGui.TableHeader("KDA\nRatio");

        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(sortSpecs.SpecsDirty) {
            SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
            sortSpecs.SpecsDirty = false;
        }

        foreach(var row in _scoreboard) {
            ImGui.TableNextColumn();
            bool isPlayer = row.Key.Player != null;
            bool isPlayerTeam = row.Key.Team == _dataModel.LocalPlayerTeam?.TeamName;
            if(_dataModel.IsSpectated) {
                isPlayerTeam = row.Key.Team == CrystallineConflictTeamName.Astra;
            }
            var rowColor = new Vector4(0, 0, 0, 0);
            switch((isPlayer, isPlayerTeam)) {
                case (true, true):
                    rowColor = _plugin.Configuration.Colors.CCPlayerTeam - new Vector4(0f, 0f, 0f, 0.7f);
                    break;
                case (true, false):
                    rowColor = _plugin.Configuration.Colors.CCEnemyTeam - new Vector4(0f, 0f, 0f, 0.7f);
                    break;
                case (false, true):
                    rowColor = _plugin.Configuration.Colors.CCPlayerTeam - new Vector4(0f, 0f, 0f, 0.3f);
                    break;
                case (false, false):
                    rowColor = _plugin.Configuration.Colors.CCEnemyTeam - new Vector4(0f, 0f, 0f, 0.3f);
                    break;
            }
            var textColor = _dataModel.LocalPlayer is not null && _dataModel.LocalPlayer.Equals(row.Key.Player) ? _plugin.Configuration.Colors.CCLocalPlayer : ImGuiColors.DalamudWhite;
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            if(isPlayer) {
                ImGui.TextColored(textColor, $" {row.Key.Player?.Name} ");
            } else {
                ImGui.TextColored(textColor, $" {MatchHelper.GetTeamName(row.Key.Team ?? CrystallineConflictTeamName.Unknown)}");
            }
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer ? row.Key.Job : "")}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.Kills) : row.Value.Item1.Kills)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.Deaths) : row.Value.Item1.Deaths)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.Assists) : row.Value.Item1.Assists)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.DamageDealt) : row.Value.Item1.DamageDealt)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.DamageTaken) : row.Value.Item1.DamageTaken)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.HPRestored) : row.Value.Item1.HPRestored)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.TimeOnCrystalDouble) : ImGuiHelper.GetTimeSpanString(row.Value.Item1.TimeOnCrystal))}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{string.Format("{0:f0}", row.Value.Item1.DamageDealtPerKA)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{string.Format("{0:f0}", row.Value.Item1.DamageDealtPerLife)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{string.Format("{0:f0}", row.Value.Item1.DamageTakenPerLife)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{string.Format("{0:f0}", row.Value.Item1.HPRestoredPerLife)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{string.Format("{0:0.00}", row.Value.Item1.KDA)}");
        }
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        Func<KeyValuePair<CrystallineConflictPostMatchRow, (CCScoreboard, CCScoreboardDouble)>, object> comparator = (r) => 0;

        //0 = name
        //1 = job
        if(columnId == 0) {
            comparator = (r) => (r.Key.Player is not null ? r.Key.Player.Name : r.Key.Team.ToString()) ?? "";
        } else if(columnId == 1) {
            comparator = (r) => r.Key.Job ?? 0;
        } else {
            bool propFound = false;
            if(_showPercentages) {
                var props = typeof(CCScoreboardDouble).GetProperties();
                foreach(var prop in props) {
                    var propId2 = prop.Name.GetHashCode();
                    if((uint)"TimeOnCrystal".GetHashCode() == columnId) {
                        comparator = (r) => r.Value.Item2.TimeOnCrystalDouble;
                        propFound = true;
                        break;
                    } else if((uint)propId2 == columnId) {
                        comparator = (r) => prop.GetValue(r.Value.Item2) ?? 0;
                        propFound = true;
                        break;
                    }
                }
            }
            if(!propFound) {
                var props = typeof(CCScoreboard).GetProperties();
                //iterate to two levels
                foreach(var prop in props) {
                    var propId = prop.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        comparator = (r) => prop.GetValue(r.Value.Item1) ?? 0;
                        break;
                    }
                }
            }
        }
        if(_plugin.Configuration.AnchorTeamNames) {
            var teamList = _scoreboard.Where(x => x.Key.Player is null).ToList();
            var playerList = _scoreboard.Where(x => x.Key.Player is not null).ToList();
            _scoreboard = teamList.Concat(direction == ImGuiSortDirection.Ascending ? playerList.OrderBy(comparator) : playerList.OrderByDescending(comparator)).ToDictionary();
        } else {
            _scoreboard = direction == ImGuiSortDirection.Ascending ? _scoreboard.ToList().OrderBy(comparator).ToDictionary()
                : _scoreboard.ToList().OrderByDescending(comparator).ToDictionary();
        }
    }

    private string BuildCSV() {
        string csv = "";
        //header
        csv += "Id,Start Time,Arena,Queue,Winner,Duration,Astra Progress,Umbra Progress,\n";
        csv += _dataModel.Id + "," + _dataModel.DutyStartTime + ","
            + (_dataModel.Arena != null ? MatchHelper.GetArenaName((CrystallineConflictMap)_dataModel.Arena!) : "") + ","
            + _dataModel.MatchType + "," + _dataModel.MatchWinner + "," + _dataModel.MatchDuration + ","
            + _dataModel.Teams[CrystallineConflictTeamName.Astra].Progress + "," + _dataModel.Teams[CrystallineConflictTeamName.Umbra].Progress + ","
            + "\n";

        //post match
        if(_dataModel.PostMatch != null) {
            csv += "\n\n\n";
            csv += "Name,HomeWorld,Job,Team,Kills,Deaths,Assists,Damage Dealt,Damage Taken,HP Restored,Time on Crystal,\n";
            foreach(var row in _scoreboard) {
                if(row.Key.Player != null) {
                    csv += row.Key.Player.Name + "," + row.Key.Player.HomeWorld + "," + row.Key.Job + ",";
                } else {
                    csv += row.Key.Team + ",,,";
                }
                csv += row.Key.Team + "," + row.Key.Kills + "," + row.Key.Deaths + "," + row.Key.Assists + "," + row.Key.DamageDealt + "," + row.Key.DamageTaken + "," + row.Key.HPRestored + "," + row.Key.TimeOnCrystal + ",";
                csv += "\n";
            }
        }
        return csv;
    }
}
