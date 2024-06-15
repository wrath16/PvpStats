using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using PvpStats.Windows.Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class CrystallineConflictJobList : CCStatsList<Job> {

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{           Name = "Job",                                                                       Id = 0,                                                             Width = 85f,                                    Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{           Name = "Role",                                                                      Id = 1,                                                             Width = 50f,                                    Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Instances",                                                           Id = (uint)"StatsAll.Matches".GetHashCode(),                        Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Job Wins",                                                                  Id = (uint)"StatsAll.Wins".GetHashCode(),                           Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Job Losses",                                                                Id = (uint)"StatsAll.Losses".GetHashCode(),                         Width = 50f + Offset,   Alignment = 2,          Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Job Win Diff.",                                                             Id = (uint)"StatsAll.WinDiff".GetHashCode(),                        Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Job Win Rate",                                                              Id = (uint)"StatsAll.WinRate".GetHashCode(),                        Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Your Matches",                                                              Id = (uint)"StatsPersonal.Matches".GetHashCode(),                   Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Your Wins",                                                                 Id = (uint)"StatsPersonal.Wins".GetHashCode(),                      Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Your Losses",                                                               Id = (uint)"StatsPersonal.Losses".GetHashCode(),                    Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Your Win Diff.",                                                            Id = (uint)"StatsPersonal.WinDiff".GetHashCode(),                   Width = 63f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Your Win Rate",                                                             Id = (uint)"StatsPersonal.WinRate".GetHashCode(),                   Width = 63f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Teammate Matches",                                                          Id = (uint)"StatsTeammate.Matches".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Teammate Wins",                                                             Id = (uint)"StatsTeammate.Wins".GetHashCode(),                      Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Teammate Losses",                                                           Id = (uint)"StatsTeammate.Losses".GetHashCode(),                    Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Teammate Win Diff.",                                                        Id = (uint)"StatsTeammate.WinDiff".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Teammate Win Rate",                                                         Id = (uint)"StatsTeammate.WinRate".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Opponent Matches",                                                          Id = (uint)"StatsOpponent.Matches".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Opponent Wins",                                                             Id = (uint)"StatsOpponent.Wins".GetHashCode(),                      Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Opponent Losses",                                                           Id = (uint)"StatsOpponent.Losses".GetHashCode(),                    Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Opponent Win Diff.",                                                        Id = (uint)"StatsOpponent.WinDiff".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Opponent Win Rate",                                                         Id = (uint)"StatsOpponent.WinRate".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills",                                                               Id = (uint)"ScoreboardTotal.Kills".GetHashCode(),                   Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Deaths",                                                              Id = (uint)"ScoreboardTotal.Deaths".GetHashCode(),                  Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Assists",                                                             Id = (uint)"ScoreboardTotal.Assists".GetHashCode(),                 Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Dealt",                Header = "Total Damage\nDealt",         Id = (uint)"ScoreboardTotal.DamageDealt".GetHashCode(),             Width = 90f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Taken",                Header = "Total Damage\nTaken",         Id = (uint)"ScoreboardTotal.DamageTaken".GetHashCode(),             Width = 90f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total HP Restored",                                                         Id = (uint)"ScoreboardTotal.HPRestored".GetHashCode(),              Width = 90f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Time on Crystal",             Header = "Total Time\non Crystal",      Id = (uint)"ScoreboardTotal.TimeOnCrystal".GetHashCode(),           Width = 75f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills/Assists",               Header = "Total Kills\n and Assists",   Id = (uint)"ScoreboardTotal.KillsAndAssists".GetHashCode(),         Width = 75f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills Per Match",                                                           Id = (uint)"ScoreboardPerMatch.Kills".GetHashCode(),                Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Match",                                                          Id = (uint)"ScoreboardPerMatch.Deaths".GetHashCode(),               Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Match",                                                         Id = (uint)"ScoreboardPerMatch.Assists".GetHashCode(),              Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Match",                                                    Id = (uint)"ScoreboardPerMatch.DamageDealt".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Match",                                                    Id = (uint)"ScoreboardPerMatch.DamageTaken".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Match",                                                     Id = (uint)"ScoreboardPerMatch.HPRestored".GetHashCode(),           Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Time on Crystal Per Match",                                                 Id = (uint)"ScoreboardPerMatch.TimeOnCrystal".GetHashCode(),        Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills/Assists Per Match",                                                   Id = (uint)"ScoreboardPerMatch.KillsAndAssists".GetHashCode(),      Width = 85f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills Per Min",                     Header = "Kills\nPer Min",              Id = (uint)"ScoreboardPerMin.Kills".GetHashCode(),                  Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Min",                                                            Id = (uint)"ScoreboardPerMin.Deaths".GetHashCode(),                 Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Min",                                                           Id = (uint)"ScoreboardPerMin.Assists".GetHashCode(),                Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageDealt".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageTaken".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Min",                                                       Id = (uint)"ScoreboardPerMin.HPRestored".GetHashCode(),             Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Time on Crystal Per Min",                                                   Id = (uint)"ScoreboardPerMin.TimeOnCrystal".GetHashCode(),          Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills/Assists Per Min",                                                     Id = (uint)"ScoreboardPerMin.KillsAndAssists".GetHashCode(),        Width = 85f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill Contrib.",                                                      Id = (uint)"ScoreboardContrib.Kills".GetHashCode(),                 Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Death Contrib.",                                                     Id = (uint)"ScoreboardContrib.Deaths".GetHashCode(),                Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Assist Contrib.",                                                    Id = (uint)"ScoreboardContrib.Assists".GetHashCode(),               Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage Dealt Contrib.",                                              Id = (uint)"ScoreboardContrib.DamageDealt".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage Taken Contrib.",                                              Id = (uint)"ScoreboardContrib.DamageTaken".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median HP Restored Contrib.",                                               Id = (uint)"ScoreboardContrib.HPRestored".GetHashCode(),            Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Time on Crystal Contrib.",                                           Id = (uint)"ScoreboardContrib.TimeOnCrystalDouble".GetHashCode(),   Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill/Assist Contrib.",   Header = "Median Kill and\nAssist Contrib", Id = (uint)"ScoreboardContrib.KillsAndAssists".GetHashCode(),       Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Kill/Assist",  Header = "Damage Dealt\nPer Kill/Assist",   Id = (uint)"ScoreboardTotal.DamageDealtPerKA".GetHashCode(),        Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageDealtPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageTakenPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Life",                                                      Id = (uint)"ScoreboardTotal.HPRestoredPerLife".GetHashCode(),       Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "KDA Ratio",                                                                 Id = (uint)"ScoreboardTotal.KDA".GetHashCode(),                     Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
    };

    protected override string TableId => "###CCJobStatsTable";

    internal StatSourceFilter StatSourceFilter { get; private set; }

    public CrystallineConflictJobList(Plugin plugin, CCTrackerWindow window) : base(plugin, window) {
        //ListModel = listModel;
        StatSourceFilter = new StatSourceFilter(plugin, window.Refresh, plugin.Configuration.CCWindowConfig.JobStatFilters.StatSourceFilter);
        window.JobStatFilters.Add(StatSourceFilter);
        //OtherPlayerFilter = playerFilter;
    }

    protected override void PreTableDraw() {
        using(var filterTable = ImRaii.Table("jobListFilterTable", 2)) {
            if(filterTable) {
                ImGui.TableSetupColumn("filterName", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 110f);
                ImGui.TableSetupColumn($"filters", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Include stats from:");
                ImGui.TableNextColumn();
                StatSourceFilter.Draw();
            }
        }
        ImGui.AlignTextToFramePadding();
        ImGuiHelper.HelpMarker("Right-click table header for column options.", false, true);
        ImGui.SameLine();
        using(ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Copy.ToIconString()}##--CopyCSV")) {
                _plugin.DataQueue.QueueDataOperation(() => {
                    ListCSV = CSVHeader();
                    foreach(var stat in _plugin.CCStatsEngine.JobStats) {
                        ListCSV += CSVRow(_plugin.CCStatsEngine.JobStats, stat.Key);
                    }
                    Task.Run(() => {
                        ImGui.SetClipboardText(ListCSV);
                    });
                });
            }
        }
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

    public override void DrawListItem(Job item) {
        ImGui.SameLine(2f * ImGuiHelpers.GlobalScale);
        ImGui.TextUnformatted($"{PlayerJobHelper.GetNameFromJob(item)}");
        if(ImGui.TableNextColumn()) {
            ImGui.TextColored(_plugin.Configuration.GetJobColor(item), $"{PlayerJobHelper.GetSubRoleFromJob(item)}");
        }

        //job
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsAll.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsAll.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsAll.Losses.ToString(), Offset);
        }
        var jobWinDiff = _plugin.CCStatsEngine.JobStats[item].StatsAll.WinDiff;
        var jobWinDiffColor = jobWinDiff > 0 ? _plugin.Configuration.Colors.Win : jobWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(jobWinDiff.ToString(), Offset, jobWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsAll.WinRate.ToString("P1"), Offset, jobWinDiffColor);
        }

        //personal
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsPersonal.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsPersonal.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsPersonal.Losses.ToString(), Offset);
        }
        var personalWinDiff = _plugin.CCStatsEngine.JobStats[item].StatsPersonal.WinDiff;
        var personalWinDiffColor = personalWinDiff > 0 ? _plugin.Configuration.Colors.Win : personalWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(personalWinDiff.ToString(), Offset, personalWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsPersonal.WinRate.ToString("P1"), Offset, personalWinDiffColor);
        }

        //teammate
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsTeammate.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsTeammate.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsTeammate.Losses.ToString(), Offset);
        }
        var teammateWinDiff = _plugin.CCStatsEngine.JobStats[item].StatsTeammate.WinDiff;
        var teammateWinDiffColor = teammateWinDiff > 0 ? _plugin.Configuration.Colors.Win : teammateWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(teammateWinDiff.ToString(), Offset, teammateWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsTeammate.WinRate.ToString("P1"), Offset, teammateWinDiffColor);
        }

        //opponent
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsOpponent.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsOpponent.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsOpponent.Losses.ToString(), Offset);
        }
        var opponentWinDiff = _plugin.CCStatsEngine.JobStats[item].StatsOpponent.WinDiff;
        var opponentWinDiffColor = opponentWinDiff > 0 ? _plugin.Configuration.Colors.Win : opponentWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(opponentWinDiff.ToString(), Offset, opponentWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].StatsOpponent.WinRate.ToString("P1"), Offset, opponentWinDiffColor);
        }

        //total
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.Kills.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.Deaths.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.Assists.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageDealt.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageTaken.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.HPRestored.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.TimeOnCrystal), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.KillsAndAssists.ToString("N0"), Offset);
        }

        //per match
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 1.0f, 4.5f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 1.5f, 3.5f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 5.0f, 7.5f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 400000f, 850000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 400000f, 850000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 350000f, 1000000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            var tcpa = _plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.TimeOnCrystal;
            ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(tcpa), (float)tcpa.TotalSeconds, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 35f, 120f, _plugin.Configuration.ColorScaleStats, Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 6.0f, 10.0f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //per min
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.1f, 0.7f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 0.25f, 0.55f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.75f, 1.5f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 75000f, 140000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 75000f, 140000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 60000f, 185000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            var tcpm = _plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.TimeOnCrystal;
            ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(tcpm), (float)tcpm.TotalSeconds, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 6f, 20f, _plugin.Configuration.ColorScaleStats, Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 1.0f, 2.0f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //team contrib
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.TimeOnCrystalDouble, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }

        //special
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageDealtPerKA, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 52000f, 100000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageDealtPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 190000f, 400000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageTakenPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 190000f, 400000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.HPRestoredPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 120000f, 600000f, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.KDA, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, 2.25f, 6.25f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
    }

    public override void OpenFullEditDetail(Job item) {
        throw new NotImplementedException();
    }

    public override void OpenItemDetail(Job item) {
    }

    public override async Task RefreshDataModel() {
        TriggerSort = true;
        await Task.CompletedTask;
    }

    //private async Task RefreshMainWindow() {
    //    await _plugin.WindowManager.Refresh();
    //}

    private string CSVRow(Dictionary<Job, CCPlayerJobStats> model, Job key) {
        string csv = "";
        foreach(var col in Columns) {
            if(col.Id == 0) {
                csv += PlayerJobHelper.GetNameFromJob(key);
            } else if(col.Id == 1) {
                csv += PlayerJobHelper.GetSubRoleFromJob(key);
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
        Func<Job, object> comparator = (r) => 0;

        //0 = job
        //1 = role
        if(columnId == 0) {
            comparator = (r) => r;
        } else if(columnId == 1) {
            comparator = (r) => PlayerJobHelper.GetSubRoleFromJob(r) ?? 0;
        } else {
            (var p1, var p2) = GetStatsPropertyFromId(columnId);
            if(p1 != null && p2 != null) {
                comparator = (r) => p2.GetValue(p1.GetValue(_plugin.CCStatsEngine.JobStats[r])) ?? 0;
            }
        }
        DataModel = direction == ImGuiSortDirection.Ascending ? DataModel.OrderBy(comparator).ToList() : DataModel.OrderByDescending(comparator).ToList();
    }
}
