using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Managers.Stats;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PvpStats.Windows.List;
internal class RivalWingsJobList : JobStatsList<RWPlayerJobStats, RivalWingsMatch> {

    public override string Name => "RW Jobs";
    protected override string TableId => "###RWJobStatsTable";

    //internal state
    ConcurrentDictionary<Job, RWPlayerJobStats> _jobStats = [];
    ConcurrentDictionary<Job, ConcurrentDictionary<int, RWScoreboardDouble>> _jobTeamContributions = [];
    ConcurrentDictionary<Job, TimeTally> _jobTimes = [];

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{           Name = "Job",                                                                       Id = 0,                                                             Width = 85f,                                    Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{           Name = "Role",                                                                      Id = 1,                                                             Width = 50f,                                    Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Instances",                                                           Id = (uint)"StatsAll.Matches".GetHashCode(),                        Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Wins",                                                                      Id = (uint)"StatsAll.Wins".GetHashCode(),                           Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Losses",                                                                    Id = (uint)"StatsAll.Losses".GetHashCode(),                         Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Win Diff.",                                                                 Id = (uint)"StatsAll.WinDiff".GetHashCode(),                        Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Win Rate",                                                                  Id = (uint)"StatsAll.WinRate".GetHashCode(),                        Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Merc Win Rate",                     Header = "Merc\nWin Rate",              Id = (uint)"MercStats.WinRate".GetHashCode(),                       Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Mid Win Rate",                      Header = "Mid\nWin Rate",               Id = (uint)"MidStats.WinRate".GetHashCode(),                        Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills",                                                               Id = (uint)"ScoreboardTotal.Kills".GetHashCode(),                   Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Deaths",                                                              Id = (uint)"ScoreboardTotal.Deaths".GetHashCode(),                  Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Assists",                                                             Id = (uint)"ScoreboardTotal.Assists".GetHashCode(),                 Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Dealt",                Header = "Total Damage\nDealt",         Id = (uint)"ScoreboardTotal.DamageDealt".GetHashCode(),             Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage to PCs",                                                       Id = (uint)"ScoreboardTotal.DamageToPCs".GetHashCode(),             Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage to Other",                                                     Id = (uint)"ScoreboardTotal.DamageToOther".GetHashCode(),           Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Taken",                Header = "Total Damage\nTaken",         Id = (uint)"ScoreboardTotal.DamageTaken".GetHashCode(),             Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total HP Restored",                                                         Id = (uint)"ScoreboardTotal.HPRestored".GetHashCode(),              Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Ceruleum",                                                            Id = (uint)"ScoreboardTotal.Ceruleum".GetHashCode(),                Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills/Assists",               Header = "Total Kills\n and Assists",   Id = (uint)"ScoreboardTotal.KillsAndAssists".GetHashCode(),         Width = 75f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills Per Match",                                                           Id = (uint)"ScoreboardPerMatch.Kills".GetHashCode(),                Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Match",                                                          Id = (uint)"ScoreboardPerMatch.Deaths".GetHashCode(),               Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Match",                                                         Id = (uint)"ScoreboardPerMatch.Assists".GetHashCode(),              Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Match",                                                    Id = (uint)"ScoreboardPerMatch.DamageDealt".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to PCs Per Match",                                                   Id = (uint)"ScoreboardPerMatch.DamageToPCs".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to Other Per Match",                                                 Id = (uint)"ScoreboardPerMatch.DamageToOther".GetHashCode(),        Width = 105f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Match",                                                    Id = (uint)"ScoreboardPerMatch.DamageTaken".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Match",                                                     Id = (uint)"ScoreboardPerMatch.HPRestored".GetHashCode(),           Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Ceruleum Per Match",            Header = "Ceruleum\nPer Match",             Id = (uint)"ScoreboardPerMatch.Ceruleum".GetHashCode(),             Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills/Assists Per Match",                                                   Id = (uint)"ScoreboardPerMatch.KillsAndAssists".GetHashCode(),      Width = 85f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills Per Min",                     Header = "Kills\nPer Min",              Id = (uint)"ScoreboardPerMin.Kills".GetHashCode(),                  Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Min",                                                            Id = (uint)"ScoreboardPerMin.Deaths".GetHashCode(),                 Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Min",                                                           Id = (uint)"ScoreboardPerMin.Assists".GetHashCode(),                Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageDealt".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to PCs Per Min",                                                     Id = (uint)"ScoreboardPerMin.DamageToPCs".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to Other Per Min",                                                   Id = (uint)"ScoreboardPerMin.DamageToOther".GetHashCode(),          Width = 105f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageTaken".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Min",                                                       Id = (uint)"ScoreboardPerMin.HPRestored".GetHashCode(),             Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Ceruleum Per Min",            Header = "Ceruleum\nPer Min",                 Id = (uint)"ScoreboardPerMin.Ceruleum".GetHashCode(),               Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills/Assists Per Min",                                                     Id = (uint)"ScoreboardPerMin.KillsAndAssists".GetHashCode(),        Width = 85f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill Contrib.",                                                      Id = (uint)"ScoreboardContrib.Kills".GetHashCode(),                 Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Death Contrib.",                                                     Id = (uint)"ScoreboardContrib.Deaths".GetHashCode(),                Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Assist Contrib.",                                                    Id = (uint)"ScoreboardContrib.Assists".GetHashCode(),               Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage Dealt Contrib.",                                              Id = (uint)"ScoreboardContrib.DamageDealt".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage to PCs Contrib.",                                             Id = (uint)"ScoreboardContrib.DamageToPCs".GetHashCode(),           Width = 120f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage to Other Contrib.",                                           Id = (uint)"ScoreboardContrib.DamageToOther".GetHashCode(),         Width = 120f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage Taken Contrib.",                                              Id = (uint)"ScoreboardContrib.DamageTaken".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median HP Restored Contrib.",                                               Id = (uint)"ScoreboardContrib.HPRestored".GetHashCode(),            Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Ceruleum Contrib.",                                                  Id = (uint)"ScoreboardContrib.Ceruleum".GetHashCode(),              Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill/Assist Contrib.",   Header = "Median Kill and\nAssist Contrib", Id = (uint)"ScoreboardContrib.KillsAndAssists".GetHashCode(),       Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Kill/Assist",  Header = "Damage Dealt\nPer Kill/Assist",   Id = (uint)"ScoreboardTotal.DamageDealtPerKA".GetHashCode(),        Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageDealtPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageTakenPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Life",                                                      Id = (uint)"ScoreboardTotal.HPRestoredPerLife".GetHashCode(),       Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "KDA Ratio",                                                                 Id = (uint)"ScoreboardTotal.KDA".GetHashCode(),                     Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
    };

    public RivalWingsJobList(Plugin plugin, StatSourceFilter? statSourceFilter, OtherPlayerFilter playerFilter) : base(plugin, statSourceFilter, playerFilter) {
        Reset();
    }

    protected override void Reset() {
        _jobStats = [];
        _jobTeamContributions = [];
        _jobTimes = [];
        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        foreach(var job in allJobs) {
            _jobStats.TryAdd(job, new());
            _jobTeamContributions.TryAdd(job, new());
            _jobTimes.TryAdd(job, new());
        }
    }

    protected override void PostRefresh(List<RivalWingsMatch> matches, List<RivalWingsMatch> additions, List<RivalWingsMatch> removals) {
        foreach(var playerStat in _jobStats) {
            RivalWingsStatsManager.SetScoreboardStats(playerStat.Value, _jobTeamContributions[playerStat.Key].Values.ToList(), _jobTimes[playerStat.Key].ToTimeSpan());
        }
        DataModel = _jobStats.Keys.ToList();
        DataModelUntruncated = DataModel;
        StatsModel = _jobStats.ToDictionary();
        base.PostRefresh(matches, additions, removals);
    }

    protected override void ProcessMatch(RivalWingsMatch match, bool remove = false) {
        if(match.PlayerScoreboards is null) return;
        var teamScoreboards = match.GetTeamScoreboards();
        if(teamScoreboards is null) return;

        foreach(var playerScoreboard in match.PlayerScoreboards) {
            var player = match.Players.FirstOrDefault(x => x.Name.Equals(playerScoreboard.Key));
            if(player is null) continue;
            bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
            bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam;
            bool isOpponent = !isLocalPlayer && !isTeammate;

            bool statEligible = true;
            bool nameMatch = player.Name.FullName.Contains(PlayerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
            if(_plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                nameMatch = _plugin.PlayerLinksService.GetAllLinkedAliases(PlayerFilter.PlayerNamesRaw).Contains(player.Name);
            }
            bool sideMatch = PlayerFilter.TeamStatus == TeamStatus.Any
                || PlayerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                || PlayerFilter.TeamStatus == TeamStatus.Opponent && !isTeammate && !isLocalPlayer;
            bool jobMatch = PlayerFilter.AnyJob || PlayerFilter.PlayerJob == player.Job;
            if(!nameMatch || !sideMatch || !jobMatch) {
                if(StatSourceFilter.InheritFromPlayerFilter) {
                    statEligible = false;
                }
            }
            if(!StatSourceFilter.FilterState[StatSource.LocalPlayer] && isLocalPlayer) {
                statEligible = false;
            } else if(!StatSourceFilter.FilterState[StatSource.Teammate] && isTeammate) {
                statEligible = false;
            } else if(!StatSourceFilter.FilterState[StatSource.Opponent] && !isTeammate && !isLocalPlayer) {
                statEligible = false;
            }

            if(statEligible && player.Job != null) {
                var job = (Job)player.Job;
                var teamScoreboard = new RWScoreboardTally(teamScoreboards[player.Team]);
                var enemyTeam = (RivalWingsTeamName)((int)(player.Team + 1) % 2);
                _jobStats.TryAdd(job, new());
                _jobTeamContributions.TryAdd(job, new());
                _jobTimes.TryAdd(job, new());
                if(remove) {
                    _jobTimes[job].RemoveTime(match.MatchDuration ?? TimeSpan.Zero);
                } else {
                    _jobTimes[job].AddTime(match.MatchDuration ?? TimeSpan.Zero);
                }
                RivalWingsStatsManager.AddPlayerJobStat(_jobStats[job], _jobTeamContributions[job], match, player, teamScoreboard, remove);

                if(match.Mercs != null) {
                    if(remove) {
                        Interlocked.Add(ref _jobStats[job].MercStats.Wins, -match.Mercs[player.Team]);
                        Interlocked.Add(ref _jobStats[job].MercStats.Losses, -match.Mercs[enemyTeam]);
                    } else {
                        Interlocked.Add(ref _jobStats[job].MercStats.Wins, match.Mercs[player.Team]);
                        Interlocked.Add(ref _jobStats[job].MercStats.Losses, match.Mercs[enemyTeam]);
                    }
                }
                if(match.Supplies != null) {
                    var totalWins = 0;
                    var totalLosses = 0;
                    foreach(var supply in match.Supplies[player.Team]) {
                        totalWins += supply.Value;
                    }
                    foreach(var supply in match.Supplies[enemyTeam]) {
                        totalLosses += supply.Value;
                    }
                    if(remove) {
                        Interlocked.Add(ref _jobStats[job].MidStats.Wins, -totalWins);
                        Interlocked.Add(ref _jobStats[job].MidStats.Losses, -totalLosses);
                    } else {
                        Interlocked.Add(ref _jobStats[job].MidStats.Wins, totalWins);
                        Interlocked.Add(ref _jobStats[job].MidStats.Losses, totalLosses);
                    }
                }
            }
        }
    }

    public override void DrawListItem(Job item) {
        ImGui.SameLine(2f * ImGuiHelpers.GlobalScale);
        ImGui.TextUnformatted($"{PlayerJobHelper.GetNameFromJob(item)}");
        if(ImGui.TableNextColumn()) {
            ImGui.TextColored(_plugin.Configuration.GetJobColor(item), $"{PlayerJobHelper.GetSubRoleFromJob(item)}");
        }

        //player
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.Losses.ToString(), Offset);
        }
        var winDiff = StatsModel[item].StatsAll.WinDiff;
        var winDiffColor = winDiff > 0 ? _plugin.Configuration.Colors.Win : winDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(winDiff.ToString(), Offset, winDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.WinRate.ToString("P1"), Offset, winDiffColor);
        }
        var mercWinDiff = StatsModel[item].MercStats.WinDiff;
        var mercWinDiffColor = mercWinDiff > 0 ? _plugin.Configuration.Colors.Win : mercWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].MercStats.WinRate.ToString("P1"), Offset, mercWinDiffColor);
        }
        var midWinDiff = StatsModel[item].MidStats.WinDiff;
        var midWinDiffColor = midWinDiff > 0 ? _plugin.Configuration.Colors.Win : midWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].MidStats.WinRate.ToString("P1"), Offset, midWinDiffColor);
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
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageToPCs.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageToOther.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageTaken.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.HPRestored.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.Ceruleum.ToString("N0"), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.KillsAndAssists.ToString("N0"), Offset);
        }

        //per match
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.KillsPerMatchRange[0], RivalWingsStatsManager.KillsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, RivalWingsStatsManager.DeathsPerMatchRange[0], RivalWingsStatsManager.DeathsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.AssistsPerMatchRange[0], RivalWingsStatsManager.AssistsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtPerMatchRange[0], RivalWingsStatsManager.DamageDealtPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageToPCs, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtToPCsPerMatchRange[0], RivalWingsStatsManager.DamageDealtToPCsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageToOther, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtToOtherPerMatchRange[0], RivalWingsStatsManager.DamageDealtToOtherPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageTakenPerMatchRange[0], RivalWingsStatsManager.DamageTakenPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.HPRestoredPerMatchRange[0], RivalWingsStatsManager.HPRestoredPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Ceruleum, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.CeruleumPerMatchRange[0], RivalWingsStatsManager.CeruleumPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.KillsPerMatchRange[0] + RivalWingsStatsManager.AssistsPerMatchRange[0], RivalWingsStatsManager.KillsPerMatchRange[1] + RivalWingsStatsManager.AssistsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //per min
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.KillsPerMinRange[0], RivalWingsStatsManager.KillsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, RivalWingsStatsManager.DeathsPerMinRange[0], RivalWingsStatsManager.DeathsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.AssistsPerMinRange[0], RivalWingsStatsManager.AssistsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtPerMinRange[0], RivalWingsStatsManager.DamageDealtPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageToPCs, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtToPCsPerMinRange[0], RivalWingsStatsManager.DamageDealtToPCsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageToOther, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageDealtToOtherPerMinRange[0], RivalWingsStatsManager.DamageDealtToOtherPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageTakenPerMinRange[0], RivalWingsStatsManager.DamageTakenPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.HPRestoredPerMinRange[0], RivalWingsStatsManager.HPRestoredPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Ceruleum, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.CeruleumPerMinRange[0], RivalWingsStatsManager.CeruleumPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.KillsPerMinRange[0] + RivalWingsStatsManager.AssistsPerMinRange[0], RivalWingsStatsManager.KillsPerMinRange[1] + RivalWingsStatsManager.AssistsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //team contrib
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageToPCs, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageToOther, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Ceruleum, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.ContribRange[0], RivalWingsStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }

        //special
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageDealtPerKA, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, RivalWingsStatsManager.DamagePerKARange[0], RivalWingsStatsManager.DamagePerKARange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageDealtPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamagePerLifeRange[0], RivalWingsStatsManager.DamagePerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageTakenPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.DamageTakenPerLifeRange[0], RivalWingsStatsManager.DamageTakenPerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.HPRestoredPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.HPRestoredPerLifeRange[0], RivalWingsStatsManager.HPRestoredPerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardTotal.KDA, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.KDARange[0], RivalWingsStatsManager.KDARange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
    }

}
