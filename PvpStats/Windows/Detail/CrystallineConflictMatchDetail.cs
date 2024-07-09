using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Detail;

internal class CrystallineConflictMatchDetail : MatchDetail<CrystallineConflictMatch> {

    private Plugin _plugin;
    private CCTeamQuickFilter _teamQuickFilter;
    private CrystallineConflictMatch _dataModel;
    private Dictionary<CrystallineConflictTeamName, CCScoreboard>? _teamScoreboard;
    private Dictionary<PlayerAlias, CCScoreboardDouble>? _playerContributions = [];
    private Dictionary<PlayerAlias, CCScoreboard>? _scoreboard;
    private Dictionary<PlayerAlias, CCScoreboard>? _unfilteredScoreboard;

    internal CrystallineConflictMatchDetail(Plugin plugin, CrystallineConflictMatch match) : base(plugin, plugin.CCCache, match) {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Appearing;
        CollapsedCondition = ImGuiCond.Appearing;
        Position = new Vector2(0, 0);
        Collapsed = false;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(650, 620),
            MaximumSize = new Vector2(5000, 5000)
        };
        Flags |= ImGuiWindowFlags.NoSavedSettings;
        //if(!plugin.Configuration.ResizeableMatchWindow) {
        //    Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        //}
        _plugin = plugin;
        _dataModel = match;
        _teamQuickFilter = new(plugin, ApplyTeamFilter);

        //sort team players
        foreach(var team in _dataModel.Teams) {
            team.Value.Players = [.. team.Value.Players.OrderBy(p => p.Job)];
        }

        //setup post match data
        if(_dataModel.PostMatch is not null) {
            _unfilteredScoreboard = match.GetPlayerScoreboards();
            _scoreboard = _unfilteredScoreboard;
            _teamScoreboard = match.GetTeamScoreboards();
            _playerContributions = match.GetPlayerContributions();
        }
        SortByColumn(0, ImGuiSortDirection.Ascending);
        CSV = BuildCSV();
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
            ImGui.Image(_plugin.TextureProvider.GetFromFile(Path.Combine(_plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "cc_logo_full.png")).GetWrapOrEmpty().ImGuiHandle,
                new Vector2(243, 275) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, new Vector4(1, 1, 1, 0.1f));

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
                                if(player0.Job != null && TextureHelper.JobIcons.TryGetValue((Job)player0.Job, out var icon)) {
                                    ImGui.Image(_plugin.WindowManager.GetTextureHandle(icon), new Vector2(24 * ImGuiHelpers.GlobalScale));
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
                                if(player1.Job != null && TextureHelper.JobIcons.TryGetValue((Job)player1.Job, out var icon)) {
                                    //ImGui.Image(_plugin.WindowManager.JobIcons[(Job)player1.Job]?.ImGuiHandle ?? _plugin.WindowManager.Icon0.ImGuiHandle, new Vector2(24 * ImGuiHelpers.GlobalScale, 24 * ImGuiHelpers.GlobalScale));
                                    ImGui.Image(_plugin.WindowManager.GetTextureHandle(icon), new Vector2(24 * ImGuiHelpers.GlobalScale));
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
            ImGui.NewLine();
            ImGui.NewLine();
            ImGuiHelper.HelpMarker("Right-click table header to show and hide columns including extra metrics.");
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Show team contributions");
            ImGui.SameLine();
            ImGuiComponents.ToggleButton("##showPercentages", ref ShowPercentages);
            ImGui.SameLine();
            ImGui.Text("Show team totals");
            ImGui.SameLine();
            ImGui.Checkbox("###showTeamRows", ref ShowTeamRows);
            ImGui.SameLine();
            _teamQuickFilter.Draw();
            DrawStatsTable();
        }
    }

