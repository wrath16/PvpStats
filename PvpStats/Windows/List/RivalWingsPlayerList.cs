﻿using Dalamud.Interface.Colors;
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
internal class RivalWingsPlayerList : PlayerStatsList<RWPlayerJobStats, RivalWingsMatch> {

    public override string Name => "RW Players";
    protected override string TableId => "###RWPlayerStatsTable";

    //internal state
    ConcurrentDictionary<PlayerAlias, RWPlayerJobStats> _playerStats = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<int, RWScoreboardDouble>> _playerTeamContributions = [];
    ConcurrentDictionary<PlayerAlias, TimeTally> _playerTimes = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<Job, CCAggregateStats>> _playerJobStatsLookup = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<PlayerAlias, InterlockedTally>> _activeLinks = [];
    //Dictionary<PlayerAlias, CCAggregateStats> _playerMercStats = [];
    //Dictionary<PlayerAlias, CCAggregateStats> _playerMidStats = [];

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{           Name = "Name",                                                                      Id = 0,                                                             Width = 200f,                                   Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{           Name = "Home World",                        Header = "Home World",                  Id = 1,                                                             Width = 110f,                                   Flags = ImGuiTableColumnFlags.WidthFixed },
        new ColumnParams{           Name = "Favored Job",                                                               Id = (uint)"StatsAll.Job".GetHashCode(),                            Width = 50f,            Alignment = 1,          Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Matches",                                                             Id = (uint)"StatsAll.Matches".GetHashCode(),                        Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Wins",                                                                      Id = (uint)"StatsAll.Wins".GetHashCode(),                           Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Losses",                                                                    Id = (uint)"StatsAll.Losses".GetHashCode(),                         Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Win Diff.",                                                                 Id = (uint)"StatsAll.WinDiff".GetHashCode(),                        Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Win Rate",                                                                  Id = (uint)"StatsAll.WinRate".GetHashCode(),                        Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Merc Win Rate",                     Header = "Merc\nWin Rate",              Id = (uint)"MercStats.WinRate".GetHashCode(),                       Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Mid Win Rate",                      Header = "Mid\nWin Rate",               Id = (uint)"MidStats.WinRate".GetHashCode(),                        Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills",                                                               Id = (uint)"ScoreboardTotal.Kills".GetHashCode(),                   Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Deaths",                                                              Id = (uint)"ScoreboardTotal.Deaths".GetHashCode(),                  Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Assists",                                                             Id = (uint)"ScoreboardTotal.Assists".GetHashCode(),                 Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Dealt",                Header = "Total Damage\nDealt",         Id = (uint)"ScoreboardTotal.DamageDealt".GetHashCode(),             Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage to PCs",                                                       Id = (uint)"ScoreboardTotal.DamageToPCs".GetHashCode(),             Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage to Other",                                                     Id = (uint)"ScoreboardTotal.DamageToOther".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Taken",                Header = "Total Damage\nTaken",         Id = (uint)"ScoreboardTotal.DamageTaken".GetHashCode(),             Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total HP Restored",                                                         Id = (uint)"ScoreboardTotal.HPRestored".GetHashCode(),              Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
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
        new NumericColumnParams{    Name = "Kills Per Min",                     Header = "Kills\nPer Min",              Id = (uint)"ScoreboardPerMin.Kills".GetHashCode(),                  Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Min",                                                            Id = (uint)"ScoreboardPerMin.Deaths".GetHashCode(),                 Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Min",                                                           Id = (uint)"ScoreboardPerMin.Assists".GetHashCode(),                Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageDealt".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to PCs Per Min",                                                     Id = (uint)"ScoreboardPerMin.DamageToPCs".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to Other Per Min",                                                   Id = (uint)"ScoreboardPerMin.DamageToOther".GetHashCode(),          Width = 105f + Offset,                          Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageTaken".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Min",                                                       Id = (uint)"ScoreboardPerMin.HPRestored".GetHashCode(),             Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Ceruleum Per Min",            Header = "Ceruleum\nPer Min",                 Id = (uint)"ScoreboardPerMin.Ceruleum".GetHashCode(),               Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
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
        new NumericColumnParams{    Name = "Kill Participation",                                                        Id = (uint)"ScoreboardTotal.KillParticipationRate".GetHashCode(),   Width = 90f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
    };

    public RivalWingsPlayerList(Plugin plugin, StatSourceFilter? statSourceFilter, MinMatchFilter? minMatchFilter, PlayerQuickSearchFilter? quickSearchFilter, OtherPlayerFilter playerFilter) : base(plugin, statSourceFilter, minMatchFilter, quickSearchFilter, playerFilter) {
        Reset();
    }

    protected override void Reset() {
        _playerStats = [];
        _playerTeamContributions = [];
        _playerTimes = [];
        _playerJobStatsLookup = [];
        _activeLinks = [];
    }

    protected override void PostRefresh(List<RivalWingsMatch> matches, List<RivalWingsMatch> additions, List<RivalWingsMatch> removals) {
        foreach(var playerStat in _playerStats) {
            playerStat.Value.StatsAll.Job = _playerJobStatsLookup[playerStat.Key].OrderByDescending(x => x.Value.Matches).FirstOrDefault().Key;
            RivalWingsStatsManager.SetScoreboardStats(playerStat.Value, _playerTeamContributions[playerStat.Key].Values.ToList(), _playerTimes[playerStat.Key].ToTimeSpan());
        }
        ActiveLinks = _activeLinks.Select(x => (x.Key, x.Value.Where(y => y.Value.Tally > 0).Select(y => (y.Key, y.Value.Tally)).ToDictionary())).ToDictionary();
        DataModel = _playerStats.Keys.ToList();
        DataModelUntruncated = DataModel;
        StatsModel = _playerStats.ToDictionary();
        ApplyQuickFilters(MinMatchFilter.MinMatches, PlayerQuickSearchFilter.SearchText);
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

            if(statEligible) {
                //handle linked aliases
                var alias = _plugin.PlayerLinksService.GetMainAlias(player.Name);
                if(alias != player.Name) {
                    _activeLinks.TryAdd(alias, new());
                    _activeLinks[alias].TryAdd(player.Name, new());
                    if(remove) {
                        _activeLinks[alias][player.Name].Subtract(1);
                    } else {
                        _activeLinks[alias][player.Name].Add(1);
                    }
                }
                var teamScoreboard = new RWScoreboardTally(teamScoreboards[player.Team]);
                var enemyTeam = (RivalWingsTeamName)((int)(player.Team + 1) % 2);
                _playerStats.TryAdd(alias, new());
                _playerTeamContributions.TryAdd(alias, new());
                _playerJobStatsLookup.TryAdd(alias, new());
                _playerTimes.TryAdd(alias, new());
                if(remove) {
                    _playerTimes[alias].RemoveTime(match.MatchDuration ?? TimeSpan.Zero);
                } else {
                    _playerTimes[alias].AddTime(match.MatchDuration ?? TimeSpan.Zero);
                }
                RivalWingsStatsManager.AddPlayerJobStat(_playerStats[alias], _playerTeamContributions[alias], match, player, teamScoreboard, remove);
                if(player.Job != null) {
                    _playerJobStatsLookup[alias].TryAdd((Job)player.Job, new());
                    RivalWingsStatsManager.IncrementAggregateStats(_playerJobStatsLookup[alias][(Job)player.Job], match, remove);
                }

                if(match.Mercs != null) {
                    var playerMercWins = match.Mercs[player.Team];
                    var enemyMercWins = match.Mercs[enemyTeam];
                    if(match.Flags.HasFlag(RWValidationFlag.DoubleMerc)) {
                        playerMercWins = (int)Math.Ceiling((double)playerMercWins / 2);
                        enemyMercWins = (int)Math.Ceiling((double)enemyMercWins / 2);
                    }
                    if(remove) {
                        Interlocked.Add(ref _playerStats[alias].MercStats.Wins, -playerMercWins);
                        Interlocked.Add(ref _playerStats[alias].MercStats.Losses, -enemyMercWins);
                    } else {
                        Interlocked.Add(ref _playerStats[alias].MercStats.Wins, playerMercWins);
                        Interlocked.Add(ref _playerStats[alias].MercStats.Losses, enemyMercWins);
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
                        Interlocked.Add(ref _playerStats[alias].MidStats.Wins, -totalWins);
                        Interlocked.Add(ref _playerStats[alias].MidStats.Losses, -totalLosses);
                    } else {
                        Interlocked.Add(ref _playerStats[alias].MidStats.Wins, totalWins);
                        Interlocked.Add(ref _playerStats[alias].MidStats.Losses, totalLosses);
                    }
                }
            }
        }
    }

    public override void DrawListItem(PlayerAlias item) {
        ImGui.SameLine(2f * ImGuiHelpers.GlobalScale);
        ImGui.TextUnformatted($"{item.Name}");
        if(ActiveLinks.ContainsKey(item)) {
            string tooltipText = "Including stats for:\n\n";
            ActiveLinks[item].Where(x => x.Value > 0).ToList().ForEach(x => tooltipText += x + "\n");
            tooltipText = tooltipText.Substring(0, tooltipText.Length - 1);
            ImGuiHelper.WrappedTooltip(tooltipText);
        }
        if(ImGui.TableNextColumn()) {
            ImGui.TextUnformatted(item.HomeWorld);
        }
        if(ImGui.TableNextColumn()) {
            var job = StatsModel[item].StatsAll.Job;
            var jobString = job.ToString() ?? "";
            ImGuiHelper.CenterAlignCursor(jobString);
            ImGui.TextColored(_plugin.Configuration.GetJobColor(job), jobString);
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
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardTotal.KillParticipationRate, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, RivalWingsStatsManager.KillParticipationRange[0], RivalWingsStatsManager.KillParticipationRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
    }
}
