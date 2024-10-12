using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Managers.Stats;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class CrystallineConflictJobList : CCStatsList<Job> {

    protected override string TableId => "###CCJobStatsTable";

    internal StatSourceFilter StatSourceFilter { get; private set; }
    internal OtherPlayerFilter PlayerFilter { get; private set; }

    //internal state
    List<CrystallineConflictMatch> _matches = new();
    int _matchesProcessed = 0;
    int _matchesTotal = 100;

    Dictionary<Job, CCPlayerJobStats> _jobStats;
    Dictionary<Job, List<CCScoreboardDouble>> _jobTeamContributions;
    Dictionary<Job, TimeSpan> _jobTimes;

    StatSourceFilter _lastJobStatSourceFilter = new();
    OtherPlayerFilter _lastPlayerFilter = new();

    List<PlayerAlias> _linkedPlayerAliases = [];

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{           Name = "Job",                                                                       Id = 0,                                                             Width = 85f,                                    Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{           Name = "Role",                                                                      Id = 1,                                                             Width = 50f,                                    Flags = ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Instances",                                                           Id = (uint)"StatsAll.Matches".GetHashCode(),                        Width = 65f + Offset,                           Flags = ImGuiTableColumnFlags.WidthFixed },
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
        new NumericColumnParams{    Name = "Total Damage Dealt",                Header = "Total Damage\nDealt",         Id = (uint)"ScoreboardTotal.DamageDealt".GetHashCode(),             Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total Damage Taken",                Header = "Total Damage\nTaken",         Id = (uint)"ScoreboardTotal.DamageTaken".GetHashCode(),             Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Total HP Restored",                                                         Id = (uint)"ScoreboardTotal.HPRestored".GetHashCode(),              Width = 100f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
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
        new NumericColumnParams{    Name = "Median Time on Crystal Contrib.",                                           Id = (uint)"ScoreboardContrib.TimeOnCrystal".GetHashCode(),         Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Median Kill/Assist Contrib.",   Header = "Median Kill and\nAssist Contrib", Id = (uint)"ScoreboardContrib.KillsAndAssists".GetHashCode(),       Width = 110f + Offset,                          Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Kill/Assist",  Header = "Damage Dealt\nPer Kill/Assist",   Id = (uint)"ScoreboardTotal.DamageDealtPerKA".GetHashCode(),        Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Dealt Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageDealtPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "Damage Taken Per Life",                                                     Id = (uint)"ScoreboardTotal.DamageTakenPerLife".GetHashCode(),      Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "HP Restored Per Life",                                                      Id = (uint)"ScoreboardTotal.HPRestoredPerLife".GetHashCode(),       Width = 95f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
        new NumericColumnParams{    Name = "KDA Ratio",                                                                 Id = (uint)"ScoreboardTotal.KDA".GetHashCode(),                     Width = 50f + Offset,                           Flags = ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.WidthFixed },
    };

    public CrystallineConflictJobList(Plugin plugin, StatSourceFilter statSourceFilter, OtherPlayerFilter playerFilter) : base(plugin) {
        StatSourceFilter = statSourceFilter;
        PlayerFilter = playerFilter;
        Reset();
    }

    private void Reset() {
        _jobStats = [];
        _jobTeamContributions = [];
        _jobTimes = [];

        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        foreach(var job in allJobs) {
            _jobStats.Add(job, new());
            _jobTimes.Add(job, TimeSpan.Zero);
            _jobTeamContributions.Add(job, new());
        }
    }

    internal Task Refresh(List<CrystallineConflictMatch> matches, List<CrystallineConflictMatch> additions, List<CrystallineConflictMatch> removals) {
        _matchesProcessed = 0;
        Stopwatch s1 = Stopwatch.StartNew();
        _linkedPlayerAliases = _plugin.PlayerLinksService.GetAllLinkedAliases(PlayerFilter.PlayerNamesRaw);
        bool jobStatFilterChange = !StatSourceFilter.Equals(_lastJobStatSourceFilter);
        bool playerFilterChange = StatSourceFilter!.InheritFromPlayerFilter && !PlayerFilter.Equals(_lastPlayerFilter);
        try {
            if(removals.Count * 2 >= _matches.Count || jobStatFilterChange || playerFilterChange) {
                //force full build
                Reset();
                _matchesTotal = matches.Count;
                ProcessMatches(matches);
            } else {
                _matchesTotal = removals.Count + additions.Count;
                ProcessMatches(removals, true);
                ProcessMatches(additions);
            }
            foreach(var jobStat in _jobStats) {
                CrystallineConflictStatsManager.SetScoreboardStats(jobStat.Value, _jobTeamContributions[jobStat.Key], _jobTimes[jobStat.Key]);
            }
            DataModel = _jobStats.Keys.ToList();
            StatsModel = _jobStats;
            GoToPage(0);
            TriggerSort = true;

            _matches = matches;

            _lastJobStatSourceFilter = new(StatSourceFilter!);
            _lastPlayerFilter = new(PlayerFilter);
        } finally {
            s1.Stop();
            _plugin.Log.Debug(string.Format("{0,-25}: {1,4} ms", $"CC Jobs Refresh", s1.ElapsedMilliseconds.ToString()));
            _matchesProcessed = 0;
        }
        return Task.CompletedTask;
    }

    private void ProcessMatch(CrystallineConflictMatch match, bool remove = false) {
        foreach(var team in match.Teams) {
            foreach(var player in team.Value.Players) {
                bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
                bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.Key == match.LocalPlayerTeam!.TeamName;
                bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;
                bool jobStatsEligible = true;
                bool nameMatch = player.Alias.FullName.Contains(PlayerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                if(_plugin.Configuration.EnablePlayerLinking && !nameMatch) {
                    nameMatch = _linkedPlayerAliases.Contains(player.Alias);
                }
                bool sideMatch = PlayerFilter.TeamStatus == TeamStatus.Any
                    || PlayerFilter.TeamStatus == TeamStatus.Teammate && isTeammate
                    || PlayerFilter.TeamStatus == TeamStatus.Opponent && isOpponent;
                bool jobMatch = PlayerFilter.AnyJob || PlayerFilter.PlayerJob == player.Job;
                if(!nameMatch || !sideMatch || !jobMatch) {
                    if(StatSourceFilter.InheritFromPlayerFilter) {
                        jobStatsEligible = false;
                    }
                }
                if(player.Job == null) {
                    jobStatsEligible = false;
                }

                if(!StatSourceFilter.FilterState[StatSource.LocalPlayer] && isLocalPlayer) {
                    jobStatsEligible = false;
                } else if(!StatSourceFilter.FilterState[StatSource.Teammate] && isTeammate) {
                    jobStatsEligible = false;
                } else if(!StatSourceFilter.FilterState[StatSource.Opponent] && !isTeammate && !isLocalPlayer) {
                    jobStatsEligible = false;
                } else if(!StatSourceFilter.FilterState[StatSource.Spectated] && match.IsSpectated) {
                    jobStatsEligible = false;
                }
                var job = (Job)player.Job!;

                if(jobStatsEligible) {
                    if(remove) {
                        _jobTimes[job] -= match.MatchDuration ?? TimeSpan.Zero;
                    } else {
                        _jobTimes[job] += match.MatchDuration ?? TimeSpan.Zero;
                    }
                    CrystallineConflictStatsManager.AddPlayerJobStat(_jobStats[job], _jobTeamContributions[job], match, team.Value, player, remove);
                }
            }
        }
    }

    private void ProcessMatches(List<CrystallineConflictMatch> matches, bool remove = false) {
        matches.ForEach(x => {
            ProcessMatch(x, remove);
            RefreshProgress = (float)_matchesProcessed++ / _matchesTotal;
        });
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
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.Losses.ToString(), Offset);
        }
        var jobWinDiff = StatsModel[item].StatsAll.WinDiff;
        var jobWinDiffColor = jobWinDiff > 0 ? _plugin.Configuration.Colors.Win : jobWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(jobWinDiff.ToString(), Offset, jobWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsAll.WinRate.ToString("P1"), Offset, jobWinDiffColor);
        }

        //personal
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsPersonal.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsPersonal.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsPersonal.Losses.ToString(), Offset);
        }
        var personalWinDiff = StatsModel[item].StatsPersonal.WinDiff;
        var personalWinDiffColor = personalWinDiff > 0 ? _plugin.Configuration.Colors.Win : personalWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(personalWinDiff.ToString(), Offset, personalWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsPersonal.WinRate.ToString("P1"), Offset, personalWinDiffColor);
        }

        //teammate
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsTeammate.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsTeammate.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsTeammate.Losses.ToString(), Offset);
        }
        var teammateWinDiff = StatsModel[item].StatsTeammate.WinDiff;
        var teammateWinDiffColor = teammateWinDiff > 0 ? _plugin.Configuration.Colors.Win : teammateWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(teammateWinDiff.ToString(), Offset, teammateWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsTeammate.WinRate.ToString("P1"), Offset, teammateWinDiffColor);
        }

        //opponent
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsOpponent.Matches.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsOpponent.Wins.ToString(), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsOpponent.Losses.ToString(), Offset);
        }
        var opponentWinDiff = StatsModel[item].StatsOpponent.WinDiff;
        var opponentWinDiffColor = opponentWinDiff > 0 ? _plugin.Configuration.Colors.Win : opponentWinDiff < 0 ? _plugin.Configuration.Colors.Loss : ImGuiColors.DalamudWhite;
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(opponentWinDiff.ToString(), Offset, opponentWinDiffColor);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].StatsOpponent.WinRate.ToString("P1"), Offset, opponentWinDiffColor);
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
            ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(StatsModel[item].ScoreboardTotal.TimeOnCrystal), Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.KillsAndAssists.ToString("N0"), Offset);
        }

        //per match
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.KillsPerMatchRange[0], CrystallineConflictStatsManager.KillsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, CrystallineConflictStatsManager.DeathsPerMatchRange[0], CrystallineConflictStatsManager.DeathsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.AssistsPerMatchRange[0], CrystallineConflictStatsManager.AssistsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageDealtPerMatchRange[0], CrystallineConflictStatsManager.DamageDealtPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageTakenPerMatchRange[0], CrystallineConflictStatsManager.DamageTakenPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.HPRestoredPerMatchRange[0], CrystallineConflictStatsManager.HPRestoredPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            if(!double.IsNaN(StatsModel[item].ScoreboardPerMatch.TimeOnCrystal)) {
                var tcpa = TimeSpan.FromSeconds(StatsModel[item].ScoreboardPerMatch.TimeOnCrystal);
                ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(tcpa), (float)tcpa.TotalSeconds, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.TimeOnCrystalPerMatchRange[0], CrystallineConflictStatsManager.TimeOnCrystalPerMatchRange[1], _plugin.Configuration.ColorScaleStats, Offset);
            }
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMatch.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.KillsPerMatchRange[0] + CrystallineConflictStatsManager.AssistsPerMatchRange[0], CrystallineConflictStatsManager.KillsPerMatchRange[1] + CrystallineConflictStatsManager.AssistsPerMatchRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //per min
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.KillsPerMinRange[0], CrystallineConflictStatsManager.KillsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, CrystallineConflictStatsManager.DeathsPerMinRange[0], CrystallineConflictStatsManager.DeathsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.AssistsPerMinRange[0], CrystallineConflictStatsManager.AssistsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageDealtPerMinRange[0], CrystallineConflictStatsManager.DamageDealtPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageTakenPerMinRange[0], CrystallineConflictStatsManager.DamageTakenPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.HPRestoredPerMinRange[0], CrystallineConflictStatsManager.HPRestoredPerMinRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            if(!double.IsNaN(StatsModel[item].ScoreboardPerMin.TimeOnCrystal)) {
                var tcpm = TimeSpan.FromSeconds(StatsModel[item].ScoreboardPerMin.TimeOnCrystal);
                ImGuiHelper.DrawNumericCell(ImGuiHelper.GetTimeSpanString(tcpm), (float)tcpm.TotalSeconds, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.TimeOnCrystalPerMinRange[0], CrystallineConflictStatsManager.TimeOnCrystalPerMinRange[1], _plugin.Configuration.ColorScaleStats, Offset);
            }
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardPerMin.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.KillsPerMinRange[0] + CrystallineConflictStatsManager.AssistsPerMinRange[0], CrystallineConflictStatsManager.KillsPerMinRange[1] + CrystallineConflictStatsManager.AssistsPerMinRange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }

        //team contrib
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Kills, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Deaths, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.Assists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageDealt, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.DamageTaken, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.HPRestored, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.TimeOnCrystal, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardContrib.KillsAndAssists, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.ContribRange[0], CrystallineConflictStatsManager.ContribRange[1], _plugin.Configuration.ColorScaleStats, "P1", Offset);
        }

        //special
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageDealtPerKA, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, CrystallineConflictStatsManager.DamagePerKARange[0], CrystallineConflictStatsManager.DamagePerKARange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageDealtPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamagePerLifeRange[0], CrystallineConflictStatsManager.DamagePerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.DamageTakenPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.DamageTakenPerLifeRange[0], CrystallineConflictStatsManager.DamageTakenPerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell(StatsModel[item].ScoreboardTotal.HPRestoredPerLife, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.HPRestoredPerLifeRange[0], CrystallineConflictStatsManager.HPRestoredPerLifeRange[1], _plugin.Configuration.ColorScaleStats, "#", Offset);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawNumericCell((float)StatsModel[item].ScoreboardTotal.KDA, _plugin.Configuration.Colors.StatLow, _plugin.Configuration.Colors.StatHigh, CrystallineConflictStatsManager.KDARange[0], CrystallineConflictStatsManager.KDARange[1], _plugin.Configuration.ColorScaleStats, "0.00", Offset);
        }
    }

    //public override async Task RefreshDataModel() {
    //    StatsModel = _plugin.CCStatsEngine.JobStats;
    //    await base.RefreshDataModel();
    //}

    public override void OpenFullEditDetail(Job item) {
        throw new NotImplementedException();
    }

    public override void OpenItemDetail(Job item) {
    }
}