    private void DrawStatsTable() {
        using var table = ImRaii.Table($"postmatchplayers##{_dataModel.Id}", 15, ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.PadOuterX);
        if(!table) return;
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 50f, 0);
        ImGui.TableSetupColumn("Home World", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 110f, 1);
        ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f, 2);
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
        ImGui.TableSetupColumn("KDA Ratio", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 50f, (uint)"KDA".GetHashCode());

        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Name", 0);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Home World", 0);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Job", 1);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Kills");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Deaths");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Assists");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage\nDealt");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage\nTaken");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("HP\nRestored");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Time on\nCrystal");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage Dealt\nper Kill/Assist");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage Dealt\nper Life");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage Taken\nper Life");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("HP Restored\nper Life");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("KDA\nRatio");
        }

        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(sortSpecs.SpecsDirty) {
            SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
            sortSpecs.SpecsDirty = false;
        }

        if(ShowTeamRows && _teamScoreboard != null) {
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, Plugin.Configuration.Colors.TeamRowText);
            foreach(var row in _teamScoreboard.Where(x => _teamQuickFilter.FilterState[x.Key])) {
                ImGui.TableNextColumn();
                Vector4 rowColor = ImGuiColors.DalamudWhite;
                if(row.Key == _dataModel.LocalPlayerTeam?.TeamName || (_dataModel.IsSpectated && row.Key == CrystallineConflictTeamName.Astra)) {
                    rowColor = _plugin.Configuration.Colors.CCPlayerTeam;
                } else {
                    rowColor = _plugin.Configuration.Colors.CCEnemyTeam;
                }
                rowColor.W = _plugin.Configuration.TeamRowAlpha;

                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
                ImGui.TextUnformatted(MatchHelper.GetTeamName(row.Key));
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();

                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.Kills}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.Deaths}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.Assists}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealt}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageTaken}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.HPRestored}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{ImGuiHelper.GetTimeSpanString(row.Value.TimeOnCrystal)}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealtPerKA}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealtPerLife}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageTakenPerLife}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.HPRestoredPerLife}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.KDA}", -11f);
                }
            }
        }

        foreach(var row in _scoreboard!) {
            //might have performance implications
            var player = Match.Players!.Where(x => x.Alias.Equals(row.Key)).FirstOrDefault();
            ImGui.TableNextColumn();
            Vector4 rowColor = ImGuiColors.DalamudWhite;
            if(player?.Team == _dataModel.LocalPlayerTeam?.TeamName || (_dataModel.IsSpectated && player?.Team == CrystallineConflictTeamName.Astra)) {
                rowColor = _plugin.Configuration.Colors.CCPlayerTeam;
            } else {
                rowColor = _plugin.Configuration.Colors.CCEnemyTeam;
            }
            rowColor.W = _plugin.Configuration.PlayerRowAlpha;

            var textColor = _dataModel.LocalPlayer is not null && _dataModel.LocalPlayer.Equals(row.Key) ? _plugin.Configuration.Colors.CCLocalPlayer : Plugin.Configuration.Colors.PlayerRowText;
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            ImGui.TextColored(textColor, $"{row.Key.Name}");
            if(ImGui.TableNextColumn()) {
                ImGui.TextColored(textColor, $"{row.Key.HomeWorld}");
            }
            if(ImGui.TableNextColumn()) {
                var jobString = $"{player?.Job}";
                ImGuiHelper.CenterAlignCursor(jobString);
                ImGui.TextColored(textColor, jobString);
            }

            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].Kills) : row.Value.Kills)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].Deaths) : row.Value.Deaths)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].Assists) : row.Value.Assists)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].DamageDealt) : row.Value.DamageDealt)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].DamageTaken) : row.Value.DamageTaken)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].HPRestored) : row.Value.HPRestored)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].TimeOnCrystal) : ImGuiHelper.GetTimeSpanString(row.Value.TimeOnCrystal))}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:f0}", row.Value.DamageDealtPerKA)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:f0}", row.Value.DamageDealtPerLife)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:f0}", row.Value.DamageTakenPerLife)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:f0}", row.Value.HPRestoredPerLife)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:0.00}", row.Value.KDA)}", -11f, textColor);
            }
        }
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        if(_unfilteredScoreboard == null || _scoreboard == null) return;

        //Func<KeyValuePair<CrystallineConflictPostMatchRow, (CCScoreboard, CCScoreboardDouble)>, object> comparator = (r) => 0;
        Func<KeyValuePair<PlayerAlias, CCScoreboard>, object> comparator = (r) => 0;
        Func<KeyValuePair<CrystallineConflictTeamName, CCScoreboard>, object> teamComparator = (r) => 0;

        //0 = name
        //1 = home world
        //2 = job
        if(columnId == 0) {
            comparator = (r) => Match.Players.First(x => x.Alias.Equals(r.Key)).Alias.Name ?? "";
            teamComparator = (r) => r.Key;
        } else if(columnId == 1) {
            comparator = (r) => Match.Players.First(x => x.Alias.Equals(r.Key)).Alias.HomeWorld ?? "";
        } else if(columnId == 2) {
            comparator = (r) => Match.Players.First(x => x.Alias.Equals(r.Key)).Job ?? 0;
        } else {
            bool propFound = false;
            if(ShowPercentages && _playerContributions != null) {
                var props = typeof(CCScoreboardDouble).GetProperties();
                foreach(var prop in props) {
                    var propId = prop.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        Plugin.Log.Debug($"sorting by {prop.Name}");
                        comparator = (r) => prop.GetValue(_playerContributions[r.Key]) ?? 0;
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
                        comparator = (r) => prop.GetValue(r.Value) ?? 0;
                        teamComparator = (r) => prop.GetValue(r.Value) ?? 0;
                        break;
                    }
                }
            }
        }

        _scoreboard = direction == ImGuiSortDirection.Ascending ? _scoreboard.OrderBy(comparator).ToDictionary()
            : _scoreboard.OrderByDescending(comparator).ToDictionary();
        _unfilteredScoreboard = direction == ImGuiSortDirection.Ascending ? _unfilteredScoreboard.OrderBy(comparator).ToDictionary()
            : _unfilteredScoreboard.OrderByDescending(comparator).ToDictionary();

        if(_teamScoreboard != null && !_plugin.Configuration.AnchorTeamNames) {
            _teamScoreboard = direction == ImGuiSortDirection.Ascending ? _teamScoreboard.OrderBy(teamComparator).ToDictionary()
            : _teamScoreboard.OrderByDescending(teamComparator).ToDictionary();
        }
    }

    private Task ApplyTeamFilter() {
        if(_scoreboard == null || _unfilteredScoreboard == null) {
            return Task.CompletedTask;
        }
        _scoreboard = _unfilteredScoreboard.Where(x => {
            var player = Match.Players.Where(y => y.Alias.Equals(x.Key)).FirstOrDefault();
            return _teamQuickFilter.FilterState[(CrystallineConflictTeamName)player.Team];
        }).ToDictionary();
        return Task.CompletedTask;
    }

    protected override string BuildCSV() {
        string csv = "";
        //header
        csv += "Id,Start Time,Arena,Queue,Winner,Duration,Astra Progress,Umbra Progress,\n";
        csv += _dataModel.Id + "," + _dataModel.DutyStartTime + ","
            + (_dataModel.Arena != null ? MatchHelper.GetArenaName((CrystallineConflictMap)_dataModel.Arena!) : "") + ","
            + _dataModel.MatchType + "," + _dataModel.MatchWinner + "," + _dataModel.MatchDuration + ","
            + _dataModel.Teams[CrystallineConflictTeamName.Astra].Progress + "," + _dataModel.Teams[CrystallineConflictTeamName.Umbra].Progress + ","
            + "\n";

        //post match
        if(_scoreboard != null) {
            csv += "\n\n\n";
            csv += "Name,HomeWorld,Job,Team,Kills,Deaths,Assists,Damage Dealt,Damage Taken,HP Restored,Time on Crystal,\n";
            var players = _dataModel.Players;
            foreach(var row in _scoreboard) {
                var player = players.Where(x => x.Alias.Equals(row.Key)).FirstOrDefault();
                csv += row.Key.Name + "," + row.Key.HomeWorld + "," + player?.Job + ",";
                csv += player?.Team + "," + row.Value.Kills + "," + row.Value.Deaths + "," + row.Value.Assists + "," + row.Value.DamageDealt + "," + row.Value.DamageTaken + "," + row.Value.HPRestored + "," + row.Value.TimeOnCrystal + ",";
                csv += "\n";
            }
        }
        return csv;
    }
}
