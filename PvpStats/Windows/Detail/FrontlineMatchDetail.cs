using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Detail;
internal class FrontlineMatchDetail : MatchDetail<FrontlineMatch> {

    private FLTeamQuickFilter _teamQuickFilter;
    private Dictionary<PlayerAlias, FLScoreboardDouble> _playerContributions = [];
    private Dictionary<FrontlineTeamName, FrontlineScoreboard> _teamScoreboard;
    private Dictionary<string, FrontlineScoreboard> _scoreboard;
    private Dictionary<string, FrontlineScoreboard> _unfilteredScoreboard;
    private bool _triggerSort;
    private bool _firstDrawComplete;
    private Vector2 _scoreboardSize;

    private FrontlineMatchTimeline? _timeline;
    private double[] _axisTicks = [];
    private string[] _axisLabels = [];
    private Dictionary<FrontlineTeamName, (float[] Xs, float[] Ys)> _teamPoints = new() {
        {FrontlineTeamName.Maelstrom, new() },
        {FrontlineTeamName.Adders, new() },
        {FrontlineTeamName.Flames, new() },
    };
    private (float[] Xs, float[] Ys) _playerBattleHigh = new();

    public FrontlineMatchDetail(Plugin plugin, FrontlineMatch match) : base(plugin, plugin.FLCache, match) {
        //Flags -= ImGuiWindowFlags.AlwaysAutoResize;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(700, 800),
            MaximumSize = new Vector2(5000, 5000)
        };

        SizeCondition = ImGuiCond.Appearing;
        switch(match.Arena) {
            case FrontlineMap.BorderlandRuins:
            case FrontlineMap.FieldsOfGlory:
                _scoreboardSize = new Vector2(930, 800);
                break;
            case FrontlineMap.SealRock:
                _scoreboardSize = new Vector2(920, 800);
                break;
            default:
            case FrontlineMap.OnsalHakair:
                _scoreboardSize = new Vector2(865, 800);
                break;
        }
        Size = new Vector2(_scoreboardSize.X, _scoreboardSize.Y);

        _timeline = Plugin.FLCache.GetTimeline(Match);
        if(Match.Flags.HasFlag(FLValidationFlag.InvalidDirector)) {
            _timeline = null;
        }
        if(_timeline != null) {
            //setup graphs
            List<double> axisTicks = new();
            List<string> axisLabels = new();
            for(int i = 0; i <= 20; i++) {
                axisTicks.Add(i * 60);
                axisLabels.Add(ImGuiHelper.GetTimeSpanString(new TimeSpan(0, i, 0)));
            }
            _axisTicks = axisTicks.ToArray();
            _axisLabels = axisLabels.ToArray();

            //point graphs
            SetupPointsGraph(FrontlineTeamName.Maelstrom);
            SetupPointsGraph(FrontlineTeamName.Adders);
            SetupPointsGraph(FrontlineTeamName.Flames);

            if(_timeline.SelfBattleHigh != null) {
                var bhEvents = _timeline.SelfBattleHigh
                .Append(new((DateTime)Match.MatchEndTime!, _timeline.SelfBattleHigh.Last().Count));
                _playerBattleHigh = (bhEvents.Select(x => (float)(x.Timestamp - Match.MatchStartTime).Value.TotalSeconds).ToArray(), bhEvents.Select(x => (float)x.Count).ToArray());
            }
        }

