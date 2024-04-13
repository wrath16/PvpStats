using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class CrystallineConflictPlayerList : CCStatsList<PlayerAlias> {

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Name", Id = 0, Width = 200f, Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{Name = "Home World", Id = 1, Width = 110f, Flags = ImGuiTableColumnFlags.WidthFixed },
        new ColumnParams{Name = "Favored Job", Id = (uint)"StatsAll.Job".GetHashCode() },
        new ColumnParams{Name = "Total Matches", Id = (uint)"StatsAll.Matches".GetHashCode() },
        new ColumnParams{Name = "Player Wins", Id = (uint)"StatsAll.Wins".GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Player Losses", Id = (uint)"StatsAll.Losses".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Player Win Diff.", Id = (uint)"StatsAll.WinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Player Win Rate", Id = (uint)"StatsAll.WinRate".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Wins", Id = (uint)"StatsPersonal.Wins".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Losses", Id = (uint)"StatsPersonal.Losses".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Win Diff.", Id = (uint)"StatsPersonal.WinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Win Rate", Id = (uint)"StatsPersonal.WinRate".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Matches", Id = (uint)"StatsTeammate.Matches".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Wins", Id = (uint)"StatsTeammate.Wins".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Losses", Id = (uint)"StatsTeammate.Losses".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Win Diff.", Id = (uint)"StatsTeammate.WinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Win Rate", Id = (uint)"StatsTeammate.WinRate".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Matches", Id = (uint)"StatsOpponent.Matches".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Wins", Id = (uint)"StatsOpponent.Wins".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Losses", Id = (uint)"StatsOpponent.Losses".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Win Diff.", Id = (uint)"StatsOpponent.WinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Win Rate", Id = (uint)"StatsOpponent.WinRate".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Kills", Id = (uint)"ScoreboardTotal.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Deaths", Id = (uint)"ScoreboardTotal.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Assists", Id = (uint)"ScoreboardTotal.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Damage Dealt", Id = (uint)"ScoreboardTotal.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Damage Taken", Id = (uint)"ScoreboardTotal.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total HP Restored", Id = (uint)"ScoreboardTotal.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Time on Crystal", Id = (uint)"ScoreboardTotal.TimeOnCrystal".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Kills/Assists", Id = (uint)"ScoreboardTotal.KillsAndAssists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Kills Per Match", Id = (uint)"ScoreboardPerMatch.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Deaths Per Match", Id = (uint)"ScoreboardPerMatch.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Assists Per Match", Id = (uint)"ScoreboardPerMatch.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Match", Id = (uint)"ScoreboardPerMatch.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Taken Per Match", Id = (uint)"ScoreboardPerMatch.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "HP Restored Per Match", Id = (uint)"ScoreboardPerMatch.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Time on Crystal Per Match", Id = (uint)"ScoreboardPerMatch.TimeOnCrystal".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Kills/Assists Per Match", Id = (uint)"ScoreboardPerMatch.KillsAndAssists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Kills Per Min", Id = (uint)"ScoreboardPerMin.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Deaths Per Min", Id = (uint)"ScoreboardPerMin.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Assists Per Min", Id = (uint)"ScoreboardPerMin.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Min", Id = (uint)"ScoreboardPerMin.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Taken Per Min", Id = (uint)"ScoreboardPerMin.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "HP Restored Per Min", Id = (uint)"ScoreboardPerMin.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Time on Crystal Per Min", Id = (uint)"ScoreboardPerMin.TimeOnCrystal".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Kills/Assists Per Min", Id = (uint)"ScoreboardPerMin.KillsAndAssists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Kill Contrib.", Id = (uint)"ScoreboardContrib.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Death Contrib.", Id = (uint)"ScoreboardContrib.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Assist Contrib.", Id = (uint)"ScoreboardContrib.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Damage Dealt Contrib.", Id = (uint)"ScoreboardContrib.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Damage Taken Contrib.", Id = (uint)"ScoreboardContrib.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median HP Restored Contrib.", Id = (uint)"ScoreboardContrib.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Time on Crystal Contrib.", Id = (uint)"ScoreboardContrib.TimeOnCrystalDouble".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Kill/Assist Contrib.", Id = (uint)"ScoreboardContrib.KillsAndAssists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Kill/Assist", Id = (uint)"ScoreboardTotal.DamageDealtPerKA".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Life", Id = (uint)"ScoreboardTotal.DamageDealtPerLife".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Taken Per Life", Id = (uint)"ScoreboardTotal.DamageTakenPerLife".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "HP Restored Per Life", Id = (uint)"ScoreboardTotal.HPRestoredPerLife".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "KDA Ratio", Id = (uint)"ScoreboardTotal.KDA".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
    };

    protected override string TableId => "###CCPlayerStatsTable";

    private List<PlayerAlias> DataModelUntruncated { get; set; } = new();
    internal bool InheritFromPlayerFilter { get; private set; } = true;
    internal uint MinMatches { get; private set; } = 1;
    private string PlayerQuickSearch { get; set; } = "";

    public CrystallineConflictPlayerList(Plugin plugin) : base(plugin) {
        MinMatches = plugin.Configuration.MatchWindowFilters.MinMatches;
        InheritFromPlayerFilter = plugin.Configuration.MatchWindowFilters.PlayersInheritFromPlayerFilter;
    }

    protected override void PreTableDraw() {
        bool inheritFromPlayerFilter = InheritFromPlayerFilter;
        if(ImGui.Checkbox($"Inherit from player filter##{GetHashCode()}", ref inheritFromPlayerFilter)) {
            _plugin!.DataQueue.QueueDataOperation(async () => {
                InheritFromPlayerFilter = inheritFromPlayerFilter;
                _plugin.Configuration.MatchWindowFilters.PlayersInheritFromPlayerFilter = inheritFromPlayerFilter;
                await _plugin.WindowManager.Refresh();
            });
        }
        ImGuiHelper.HelpMarker("Will only include stats for players who match all conditions of the player filter.");

        int minMatches = (int)MinMatches;
        ImGuiHelper.SetDynamicWidth(150f, 250f, 3f);
        if(ImGui.SliderInt("Min. matches", ref minMatches, 1, 100)) {
            MinMatches = (uint)minMatches;
            _plugin.Configuration.MatchWindowFilters.MinMatches = MinMatches;
            ApplyQuickFilters(MinMatches, PlayerQuickSearch);
        }
        ImGui.SameLine();
        string quickSearch = PlayerQuickSearch;
        ImGuiHelper.SetDynamicWidth(150f, 250f, 3f);
        if(ImGui.InputTextWithHint("###playerQuickSearch", "Search...", ref quickSearch, 100)) {
            PlayerQuickSearch = quickSearch;
            ApplyQuickFilters(MinMatches, PlayerQuickSearch);
        }
        ImGuiHelper.HelpMarker("Comma separate multiple players.");

        ImGui.AlignTextToFramePadding();
        ImGuiHelper.HelpMarker("Right-click table header for column options.", false, true);
        ImGui.SameLine();
        //ImGuiHelper.CSVButton(ListCSV);
        using(ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Copy.ToIconString()}##--CopyCSV")) {
                _plugin.DataQueue.QueueDataOperation(() => {
                    ListCSV = CSVHeader();
                    foreach(var player in DataModel) {
                        ListCSV += CSVRow(_plugin.CCStatsEngine.PlayerStats, player);
                    }
                    //foreach(var stat in _plugin.CCStatsEngine.PlayerStats) {
                    //    ListCSV += CSVRow(_plugin.CCStatsEngine.PlayerStats, stat.Key);
                    //}
                    Task.Run(() => {
                        ImGui.SetClipboardText(ListCSV);
                    });
                });
            }
        }
        ImGuiHelper.WrappedTooltip("Copy CSV to clipboard");

        ImGui.SameLine();
        ImGui.TextUnformatted($"Total players:   {DataModel.Count}");

    }

    protected override void PostColumnSetup() {
        ImGui.TableSetupScrollFreeze(1, 1);
        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(sortSpecs.SpecsDirty || TriggerSort) {
            TriggerSort = false;
            sortSpecs.SpecsDirty = false;
            _plugin.DataQueue.QueueDataOperation(() => {
                SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
                GoToPage(0);
            });
        }
    }

    public override void DrawListItem(PlayerAlias item) {
        ImGui.TextUnformatted($"{item.Name}");
        if(_plugin.CCStatsEngine.ActiveLinks.ContainsKey(item)) {
            string tooltipText = "Including stats for:\n\n";
            _plugin.CCStatsEngine.ActiveLinks[item].ForEach(x => tooltipText += x + "\n");
            tooltipText = tooltipText.Substring(0, tooltipText.Length - 1);
            ImGuiHelper.WrappedTooltip(tooltipText);
        }
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{item.HomeWorld}");
        ImGui.TableNextColumn();
        var job = _plugin.CCStatsEngine.PlayerStats[item].StatsAll.Job;
        if(job != null) {
            ImGui.TextColored(ImGuiHelper.GetJobColor(job), $"{job}");
        }
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsAll.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsAll.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsAll.Losses}");
        ImGui.TableNextColumn();
        var playerWinDiff = _plugin.CCStatsEngine.PlayerStats[item].StatsAll.WinDiff;
        var playerWinDiffColor = playerWinDiff > 0 ? ImGuiColors.HealerGreen : playerWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(playerWinDiffColor, $"{playerWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(playerWinDiffColor, $"{string.Format("{0:P1}%", _plugin.CCStatsEngine.PlayerStats[item].StatsAll.WinRate)}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsPersonal.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsPersonal.Losses}");
        ImGui.TableNextColumn();
        var selfWinDiff = _plugin.CCStatsEngine.PlayerStats[item].StatsPersonal.WinDiff;
        var selfAllWinDiffColor = selfWinDiff > 0 ? ImGuiColors.HealerGreen : selfWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(selfAllWinDiffColor, $"{selfWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(selfAllWinDiffColor, $"{string.Format("{0:P1}%", _plugin.CCStatsEngine.PlayerStats[item].StatsPersonal.WinRate)}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsTeammate.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsTeammate.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsTeammate.Losses}");
        ImGui.TableNextColumn();
        var teammateWinDiff = _plugin.CCStatsEngine.PlayerStats[item].StatsTeammate.WinDiff;
        var teammateWinDiffColor = teammateWinDiff > 0 ? ImGuiColors.HealerGreen : teammateWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(teammateWinDiffColor, $"{teammateWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(_plugin.CCStatsEngine.PlayerStats[item].StatsTeammate.WinRate, teammateWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsOpponent.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsOpponent.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].StatsOpponent.Losses}");
        ImGui.TableNextColumn();
        var opponentWinDiff = _plugin.CCStatsEngine.PlayerStats[item].StatsOpponent.WinDiff;
        var opponentWinDiffColor = opponentWinDiff > 0 ? ImGuiColors.HealerGreen : opponentWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(opponentWinDiffColor, $"{opponentWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(_plugin.CCStatsEngine.PlayerStats[item].StatsOpponent.WinRate, opponentWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.Kills.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.Deaths.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.Assists.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.DamageDealt.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.DamageTaken.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.HPRestored.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.TimeOnCrystal));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.KillsAndAssists.ToString("N0")}");

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMatch.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 1.0f, 4.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMatch.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 1.5f, 3.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMatch.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 5.0f, 7.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMatch.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 400000f, 850000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMatch.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 400000f, 850000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMatch.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 350000f, 1000000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        var tcpa = _plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMatch.TimeOnCrystal;
        if(_plugin.Configuration.ColorScaleStats) {
            ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 35f, 120f, (float)tcpa.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpa));
        } else {
            ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(tcpa));
        }
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMatch.KillsAndAssists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 6.0f, 10.0f, _plugin.Configuration.ColorScaleStats, "0.00");

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMin.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.7f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMin.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 0.25f, 0.55f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMin.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.75f, 1.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMin.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 75000f, 140000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMin.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 75000f, 140000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMin.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 60000f, 185000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        var tcpm = _plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMin.TimeOnCrystal;
        if(_plugin.Configuration.ColorScaleStats) {
            ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 6f, 20f, (float)tcpm.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpm));
        } else {
            ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(tcpm));
        }
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardPerMin.KillsAndAssists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 1.0f, 2.0f, _plugin.Configuration.ColorScaleStats, "0.00");

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardContrib.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardContrib.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardContrib.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardContrib.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardContrib.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardContrib.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardContrib.TimeOnCrystalDouble, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardContrib.KillsAndAssists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.DamageDealtPerKA, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 52000f, 100000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.DamageDealtPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 190000f, 400000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.DamageTakenPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 190000f, 400000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.HPRestoredPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 120000f, 600000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.PlayerStats[item].ScoreboardTotal.KDA, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 2.25f, 6.25f, _plugin.Configuration.ColorScaleStats, "0.00");
    }

    //we don't need this
    public override void OpenFullEditDetail(PlayerAlias item) {
        return;
    }

    public override void OpenItemDetail(PlayerAlias item) {
        return;
    }

    public override async Task RefreshDataModel() {
        DataModelUntruncated = DataModel;
        ApplyQuickFilters(MinMatches, PlayerQuickSearch);
        TriggerSort = true;
        await Task.CompletedTask;
    }

    private void ApplyQuickFilters(uint minMatches, string searchText) {
        List<PlayerAlias> DataModelTruncated = new();
        var playerNames = searchText.Trim().Split(",").ToList();
        foreach(var player in DataModelUntruncated) {
            bool minMatchPass = _plugin.CCStatsEngine.PlayerStats[player].StatsAll.Matches >= minMatches;
            bool namePass = searchText.IsNullOrEmpty()
                || playerNames.Any(x => x.Length > 0 && player.FullName.Contains(x.Trim(), StringComparison.OrdinalIgnoreCase))
                || playerNames.Any(x => x.Length > 0 && _plugin.CCStatsEngine.ActiveLinks.Where(y => y.Key.Equals(player)).Any(y => y.Value.Any(z => z.FullName.Contains(x.Trim(), StringComparison.OrdinalIgnoreCase))))
                ;
            if(minMatchPass && namePass) {
                DataModelTruncated.Add(player);
            }
        }
        DataModel = DataModelTruncated;
        GoToPage(0);
    }

    private string CSVRow(Dictionary<PlayerAlias, CCPlayerJobStats> model, PlayerAlias key) {
        string csv = "";
        foreach(var col in Columns) {
            if(col.Id == 0) {
                csv += key.Name;
            } else if(col.Id == 1) {
                csv += key.HomeWorld;
            } else {
                //find property
                (var p1, var p2) = GetStatsPropertyFromId(col.Id);
                if(p1 != null && p2 != null) {
                    csv += p2.GetValue(p1.GetValue(model[key])) ?? 0;
                }
            }
            csv += ",";
        }
        csv += "\n";
        return csv;
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        //_plugin.Log.Debug($"Sorting by {columnId}");
        Func<PlayerAlias, object> comparator = (r) => 0;

        //0 = name
        //1 = homeworld
        if(columnId == 0) {
            comparator = (r) => r.Name;
        } else if(columnId == 1) {
            comparator = (r) => r.HomeWorld;
        } else {
            (var p1, var p2) = GetStatsPropertyFromId(columnId);
            if(p1 != null && p2 != null) {
                comparator = (r) => p2.GetValue(p1.GetValue(_plugin.CCStatsEngine.PlayerStats[r])) ?? 0;
            }
        }
        DataModel = direction == ImGuiSortDirection.Ascending ? DataModel.OrderBy(comparator).ToList() : DataModel.OrderByDescending(comparator).ToList();
        DataModelUntruncated = direction == ImGuiSortDirection.Ascending ? DataModelUntruncated.OrderBy(comparator).ToList() : DataModelUntruncated.OrderByDescending(comparator).ToList();
    }
}
