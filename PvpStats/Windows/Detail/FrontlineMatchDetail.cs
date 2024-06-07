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
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Detail;
internal class FrontlineMatchDetail : MatchDetail<FrontlineMatch> {

    private FLTeamQuickFilter _teamQuickFilter;
    private Dictionary<PlayerAlias, FLScoreboardDouble> _playerContributions = [];
    private Dictionary<string, FrontlineScoreboard> _scoreboard;
    private Dictionary<string, FrontlineScoreboard> _unfilteredScoreboard;
    private bool _triggerSort;
    private bool _firstDrawComplete;

    public FrontlineMatchDetail(Plugin plugin, FrontlineMatch match) : base(plugin, plugin.FLCache, match) {
        //Flags -= ImGuiWindowFlags.AlwaysAutoResize;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(860, 800),
            MaximumSize = new Vector2(5000, 5000)
        };

        SizeCondition = ImGuiCond.Appearing;
        switch(match.Arena) {
            case FrontlineMap.FieldsOfGlory:
            case FrontlineMap.SealRock:
                Size = new Vector2(930, 800);
                break;
            default:
            case FrontlineMap.OnsalHakair:
                Size = new Vector2(865, 800);
                break;
        }

        CSV = BuildCSV();
        _teamQuickFilter = new(plugin, ApplyTeamFilter);
        _unfilteredScoreboard = match.PlayerScoreboards;
        _scoreboard = _unfilteredScoreboard;
        _playerContributions = match.GetPlayerContributions();
    }

    public override void Draw() {
        //if(Plugin.Configuration.ShowBackgroundImage) {
        //    var cursorPosBefore = ImGui.GetCursorPos();
        //    ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - (250 / 2 + 0f) * ImGuiHelpers.GlobalScale);
        //    ImGui.SetCursorPosY((ImGui.GetCursorPos().Y + 40f * ImGuiHelpers.GlobalScale));
        //    ImGui.Image(Plugin.WindowManager.FLBannerImage.ImGuiHandle, new Vector2(2, 240) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, new Vector4(1, 1, 1, 0.1f));
        //    ImGui.SetCursorPos(cursorPosBefore);
        //}
        using(var table = ImRaii.Table("header", 3, ImGuiTableFlags.PadOuterX)) {
            if(table) {
                ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                //ImGui.Indent();
                if(Match.Arena != null) {
                    ImGui.Text($"{MatchHelper.GetFrontlineArenaName((FrontlineMap)Match.Arena)}");
                }
                ImGui.TableNextColumn();
                DrawFunctions();
                ImGui.TableNextColumn();
                var dutyStartTime = Match.DutyStartTime.ToString();
                ImGuiHelper.RightAlignCursor(dutyStartTime);
                ImGui.Text($"{dutyStartTime}");

                ImGui.TableNextRow(ImGuiTableRowFlags.None, 5f * ImGuiHelpers.GlobalScale);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(MatchHelper.GetFrontlineArenaType(Match.Arena));
                ImGui.TableNextColumn();
                //Vector4 color;
                //switch(Match.Result) {
                //    case 0:
                //        color = Plugin.Configuration.Colors.Win; break;
                //    case 1:
                //    default:
                //        color = Plugin.Configuration.Colors.Other; break;
                //    case 2:
                //        color = Plugin.Configuration.Colors.Loss; break;
                //}
                //string resultText = Match.Result != null ? ImGuiHelper.AddOrdinal((int)Match.Result).ToUpper() : "???";
                //ImGuiHelpers.CenterCursorForText(resultText);
                //ImGui.TextColored(color, resultText);
                DrawPlacement(Match.Result, true);
                ImGui.TableNextColumn();
                if(Match.MatchDuration != null) {
                    string durationText = ImGuiHelper.GetTimeSpanString((TimeSpan)Match.MatchDuration);
                    ImGuiHelper.RightAlignCursor(durationText);
                    ImGui.Text(durationText);
                }
            }
        }
        //DrawTeamStatsTable();
        //ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
        using(var table = ImRaii.Table("teamstats", 4, ImGuiTableFlags.None)) {
            if(table) {
                ImGui.TableSetupColumn("descriptions", ImGuiTableColumnFlags.WidthFixed, 190f * ImGuiHelpers.GlobalScale);
                var columnWidth = (ImGui.GetContentRegionMax().X / 2 - (190f * ImGuiHelpers.GlobalScale + ImGui.GetStyle().CellPadding.X * 4 + ImGui.GetStyle().WindowPadding.X / 2)) * 2 / 3;
                ImGui.TableSetupColumn("team1", ImGuiTableColumnFlags.WidthFixed, columnWidth);
                ImGui.TableSetupColumn("team2", ImGuiTableColumnFlags.WidthFixed, columnWidth);
                ImGui.TableSetupColumn("team3", ImGuiTableColumnFlags.WidthFixed, columnWidth);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                using(var rowDescTable = ImRaii.Table("rowDescTable", 1, ImGuiTableFlags.None)) {
                    if(rowDescTable) {
                        ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch, 150f * ImGuiHelpers.GlobalScale);
                        ImGui.TableNextColumn();
                        ImGui.Text("");
                        ImGui.TableNextColumn();
                        ImGui.Text("");
                        ImGui.TableNextColumn();
                        DrawRowDescription("Total points:");
                        ImGui.TableNextColumn();
                        string specialRow = Match.Arena switch {
                            FrontlineMap.FieldsOfGlory => "Points earned from ice:",
                            FrontlineMap.SealRock => "Points earned from tomeliths:",
                            FrontlineMap.OnsalHakair => "Points earned from ovoos:",
                            _ => "",
                        };
                        DrawRowDescription(specialRow);
                        ImGui.TableNextColumn();
                        DrawRowDescription("Points earned from kills:");
                        ImGui.TableNextColumn();
                        DrawRowDescription("Points lost from deaths:");
                        ImGui.TableNextColumn();
                        DrawRowDescription("Kill/death point diff.:");
                    }
                }
                foreach(var team in Match.Teams) {
                    ImGui.TableNextColumn();
                    DrawTeamStatTable(team.Key);
                }
            }
        }
        ImGuiComponents.ToggleButton("##showPercentages", ref ShowPercentages);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Show team contributions");
        ImGuiHelper.HelpMarker("Right-click table header to show and hide columns including extra metrics.");
        ImGui.SameLine();
        _teamQuickFilter.Draw();
        DrawPlayerStatsTable();
    }

    private void DrawTeamName(FrontlineTeamName team) {
        var color = Plugin.Configuration.GetFrontlineTeamColor(team);
        var text = MatchHelper.GetTeamName(team);
        ImGuiHelper.CenterAlignCursor(text);
        ImGui.TextColored(color, text);
    }

    private void DrawPlacement(int? placement, bool windowCenter = false) {
        var color = placement switch {
            0 => Plugin.Configuration.Colors.Win,
            2 => Plugin.Configuration.Colors.Loss,
            _ => Plugin.Configuration.Colors.Other,
        };
        string resultText = placement != null ? ImGuiHelper.AddOrdinal((int)placement + 1).ToUpper() : "???";
        if(windowCenter) {
            ImGuiHelpers.CenterCursorForText(resultText);
        } else {
            ImGuiHelper.CenterAlignCursor(resultText);
        }
        ImGui.TextColored(color, resultText);
    }

    private void DrawRowDescription(string desc) {
        ImGuiHelper.RightAlignCursor(desc);
        ImGui.TextUnformatted(desc);
    }

    private void DrawPlayerStatsTable() {
        var tableFlags = ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.PadOuterX;
        //this is hacky
        int columnCount = 16;
        if(Match.Arena == FrontlineMap.FieldsOfGlory) {
            columnCount += 2;
        }
        if(Match.Arena == FrontlineMap.SealRock) {
            columnCount += 1;
        }
        if(Match.MaxBattleHigh != null) {
            columnCount += 1;
            //tableFlags &= ~ImGuiTableFlags.PadOuterX;
        }

        using var table = ImRaii.Table($"postmatchplayers##{Match.Id}", columnCount, tableFlags, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
        //new Vector2(ImGui.GetContentRegionAvail().X, 550f * ImGuiHelpers.GlobalScale)
        if(!table) return;
        ImGui.TableSetupColumn("Alliance", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 10f, 3);
        if(Match.MaxBattleHigh != null) {
            ImGui.TableSetupColumn("Peak Battle High", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 15f, 4);
        }
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 200f, 0);
        ImGui.TableSetupColumn("Home World", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 110f, 1);
        ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f, 2);
        ImGui.TableSetupColumn("Kills", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Kills".GetHashCode());
        ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Deaths".GetHashCode());
        ImGui.TableSetupColumn("Assists", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Assists".GetHashCode());
        if(Match.Arena == FrontlineMap.FieldsOfGlory) {
            ImGui.TableSetupColumn("Damage to PCs", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToPCs".GetHashCode());
            ImGui.TableSetupColumn("Ice Damage", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToOther".GetHashCode());
            ImGui.TableSetupColumn("Damage Dealt", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        } else {
            ImGui.TableSetupColumn("Damage Dealt", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        }
        ImGui.TableSetupColumn("Damage Taken", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageTaken".GetHashCode());
        ImGui.TableSetupColumn("HP Restored", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"HPRestored".GetHashCode());
        ImGui.TableSetupColumn("Special", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 60f, (uint)"Special1".GetHashCode());
        if(Match.Arena == FrontlineMap.SealRock) {
            ImGui.TableSetupColumn("Occupations", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"Occupations".GetHashCode());
        }
        ImGui.TableSetupColumn("Damage Dealt per Kill/Assist", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerKA".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerLife".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageTakenPerLife".GetHashCode());
        ImGui.TableSetupColumn("HP Restored per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"HPRestoredPerLife".GetHashCode());
        ImGui.TableSetupColumn("KDA Ratio", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"KDA".GetHashCode());

        if(Match.MaxBattleHigh != null) {
            ImGui.TableSetupScrollFreeze(3, 1);
        } else {
            ImGui.TableSetupScrollFreeze(2, 1);
        }

        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(!_firstDrawComplete) {
            sortSpecs.Specs.ColumnUserID = 0;
            sortSpecs.Specs.SortDirection = ImGuiSortDirection.Ascending;
            _triggerSort = true;
            _firstDrawComplete = true;
        }
        if(sortSpecs.SpecsDirty || _triggerSort) {
            _triggerSort = false;
            SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
            sortSpecs.SpecsDirty = false;
        }

        ImGui.TableNextColumn();
        ImGui.TableHeader("Alliance\n\n");
        if(Match.MaxBattleHigh != null) {
            ImGui.TableNextColumn();
            ImGui.TableHeader("\n");
        }
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        //ImGuiHelper.CenterAlignCursor("Name");
        ImGui.TableHeader("Name");
        ImGui.TableNextColumn();
        //ImGuiHelper.CenterAlignCursor("Home World");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeader("Home World");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        //ImGuiHelper.CenterAlignCursor("Job");
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
        if(Match.Arena == FrontlineMap.FieldsOfGlory) {
            ImGui.TableNextColumn();
            ImGui.TableHeader("Damage\nto PCs");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Damage\nto Other");
        }
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage\nDealt");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage\nTaken");
        ImGui.TableNextColumn();
        ImGui.TableHeader("HP\nRestored");
        ImGui.TableNextColumn();
        ImGui.TableHeader("");
        ImGuiHelper.HelpMarker("Not sure what this is. It's related to healing.");
        if(Match.Arena == FrontlineMap.SealRock) {
            ImGui.TableNextColumn();
            ImGui.TableHeader("Occup-\nations");
        }
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

        foreach(var row in _scoreboard) {
            var player = Match.Players.Where(x => x.Name.Equals(row.Key)).First();
            var playerAlias = (PlayerAlias)row.Key;
            //bool isPlayer = row.Key.Player != null;
            //bool isPlayerTeam = row.Key.Team == _dataModel.LocalPlayerTeam?.TeamName;
            var rowColor = Plugin.Configuration.GetFrontlineTeamColor(player.Team) - new Vector4(0f, 0f, 0f, 0.7f);
            var textColor = Match.LocalPlayer is not null && Match.LocalPlayer.Equals(playerAlias) ? Plugin.Configuration.Colors.CCLocalPlayer : ImGuiColors.DalamudWhite;
            ImGui.TableNextColumn();
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            string alliance = MatchHelper.GetAllianceLetter(player.Alliance);
            ImGui.TextColored(textColor, $" {alliance} ");
            if(Match.MaxBattleHigh != null) {
                ImGui.TableNextColumn();
                if(Match.MaxBattleHigh.TryGetValue(playerAlias, out var peakBattleHigh) && Plugin.WindowManager.BattleHighIcons.TryGetValue(peakBattleHigh, out var icon)) {
                    //using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(0, 0));

                    var size = 15f;
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2f * ImGuiHelpers.GlobalScale);
                    ImGui.Image(icon?.ImGuiHandle ?? Plugin.WindowManager.Icon0.ImGuiHandle, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0.12f), new Vector2(0.88f));
                }
                //ImGui.TableHeader("Peak Battle High\n\n");
            }
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $" {playerAlias.Name} ");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{player.Name.HomeWorld}");
            ImGui.TableNextColumn();
            //ImGuiHelper.CenterAlignCursor(player.Job?.ToString() ?? "");
            ImGui.TextColored(textColor, $"{player.Job}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Kills) : row.Value.Kills)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Deaths) : row.Value.Deaths)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Assists) : row.Value.Assists)}");

            if(Match.Arena == FrontlineMap.FieldsOfGlory) {
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].DamageToPCs) : row.Value.DamageToPCs)}");
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].DamageToOther) : row.Value.DamageToOther)}");
            }
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].DamageDealt) : row.Value.DamageDealt)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].DamageTaken) : row.Value.DamageTaken)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].HPRestored) : row.Value.HPRestored)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Special1) : row.Value.Special1)}");
            if(Match.Arena == FrontlineMap.SealRock) {
                ImGui.TableNextColumn();
                ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Occupations) : row.Value.Occupations)}");
            }
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{row.Value.DamageDealtPerKA}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{row.Value.DamageDealtPerLife}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{row.Value.DamageTakenPerLife}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{row.Value.HPRestoredPerLife}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{string.Format("{0:0.00}", row.Value.KDA)}");
        }
    }

    private void DrawTeamStatTable(FrontlineTeamName teamName) {

        var flags = ImGuiTableFlags.None;
        if(teamName == Match.LocalPlayerTeam) {
            flags |= ImGuiTableFlags.BordersOuter;
            
        }
        using var style = ImRaii.PushColor(ImGuiCol.TableBorderStrong, Plugin.Configuration.Colors.CCLocalPlayer);
        using var table = ImRaii.Table("teamstats", 1, flags);
        if(!table) return;
        var team = Match.Teams[teamName];

        ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X);

        ImGui.TableNextColumn();
        DrawTeamName(teamName);

        ImGui.TableNextColumn();
        DrawPlacement(team.Placement);

        ImGui.TableNextColumn();
        var totalPoints = team.TotalPoints.ToString();
        ImGuiHelper.CenterAlignCursor(totalPoints);
        ImGui.Text(totalPoints);

        ImGui.TableNextColumn();
        if(Match.Arena == FrontlineMap.OnsalHakair || Match.Arena == FrontlineMap.SealRock) {
            var specialPoints = team.OccupationPoints.ToString();
            ImGuiHelper.CenterAlignCursor(specialPoints);
            ImGui.Text(specialPoints);
        } else {
            var specialPoints = team.TargetablePoints.ToString();
            ImGuiHelper.CenterAlignCursor(specialPoints);
            ImGui.Text(specialPoints);
        }

        ImGui.TableNextColumn();
        var killPoints = team.KillPoints.ToString();
        ImGuiHelper.CenterAlignCursor(killPoints);
        ImGui.Text(killPoints);

        ImGui.TableNextColumn();
        var deathPoints = team.DeathPointLosses.ToString();
        ImGuiHelper.CenterAlignCursor(deathPoints);
        ImGui.Text(deathPoints);

        ImGui.TableNextColumn();
        var minusWidth = ImGui.CalcTextSize("-").X;
        var diffPoints = int.Abs(team.KillPointsDiff).ToString();
        ImGuiHelper.CenterAlignCursor(diffPoints);
        var currentCursor = ImGui.GetCursorPos();
        ImGui.TextUnformatted(diffPoints);
        if(team.KillPointsDiff < 0) {
            ImGui.SetCursorPos(new Vector2(currentCursor.X - minusWidth, currentCursor.Y));
            ImGui.TextUnformatted("-");
        }

        //for spacing
        ImGui.TableNextColumn();
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        Func<KeyValuePair<string, FrontlineScoreboard>, object> comparator = (r) => 0;

        //0 = name
        //1 = homeworld
        //2 = job
        if(columnId == 0) {
            comparator = (r) => Match.Players.First(x => x.Name.Equals(r.Key)).Name.Name ?? "";
        } else if(columnId == 1) {
            comparator = (r) => Match.Players.First(x => x.Name.Equals(r.Key)).Name.HomeWorld ?? "";
        } else if(columnId == 2) {
            comparator = (r) => Match.Players.First(x => x.Name.Equals(r.Key)).Job ?? 0;
        } else if(columnId == 3) {
            comparator = (r) => Match.Players?.First(x => x.Name.Equals(r.Key)).TeamAlliance ?? 0;
        } else if(columnId == 4) {
            comparator = (r) => Match.MaxBattleHigh?.First(x => x.Key.Equals(r.Key)).Value ?? 0;
        } else {
            bool propFound = false;
            if(ShowPercentages) {
                var props = typeof(FLScoreboardDouble).GetProperties();
                foreach(var prop in props) {
                    var propId = prop.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        Plugin.Log.Debug($"sorting by {prop.Name}");
                        comparator = (r) => prop.GetValue(_playerContributions[(PlayerAlias)r.Key]) ?? 0;
                        propFound = true;
                        break;
                    }
                }
            }
            if(!propFound) {
                var props = typeof(FrontlineScoreboard).GetProperties();
                foreach(var prop in props) {
                    var propId = prop.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        Plugin.Log.Debug($"sorting by {prop.Name}");
                        comparator = (r) => prop.GetValue(r.Value) ?? 0;
                        break;
                    }
                }
            }
        }

        //if(_plugin.Configuration.AnchorTeamNames) {
        //    var teamList = _scoreboard.Where(x => x.Key.Player is null).ToList();
        //    var playerList = _scoreboard.Where(x => x.Key.Player is not null).ToList();
        //    _scoreboard = teamList.Concat(direction == ImGuiSortDirection.Ascending ? playerList.OrderBy(comparator) : playerList.OrderByDescending(comparator)).ToDictionary();
        //} else {
        //    _scoreboard = direction == ImGuiSortDirection.Ascending ? _scoreboard.ToList().OrderBy(comparator).ToDictionary()
        //        : _scoreboard.ToList().OrderByDescending(comparator).ToDictionary();
        //}
        _scoreboard = direction == ImGuiSortDirection.Ascending ? _scoreboard.OrderBy(comparator).ToDictionary()
            : _scoreboard.OrderByDescending(comparator).ToDictionary();
        _unfilteredScoreboard = direction == ImGuiSortDirection.Ascending ? _unfilteredScoreboard.OrderBy(comparator).ToDictionary()
            : _unfilteredScoreboard.OrderByDescending(comparator).ToDictionary();
    }

    private Task ApplyTeamFilter() {
        _scoreboard = _unfilteredScoreboard.Where(x => {
            var player = Match.Players.Where(y => y.Name.Equals(x.Key)).First();
            return _teamQuickFilter.FilterState[player.Team];
        }).ToDictionary();
        //_triggerSort = true;
        return Task.CompletedTask;
    }

    protected override string BuildCSV() {
        string csv = "";

        //header
        csv += "Id,Start Time,Arena,Duration,\n";
        csv += Match.Id + "," + Match.DutyStartTime + ","
            + (Match.Arena != null ? MatchHelper.GetFrontlineArenaName((FrontlineMap)Match.Arena!) : "") + ","
            + Match.MatchDuration + ","
            + "\n";

        //team stats
        csv += "\n\n\n";
        csv += "Team,Placement,Total Points,Occupation Points,NPC Points,Kill Points,Death Point Losses\n";
        foreach(var team in Match.Teams) {
            csv += team.Key + "," + team.Value.Placement + "," + team.Value.TotalPoints + "," + team.Value.OccupationPoints + "," + team.Value.TargetablePoints + ","
            + team.Value.KillPoints + "," + team.Value.DeathPointLosses + ","
            + "\n";
        }

        //player stats
        csv += "\n\n\n";
        csv += "Name,Home World,Job,Kills,Deaths,Assists,Damage Dealt,Damage to PCs,Damage To Other,Damage Taken, HP Restored,Special,Occupations\n";
        foreach(var player in Match.Players) {
            var scoreboard = Match.PlayerScoreboards[player.Name];
            csv += player.Name.Name + "," + player.Name.HomeWorld + "," + player.Job + "," + scoreboard.Kills + "," + scoreboard.Deaths + "," + scoreboard.Assists + ","
                + scoreboard.DamageDealt + "," + scoreboard.DamageToPCs + "," + scoreboard.DamageToOther + "," + scoreboard.DamageTaken + "," + scoreboard.HPRestored + ","
                + scoreboard.Special1 + "," + scoreboard.Occupations + ","
                + "\n";
        }
        return csv;
    }
}