        CSV = BuildCSV();
        _teamQuickFilter = new(plugin, ApplyTeamFilter);
        _unfilteredScoreboard = match.PlayerScoreboards;
        _scoreboard = _unfilteredScoreboard;
        _teamScoreboard = match.GetTeamScoreboards();
        _playerContributions = match.GetPlayerContributions();
        _triggerSort = true;
    }

    public override void Draw() {
        base.Draw();
        if(Plugin.Configuration.ShowBackgroundImage) {
            var cursorPosBefore = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - (259 / 2 + 0f) * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY((ImGui.GetCursorPos().Y + 40f * ImGuiHelpers.GlobalScale));
            ImGui.Image(Plugin.TextureProvider.GetFromFile(Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "fl_logo.png")).GetWrapOrEmpty().Handle,
                new Vector2(259, 233) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, new Vector4(1, 1, 1, 0.1f));
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
                columnWidth = Math.Max(columnWidth, 150f * ImGuiHelpers.GlobalScale);
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
                            FrontlineMap.BorderlandRuins => "Points earned from occupations:",
                            FrontlineMap.FieldsOfGlory => "Points earned from ice:",
                            FrontlineMap.SealRock => "Points earned from tomeliths:",
                            FrontlineMap.OnsalHakair => "Points earned from ovoos:",
                            _ => "",
                        };
                        DrawRowDescription(specialRow);
                        if(Match.Arena == FrontlineMap.BorderlandRuins) {
                            ImGui.TableNextColumn();
                            DrawRowDescription("Points earned from drones:");
                        }
                        ImGui.TableNextColumn();
                        DrawRowDescription("Points earned from kills:");
                        ImGui.TableNextColumn();
                        DrawRowDescription("Points lost from deaths:");
                        ImGui.TableNextColumn();
                        DrawRowDescription("Kill/death point diff.:");
                    }
                }
                foreach(var team in Match.Teams.OrderBy(x => {
                    if(Plugin.Configuration.OrderFrontlineTeamsByPlacement ?? false) {
                        return x.Value.Placement;
                    } else if(Plugin.Configuration.LeftPlayerTeam) {
                        return Convert.ToInt32(x.Key != Match.LocalPlayerTeam);
                    } else {
                        return 1;
                    }
                })) {
                    ImGui.TableNextColumn();
                    DrawTeamStatTable(team.Key);
                }
            }
        }

        if(_timeline != null) {
            using(var tabBar = ImRaii.TabBar("TabBar")) {
                if(Match.PlayerScoreboards != null) {
                    using var tab = ImRaii.TabItem("Scoreboard");
                    if(tab) {
                        if(CurrentTab != "Scoreboard") {
                            SetWindowSize(_scoreboardSize);
                            CurrentTab = "Scoreboard";
                        }
                        DrawScoreboard();
                    }
                }
                using(var tab2 = ImRaii.TabItem("Graphs")) {
                    if(tab2) {
                        if(CurrentTab != "Graphs") {
                            SetWindowSize(new Vector2(975, 800));
                            CurrentTab = "Graphs";
                        }
                        DrawGraphs();
                    }
                }
            }
        } else {
            ImGui.NewLine();
            DrawScoreboard();
        }
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
        ImGuiHelper.RightAlignCursor2(desc, -5f * ImGuiHelpers.GlobalScale);
        ImGui.TextUnformatted(desc);
    }

    private void DrawPlayerStatsTable() {
        var tableFlags = ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollX
            | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.PadOuterX;
        if(Plugin.Configuration.StretchScoreboardColumns ?? false) {
            tableFlags -= ImGuiTableFlags.ScrollX;
        }
        //this is hacky
        int columnCount = 16;
        if(Match.Arena == FrontlineMap.FieldsOfGlory || Match.Arena == FrontlineMap.BorderlandRuins) {
            columnCount += 2;
        }
        if(Match.Arena == FrontlineMap.SealRock || Match.Arena == FrontlineMap.BorderlandRuins) {
            columnCount += 1;
        }
        if(Match.MaxBattleHigh != null) {
            columnCount += 1;
            //tableFlags &= ~ImGuiTableFlags.PadOuterX;
        }

        using var table = ImRaii.Table($"postmatchplayers##{Match.Id}", columnCount, tableFlags, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
        //new Vector2(ImGui.GetContentRegionAvail().X, 550f * ImGuiHelpers.GlobalScale)
        if(!table) return;

        var widthStyle = Plugin.Configuration.StretchScoreboardColumns ?? false ? ImGuiTableColumnFlags.WidthStretch : ImGuiTableColumnFlags.WidthFixed;
        ImGui.TableSetupColumn("Alliance", widthStyle | ImGuiTableColumnFlags.NoHeaderLabel, ImGuiHelpers.GlobalScale * 10f, 3);
        if(Match.MaxBattleHigh != null) {
            ImGui.TableSetupColumn("Peak Battle High", widthStyle | ImGuiTableColumnFlags.NoHeaderLabel, ImGuiHelpers.GlobalScale * 15f, 4);
        }
        ImGui.TableSetupColumn("Name", widthStyle, ImGuiHelpers.GlobalScale * 200f, 0);
        ImGui.TableSetupColumn("Home World", widthStyle, ImGuiHelpers.GlobalScale * 110f, 1);
        ImGui.TableSetupColumn("Job", widthStyle, ImGuiHelpers.GlobalScale * 50f, 2);
        ImGui.TableSetupColumn("Kills", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Kills".GetHashCode());
        ImGui.TableSetupColumn("Deaths", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Deaths".GetHashCode());
        ImGui.TableSetupColumn("Assists", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Assists".GetHashCode());
        if(Match.Arena == FrontlineMap.FieldsOfGlory) {
            ImGui.TableSetupColumn("Damage to PCs", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToPCs".GetHashCode());
            ImGui.TableSetupColumn("Ice Damage", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToOther".GetHashCode());
            ImGui.TableSetupColumn("Damage Dealt", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        } else if(Match.Arena == FrontlineMap.BorderlandRuins) {
            ImGui.TableSetupColumn("Damage to PCs", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToPCs".GetHashCode());
            ImGui.TableSetupColumn("Drone Damage", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToOther".GetHashCode());
            ImGui.TableSetupColumn("Damage Dealt", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        } else {
            ImGui.TableSetupColumn("Damage Dealt", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        }
        ImGui.TableSetupColumn("Damage Taken", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageTaken".GetHashCode());
        ImGui.TableSetupColumn("HP Restored", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"HPRestored".GetHashCode());
        ImGui.TableSetupColumn("Special", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 65f, (uint)"Special1".GetHashCode());
        if(Match.Arena == FrontlineMap.SealRock || Match.Arena == FrontlineMap.BorderlandRuins) {
            ImGui.TableSetupColumn("Occupations", widthStyle, ImGuiHelpers.GlobalScale * 55f, (uint)"Occupations".GetHashCode());
        }
        ImGui.TableSetupColumn("Damage Dealt per Kill/Assist", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerKA".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerLife".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageTakenPerLife".GetHashCode());
        ImGui.TableSetupColumn("HP Restored per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"HPRestoredPerLife".GetHashCode());
        ImGui.TableSetupColumn("KDA Ratio", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 50f, (uint)"KDA".GetHashCode());

        if(Match.MaxBattleHigh != null) {
            ImGui.TableSetupScrollFreeze(3, 1);
        } else {
            ImGui.TableSetupScrollFreeze(2, 1);
        }

        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        //if(!_firstDrawComplete) {
        //    sortSpecs.Specs.ColumnUserID = 0;
        //    //sortSpecs.Specs.ColumnIndex = 2;
        //    sortSpecs.Specs.SortDirection = ImGuiSortDirection.Ascending;
        //    sortSpecs.SpecsDirty = true;
        //    _triggerSort = true;
        //    _firstDrawComplete = true;
        //}
        if(sortSpecs.SpecsDirty || _triggerSort) {
            _triggerSort = false;
            SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
            sortSpecs.SpecsDirty = false;
        }

        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Alliance", 0, false);
        }
        if(Match.MaxBattleHigh != null) {
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawTableHeader("Peak Battle High", 0, false);
            }
        }
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
        if(Match.Arena == FrontlineMap.FieldsOfGlory || Match.Arena == FrontlineMap.BorderlandRuins) {
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawTableHeader("Damage\nto PCs");
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawTableHeader("Damage\nto Other");
            }
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
            ImGuiHelper.RightAlignCursor2("(?)", -20f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("");
            ImGuiHelper.HelpMarker("Not sure what this is. It's related to healing.", true);
        }
        if(Match.Arena == FrontlineMap.SealRock || Match.Arena == FrontlineMap.BorderlandRuins) {
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawTableHeader("Occup-\nations");
            }
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

        if(ShowTeamRows) {
            foreach(var row in _teamScoreboard.Where(x => _teamQuickFilter.FilterState[x.Key])) {
                using var textColor = ImRaii.PushColor(ImGuiCol.Text, Plugin.Configuration.Colors.TeamRowText);
                var rowColor = Plugin.Configuration.GetFrontlineTeamColor(row.Key);
                rowColor.W = Plugin.Configuration.TeamRowAlpha;
                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
                if(Match.MaxBattleHigh != null) {
                    ImGui.TableNextColumn();
                }
                if(ImGui.TableNextColumn()) {
                    ImGui.TextUnformatted(MatchHelper.GetTeamName(row.Key));
                }
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
                if(Match.Arena == FrontlineMap.FieldsOfGlory || Match.Arena == FrontlineMap.BorderlandRuins) {
                    if(ImGui.TableNextColumn()) {
                        ImGuiHelper.DrawNumericCell($"{row.Value.DamageToPCs}", -11f);
                    }
                    if(ImGui.TableNextColumn()) {
                        ImGuiHelper.DrawNumericCell($"{row.Value.DamageToOther}", -11f);
                    }
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
                    ImGuiHelper.DrawNumericCell($"{row.Value.Special1}", -11f);
                }
                if(Match.Arena == FrontlineMap.SealRock || Match.Arena == FrontlineMap.BorderlandRuins) {
                    if(ImGui.TableNextColumn()) {
                        ImGuiHelper.DrawNumericCell($"{row.Value.Occupations}", -11f);
                    }
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
                    ImGuiHelper.DrawNumericCell($"{string.Format("{0:0.00}", row.Value.KDA)}", -11f);
                }
            }
        }

        foreach(var row in _scoreboard) {
            var player = Match.Players.Where(x => x.Name.Equals(row.Key)).First();
            var playerAlias = (PlayerAlias)row.Key;
            //bool isPlayer = row.Key.Player != null;
            //bool isPlayerTeam = row.Key.Team == _dataModel.LocalPlayerTeam?.TeamName;
            var rowColor = Plugin.Configuration.GetFrontlineTeamColor(player.Team);
            rowColor.W = Plugin.Configuration.PlayerRowAlpha;
            var textColor = Match.LocalPlayer is not null && Match.LocalPlayer.Equals(playerAlias) ? Plugin.Configuration.Colors.CCLocalPlayer : Plugin.Configuration.Colors.PlayerRowText;
            ImGui.TableNextColumn();
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            string alliance = MatchHelper.GetAllianceLetter(player.Alliance);
            ImGui.TextColored(textColor, $"{alliance}");
            if(Match.MaxBattleHigh != null) {
                if(ImGui.TableNextColumn()) {
                    if(Match.MaxBattleHigh.TryGetValue(playerAlias, out var peakBattleHigh)) {
                        var icon = TextureHelper.GetBattleHighIcon((uint)peakBattleHigh);
                        if(icon != null) {
                            var size = 16f;
                            var cursorBefore = ImGui.GetCursorPosY();
                            ImGui.Image(Plugin.WindowManager.GetTextureHandle((uint)icon), new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0f), new Vector2(0.88f));
                        }
                    }
                }
            }
            if(ImGui.TableNextColumn()) {
                ImGui.TextColored(textColor, $"{playerAlias.Name}");
            }
            if(ImGui.TableNextColumn()) {
                ImGui.TextColored(textColor, $"{player.Name.HomeWorld}");
            }
            if(ImGui.TableNextColumn()) {
                var jobString = $"{player.Job}";
                ImGuiHelper.CenterAlignCursor(jobString);
                ImGui.TextColored(textColor, jobString);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].Kills) : row.Value.Kills)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].Deaths) : row.Value.Deaths)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].Assists) : row.Value.Assists)}", -11f, textColor);
            }
            if(Match.Arena == FrontlineMap.FieldsOfGlory || Match.Arena == FrontlineMap.BorderlandRuins) {
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].DamageToPCs) : row.Value.DamageToPCs)}", -11f, textColor);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].DamageToOther) : row.Value.DamageToOther)}", -11f, textColor);
                }
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].DamageDealt) : row.Value.DamageDealt)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].DamageTaken) : row.Value.DamageTaken)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].HPRestored) : row.Value.HPRestored)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].Special1) : row.Value.Special1)}", -11f, textColor);
            }
            if(Match.Arena == FrontlineMap.SealRock || Match.Arena == FrontlineMap.BorderlandRuins) {
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions[player.Name].Occupations) : row.Value.Occupations)}", -11f, textColor);
                }
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealtPerKA}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealtPerLife}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{row.Value.DamageTakenPerLife}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{row.Value.HPRestoredPerLife}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:0.00}", row.Value.KDA)}", -11f, textColor);
            }
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
        if(Match.Arena == FrontlineMap.OnsalHakair || Match.Arena == FrontlineMap.SealRock || Match.Arena == FrontlineMap.BorderlandRuins) {
            var specialPoints = team.OccupationPoints.ToString();
            ImGuiHelper.CenterAlignCursor(specialPoints);
            ImGui.Text(specialPoints);
        } else {
            var specialPoints = team.TargetablePoints.ToString();
            ImGuiHelper.CenterAlignCursor(specialPoints);
            ImGui.Text(specialPoints);
        }

        if(Match.Arena == FrontlineMap.BorderlandRuins) {
            ImGui.TableNextColumn();
            var dronePoints = team.DronePoints.ToString();
            ImGuiHelper.CenterAlignCursor(dronePoints);
            ImGui.Text(dronePoints);
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

    private void DrawScoreboard() {
        ImGui.NewLine();
        ImGuiHelper.HelpMarker("Right-click table header to show and hide columns including extra metrics.", true, true);
        using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
            ImGui.SameLine();
        }
        //ImGui.AlignTextToFramePadding();
        ImGuiComponents.ToggleButton("##showPercentages", ref ShowPercentages);
        ImGui.SameLine();
        ImGui.Text("Show team contributions");
        using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
            ImGui.SameLine();
        }
        ImGui.Checkbox("###showTeamRows", ref ShowTeamRows);
        ImGui.SameLine();
        ImGui.Text("Show team totals");
        using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
            ImGui.SameLine();
        }
        _teamQuickFilter.Draw();
        DrawPlayerStatsTable();
    }

    private void DrawGraphs() {
        //filters

        using var child = ImRaii.Child("graphChild", ImGui.GetContentRegionAvail(), true);
        if(child) {
            if(_timeline?.TeamPoints != null) {
                DrawTeamPointsGraph();
            }
            if(_timeline?.SelfBattleHigh != null) {
                DrawBattleHighGraph();
            }
        }
    }

    private void DrawTeamPointsGraph() {
        using var plot = ImRaii.Plot("Team Points", new Vector2(ImGui.GetContentRegionAvail().X, 500f * ImGuiHelpers.GlobalScale), ImPlotFlags.None);

        if(!plot) {
            return;
        }

        var maxScore = MatchHelper.GetFrontlineMaxPoints(Match);

        ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Linear);
        ImPlot.SetupAxesLimits(0, 1200, 0, maxScore, ImPlotCond.Once);
        ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, 0, 1200);
        ImPlot.SetupAxisLimitsConstraints(ImAxis.Y1, 0, maxScore);

        ImPlot.SetupAxes("Match Time", "", ImPlotAxisFlags.None, ImPlotAxisFlags.None);
        ImPlot.SetupLegend(ImPlotLocation.NorthWest, ImPlotLegendFlags.None);

        ImPlot.SetupAxisTicks(ImAxis.X1, ref _axisTicks[0], _axisTicks.Length, _axisLabels);

        using(var style = ImRaii.PushColor(ImPlotCol.Line, Plugin.Configuration.GetFrontlineTeamColor(FrontlineTeamName.Maelstrom))) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 2f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Maelstrom", ref _teamPoints[FrontlineTeamName.Maelstrom].Xs[0],
                ref _teamPoints[FrontlineTeamName.Maelstrom].Ys[0],
                _teamPoints[FrontlineTeamName.Maelstrom].Xs.Length, ImPlotStairsFlags.None);
        }
        using(var style = ImRaii.PushColor(ImPlotCol.Line, Plugin.Configuration.GetFrontlineTeamColor(FrontlineTeamName.Adders))) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 2f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Adders", ref _teamPoints[FrontlineTeamName.Adders].Xs[0],
                ref _teamPoints[FrontlineTeamName.Adders].Ys[0],
                _teamPoints[FrontlineTeamName.Adders].Xs.Length, ImPlotStairsFlags.None);
        }
        using(var style = ImRaii.PushColor(ImPlotCol.Line, Plugin.Configuration.GetFrontlineTeamColor(FrontlineTeamName.Flames))) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 2f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Flames", ref _teamPoints[FrontlineTeamName.Flames].Xs[0],
                ref _teamPoints[FrontlineTeamName.Flames].Ys[0],
                _teamPoints[FrontlineTeamName.Flames].Xs.Length, ImPlotStairsFlags.None);
        }
    }

    private void DrawBattleHighGraph() {
        using var plot = ImRaii.Plot("Self Battle High", new Vector2(ImGui.GetContentRegionAvail().X, 500f * ImGuiHelpers.GlobalScale), ImPlotFlags.NoLegend);

        if(!plot) {
            return;
        }

        var maxMatchLength = 1200;
        float[] bhXs = [0, maxMatchLength];
        float[] bh1Ys = [20, 20];
        float[] bh2Ys = [40, 40];
        float[] bh3Ys = [60, 60];
        float[] bh4Ys = [80, 80];
        float[] bh5Ys = [100, 100];

        ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Linear);
        ImPlot.SetupAxesLimits(0, maxMatchLength, 0, 110, ImPlotCond.Once);
        ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, 0, maxMatchLength);
        ImPlot.SetupAxisLimitsConstraints(ImAxis.Y1, 0, 110);

        ImPlot.SetupAxes("Match Time", "", ImPlotAxisFlags.None, ImPlotAxisFlags.None);
        //ImPlot.SetupLegend(ImPlotLocation.NorthWest, ImPlotLegendFlags.Horizontal);

        ImPlot.SetupAxisTicks(ImAxis.X1, ref _axisTicks[0], _axisTicks.Length, _axisLabels);

        using(var style = ImRaii.PushColor(ImPlotCol.Line, ImGuiColors.ParsedBlue)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 1f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("BH1", ref bhXs[0],
                ref bh1Ys[0],
                2, ImPlotStairsFlags.None);
        }
        using(var style = ImRaii.PushColor(ImPlotCol.Line, ImGuiColors.ParsedGreen)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 1f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("BH2", ref bhXs[0],
                ref bh2Ys[0],
                2, ImPlotStairsFlags.None);
        }
        using(var style = ImRaii.PushColor(ImPlotCol.Line, ImGuiColors.ParsedGold)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 1f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("BH3", ref bhXs[0],
                ref bh3Ys[0],
                2, ImPlotStairsFlags.None);
        }
        using(var style = ImRaii.PushColor(ImPlotCol.Line, ImGuiColors.ParsedOrange)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 1f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("BH4", ref bhXs[0],
                ref bh4Ys[0],
                2, ImPlotStairsFlags.None);
        }
        using(var style = ImRaii.PushColor(ImPlotCol.Line, ImGuiColors.DPSRed)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 1f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("BH4", ref bhXs[0],
                ref bh5Ys[0],
                2, ImPlotStairsFlags.None);
        }

        using(var style = ImRaii.PushColor(ImPlotCol.Line, ImGuiColors.DalamudWhite)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 2f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Battle High", ref _playerBattleHigh.Xs[0],
                ref _playerBattleHigh.Ys[0],
                _playerBattleHigh.Xs.Length, ImPlotStairsFlags.None);
        }
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        Func<KeyValuePair<string, FrontlineScoreboard>, object> comparator = (r) => 0;
        Func<KeyValuePair<FrontlineTeamName, FrontlineScoreboard>, object> teamComparator = (r) => 0;

        //0 = name
        //1 = homeworld
        //2 = job
        //3 = alliance
        //4 = peak BH
        if(columnId == 0) {
            comparator = (r) => Match.Players.First(x => x.Name.Equals(r.Key)).Name.Name ?? "";
            teamComparator = (r) => r.Key;
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
                        //Plugin.Log.Debug($"sorting by {prop.Name}");
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
                        //Plugin.Log.Debug($"sorting by {prop.Name}");
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

        if(!Plugin.Configuration.AnchorTeamNames) {
            _teamScoreboard = direction == ImGuiSortDirection.Ascending ? _teamScoreboard.OrderBy(teamComparator).ToDictionary()
                : _teamScoreboard.OrderByDescending(teamComparator).ToDictionary();
        }
    }

    private Task ApplyTeamFilter() {
        _scoreboard = _unfilteredScoreboard.Where(x => {
            var player = Match.Players.Where(y => y.Name.Equals(x.Key)).First();
            return _teamQuickFilter.FilterState[player.Team];
        }).ToDictionary();
        //_triggerSort = true;
        return Task.CompletedTask;
    }

    private void SetupPointsGraph(FrontlineTeamName team) {
        if(_timeline?.TeamPoints == null) {
            return;
        }
        var pointEvents = _timeline.TeamPoints[team]
            //.Where(x => x.Points != 0 || (x.Timestamp - Match.MatchStartTime).Value.TotalSeconds > 10)
            .Append(new((DateTime)Match.MatchEndTime!, _timeline.TeamPoints[team].Last().Points));
        _teamPoints[team] = (pointEvents.Select(x => (float)(x.Timestamp - Match.MatchStartTime).Value.TotalSeconds).ToArray(), pointEvents.Select(x => (float)x.Points).ToArray());
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
