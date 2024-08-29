using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Managers.Stats;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using PvpStats.Windows.Tracker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class FrontlineJobList : FLStatsList<Job> {

    protected override string TableId => "###FLJobStatsTable";

    internal FLStatSourceFilter StatSourceFilter { get; private set; }

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{           Name = "Job",                                                                       Id = 0,                                                             Width = 85f,                                    Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{           Name = "Role",                                                                      Id = 1,                                                             Width = 50f,                                    Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Instances",                                                           Id = (uint)"StatsAll.Matches".GetHashCode(),                        Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "1st Places",                                                                Id = (uint)"StatsAll.FirstPlaces".GetHashCode(),                    Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "2nd Places",                                                                Id = (uint)"StatsAll.SecondPlaces".GetHashCode(),                   Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "3rd Places",                                                                Id = (uint)"StatsAll.ThirdPlaces".GetHashCode(),                    Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Win Rate",                                                                  Id = (uint)"StatsAll.WinRate".GetHashCode(),                        Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Average Place",                                                             Id = (uint)"StatsAll.AveragePlace".GetHashCode(),                   Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Your Matches",                                                              Id = (uint)"StatsPersonal.Matches".GetHashCode(),                   Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Your Wins",                                                                 Id = (uint)"StatsPersonal.Wins".GetHashCode(),                      Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Your Losses",                                                               Id = (uint)"StatsPersonal.Losses".GetHashCode(),                    Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Your Win Diff.",                                                            Id = (uint)"StatsPersonal.WinDiff".GetHashCode(),                   Width = 63f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Your Win Rate",                                                             Id = (uint)"StatsPersonal.WinRate".GetHashCode(),                   Width = 63f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Teammate Matches",                                                          Id = (uint)"StatsTeammate.Matches".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Teammate Wins",                                                             Id = (uint)"StatsTeammate.Wins".GetHashCode(),                      Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Teammate Losses",                                                           Id = (uint)"StatsTeammate.Losses".GetHashCode(),                    Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Teammate Win Diff.",                                                        Id = (uint)"StatsTeammate.WinDiff".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Teammate Win Rate",                                                         Id = (uint)"StatsTeammate.WinRate".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Opponent Matches",                                                          Id = (uint)"StatsOpponent.Matches".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Opponent Wins",                                                             Id = (uint)"StatsOpponent.Wins".GetHashCode(),                      Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Opponent Losses",                                                           Id = (uint)"StatsOpponent.Losses".GetHashCode(),                    Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Opponent Win Diff.",                                                        Id = (uint)"StatsOpponent.WinDiff".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        //new NumericColumnParams{    Name = "Opponent Win Rate",                                                         Id = (uint)"StatsOpponent.WinRate".GetHashCode(),                   Width = 70f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills",                                                               Id = (uint)"ScoreboardTotal.Kills".GetHashCode(),                   Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Deaths",                                                              Id = (uint)"ScoreboardTotal.Deaths".GetHashCode(),                  Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Assists",                                                             Id = (uint)"ScoreboardTotal.Assists".GetHashCode(),                 Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Dealt",                Header = "Total Damage\nDealt",         Id = (uint)"ScoreboardTotal.DamageDealt".GetHashCode(),             Width = 90f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Taken",                Header = "Total Damage\nTaken",         Id = (uint)"ScoreboardTotal.DamageTaken".GetHashCode(),             Width = 90f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total HP Restored",                                                         Id = (uint)"ScoreboardTotal.HPRestored".GetHashCode(),              Width = 90f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills/Assists",               Header = "Total Kills\n and Assists",   Id = (uint)"ScoreboardTotal.KillsAndAssists".GetHashCode(),         Width = 75f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills Per Match",                                                           Id = (uint)"ScoreboardPerMatch.Kills".GetHashCode(),                Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Match",                                                          Id = (uint)"ScoreboardPerMatch.Deaths".GetHashCode(),               Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Match",                                                         Id = (uint)"ScoreboardPerMatch.Assists".GetHashCode(),              Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Match",                                                    Id = (uint)"ScoreboardPerMatch.DamageDealt".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Match",                                                    Id = (uint)"ScoreboardPerMatch.DamageTaken".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Match",                                                     Id = (uint)"ScoreboardPerMatch.HPRestored".GetHashCode(),           Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills/Assists Per Match",                                                   Id = (uint)"ScoreboardPerMatch.KillsAndAssists".GetHashCode(),      Width = 85f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills Per Min",                     Header = "Kills\nPer Min",              Id = (uint)"ScoreboardPerMin.Kills".GetHashCode(),                  Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Min",                                                            Id = (uint)"ScoreboardPerMin.Deaths".GetHashCode(),                 Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Min",                                                           Id = (uint)"ScoreboardPerMin.Assists".GetHashCode(),                Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageDealt".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageTaken".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Min",                                                       Id = (uint)"ScoreboardPerMin.HPRestored".GetHashCode(),             Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills/Assists Per Min",                                                     Id = (uint)"ScoreboardPerMin.KillsAndAssists".GetHashCode(),        Width = 85f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill Contrib.",                                                      Id = (uint)"ScoreboardContrib.Kills".GetHashCode(),                 Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Death Contrib.",                                                     Id = (uint)"ScoreboardContrib.Deaths".GetHashCode(),                Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Assist Contrib.",                                                    Id = (uint)"ScoreboardContrib.Assists".GetHashCode(),               Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage Dealt Contrib.",                                              Id = (uint)"ScoreboardContrib.DamageDealt".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage Taken Contrib.",                                              Id = (uint)"ScoreboardContrib.DamageTaken".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median HP Restored Contrib.",                                               Id = (uint)"ScoreboardContrib.HPRestored".GetHashCode(),            Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill/Assist Contrib.",   Header = "Median Kill and\nAssist Contrib", Id = (uint)"ScoreboardContrib.KillsAndAssists".GetHashCode(),       Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Kill/Assist",  Header = "Damage Dealt\nPer Kill/Assist",   Id = (uint)"ScoreboardTotal.DamageDealtPerKA".GetHashCode(),        Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageDealtPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageTakenPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Life",                                                      Id = (uint)"ScoreboardTotal.HPRestoredPerLife".GetHashCode(),       Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Battle High Per Life",                                                      Id = (uint)"BattleHighPerLife".GetHashCode(),                       Width = 75f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "KDA Ratio",                                                                 Id = (uint)"ScoreboardTotal.KDA".GetHashCode(),                     Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
    };

    public FrontlineJobList(Plugin plugin, FLTrackerWindow window) : base(plugin, window) {
        //ListModel = listModel;
        StatSourceFilter = new FLStatSourceFilter(plugin, window.Refresh);
        //window.JobStatFilters.Add(StatSourceFilter);
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
        CSVButton();
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
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.FirstPlaces.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.SecondPlaces.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.ThirdPlaces.ToString(), Offset);
        }
        var jobWinDiffColor = _plugin.Configuration.GetFrontlineWinRateColor(StatsModel[item].StatsAll);
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.WinRate.ToString("P1"), Offset, jobWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].StatsAll.AveragePlace, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 1.5f, 2.5f, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //total
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.Kills.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.Deaths.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.Assists.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageDealt.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageTaken.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.HPRestored.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.KillsAndAssists.ToString("N0"), Offset);
        }

        //per match
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KillsPerMatchRange[0], FrontlineStatsManager.KillsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, FrontlineStatsManager.DeathsPerMatchRange[0], FrontlineStatsManager.DeathsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.AssistsPerMatchRange[0], FrontlineStatsManager.AssistsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageDealtPerMatchRange[0], FrontlineStatsManager.DamageDealtPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageTakenPerMatchRange[0], FrontlineStatsManager.DamageTakenPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.HPRestoredPerMatchRange[0], FrontlineStatsManager.HPRestoredPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KillsPerMatchRange[0] + FrontlineStatsManager.AssistsPerMatchRange[0], FrontlineStatsManager.KillsPerMatchRange[1] + FrontlineStatsManager.AssistsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //per min
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KillsPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.KillsPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, FrontlineStatsManager.DeathsPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.DeathsPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.AssistsPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.AssistsPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageDealtPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.DamageDealtPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageTakenPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.DamageTakenPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.HPRestoredPerMatchRange[0] / FrontlineStatsManager.AverageMatchLength, FrontlineStatsManager.HPRestoredPerMatchRange[1] / FrontlineStatsManager.AverageMatchLength, _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, (FrontlineStatsManager.KillsPerMatchRange[0] + FrontlineStatsManager.AssistsPerMatchRange[0]) / FrontlineStatsManager.AverageMatchLength, (FrontlineStatsManager.KillsPerMatchRange[1] + FrontlineStatsManager.AssistsPerMatchRange[1]) / FrontlineStatsManager.AverageMatchLength, _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //team contrib
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }

        //special
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageDealtPerKA, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, FrontlineStatsManager.DamagePerKARange[0], FrontlineStatsManager.DamagePerKARange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageDealtPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamagePerLifeRange[0], FrontlineStatsManager.DamagePerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageTakenPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageTakenPerLifeRange[0], FrontlineStatsManager.DamageTakenPerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.HPRestoredPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.HPRestoredPerLifeRange[0], FrontlineStatsManager.HPRestoredPerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].BattleHighPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.BattleHighPerLifeRange[0], FrontlineStatsManager.BattleHighPerLifeRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardTotal.KDA, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KDARange[0], FrontlineStatsManager.KDARange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
    }

    public override async Task RefreshDataModel() {
        StatsModel = _plugin.FLStatsEngine.JobStats;
        await base.RefreshDataModel();
    }

    //private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
    //    //_plugin.Log.Debug($"Sorting by {columnId}");
    //    Func<Job, object> comparator = (r) => 0;

    //    //0 = job
    //    //1 = role
    //    if(columnId == 0) {
    //        comparator = (r) => r;
    //    } else if(columnId == 1) {
    //        comparator = (r) => PlayerJobHelper.GetSubRoleFromJob(r) ?? 0;
    //    } else {
    //        (var p1, var p2) = GetStatsPropertyFromId(columnId);

    //        if(p1 != null && p2 == null) {
    //            comparator = (r) => p1.GetValue(StatsModel[r]) ?? 0;
    //        } else if(p1 != null && p2 != null) {
    //            comparator = (r) => p2.GetValue(p1.GetValue(StatsModel[r])) ?? 0;
    //        }
    //    }
    //    DataModel = direction == ImGuiSortDirection.Ascending ? DataModel.OrderBy(comparator).ToList() : DataModel.OrderByDescending(comparator).ToList();
    //}

    //private string CSVRow(Dictionary<Job, FLPlayerJobStats> model, Job key) {
    //    string csv = "";
    //    foreach(var col in Columns) {
    //        if(col.Id == 0) {
    //            csv += PlayerJobHelper.GetNameFromJob(key);
    //        } else if(col.Id == 1) {
    //            csv += PlayerJobHelper.GetSubRoleFromJob(key);
    //        } else {
    //            //find property
    //            (var p1, var p2) = GetStatsPropertyFromId(col.Id);
    //            if(p1 != null && p2 == null) {
    //                csv += p1.GetValue(model[key]) ?? 0;
    //            } else if(p1 != null && p2 != null) {
    //                csv += p2.GetValue(p1.GetValue(model[key])) ?? 0;
    //            }
    //        }
    //        csv += ",";
    //    }
    //    csv += "\n";
    //    return csv;
    //}
}
