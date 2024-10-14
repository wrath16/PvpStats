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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class FrontlinePlayerList : PlayerStatsList<FLPlayerJobStats, FrontlineMatch> {

    protected override string TableId => "###FLPlayerStatsTable";

    //internal state
    ConcurrentQueue<PlayerAlias> _players = [];
    ConcurrentDictionary<PlayerAlias, FLPlayerJobStats> _playerStats = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<int, FLScoreboardDouble>> _playerTeamContributions = [];
    ConcurrentDictionary<PlayerAlias, TimeTally> _playerTimes = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<Job, FLAggregateStats>> _playerJobStatsLookup = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<PlayerAlias, int>> _activeLinks = [];
    ConcurrentDictionary<PlayerAlias, FLPlayerJobStats> _shatterStats = [];
    ConcurrentDictionary<PlayerAlias, ConcurrentDictionary<int, FLScoreboardDouble>> _shatterTeamContributions = [];
    ConcurrentDictionary<PlayerAlias, TimeTally> _shatterTimes = [];

    List<PlayerAlias> _linkedPlayerAliases = [];

    StatSourceFilter _lastStatSourceFilter = new();
    OtherPlayerFilter _lastPlayerFilter = new();

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{           Name = "Name",                                                                      Id = 0,                                                             Width = 200f,                                   Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{           Name = "Home World",                        Header = "Home World",                  Id = 1,                                                             Width = 110f,                                   Flags = ImGuiTableColumnFlags.WidthFixed },
        new ColumnParams{           Name = "Favored Job",                                                               Id = (uint)"StatsAll.Job".GetHashCode(),                            Width = 50f,            Alignment = 1,          Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Matches",                                                             Id = (uint)"StatsAll.Matches".GetHashCode(),                        Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "1st Places",                                                                Id = (uint)"StatsAll.FirstPlaces".GetHashCode(),                    Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "2nd Places",                                                                Id = (uint)"StatsAll.SecondPlaces".GetHashCode(),                   Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "3rd Places",                                                                Id = (uint)"StatsAll.ThirdPlaces".GetHashCode(),                    Width = 45f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Win Rate",                                                                  Id = (uint)"StatsAll.WinRate".GetHashCode(),                        Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Average Place",                                                             Id = (uint)"StatsAll.AveragePlace".GetHashCode(),                   Width = 55f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills",                                                               Id = (uint)"ScoreboardTotal.Kills".GetHashCode(),                   Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Deaths",                                                              Id = (uint)"ScoreboardTotal.Deaths".GetHashCode(),                  Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Assists",                                                             Id = (uint)"ScoreboardTotal.Assists".GetHashCode(),                 Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Dealt",                Header = "Total Damage\nDealt",         Id = (uint)"ScoreboardTotal.DamageDealt".GetHashCode(),             Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage to PCs",                                                       Id = (uint)"ScoreboardTotal.DamageToPCs".GetHashCode(),             Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage to Other",                                                     Id = (uint)"ScoreboardTotal.DamageToOther".GetHashCode(),           Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Taken",                Header = "Total Damage\nTaken",         Id = (uint)"ScoreboardTotal.DamageTaken".GetHashCode(),             Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total HP Restored",                                                         Id = (uint)"ScoreboardTotal.HPRestored".GetHashCode(),              Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Kills/Assists",               Header = "Total Kills\n and Assists",   Id = (uint)"ScoreboardTotal.KillsAndAssists".GetHashCode(),         Width = 75f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills Per Match",                                                           Id = (uint)"ScoreboardPerMatch.Kills".GetHashCode(),                Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Match",                                                          Id = (uint)"ScoreboardPerMatch.Deaths".GetHashCode(),               Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Match",                                                         Id = (uint)"ScoreboardPerMatch.Assists".GetHashCode(),              Width = 73f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Match",                                                    Id = (uint)"ScoreboardPerMatch.DamageDealt".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to PCs Per Match",                                                   Id = (uint)"ScoreboardPerMatch.DamageToPCs".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to Other Per Match",                                                 Id = (uint)"ScoreboardPerMatch.DamageToOther".GetHashCode(),        Width = 105f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Match",                                                    Id = (uint)"ScoreboardPerMatch.DamageTaken".GetHashCode(),          Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Match",                                                     Id = (uint)"ScoreboardPerMatch.HPRestored".GetHashCode(),           Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills/Assists Per Match",                                                   Id = (uint)"ScoreboardPerMatch.KillsAndAssists".GetHashCode(),      Width = 85f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills Per Min",                     Header = "Kills\nPer Min",              Id = (uint)"ScoreboardPerMin.Kills".GetHashCode(),                  Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Deaths Per Min",                                                            Id = (uint)"ScoreboardPerMin.Deaths".GetHashCode(),                 Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Assists Per Min",                                                           Id = (uint)"ScoreboardPerMin.Assists".GetHashCode(),                Width = 60f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageDealt".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to PCs Per Min",                                                     Id = (uint)"ScoreboardPerMin.DamageToPCs".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage to Other Per Min",                                                   Id = (uint)"ScoreboardPerMin.DamageToOther".GetHashCode(),          Width = 105f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Min",                                                      Id = (uint)"ScoreboardPerMin.DamageTaken".GetHashCode(),            Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Min",                                                       Id = (uint)"ScoreboardPerMin.HPRestored".GetHashCode(),             Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Kills/Assists Per Min",                                                     Id = (uint)"ScoreboardPerMin.KillsAndAssists".GetHashCode(),        Width = 85f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill Contrib.",                                                      Id = (uint)"ScoreboardContrib.Kills".GetHashCode(),                 Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Death Contrib.",                                                     Id = (uint)"ScoreboardContrib.Deaths".GetHashCode(),                Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Assist Contrib.",                                                    Id = (uint)"ScoreboardContrib.Assists".GetHashCode(),               Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage Dealt Contrib.",                                              Id = (uint)"ScoreboardContrib.DamageDealt".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage to PCs Contrib.",                                             Id = (uint)"ScoreboardContrib.DamageToPCs".GetHashCode(),           Width = 120f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage to Other Contrib.",                                           Id = (uint)"ScoreboardContrib.DamageToOther".GetHashCode(),         Width = 120f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Damage Taken Contrib.",                                              Id = (uint)"ScoreboardContrib.DamageTaken".GetHashCode(),           Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median HP Restored Contrib.",                                               Id = (uint)"ScoreboardContrib.HPRestored".GetHashCode(),            Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill/Assist Contrib.",   Header = "Median Kill and\nAssist Contrib", Id = (uint)"ScoreboardContrib.KillsAndAssists".GetHashCode(),       Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Kill/Assist",  Header = "Damage Dealt\nPer Kill/Assist",   Id = (uint)"ScoreboardTotal.DamageDealtPerKA".GetHashCode(),        Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageDealtPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageTakenPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Life",                                                      Id = (uint)"ScoreboardTotal.HPRestoredPerLife".GetHashCode(),       Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Battle High Per Life",                                                      Id = (uint)"BattleHighPerLife".GetHashCode(),                       Width = 75f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "KDA Ratio",                                                                 Id = (uint)"ScoreboardTotal.KDA".GetHashCode(),                     Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
    };

    public FrontlinePlayerList(Plugin plugin, PlayerStatSourceFilter statSourceFilter, MinMatchFilter minMatchFilter, PlayerQuickSearchFilter quickSearchFilter, OtherPlayerFilter playerFilter) : base(plugin, statSourceFilter, minMatchFilter, quickSearchFilter, playerFilter) {
        Reset();
    }

    private void Reset() {
        _players = [];
        _playerStats = [];
        _playerTeamContributions = [];
        _playerTimes = [];
        _playerJobStatsLookup = [];
        _activeLinks = [];
        _shatterStats = [];
        _shatterTeamContributions = [];
        _shatterTimes = [];
    }

    internal async Task Refresh(List<FrontlineMatch> matches, List<FrontlineMatch> additions, List<FrontlineMatch> removals) {
        MatchesProcessed = 0;
        Stopwatch s1 = Stopwatch.StartNew();
        _linkedPlayerAliases = _plugin.PlayerLinksService.GetAllLinkedAliases(PlayerFilter.PlayerNamesRaw);
        bool removalThresholdMet = removals.Count * 2 >= Matches.Count;
        bool statFilterChange = !StatSourceFilter.Equals(_lastStatSourceFilter);
        bool playerFilterChange = StatSourceFilter!.InheritFromPlayerFilter && !PlayerFilter.Equals(_lastPlayerFilter);
        try {
            //_plugin.Log.Debug($"total old: {Matches.Count} additions: {additions.Count} removals: {removals.Count} threshold: {removalThresholdMet} sfc: {statFilterChange} pfc: {playerFilterChange}");
            if(removalThresholdMet || statFilterChange || playerFilterChange) {
                //force full build
                //_plugin.Log.Debug("players full rebuild");
                Reset();
                MatchesTotal = matches.Count;
                await ProcessMatches(matches);
            } else {
                MatchesTotal = removals.Count + additions.Count;
                //_plugin.Log.Debug($"adding: {additions.Count} removing: {removals.Count}");
                await ProcessMatches(removals, true);
                await ProcessMatches(additions);
            }

            //_plugin.Log.Debug($"playerStats: {_playerStats.Count} jobStatsLookup: {_playerJobStatsLookup.Count} teamContribs: {_playerTeamContributions.Count} playerTimes: {_playerTimes.Count} " +
            //    $"shatterStats: {_shatterStats.Count} shatterContribs: {_shatterTeamContributions.Count} shatterTimes: {_shatterTimes.Count}");

            foreach(var playerStat in _playerStats) {
                playerStat.Value.StatsAll.Job = _playerJobStatsLookup[playerStat.Key].OrderByDescending(x => x.Value.Matches).FirstOrDefault().Key;
                FrontlineStatsManager.SetScoreboardStats(playerStat.Value, _playerTeamContributions[playerStat.Key].Values.ToList(), _playerTimes[playerStat.Key].ToTimeSpan());
                FrontlineStatsManager.SetScoreboardStats(_shatterStats[playerStat.Key], _shatterTeamContributions[playerStat.Key].Values.ToList(), _shatterTimes[playerStat.Key].ToTimeSpan());
                playerStat.Value.ScoreboardTotal.DamageToOther = _shatterStats[playerStat.Key].ScoreboardTotal.DamageToOther;
                playerStat.Value.ScoreboardPerMatch.DamageToOther = _shatterStats[playerStat.Key].ScoreboardPerMatch.DamageToOther;
                playerStat.Value.ScoreboardPerMin.DamageToOther = _shatterStats[playerStat.Key].ScoreboardPerMin.DamageToOther;
                playerStat.Value.ScoreboardContrib.DamageToOther = _shatterStats[playerStat.Key].ScoreboardContrib.DamageToOther;
            }

            //this may be incorrect when removing matches
            ActiveLinks = _activeLinks.Select(x => (x.Key, x.Value.ToDictionary())).ToDictionary();
            DataModel = _playerStats.Keys.ToList();
            DataModelUntruncated = DataModel;
            StatsModel = _playerStats.ToDictionary();
            ApplyQuickFilters(MinMatchFilter.MinMatches, PlayerQuickSearchFilter.SearchText);
            GoToPage(0);
            TriggerSort = true;

            Matches = matches;

            _lastStatSourceFilter = new(StatSourceFilter!);
            _lastPlayerFilter = new(PlayerFilter);
        } finally {
            s1.Stop();
            _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"FL Players Refresh", s1.ElapsedMilliseconds.ToString()));
            MatchesProcessed = 0;
        }
        return;
    }

    protected override void ProcessMatch(FrontlineMatch match, bool remove = false) {
        List<Task> playerTasks = [];
        if(match.PlayerScoreboards != null) {
            var teamScoreboards = match.GetTeamScoreboards();
            foreach(var playerScoreboard in match.PlayerScoreboards) {
                var player = match.Players.FirstOrDefault(x => x.Name.Equals(playerScoreboard.Key));
                if(player is null) return;
                bool isLocalPlayer = player.Name.Equals(match.LocalPlayer);
                bool isTeammate = !isLocalPlayer && player.Team == match.LocalPlayerTeam;
                bool isOpponent = !isLocalPlayer && !isTeammate;

                bool statEligible = true;
                bool nameMatch = player.Name.FullName.Contains(PlayerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                if(_plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                    nameMatch = _linkedPlayerAliases.Contains(player.Name);
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
                        _activeLinks[alias].TryAdd(player.Name, 0);
                        if(remove) {
                            _activeLinks[alias][player.Name]--;
                        } else {
                            _activeLinks[alias][player.Name]++;
                        }
                    }
                    var teamScoreboard = new FLScoreboardTally(match.GetTeamScoreboards()[player.Team]);
                    _playerStats.TryAdd(alias, new());
                    _playerTeamContributions.TryAdd(alias, new());
                    _playerJobStatsLookup.TryAdd(alias, new());
                    _playerTimes.TryAdd(alias, new());
                    _shatterStats.TryAdd(alias, new());
                    _shatterTeamContributions.TryAdd(alias, new());
                    _shatterTimes.TryAdd(alias, new());

                    if(remove) {
                        _playerTimes[alias].RemoveTime(match.MatchDuration ?? TimeSpan.Zero);
                    } else {
                        _playerTimes[alias].AddTime(match.MatchDuration ?? TimeSpan.Zero);
                    }

                    FrontlineStatsManager.AddPlayerJobStat(_playerStats[alias], _playerTeamContributions[alias], match, player, teamScoreboard, remove);
                    if(player.Job != null) {
                        _playerJobStatsLookup[alias].TryAdd((Job)player.Job, new());
                        FrontlineStatsManager.IncrementAggregateStats(_playerJobStatsLookup[alias][(Job)player.Job], match, remove);
                    }

                    //shatter only
                    if(match.Arena == FrontlineMap.FieldsOfGlory) {
                        if(remove) {
                            _shatterTimes[alias].RemoveTime(match.MatchDuration ?? TimeSpan.Zero);
                        } else {
                            _shatterTimes[alias].AddTime(match.MatchDuration ?? TimeSpan.Zero);
                        }
                        FrontlineStatsManager.AddPlayerJobStat(_shatterStats[alias], _shatterTeamContributions[alias], match, player, teamScoreboard, remove);
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
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.FirstPlaces.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.SecondPlaces.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.ThirdPlaces.ToString(), Offset);
        }
        var winDiffColor = _plugin.Configuration.GetFrontlineWinRateColor(StatsModel[item].StatsAll);
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.WinRate.ToString("P1"), Offset, winDiffColor);
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
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageToPCs, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageDealtToPCsPerMatchRange[0], FrontlineStatsManager.DamageDealtToPCsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageToOther, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageToOtherPerMatchRange[0], FrontlineStatsManager.DamageToOtherPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
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
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KillsPerMinRange[0], FrontlineStatsManager.KillsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, FrontlineStatsManager.DeathsPerMinRange[0], FrontlineStatsManager.DeathsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.AssistsPerMinRange[0], FrontlineStatsManager.AssistsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageDealtPerMinRange[0], FrontlineStatsManager.DamageDealtPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageToPCs, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageDealtToPCsPerMinRange[0], FrontlineStatsManager.DamageDealtToPCsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageToOther, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageToOtherPerMinRange[0], FrontlineStatsManager.DamageToOtherPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.DamageTakenPerMinRange[0], FrontlineStatsManager.DamageTakenPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.HPRestoredPerMinRange[0], FrontlineStatsManager.HPRestoredPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.KillsPerMinRange[0] + FrontlineStatsManager.AssistsPerMinRange[0], FrontlineStatsManager.KillsPerMinRange[1] + FrontlineStatsManager.AssistsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
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
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageToPCs, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageToOther, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, FrontlineStatsManager.ContribRange[0], FrontlineStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
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
}
