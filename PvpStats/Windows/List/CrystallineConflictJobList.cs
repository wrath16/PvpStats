﻿using Dalamud.Interface.Colors;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Windows.List;
internal class CrystallineConflictJobList : FilteredList<Job> {

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
        new ColumnParams{Name = "Kills Per Match", Id = (uint)"ScoreboardPerMatch.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Deaths Per Match", Id = (uint)"ScoreboardPerMatch.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Assists Per Match", Id = (uint)"ScoreboardPerMatch.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Match", Id = (uint)"ScoreboardPerMatch.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Taken Per Match", Id = (uint)"ScoreboardPerMatch.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "HP Restored Per Match", Id = (uint)"ScoreboardPerMatch.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Time on Crystal Per Match", Id = (uint)"ScoreboardPerMatch.TimeOnCrystal".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Kills Per Min", Id = (uint)"ScoreboardPerMin.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Deaths Per Min", Id = (uint)"ScoreboardPerMin.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Assists Per Min", Id = (uint)"ScoreboardPerMin.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Min", Id = (uint)"ScoreboardPerMin.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Taken Per Min", Id = (uint)"ScoreboardPerMin.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "HP Restored Per Min", Id = (uint)"ScoreboardPerMin.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Time on Crystal Per Min", Id = (uint)"ScoreboardPerMin.TimeOnCrystal".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Kill Contrib.", Id = (uint)"ScoreboardContrib.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Death Contrib.", Id = (uint)"ScoreboardContrib.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Assist Contrib.", Id = (uint)"ScoreboardContrib.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Damage Dealt Contrib.", Id = (uint)"ScoreboardContrib.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Damage Taken Contrib.", Id = (uint)"ScoreboardContrib.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median HP Restored Contrib.", Id = (uint)"ScoreboardContrib.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Time on Crystal Contrib.", Id = (uint)"ScoreboardContrib.TimeOnCrystalDouble".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Kill/Assist", Id = (uint)"ScoreboardTotal.DamageDealtPerKA".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Life", Id = (uint)"ScoreboardTotal.DamageDealtPerLife".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Taken Per Life", Id = (uint)"ScoreboardTotal.DamageTakenPerLife".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "HP Restored Per Life", Id = (uint)"ScoreboardTotal.HPRestoredPerLife".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
    };

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable
        | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;
    protected override bool ShowHeader { get; set; } = true;
    protected override bool ChildWindow { get; set; } = false;
    protected override string TableId => "###CCJobStatsTable";

    public Dictionary<Job, CCPlayerJobStats> StatsModel { get; private set; } = new();
    private CrystallineConflictList ListModel { get; init; }
    private bool _triggerSort = false;

    public CrystallineConflictJobList(Plugin plugin, CrystallineConflictList listModel) : base(plugin) {
        ListModel = listModel;
    }

    protected override void PreTableDraw() {
        //var minColor = ImGuiColors.DalamudWhite;
        //var maxColor = ImGuiColors.DPSRed;

        //var color = ImGuiHelper.ColorScale(minColor, maxColor, 1, 5, 3);
        //ImGui.TextColored(minColor, $"min, {minColor.X} {minColor.Y} {minColor.Z} {minColor.W}");
        //ImGui.TextColored(maxColor, $"max, {maxColor.X} {maxColor.Y} {maxColor.Z} {maxColor.W}");
        //ImGui.TextColored(color, $"test, {color.X} {color.Y} {color.Z} {color.W}");
    }

