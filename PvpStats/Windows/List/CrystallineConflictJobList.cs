using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class CrystallineConflictJobList : CCStatsList<Job> {

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Job", Id = 0, Width = 85f, Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{Name = "Role", Id = 1, Width = 50f, Flags = ImGuiTableColumnFlags.WidthFixed },
        new ColumnParams{Name = "Total Instances", Id = (uint)"StatsAll.Matches".GetHashCode() },
        new ColumnParams{Name = "Job Wins", Id = (uint)"StatsAll.Wins".GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Job Losses", Id = (uint)"StatsAll.Losses".GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Job Win Diff.", Id = (uint)"StatsAll.WinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Job Win Rate", Id = (uint)"StatsAll.WinRate".GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Your Matches", Id = (uint)"StatsPersonal.Matches".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
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

    protected override string TableId => "###CCJobStatsTable";

    internal StatSourceFilter StatSourceFilter { get; private set; }

    public CrystallineConflictJobList(Plugin plugin) : base(plugin) {
        //ListModel = listModel;
        StatSourceFilter = new(plugin, RefreshMainWindow, plugin.Configuration.MatchWindowFilters.StatSourceFilter);
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
        ImGui.TextUnformatted($"{PlayerJobHelper.GetNameFromJob(item)}");
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiHelper.GetJobColor(item), $"{PlayerJobHelper.GetSubRoleFromJob(item)}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsAll.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsAll.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsAll.Losses}");
        ImGui.TableNextColumn();
        var jobWinDiff = _plugin.CCStatsEngine.JobStats[item].StatsAll.WinDiff;
        var jobWinDiffColor = jobWinDiff > 0 ? ImGuiColors.HealerGreen : jobWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(jobWinDiffColor, $"{jobWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(_plugin.CCStatsEngine.JobStats[item].StatsAll.WinRate, jobWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsPersonal.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsPersonal.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsPersonal.Losses}");
        ImGui.TableNextColumn();
        var personalWinDiff = _plugin.CCStatsEngine.JobStats[item].StatsPersonal.WinDiff;
        var personalWinDiffColor = personalWinDiff > 0 ? ImGuiColors.HealerGreen : personalWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(personalWinDiffColor, $"{personalWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(_plugin.CCStatsEngine.JobStats[item].StatsPersonal.WinRate, personalWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsTeammate.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsTeammate.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsTeammate.Losses}");
        ImGui.TableNextColumn();
        var teammateWinDiff = _plugin.CCStatsEngine.JobStats[item].StatsTeammate.WinDiff;
        var teammateWinDiffColor = teammateWinDiff > 0 ? ImGuiColors.HealerGreen : teammateWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(teammateWinDiffColor, $"{teammateWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(_plugin.CCStatsEngine.JobStats[item].StatsTeammate.WinRate, teammateWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsOpponent.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsOpponent.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].StatsOpponent.Losses}");
        ImGui.TableNextColumn();
        var opponentWinDiff = _plugin.CCStatsEngine.JobStats[item].StatsOpponent.WinDiff;
        var opponentWinDiffColor = opponentWinDiff > 0 ? ImGuiColors.HealerGreen : opponentWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(opponentWinDiffColor, $"{opponentWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(_plugin.CCStatsEngine.JobStats[item].StatsOpponent.WinRate, opponentWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.Kills.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.Deaths.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.Assists.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageDealt.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageTaken.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.HPRestored.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.TimeOnCrystal));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.KillsAndAssists.ToString("N0")}");

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 1.0f, 4.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 1.5f, 3.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 5.0f, 7.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 400000f, 850000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 400000f, 850000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 350000f, 1000000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        var tcpa = _plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.TimeOnCrystal;
        if(_plugin.Configuration.ColorScaleStats) {
            ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 35f, 120f, (float)tcpa.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpa));
        } else {
            ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(tcpa));
        }
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMatch.KillsAndAssists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 6.0f, 10.0f, _plugin.Configuration.ColorScaleStats, "0.00");

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.7f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 0.25f, 0.55f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.75f, 1.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 75000f, 140000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 75000f, 140000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 60000f, 185000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        var tcpm = _plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.TimeOnCrystal;
        if(_plugin.Configuration.ColorScaleStats) {
            ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 6f, 20f, (float)tcpm.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpm));
        } else {
            ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(tcpm));
        }
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardPerMin.KillsAndAssists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 1.0f, 2.0f, _plugin.Configuration.ColorScaleStats, "0.00");

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.TimeOnCrystalDouble, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardContrib.KillsAndAssists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageDealtPerKA, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 52000f, 100000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageDealtPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 190000f, 400000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.DamageTakenPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 190000f, 400000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.HPRestoredPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 120000f, 600000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)_plugin.CCStatsEngine.JobStats[item].ScoreboardTotal.KDA, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 2.25f, 6.25f, _plugin.Configuration.ColorScaleStats, "0.00");
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

    private async Task RefreshMainWindow() {
        await _plugin.WindowManager.Refresh();
    }

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
