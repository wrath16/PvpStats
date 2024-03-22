using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Linq;

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

    protected override string TableId => "###CCPlayerStatsTable";

    private List<PlayerAlias> DataModelUntruncated { get; set; } = new();
    private int PlayerCount { get; set; }
    private uint MinMatches { get; set; } = 1;

    public CrystallineConflictPlayerList(Plugin plugin, CrystallineConflictList listModel, OtherPlayerFilter playerFilter) : base(plugin) {
        ListModel = listModel;
        OtherPlayerFilter = playerFilter;
        MinMatches = plugin.Configuration.MatchWindowFilters.MinMatches;
    }

    protected override void PreTableDraw() {
        int minMatches = (int)MinMatches;
        ImGui.SetNextItemWidth(float.Max(ImGui.GetContentRegionAvail().X / 3f, 150f * ImGuiHelpers.GlobalScale));
        if(ImGui.SliderInt("Min. matches", ref minMatches, 1, 100)) {
            MinMatches = (uint)minMatches;
            _plugin.DataQueue.QueueDataOperation(() => {
                _plugin.Configuration.MatchWindowFilters.MinMatches = MinMatches;
                //_plugin.Configuration.Save();
                //RefreshDataModel();
                RemoveByMatchCount(MinMatches);
            });
        }
        ImGui.AlignTextToFramePadding();
        ImGuiHelper.HelpMarker("Right-click table header for column options.", false);
        ImGui.SameLine();
        ImGuiHelper.CSVButton(ListCSV);
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
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{item.HomeWorld}");
        ImGui.TableNextColumn();
        var job = StatsModel[item].StatsAll.Job;
        if(job != null) {
            ImGui.TextColored(ImGuiHelper.GetJobColor(job), $"{job}");
        }
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsAll.Matches}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsAll.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsAll.Losses}");
        ImGui.TableNextColumn();
        var playerWinDiff = StatsModel[item].StatsAll.WinDiff;
        var playerWinDiffColor = playerWinDiff > 0 ? ImGuiColors.HealerGreen : playerWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(playerWinDiffColor, $"{playerWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(playerWinDiffColor, $"{string.Format("{0:P1}%", StatsModel[item].StatsAll.WinRate)}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsPersonal.Wins}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{StatsModel[item].StatsPersonal.Losses}");
        ImGui.TableNextColumn();
        var selfWinDiff = StatsModel[item].StatsPersonal.WinDiff;
        var selfAllWinDiffColor = selfWinDiff > 0 ? ImGuiColors.HealerGreen : selfWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(selfAllWinDiffColor, $"{selfWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(selfAllWinDiffColor, $"{string.Format("{0:P1}%", StatsModel[item].StatsPersonal.WinRate)}");

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
            ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(tcpa));
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
            ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 4f, 25f, (float)tcpm.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpm));
        } else {
            ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(tcpm));
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

        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(StatsModel[item].ScoreboardTotal.DamageDealtPerKA, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 50000f, 100000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(StatsModel[item].ScoreboardTotal.DamageDealtPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 200000f, 400000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(StatsModel[item].ScoreboardTotal.DamageTakenPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 200000f, 400000f, _plugin.Configuration.ColorScaleStats, "#");
        ImGui.TableNextColumn();
        ImGuiHelper.DrawColorScale(StatsModel[item].ScoreboardTotal.HPRestoredPerLife, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 100000f, 500000f, _plugin.Configuration.ColorScaleStats, "#");

    }

    //we don't need this
    public override void OpenFullEditDetail(PlayerAlias item) {
        return;
    }

    public override void OpenItemDetail(PlayerAlias item) {
        return;
    }

    public override void RefreshDataModel() {
        Dictionary<PlayerAlias, CCPlayerJobStats> statsModel = new();
        Dictionary<PlayerAlias, List<CCScoreboardDouble>> teamContributions = new();
        Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> jobStats = new();

        foreach(var match in ListModel.DataModel) {
            foreach(var team in match.Teams) {
                foreach(var player in team.Value.Players) {
                    bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
                    bool isTeammate = !match.IsSpectated && team.Key == match.LocalPlayerTeam!.TeamName;
                    //check against filters
                    bool nameMatch = player.Alias.FullName.Contains(OtherPlayerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                    bool sideMatch = OtherPlayerFilter.TeamStatus == TeamStatus.Any
                        || OtherPlayerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                        || OtherPlayerFilter.TeamStatus == TeamStatus.Opponent && !isTeammate && !isLocalPlayer;
                    bool jobMatch = OtherPlayerFilter.AnyJob || OtherPlayerFilter.PlayerJob == player.Job;
                    if(!nameMatch || !sideMatch || !jobMatch) {
                        continue;
                    }

                    if(!statsModel.ContainsKey(player.Alias)) {
                        statsModel.Add(player.Alias, new());
                        teamContributions.Add(player.Alias, new());
                        jobStats.Add(player.Alias, new());
                    }

                    statsModel[player.Alias].StatsAll.Matches++;
                    if(match.MatchWinner == team.Key) {
                        statsModel[player.Alias].StatsAll.Wins++;
                    } else if(match.MatchWinner != null) {
                        statsModel[player.Alias].StatsAll.Losses++;
                    }

                    if(!match.IsSpectated) {
                        if(isTeammate) {
                            statsModel[player.Alias].StatsTeammate.Matches++;
                            if(match.IsWin) {
                                statsModel[player.Alias].StatsTeammate.Wins++;
                            } else if(match.MatchWinner != null) {
                                statsModel[player.Alias].StatsTeammate.Losses++;
                            }
                        } else {
                            statsModel[player.Alias].StatsOpponent.Matches++;
                            if(match.IsWin) {
                                statsModel[player.Alias].StatsOpponent.Wins++;
                            } else if(match.MatchWinner != null) {
                                statsModel[player.Alias].StatsOpponent.Losses++;
                            }
                        }
                    }

                    if(match.PostMatch != null) {
                        var playerTeamScoreboard = match.PostMatch.Teams.Where(x => x.Key == team.Key).FirstOrDefault().Value;
                        var playerScoreboard = playerTeamScoreboard.PlayerStats.Where(x => x.Player?.Equals(player.Alias) ?? false).FirstOrDefault();
                        if(playerScoreboard != null) {
                            statsModel[player.Alias].ScoreboardTotal.MatchTime += match.PostMatch.MatchDuration;
                            statsModel[player.Alias].ScoreboardTotal.Kills += (ulong)playerScoreboard.Kills;
                            statsModel[player.Alias].ScoreboardTotal.Deaths += (ulong)playerScoreboard.Deaths;
                            statsModel[player.Alias].ScoreboardTotal.Assists += (ulong)playerScoreboard.Assists;
                            statsModel[player.Alias].ScoreboardTotal.DamageDealt += (ulong)playerScoreboard.DamageDealt;
                            statsModel[player.Alias].ScoreboardTotal.DamageTaken += (ulong)playerScoreboard.DamageTaken;
                            statsModel[player.Alias].ScoreboardTotal.HPRestored += (ulong)playerScoreboard.HPRestored;
                            statsModel[player.Alias].ScoreboardTotal.TimeOnCrystal += playerScoreboard.TimeOnCrystal;

                            teamContributions[player.Alias].Add(new() {
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

                    if(player.Job != null) {
                        if(!jobStats[player.Alias].ContainsKey((Job)player.Job)) {
                            jobStats[player.Alias].Add((Job)player.Job, new());
                        }
                        jobStats[player.Alias][(Job)player.Job].Matches++;
                    }
                }
            }
        }

        foreach(var playerStat in statsModel) {
            //set favored job
            playerStat.Value.StatsAll.Job = jobStats[playerStat.Key].OrderByDescending(x => x.Value.Matches).FirstOrDefault().Key;
            var statMatches = teamContributions[playerStat.Key].Count;
            //set average stats
            if(statMatches > 0) {
                playerStat.Value.StatsPersonal.Matches = playerStat.Value.StatsTeammate.Matches + playerStat.Value.StatsOpponent.Matches;
                playerStat.Value.StatsPersonal.Wins = playerStat.Value.StatsTeammate.Wins + playerStat.Value.StatsOpponent.Wins;
                playerStat.Value.StatsPersonal.Losses = playerStat.Value.StatsTeammate.Losses + playerStat.Value.StatsOpponent.Losses;

                playerStat.Value.ScoreboardPerMatch.Kills = (double)playerStat.Value.ScoreboardTotal.Kills / statMatches;
                playerStat.Value.ScoreboardPerMatch.Deaths = (double)playerStat.Value.ScoreboardTotal.Deaths / statMatches;
                playerStat.Value.ScoreboardPerMatch.Assists = (double)playerStat.Value.ScoreboardTotal.Assists / statMatches;
                playerStat.Value.ScoreboardPerMatch.DamageDealt = (double)playerStat.Value.ScoreboardTotal.DamageDealt / statMatches;
                playerStat.Value.ScoreboardPerMatch.DamageTaken = (double)playerStat.Value.ScoreboardTotal.DamageTaken / statMatches;
                playerStat.Value.ScoreboardPerMatch.HPRestored = (double)playerStat.Value.ScoreboardTotal.HPRestored / statMatches;
                playerStat.Value.ScoreboardPerMatch.TimeOnCrystal = playerStat.Value.ScoreboardTotal.TimeOnCrystal / statMatches;

                var matchTime = playerStat.Value.ScoreboardTotal.MatchTime;
                playerStat.Value.ScoreboardPerMin.Kills = playerStat.Value.ScoreboardTotal.Kills / matchTime.TotalMinutes;
                playerStat.Value.ScoreboardPerMin.Deaths = playerStat.Value.ScoreboardTotal.Deaths / matchTime.TotalMinutes;
                playerStat.Value.ScoreboardPerMin.Assists = playerStat.Value.ScoreboardTotal.Assists / matchTime.TotalMinutes;
                playerStat.Value.ScoreboardPerMin.DamageDealt = playerStat.Value.ScoreboardTotal.DamageDealt / matchTime.TotalMinutes;
                playerStat.Value.ScoreboardPerMin.DamageTaken = playerStat.Value.ScoreboardTotal.DamageTaken / matchTime.TotalMinutes;
                playerStat.Value.ScoreboardPerMin.HPRestored = playerStat.Value.ScoreboardTotal.HPRestored / matchTime.TotalMinutes;
                playerStat.Value.ScoreboardPerMin.TimeOnCrystal = playerStat.Value.ScoreboardTotal.TimeOnCrystal / matchTime.TotalMinutes;

                playerStat.Value.ScoreboardContrib.Kills = teamContributions[playerStat.Key].OrderBy(x => x.Kills).ElementAt(statMatches / 2).Kills;
                playerStat.Value.ScoreboardContrib.Deaths = teamContributions[playerStat.Key].OrderBy(x => x.Deaths).ElementAt(statMatches / 2).Deaths;
                playerStat.Value.ScoreboardContrib.Assists = teamContributions[playerStat.Key].OrderBy(x => x.Assists).ElementAt(statMatches / 2).Assists;
                playerStat.Value.ScoreboardContrib.DamageDealt = teamContributions[playerStat.Key].OrderBy(x => x.DamageDealt).ElementAt(statMatches / 2).DamageDealt;
                playerStat.Value.ScoreboardContrib.DamageTaken = teamContributions[playerStat.Key].OrderBy(x => x.DamageTaken).ElementAt(statMatches / 2).DamageTaken;
                playerStat.Value.ScoreboardContrib.HPRestored = teamContributions[playerStat.Key].OrderBy(x => x.HPRestored).ElementAt(statMatches / 2).HPRestored;
                playerStat.Value.ScoreboardContrib.TimeOnCrystalDouble = teamContributions[playerStat.Key].OrderBy(x => x.TimeOnCrystalDouble).ElementAt(statMatches / 2).TimeOnCrystalDouble;
            }
            ListCSV += CSVRow(statsModel, playerStat.Key);
        }
        try {
            RefreshLock.Wait();
            DataModel = statsModel.Keys.ToList();
            DataModelUntruncated = DataModel;
            StatsModel = statsModel;
            PlayerCount = DataModel.Count;
            RemoveByMatchCount(MinMatches);
            TriggerSort = true;
        } finally {
            RefreshLock.Release();
        }
    }

    private void RemoveByMatchCount(uint minMatches) {
        List<PlayerAlias> DataModelTruncated = new();
        foreach(var player in DataModelUntruncated) {
            if(StatsModel[player].StatsAll.Matches >= minMatches) {
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
                comparator = (r) => p2.GetValue(p1.GetValue(StatsModel[r])) ?? 0;
            }
        }
        DataModel = direction == ImGuiSortDirection.Ascending ? DataModel.OrderBy(comparator).ToList() : DataModel.OrderByDescending(comparator).ToList();
        DataModelUntruncated = direction == ImGuiSortDirection.Ascending ? DataModelUntruncated.OrderBy(comparator).ToList() : DataModelUntruncated.OrderByDescending(comparator).ToList();
    }
}
