using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Linq;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace PvpStats.Windows.Detail;
internal class FrontlineMatchDetail : MatchDetail<FrontlineMatch> {


    public FrontlineMatchDetail(Plugin plugin, FrontlineMatch match) : base(plugin, plugin.FLCache, match) {
        //Flags -= ImGuiWindowFlags.AlwaysAutoResize;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(750, 400),
            MaximumSize = new Vector2(5000, 5000)
        };
    }

    public override void Draw() {
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
                ImGui.Text($"");
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
                DrawPlacement(Match.Result);
                ImGui.TableNextColumn();
                if(Match.MatchDuration != null) {
                    string durationText = ImGuiHelper.GetTimeSpanString((TimeSpan)Match.MatchDuration);
                    ImGuiHelper.RightAlignCursor(durationText);
                    ImGui.Text(durationText);
                }
            }
        }
        DrawTeamStatsTable();
    }

    private void DrawTeamStatsTable() {
        using var table = ImRaii.Table("teamstats", 4, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.Borders);
        if(!table) return;
        var team1 = Match.Teams.ElementAt(0);
        var team2 = Match.Teams.ElementAt(1);
        var team3 = Match.Teams.ElementAt(2);

        ImGui.TableSetupColumn("rows", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.NoClip);
        ImGui.TableSetupColumn("team1", ImGuiTableColumnFlags.WidthFixed, 150f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("team2", ImGuiTableColumnFlags.WidthFixed, 150f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("team3", ImGuiTableColumnFlags.WidthFixed, 150f * ImGuiHelpers.GlobalScale);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        DrawTeamName(team1.Key);
        ImGui.TableNextColumn();
        DrawTeamName(team2.Key);
        ImGui.TableNextColumn();
        DrawTeamName(team3.Key);

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        DrawPlacement(team1.Value.Placement);
        ImGui.TableNextColumn();
        DrawPlacement(team2.Value.Placement);
        ImGui.TableNextColumn();
        DrawPlacement(team3.Value.Placement);

        ImGui.TableNextColumn();
        var text1 = "Total points: ";
        ImGuiHelper.RightAlignCursor(text1);
        ImGui.TextUnformatted(text1);
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team1.Value.TotalPoints.ToString());
        ImGui.Text(team1.Value.TotalPoints.ToString());
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team2.Value.TotalPoints.ToString());
        ImGui.Text(team2.Value.TotalPoints.ToString());
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team3.Value.TotalPoints.ToString());
        ImGui.Text(team3.Value.TotalPoints.ToString());

        ImGui.TableNextColumn();
        string text4 = Match.Arena switch {
            FrontlineMap.FieldsOfGlory => "Points earned from ice: ",
            FrontlineMap.SealRock => "Points earned from tomeliths: ",
            FrontlineMap.OnsalHakair => "Points earned from ovoos: ",
            _ => "",
        };
        ImGuiHelper.RightAlignCursor(text4);
        ImGui.TextUnformatted(text4);

        if(Match.Arena == FrontlineMap.OnsalHakair || Match.Arena == FrontlineMap.SealRock) {
            ImGui.TableNextColumn();
            ImGuiHelper.CenterAlignCursor(team1.Value.OccupationPoints.ToString());
            ImGui.Text(team1.Value.OccupationPoints.ToString());
            ImGui.TableNextColumn();
            ImGuiHelper.CenterAlignCursor(team2.Value.OccupationPoints.ToString());
            ImGui.Text(team2.Value.OccupationPoints.ToString());
            ImGui.TableNextColumn();
            ImGuiHelper.CenterAlignCursor(team3.Value.OccupationPoints.ToString());
            ImGui.Text(team3.Value.OccupationPoints.ToString());
        } else {
            ImGui.TableNextColumn();
            ImGuiHelper.CenterAlignCursor(team1.Value.TargetablePoints.ToString());
            ImGui.Text(team1.Value.TargetablePoints.ToString());
            ImGui.TableNextColumn();
            ImGuiHelper.CenterAlignCursor(team2.Value.TargetablePoints.ToString());
            ImGui.Text(team2.Value.TargetablePoints.ToString());
            ImGui.TableNextColumn();
            ImGuiHelper.CenterAlignCursor(team3.Value.TargetablePoints.ToString());
            ImGui.Text(team3.Value.TargetablePoints.ToString());
        }

        ImGui.TableNextColumn();
        var text2 = "Points earned from kills: ";
        ImGuiHelper.RightAlignCursor(text2);
        ImGui.TextUnformatted(text2);
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team1.Value.KillPoints.ToString());
        ImGui.Text(team1.Value.KillPoints.ToString());
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team2.Value.KillPoints.ToString());
        ImGui.Text(team2.Value.KillPoints.ToString());
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team3.Value.KillPoints.ToString());
        ImGui.Text(team3.Value.KillPoints.ToString());

        ImGui.TableNextColumn();
        var text3 = "Points lost from deaths: ";
        ImGuiHelper.RightAlignCursor(text3);
        ImGui.TextUnformatted(text3);
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team1.Value.DeathPointLosses.ToString());
        ImGui.Text(team1.Value.DeathPointLosses.ToString());
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team2.Value.DeathPointLosses.ToString());
        ImGui.Text(team2.Value.DeathPointLosses.ToString());
        ImGui.TableNextColumn();
        ImGuiHelper.CenterAlignCursor(team3.Value.DeathPointLosses.ToString());
        ImGui.Text(team3.Value.DeathPointLosses.ToString());
    }

    private void DrawTeamName(FrontlineTeamName team) {
        var color = Plugin.Configuration.GetFrontlineTeamColor(team);
        var text = MatchHelper.GetTeamName(team);
        ImGuiHelper.CenterAlignCursor(text);
        ImGui.TextColored(color, text);
    }

    private void DrawPlacement(int? placement) {
        var color = placement switch {
            0 => Plugin.Configuration.Colors.Win,
            2 => Plugin.Configuration.Colors.Loss,
            _ => Plugin.Configuration.Colors.Other,
        };
        string resultText = placement != null ? ImGuiHelper.AddOrdinal((int)placement + 1).ToUpper() : "???";
        ImGuiHelper.CenterAlignCursor(resultText);
        ImGui.TextColored(color, resultText);
    }

    //private void DrawPlayerStatsTable() {
    //    //this is hacky
    //    int columnCount = 14;
    //    if(Match.Arena == FrontlineMap.FieldsOfGlory) {
    //        columnCount += 2;
    //    }
    //    if(Match.Arena == FrontlineMap.SealRock) {
    //        columnCount += 1;
    //    }
    //    using var table = ImRaii.Table($"postmatchplayers##{Match.Id}", columnCount, ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.NoSavedSettings);
    //    if(!table) return;
    //    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 50f, 0);
    //    ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f, 1);
    //    ImGui.TableSetupColumn("Kills", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Kills".GetHashCode());
    //    ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Deaths".GetHashCode());
    //    ImGui.TableSetupColumn("Assists", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Assists".GetHashCode());
    //    if(Match.Arena == FrontlineMap.FieldsOfGlory) {
    //        ImGui.TableSetupColumn("Damage to PCs", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToPCs".GetHashCode());
    //        ImGui.TableSetupColumn("Ice Damage", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToOther".GetHashCode());
    //        ImGui.TableSetupColumn("Damage Dealt", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
    //    } else {
    //        ImGui.TableSetupColumn("Damage Dealt", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
    //    }
    //    ImGui.TableSetupColumn("Damage Taken", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageTaken".GetHashCode());
    //    ImGui.TableSetupColumn("HP Restored", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"HPRestored".GetHashCode());
    //    ImGui.TableSetupColumn("Special", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 60f, (uint)"Special1".GetHashCode());
    //    if(Match.Arena == FrontlineMap.SealRock) {
    //        ImGui.TableSetupColumn("Occupations", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"Occupations".GetHashCode());
    //    }
    //    ImGui.TableSetupColumn("Damage Dealt per Kill/Assist", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerKA".GetHashCode());
    //    ImGui.TableSetupColumn("Damage Dealt per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerLife".GetHashCode());
    //    ImGui.TableSetupColumn("Damage Taken per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageTakenPerLife".GetHashCode());
    //    ImGui.TableSetupColumn("HP Restored per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"HPRestoredPerLife".GetHashCode());
    //    ImGui.TableSetupColumn("KDA Ratio", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"KDA".GetHashCode());

    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("");
    //    ImGui.TableNextColumn();
    //    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
    //    ImGui.TableHeader("Job");
    //    ImGui.TableNextColumn();
    //    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
    //    ImGui.TableHeader("Kills");
    //    ImGui.TableNextColumn();
    //    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
    //    ImGui.TableHeader("Deaths");
    //    ImGui.TableNextColumn();
    //    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
    //    ImGui.TableHeader("Assists");
    //    if(Match.Arena == FrontlineMap.FieldsOfGlory) {
    //        ImGui.TableNextColumn();
    //        ImGui.TableHeader("Damage\nto PCs");
    //        ImGui.TableNextColumn();
    //        ImGui.TableHeader("Damage\nto Other");
    //    }
    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("Damage\nDealt");
    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("Damage\nTaken");
    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("HP\nRestored");
    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("???");
    //    ImGuiHelper.HelpMarker("Not sure what this is.");
    //    ImGui.TableNextColumn();
    //    if(Match.Arena == FrontlineMap.FieldsOfGlory) {
    //        ImGui.TableHeader("Occupations");
    //        ImGui.TableNextColumn();
    //    }
    //    ImGui.TableHeader("Damage Dealt\nper Kill/Assist");
    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("Damage Dealt\nper Life");
    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("Damage Taken\nper Life");
    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("HP Restored\nper Life");
    //    ImGui.TableNextColumn();
    //    ImGui.TableHeader("KDA\nRatio");

    //    //column sorting
    //    ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
    //    if(sortSpecs.SpecsDirty) {
    //        //SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
    //        sortSpecs.SpecsDirty = false;
    //    }

    //    foreach(var row in Match.PlayerScoreboards) {
    //        var player = Match.Players.Where(x => x.Name.Equals(row.Key)).First();
    //        var playerAlias = (PlayerAlias)row.Key;
    //        ImGui.TableNextColumn();
    //        //bool isPlayer = row.Key.Player != null;
    //        //bool isPlayerTeam = row.Key.Team == _dataModel.LocalPlayerTeam?.TeamName;
    //        var rowColor = Plugin.Configuration.GetFrontlineTeamColor(player.Team) - new Vector4(0f, 0f, 0f, 0.7f);
    //        //switch((isPlayer, isPlayerTeam)) {
    //        //    case (true, true):
    //        //        rowColor = _plugin.Configuration.Colors.CCPlayerTeam - new Vector4(0f, 0f, 0f, 0.7f);
    //        //        break;
    //        //    case (true, false):
    //        //        rowColor = _plugin.Configuration.Colors.CCEnemyTeam - new Vector4(0f, 0f, 0f, 0.7f);
    //        //        break;
    //        //    case (false, true):
    //        //        rowColor = _plugin.Configuration.Colors.CCPlayerTeam - new Vector4(0f, 0f, 0f, 0.3f);
    //        //        break;
    //        //    case (false, false):
    //        //        rowColor = _plugin.Configuration.Colors.CCEnemyTeam - new Vector4(0f, 0f, 0f, 0.3f);
    //        //        break;
    //        //}
    //        var textColor = Match.LocalPlayer is not null && Match.LocalPlayer.Equals(playerAlias) ? new Vector4(0.1f, 0.1f, 0.1f, 1f) : ImGuiColors.DalamudWhite;
    //        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
    //        ImGui.TextColored(textColor, $" {playerAlias.Name} ");
    //        //if(isPlayer) {
    //        //    ImGui.TextColored(textColor, $" {row.Key.Player?.Name} ");
    //        //} else {
    //        //    ImGui.TextColored(textColor, $" {MatchHelper.GetTeamName(row.Key.Team ?? CrystallineConflictTeamName.Unknown)}");
    //        //}
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{player.Job}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{row.Value.Kills}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{row.Value.Deaths}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{row.Value.Assists}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{row.Value.DamageDealt}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{row.Value.DamageTaken}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.HPRestored) : row.Value.Item1.HPRestored)}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{(isPlayer && _showPercentages ? string.Format("{0:P1}%", row.Value.Item2.TimeOnCrystalDouble) : ImGuiHelper.GetTimeSpanString(row.Value.Item1.TimeOnCrystal))}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{string.Format("{0:f0}", row.Value.Item1.DamageDealtPerKA)}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{string.Format("{0:f0}", row.Value.Item1.DamageDealtPerLife)}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{string.Format("{0:f0}", row.Value.Item1.DamageTakenPerLife)}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{string.Format("{0:f0}", row.Value.Item1.HPRestoredPerLife)}");
    //        ImGui.TableNextColumn();
    //        ImGui.TextColored(textColor, $"{string.Format("{0:0.00}", row.Value.Item1.KDA)}");
    //    }
    //}

    protected override string BuildCSV() {
        throw new NotImplementedException();
    }
}