    protected override void PostColumnSetup() {
        ImGui.TableSetupScrollFreeze(1, 1);
        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(sortSpecs.SpecsDirty || _triggerSort) {
            _triggerSort = false;
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
        ImGui.TextUnformatted($"{StatsModel[item].StatsAll.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsAll.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsAll.Losses}");
        ImGui.TableNextColumn();
        var jobWinDiff = StatsModel[item].StatsAll.WinDiff;
        var jobWinDiffColor = jobWinDiff > 0 ? ImGuiColors.HealerGreen : jobWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(jobWinDiffColor, $"{jobWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(StatsModel[item].StatsAll.WinRate, jobWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsPersonal.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsPersonal.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsPersonal.Losses}");
        ImGui.TableNextColumn();
        var personalWinDiff = StatsModel[item].StatsPersonal.WinDiff;
        var personalWinDiffColor = personalWinDiff > 0 ? ImGuiColors.HealerGreen : personalWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(personalWinDiffColor, $"{personalWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(StatsModel[item].StatsPersonal.WinRate, personalWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsTeammate.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsTeammate.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsTeammate.Losses}");
        ImGui.TableNextColumn();
        var teammateWinDiff = StatsModel[item].StatsTeammate.WinDiff;
        var teammateWinDiffColor = teammateWinDiff > 0 ? ImGuiColors.HealerGreen : teammateWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(teammateWinDiffColor, $"{teammateWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(StatsModel[item].StatsTeammate.WinRate, teammateWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsOpponent.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsOpponent.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsOpponent.Losses}");
        ImGui.TableNextColumn();
        var opponentWinDiff = StatsModel[item].StatsOpponent.WinDiff;
        var opponentWinDiffColor = opponentWinDiff > 0 ? ImGuiColors.HealerGreen : opponentWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(opponentWinDiffColor, $"{opponentWinDiff}");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawPercentage(StatsModel[item].StatsOpponent.WinRate, opponentWinDiffColor);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.Kills.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.Deaths.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.Assists.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.DamageDealt.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.DamageTaken.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.HPRestored.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(StatsModel[item].ScoreboardTotal.TimeOnCrystal));

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMatch.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 1.0f, 4.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMatch.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 1.5f, 3.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMatch.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 5.0f, 8.0f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMatch.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 400000f, 900000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMatch.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 400000f, 900000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMatch.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 300000f, 1000000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        var tcpa = StatsModel[item].ScoreboardPerMatch.TimeOnCrystal;
        if(_plugin.Configuration.ColorScaleStats) {
            ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 30f, 120f, (float)tcpa.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpa));
        } else {
            ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(StatsModel[item].ScoreboardPerMatch.TimeOnCrystal));
        }

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMin.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.7f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMin.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 0.2f, 0.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMin.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.7f, 1.5f, _plugin.Configuration.ColorScaleStats, "0.00");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMin.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 70000f, 150000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMin.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 70000f, 150000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardPerMin.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 50000f, 200000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        var tcpm = StatsModel[item].ScoreboardPerMin.TimeOnCrystal;
        if(_plugin.Configuration.ColorScaleStats) {
            ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0f, 30f, (float)tcpm.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpm));
        } else {
            ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(StatsModel[item].ScoreboardPerMin.TimeOnCrystal));
        }

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardContrib.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardContrib.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardContrib.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardContrib.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardContrib.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardContrib.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale((float)StatsModel[item].ScoreboardContrib.TimeOnCrystalDouble, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);

        //ImGui.TableNextColumn();
        //ImGuiHelper.DrawPercentage(StatsModel[item].ScoreboardContrib.Kills);
        //ImGui.TableNextColumn();
        //ImGuiHelper.DrawPercentage(StatsModel[item].ScoreboardContrib.Deaths);
        //ImGui.TableNextColumn();
        //ImGuiHelper.DrawPercentage(StatsModel[item].ScoreboardContrib.Assists);
        //ImGui.TableNextColumn();
        //ImGuiHelper.DrawPercentage(StatsModel[item].ScoreboardContrib.DamageDealt);
        //ImGui.TableNextColumn();
        //ImGuiHelper.DrawPercentage(StatsModel[item].ScoreboardContrib.DamageTaken);
        //ImGui.TableNextColumn();
        //ImGuiHelper.DrawPercentage(StatsModel[item].ScoreboardContrib.HPRestored);
        //ImGui.TableNextColumn();
        //ImGuiHelper.DrawPercentage(StatsModel[item].ScoreboardContrib.TimeOnCrystalDouble);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.DamageDealtPerKA}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.DamageDealtPerLife}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.DamageTakenPerLife}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].ScoreboardTotal.HPRestoredPerLife}");
    }

    public override void OpenFullEditDetail(Job item) {
        throw new NotImplementedException();
    }

    public override void OpenItemDetail(Job item) {
    }

    public override void RefreshDataModel() {
        Dictionary<Job, CCPlayerJobStats> statsModel = new();
        Dictionary<Job, List<CCScoreboardDouble>> teamContributions = new();
        //Dictionary<Job, (CCPlayerJobStats, List<CCScoreboardDouble>)> data = new();

        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        foreach(var job in allJobs) {
            //DataModel.Add(job);
            statsModel.Add(job, new());
            teamContributions.Add(job, new());

            //data.Add(job, (new(), new()));
        }

        foreach(var match in ListModel.DataModel) {
            foreach(var team in match.Teams) {
                foreach(var player in team.Value.Players) {
                    if(player.Job != null) {
                        var job = (Job)player.Job;

                        statsModel[job].StatsAll.Matches++;
                        if(match.MatchWinner == team.Key) {
                            statsModel[job].StatsAll.Wins++;
                        } else if(match.MatchWinner != null) {
                            statsModel[job].StatsAll.Losses++;
                        }

                        if(player.Alias.Equals(match.LocalPlayer)) {
                            statsModel[job].StatsPersonal.Matches++;
                            if(match.IsWin) {
                                statsModel[job].StatsPersonal.Wins++;
                            } else if(match.MatchWinner != null) {
                                statsModel[job].StatsPersonal.Losses++;
                            }
                        }

                        if(!match.IsSpectated) {
                            if(team.Key == match.LocalPlayerTeam!.TeamName) {
                                statsModel[job].StatsTeammate.Matches++;
                                if(match.IsWin) {
                                    statsModel[job].StatsTeammate.Wins++;
                                } else if(match.MatchWinner != null) {
                                    statsModel[job].StatsTeammate.Losses++;
                                }
                            } else {
                                statsModel[job].StatsOpponent.Matches++;
                                if(match.IsWin) {
                                    statsModel[job].StatsOpponent.Wins++;
                                } else if(match.MatchWinner != null) {
                                    statsModel[job].StatsOpponent.Losses++;
                                }
                            }
                        }

                        if(match.PostMatch != null) {
                            var playerTeamScoreboard = match.PostMatch.Teams.Where(x => x.Key == team.Key).FirstOrDefault().Value;
                            var playerScoreboard = playerTeamScoreboard.PlayerStats.Where(x => x.Player?.Equals(player.Alias) ?? false).FirstOrDefault();
                            if(playerScoreboard != null) {
                                statsModel[job].ScoreboardTotal.MatchTime += match.PostMatch.MatchDuration;
                                statsModel[job].ScoreboardTotal.Kills += (ulong)playerScoreboard.Kills;
                                statsModel[job].ScoreboardTotal.Deaths += (ulong)playerScoreboard.Deaths;
                                statsModel[job].ScoreboardTotal.Assists += (ulong)playerScoreboard.Assists;
                                statsModel[job].ScoreboardTotal.DamageDealt += (ulong)playerScoreboard.DamageDealt;
                                statsModel[job].ScoreboardTotal.DamageTaken += (ulong)playerScoreboard.DamageTaken;
                                statsModel[job].ScoreboardTotal.HPRestored += (ulong)playerScoreboard.HPRestored;
                                statsModel[job].ScoreboardTotal.TimeOnCrystal += playerScoreboard.TimeOnCrystal;

                                teamContributions[job].Add(new() {
                                    Kills = playerTeamScoreboard.TeamStats.Kills != 0 ? (double)playerScoreboard.Kills / playerTeamScoreboard.TeamStats.Kills : 0,
                                    Deaths = playerTeamScoreboard.TeamStats.Deaths != 0 ? (double)playerScoreboard.Deaths / playerTeamScoreboard.TeamStats.Deaths : 0,
                                    Assists = playerTeamScoreboard.TeamStats.Assists != 0 ? (double)playerScoreboard.Assists / playerTeamScoreboard.TeamStats.Assists : 0,
                                    DamageDealt = playerTeamScoreboard.TeamStats.DamageDealt != 0 ? (double)playerScoreboard.DamageDealt / playerTeamScoreboard.TeamStats.DamageDealt : 0,
                                    DamageTaken = playerTeamScoreboard.TeamStats.DamageTaken != 0 ? (double)playerScoreboard.DamageTaken / playerTeamScoreboard.TeamStats.DamageTaken : 0,
                                    HPRestored = playerTeamScoreboard.TeamStats.HPRestored != 0 ? (double)playerScoreboard.HPRestored / playerTeamScoreboard.TeamStats.HPRestored : 0,
                                    TimeOnCrystalDouble = playerTeamScoreboard.TeamStats.TimeOnCrystal.Ticks != 0 ? playerScoreboard.TimeOnCrystal / playerTeamScoreboard.TeamStats.TimeOnCrystal : 0,
                                });
                            }
                        }
                    }
                }
            }
        }
        foreach(var jobStat in statsModel) {
            var job = jobStat.Key;
            var statMatches = teamContributions[job].Count;
            //set average stats
            if(statMatches > 0) {
                jobStat.Value.ScoreboardPerMatch.Kills = (double)jobStat.Value.ScoreboardTotal.Kills / statMatches;
                jobStat.Value.ScoreboardPerMatch.Deaths = (double)jobStat.Value.ScoreboardTotal.Deaths / statMatches;
                jobStat.Value.ScoreboardPerMatch.Assists = (double)jobStat.Value.ScoreboardTotal.Assists / statMatches;
                jobStat.Value.ScoreboardPerMatch.DamageDealt = (double)jobStat.Value.ScoreboardTotal.DamageDealt / statMatches;
                jobStat.Value.ScoreboardPerMatch.DamageTaken = (double)jobStat.Value.ScoreboardTotal.DamageTaken / statMatches;
                jobStat.Value.ScoreboardPerMatch.HPRestored = (double)jobStat.Value.ScoreboardTotal.HPRestored / statMatches;
                jobStat.Value.ScoreboardPerMatch.TimeOnCrystal = jobStat.Value.ScoreboardTotal.TimeOnCrystal / statMatches;

                var matchTime = jobStat.Value.ScoreboardTotal.MatchTime;
                jobStat.Value.ScoreboardPerMin.Kills = jobStat.Value.ScoreboardTotal.Kills / matchTime.TotalMinutes;
                jobStat.Value.ScoreboardPerMin.Deaths = jobStat.Value.ScoreboardTotal.Deaths / matchTime.TotalMinutes;
                jobStat.Value.ScoreboardPerMin.Assists = jobStat.Value.ScoreboardTotal.Assists / matchTime.TotalMinutes;
                jobStat.Value.ScoreboardPerMin.DamageDealt = jobStat.Value.ScoreboardTotal.DamageDealt / matchTime.TotalMinutes;
                jobStat.Value.ScoreboardPerMin.DamageTaken = jobStat.Value.ScoreboardTotal.DamageTaken / matchTime.TotalMinutes;
                jobStat.Value.ScoreboardPerMin.HPRestored = jobStat.Value.ScoreboardTotal.HPRestored / matchTime.TotalMinutes;
                jobStat.Value.ScoreboardPerMin.TimeOnCrystal = jobStat.Value.ScoreboardTotal.TimeOnCrystal / matchTime.TotalMinutes;

                jobStat.Value.ScoreboardContrib.Kills = teamContributions[job].OrderBy(x => x.Kills).ElementAt(statMatches / 2).Kills;
                jobStat.Value.ScoreboardContrib.Deaths = teamContributions[job].OrderBy(x => x.Deaths).ElementAt(statMatches / 2).Deaths;
                jobStat.Value.ScoreboardContrib.Assists = teamContributions[job].OrderBy(x => x.Assists).ElementAt(statMatches / 2).Assists;
                jobStat.Value.ScoreboardContrib.DamageDealt = teamContributions[job].OrderBy(x => x.DamageDealt).ElementAt(statMatches / 2).DamageDealt;
                jobStat.Value.ScoreboardContrib.DamageTaken = teamContributions[job].OrderBy(x => x.DamageTaken).ElementAt(statMatches / 2).DamageTaken;
                jobStat.Value.ScoreboardContrib.HPRestored = teamContributions[job].OrderBy(x => x.HPRestored).ElementAt(statMatches / 2).HPRestored;
                jobStat.Value.ScoreboardContrib.TimeOnCrystalDouble = teamContributions[job].OrderBy(x => x.TimeOnCrystalDouble).ElementAt(statMatches / 2).TimeOnCrystalDouble;
            }
        }
        try {
            RefreshLock.Wait();
            DataModel = statsModel.Keys.ToList();
            StatsModel = statsModel;
            _triggerSort = true;
        } finally {
            RefreshLock.Release();
        }
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
            //iterate to two levels
            var props = typeof(CCPlayerJobStats).GetProperties();
            foreach(var prop in props) {
                var props2 = prop.PropertyType.GetProperties();
                foreach(var prop2 in props2) {
                    var propId = $"{prop.Name}.{prop2.Name}".GetHashCode();
                    if((uint)propId == columnId) {
                        comparator = (r) => prop2.GetValue(prop.GetValue(StatsModel[r])) ?? 0;
                    }
                }
            }
        }
        DataModel = direction == ImGuiSortDirection.Ascending ? DataModel.OrderBy(comparator).ToList() : DataModel.OrderByDescending(comparator).ToList();
    }
}