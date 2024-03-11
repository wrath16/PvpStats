using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Windows.List;
internal class CrystallineConflictPlayerList : FilteredList<PlayerAlias> {

    private class PlayerStats {
        public Job FavoredJob;
        public uint MatchesAll, PlayerWinsAll, SelfWinsAll, PlayerLossesAll, SelfLossesAll, MatchesTeammate, SelfWinsTeammate, SelfLossesTeammate, MatchesOpponent, SelfWinsOpponent, SelfLossesOpponent;
        public int PlayerWinDiff, SelfAllWinDiff, SelfTeammateWinDiff, SelfOpponentWinDiff;
        public double PlayerWinrateAll, SelfWinrateAll, SelfWinrateTeammate, SelfWinrateOpponent;
        public Dictionary<Job, JobStats> JobStats = new();

        //scoreboard stuff
        public uint ScoreboardMatches;
        //public double AvgKills, AvgDeaths, AvgAssists, AvgDamageDealt, AvgDamageTaken, AvgHPRestored;
        public TimeSpan TotalTimeOnCrystal = TimeSpan.Zero, TotalMatchTime = TimeSpan.Zero;
        public ulong TotalKills, TotalDeaths, TotalAssists, TotalDamageDealt, TotalDamageTaken, TotalHPRestored;
        //public double KillsPerMin, DeathsPerMin, AssistsPerMin, DamageDealtPerMin, DamageTakenPerMin, HPRestoredPerMin;
        public ScoreboardDouble StatsPerMatch = new(), StatsPerMin = new(), StatsMedianTeamContribution = new();
        public List<ScoreboardDouble> TeamContribs = new();

        public void AddJobStat(Job job, bool isWin) {
            if(JobStats.ContainsKey(job)) {
                JobStats[job].Matches++;
                JobStats[job].Wins += isWin ? 1 : 0;
            } else {
                JobStats.Add(job, new() {
                    Matches = 1,
                    Wins = isWin ? 1 : 0
                });
            }
        }
        private float _maxNameLength, _maxWorldLength;
    }

    private class JobStats {
        public int Matches, Wins;
    }

    private class ScoreboardDouble {
        public double Kills, Deaths, Assists, DamageDealt, DamageTaken, HPRestored, TimeOnCrystalDouble;
        public TimeSpan TimeOnCrystal;
    }

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Name", Id = 0, Width = 200f, Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{Name = "Home World", Id = 1, Width = 110f, Flags = ImGuiTableColumnFlags.WidthFixed },
        new ColumnParams{Name = "Favored Job", Id = (uint)"FavoredJob".GetHashCode() },
        new ColumnParams{Name = "Total Matches", Id = (uint)"MatchesAll".GetHashCode() },
        new ColumnParams{Name = "Player Wins", Id = (uint)"PlayerWinsAll".GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Player Losses", Id = (uint)"PlayerLossesAll".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Player Win Diff.", Id = (uint)"PlayerWinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Player Win Rate", Id = (uint)"PlayerWinrateAll".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Wins", Id = (uint)"SelfWinsAll".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Losses", Id = (uint)"SelfLossesAll".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Win Diff.", Id = (uint)"SelfAllWinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Win Rate", Id = (uint)"SelfWinrateAll".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Matches", Id = (uint)"MatchesTeammate".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Wins", Id = (uint)"SelfWinsTeammate".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Losses", Id = (uint)"SelfLossesTeammate".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Win Diff.", Id = (uint)"SelfTeammateWinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Win Rate", Id = (uint)"SelfWinrateTeammate".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Matches", Id = (uint)"MatchesOpponent".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Wins", Id = (uint)"SelfWinsOpponent".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Losses", Id = (uint)"SelfLossesOpponent".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Win Diff.", Id = (uint)"SelfOpponentWinDiff".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Win Rate", Id = (uint)"SelfWinrateOpponent".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Kills", Id = (uint)"TotalKills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Deaths", Id = (uint)"TotalDeaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Assists", Id = (uint)"TotalAssists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Damage Dealt", Id = (uint)"TotalDamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Damage Taken", Id = (uint)"TotalDamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total HP Restored", Id = (uint)"TotalHPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Total Time on Crystal", Id = (uint)"TotalTimeOnCrystal".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Kills Per Match", Id = (uint)"StatsPerMatch.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Deaths Per Match", Id = (uint)"StatsPerMatch.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Assists Per Match", Id = (uint)"StatsPerMatch.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Match", Id = (uint)"StatsPerMatch.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Taken Per Match", Id = (uint)"StatsPerMatch.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "HP Restored Per Match", Id = (uint)"StatsPerMatch.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Time on Crystal Per Match", Id = (uint)"StatsPerMatch.TimeOnCrystal".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Kills Per Min", Id = (uint)"StatsPerMin.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Deaths Per Min", Id = (uint)"StatsPerMin.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Assists Per Min", Id = (uint)"StatsPerMin.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Dealt Per Min", Id = (uint)"StatsPerMin.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Damage Taken Per Min", Id = (uint)"StatsPerMin.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "HP Restored Per Min", Id = (uint)"StatsPerMin.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Time on Crystal Per Min", Id = (uint)"StatsPerMin.TimeOnCrystal".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Kill Contrib.", Id = (uint)"StatsMedianTeamContribution.Kills".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Death Contrib.", Id = (uint)"StatsMedianTeamContribution.Deaths".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Assist Contrib.", Id = (uint)"StatsMedianTeamContribution.Assists".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Damage Dealt Contrib.", Id = (uint)"StatsMedianTeamContribution.DamageDealt".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Damage Taken Contrib.", Id = (uint)"StatsMedianTeamContribution.DamageTaken".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median HP Restored Contrib.", Id = (uint)"StatsMedianTeamContribution.HPRestored".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Median Time on Crystal Contrib.", Id = (uint)"StatsMedianTeamContribution.TimeOnCrystalDouble".GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
    };

    private CrystallineConflictList ListModel { get; init; }
    private List<PlayerAlias> DataModelUntruncated { get; set; } = new();
    private OtherPlayerFilter OtherPlayerFilter { get; init; }
    private Dictionary<PlayerAlias, PlayerStats> Stats = new();
    private int PlayerCount { get; set; }
    //private int MatchCount { get; set; }
    private uint MinMatches { get; set; } = 1;

    private bool _triggerSort = false;

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable
        | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;

    protected override bool ShowHeader { get; set; } = true;
    protected override bool ChildWindow { get; set; } = false;
    protected override string TableId => "###CCPlayerStatsTable";

    //public CrystallineConflictPlayerList(Plugin plugin) : base(plugin) {
    //}

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
        ImGuiHelper.HelpMarker("Right-click table header for column options.", false);
        ImGui.SameLine();
        ImGui.TextUnformatted($"Total players:   {PlayerCount}");

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

    public override void DrawListItem(PlayerAlias item) {
        ImGui.TextUnformatted($"{item.Name}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{item.HomeWorld}");
        ImGui.TableNextColumn();
        var job = Stats[item].FavoredJob;
        var role = PlayerJobHelper.GetSubRoleFromJob(job);
        var jobColor = ImGuiColors.DalamudWhite;
        switch(role) {
            case JobSubRole.TANK:
                jobColor = ImGuiColors.TankBlue;
                break;
            case JobSubRole.HEALER:
                jobColor = ImGuiColors.HealerGreen;
                break;
            case JobSubRole.RANGED:
                jobColor = ImGuiColors.DalamudOrange;
                break;
            case JobSubRole.MELEE:
                jobColor = ImGuiColors.DPSRed;
                break;
            case JobSubRole.CASTER:
                jobColor = ImGuiColors.ParsedPink;
                break;
            default:
                break;
        }
        ImGui.TextColored(jobColor, $"{Stats[item].FavoredJob}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].MatchesAll}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].PlayerWinsAll}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].PlayerLossesAll}");
        ImGui.TableNextColumn();
        var playerWinDiff = Stats[item].PlayerWinDiff;
        var playerWinDiffColor = playerWinDiff > 0 ? ImGuiColors.HealerGreen : playerWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(playerWinDiffColor, $"{playerWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(playerWinDiffColor, $"{string.Format("{0:P1}%", Stats[item].PlayerWinrateAll)}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].SelfWinsAll}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].SelfLossesAll}");
        ImGui.TableNextColumn();
        var selfWinDiff = Stats[item].SelfAllWinDiff;
        var selfAllWinDiffColor = selfWinDiff > 0 ? ImGuiColors.HealerGreen : selfWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(selfAllWinDiffColor, $"{selfWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(selfAllWinDiffColor, $"{string.Format("{0:P1}%", Stats[item].SelfWinrateAll)}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].MatchesTeammate}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].SelfWinsTeammate}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].SelfLossesTeammate}");
        ImGui.TableNextColumn();
        var teammateWinDiff = Stats[item].SelfTeammateWinDiff;
        var teammateWinDiffColor = teammateWinDiff > 0 ? ImGuiColors.HealerGreen : teammateWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(teammateWinDiffColor, $"{teammateWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(teammateWinDiffColor, $"{string.Format("{0:P1}%", Stats[item].SelfWinrateTeammate)}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].MatchesOpponent}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].SelfWinsOpponent}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].SelfLossesOpponent}");
        ImGui.TableNextColumn();
        var OpponentWinDiff = Stats[item].SelfOpponentWinDiff;
        var OpponentWinDiffColor = OpponentWinDiff > 0 ? ImGuiColors.HealerGreen : OpponentWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(OpponentWinDiffColor, $"{OpponentWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(OpponentWinDiffColor, $"{string.Format("{0:P1}%", Stats[item].SelfWinrateOpponent)}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].TotalKills.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].TotalDeaths.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].TotalAssists.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].TotalDamageDealt.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].TotalDamageTaken.ToString("N0")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].TotalHPRestored.ToString("N0")}");
        ImGui.TableNextColumn();
        var totalTimeCrystal = Stats[item].TotalTimeOnCrystal;
        string totalTimeOnCrystalDisplay = $"{(totalTimeCrystal.TotalHours > 0 ? $"{(int)totalTimeCrystal.TotalHours}:" : "")}";
        totalTimeOnCrystalDisplay += totalTimeCrystal.ToString(@"mm\:ss");
        ImGui.TextUnformatted(totalTimeOnCrystalDisplay);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMatch.Kills.ToString("0.00")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMatch.Deaths.ToString("0.00")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMatch.Assists.ToString("0.00")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMatch.DamageDealt.ToString("#")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMatch.DamageTaken.ToString("#")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMatch.HPRestored.ToString("#")}");
        ImGui.TableNextColumn();
        var crystalTimePerMatch = Stats[item].StatsPerMatch.TimeOnCrystal;
        ImGui.TextUnformatted($"{crystalTimePerMatch.Minutes}{crystalTimePerMatch.ToString(@"\:ss")}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMin.Kills.ToString("0.00")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMin.Deaths.ToString("0.00")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMin.Assists.ToString("0.00")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMin.DamageDealt.ToString("#")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMin.DamageTaken.ToString("#")}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{Stats[item].StatsPerMin.HPRestored.ToString("#")}");
        ImGui.TableNextColumn();
        var crystalTimePerMin = Stats[item].StatsPerMin.TimeOnCrystal;
        ImGui.TextUnformatted($"{crystalTimePerMin.Minutes}{crystalTimePerMin.ToString(@"\:ss")}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(string.Format("{0:P1}", Stats[item].StatsMedianTeamContribution.Kills));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(string.Format("{0:P1}", Stats[item].StatsMedianTeamContribution.Deaths));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(string.Format("{0:P1}", Stats[item].StatsMedianTeamContribution.Assists));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(string.Format("{0:P1}", Stats[item].StatsMedianTeamContribution.DamageDealt));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(string.Format("{0:P1}", Stats[item].StatsMedianTeamContribution.DamageTaken));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(string.Format("{0:P1}", Stats[item].StatsMedianTeamContribution.HPRestored));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(string.Format("{0:P1}", Stats[item].StatsMedianTeamContribution.TimeOnCrystalDouble));
    }

    //we don't need this
    public override void OpenFullEditDetail(PlayerAlias item) {
        return;
    }

    public override void OpenItemDetail(PlayerAlias item) {
        return;
    }

    public override void RefreshDataModel() {
        Dictionary<PlayerAlias, PlayerStats> playerStats = new();
        foreach(var match in ListModel.DataModel) {
            foreach(var team in match.Teams) {
                foreach(var player in team.Value.Players) {
                    //check against filters
                    bool nameMatch = player.Alias.FullName.Contains(OtherPlayerFilter.PlayerNamesRaw, StringComparison.OrdinalIgnoreCase);
                    bool sideMatch = OtherPlayerFilter.TeamStatus == TeamStatus.Any
                        || OtherPlayerFilter.TeamStatus == TeamStatus.Teammate && team.Key == match.LocalPlayerTeam?.TeamName
                        || OtherPlayerFilter.TeamStatus == TeamStatus.Opponent && team.Key != match.LocalPlayerTeam?.TeamName;
                    bool jobMatch = OtherPlayerFilter.AnyJob || OtherPlayerFilter.PlayerJob == player.Job;
                    if(!nameMatch || !sideMatch || !jobMatch) {
                        continue;
                    }

                    if(!playerStats.ContainsKey(player.Alias)) {
                        playerStats.Add(player.Alias, new());
                    }
                    playerStats[player.Alias].MatchesAll++;
                    bool isPlayerWin = false;
                    if(!match.IsSpectated) {
                        if(match.IsWin) {
                            playerStats[player.Alias].SelfWinsAll++;
                            if(team.Key == match.LocalPlayerTeam!.TeamName) {
                                playerStats[player.Alias].SelfWinsTeammate++;
                                playerStats[player.Alias].MatchesTeammate++;
                                playerStats[player.Alias].PlayerWinsAll++;
                                isPlayerWin = true;
                            } else {
                                playerStats[player.Alias].SelfWinsOpponent++;
                                playerStats[player.Alias].MatchesOpponent++;
                                playerStats[player.Alias].PlayerLossesAll++;
                            }
                        } else if(match.MatchWinner != null) {
                            playerStats[player.Alias].SelfLossesAll++;
                            if(team.Key == match.LocalPlayerTeam!.TeamName) {
                                playerStats[player.Alias].SelfLossesTeammate++;
                                playerStats[player.Alias].MatchesTeammate++;
                                playerStats[player.Alias].PlayerLossesAll++;
                            } else {
                                playerStats[player.Alias].SelfLossesOpponent++;
                                playerStats[player.Alias].MatchesOpponent++;
                                playerStats[player.Alias].PlayerWinsAll++;
                                isPlayerWin = true;
                            }
                        } else {
                            //draw/unfinished
                        }
                    } else {
                        //handle spectated logic here
                        if(team.Key == match.MatchWinner) {
                            playerStats[player.Alias].PlayerWinsAll++;
                        } else if(match.MatchWinner != null) {
                            playerStats[player.Alias].PlayerLossesAll++;
                        } else {
                            //draw/unfinished
                        }
                    }
                    if(player.Job != null) {
                        playerStats[player.Alias].AddJobStat((Job)player.Job, isPlayerWin);
                    }

                    //add scoreboard stats
                    if(match.PostMatch != null) {
                        //var teamScoreboard = match.PostMatch.Teams.Where(x => x.Value.PlayerStats.Where(y => y.Player != null && y.Player.Equals(player.Alias)).Count() > 0).FirstOrDefault();
                        //var playerScoreboard = teamScoreboard.Value.PlayerStats.Where(y => y.Player != null && y.Player.Equals(player.Alias)).FirstOrDefault();
                        var playerTeamScoreboard = match.PostMatch.Teams.Where(x => x.Key == team.Key).FirstOrDefault().Value;
                        var playerScoreboard = playerTeamScoreboard.PlayerStats.Where(x => x.Player?.Equals(player.Alias) ?? false).FirstOrDefault();
                        if(playerScoreboard != null) {
                            playerStats[player.Alias].ScoreboardMatches++;
                            playerStats[player.Alias].TotalMatchTime += match.PostMatch.MatchDuration;
                            playerStats[player.Alias].TotalKills += (ulong)playerScoreboard.Kills;
                            playerStats[player.Alias].TotalDeaths += (ulong)playerScoreboard.Deaths;
                            playerStats[player.Alias].TotalAssists += (ulong)playerScoreboard.Assists;
                            playerStats[player.Alias].TotalDamageDealt += (ulong)playerScoreboard.DamageDealt;
                            playerStats[player.Alias].TotalDamageTaken += (ulong)playerScoreboard.DamageTaken;
                            playerStats[player.Alias].TotalHPRestored += (ulong)playerScoreboard.HPRestored;
                            playerStats[player.Alias].TotalTimeOnCrystal += playerScoreboard.TimeOnCrystal;

                            playerStats[player.Alias].TeamContribs.Add(new() {
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

        foreach(var playerStat in playerStats) {
            //remove players who don't meet match threshold
            if(playerStat.Value.MatchesAll < MinMatches) {
                playerStats.Remove(playerStat.Key);
            }

            //set win rates
            playerStat.Value.PlayerWinrateAll = (double)playerStat.Value.PlayerWinsAll / playerStat.Value.MatchesAll;
            playerStat.Value.SelfWinrateAll = (double)playerStat.Value.SelfWinsAll / playerStat.Value.MatchesAll;
            playerStat.Value.SelfWinrateTeammate = playerStat.Value.MatchesTeammate != 0 ? (double)playerStat.Value.SelfWinsTeammate / playerStat.Value.MatchesTeammate : 0;
            playerStat.Value.SelfWinrateOpponent = playerStat.Value.MatchesOpponent != 0 ? (double)playerStat.Value.SelfWinsOpponent / playerStat.Value.MatchesOpponent : 0;

            //set diffs
            playerStat.Value.PlayerWinDiff = (int)playerStat.Value.PlayerWinsAll - (int)playerStat.Value.PlayerLossesAll;
            playerStat.Value.SelfAllWinDiff = (int)playerStat.Value.SelfWinsAll - (int)playerStat.Value.SelfLossesAll;
            playerStat.Value.SelfTeammateWinDiff = (int)playerStat.Value.SelfWinsTeammate - (int)playerStat.Value.SelfLossesTeammate;
            playerStat.Value.SelfOpponentWinDiff = (int)playerStat.Value.SelfWinsOpponent - (int)playerStat.Value.SelfLossesOpponent;

            //set favored job
            playerStat.Value.FavoredJob = playerStat.Value.JobStats.OrderByDescending(x => x.Value.Matches).FirstOrDefault().Key;

            //set average stats
            if(playerStat.Value.ScoreboardMatches > 0) {
                playerStat.Value.StatsPerMatch.Kills = (double)playerStat.Value.TotalKills / playerStat.Value.ScoreboardMatches;
                playerStat.Value.StatsPerMatch.Deaths = (double)playerStat.Value.TotalDeaths / playerStat.Value.ScoreboardMatches;
                playerStat.Value.StatsPerMatch.Assists = (double)playerStat.Value.TotalAssists / playerStat.Value.ScoreboardMatches;
                playerStat.Value.StatsPerMatch.DamageDealt = (double)playerStat.Value.TotalDamageDealt / playerStat.Value.ScoreboardMatches;
                playerStat.Value.StatsPerMatch.DamageTaken = (double)playerStat.Value.TotalDamageTaken / playerStat.Value.ScoreboardMatches;
                playerStat.Value.StatsPerMatch.HPRestored = (double)playerStat.Value.TotalHPRestored / playerStat.Value.ScoreboardMatches;
                playerStat.Value.StatsPerMatch.TimeOnCrystal = playerStat.Value.TotalTimeOnCrystal / playerStat.Value.ScoreboardMatches;

                playerStat.Value.StatsPerMin.Kills = playerStat.Value.TotalKills / playerStat.Value.TotalMatchTime.TotalMinutes;
                playerStat.Value.StatsPerMin.Deaths = playerStat.Value.TotalDeaths / playerStat.Value.TotalMatchTime.TotalMinutes;
                playerStat.Value.StatsPerMin.Assists = playerStat.Value.TotalAssists / playerStat.Value.TotalMatchTime.TotalMinutes;
                playerStat.Value.StatsPerMin.DamageDealt = playerStat.Value.TotalDamageDealt / playerStat.Value.TotalMatchTime.TotalMinutes;
                playerStat.Value.StatsPerMin.DamageTaken = playerStat.Value.TotalDamageTaken / playerStat.Value.TotalMatchTime.TotalMinutes;
                playerStat.Value.StatsPerMin.HPRestored = playerStat.Value.TotalHPRestored / playerStat.Value.TotalMatchTime.TotalMinutes;
                playerStat.Value.StatsPerMin.TimeOnCrystal = playerStat.Value.TotalTimeOnCrystal / playerStat.Value.TotalMatchTime.TotalMinutes;

                playerStat.Value.StatsMedianTeamContribution.Kills = playerStat.Value.TeamContribs.OrderBy(x => x.Kills).ElementAt((int)playerStat.Value.ScoreboardMatches / 2).Kills;
                playerStat.Value.StatsMedianTeamContribution.Deaths = playerStat.Value.TeamContribs.OrderBy(x => x.Deaths).ElementAt((int)playerStat.Value.ScoreboardMatches / 2).Deaths;
                playerStat.Value.StatsMedianTeamContribution.Assists = playerStat.Value.TeamContribs.OrderBy(x => x.Assists).ElementAt((int)playerStat.Value.ScoreboardMatches / 2).Assists;
                playerStat.Value.StatsMedianTeamContribution.DamageDealt = playerStat.Value.TeamContribs.OrderBy(x => x.DamageDealt).ElementAt((int)playerStat.Value.ScoreboardMatches / 2).DamageDealt;
                playerStat.Value.StatsMedianTeamContribution.DamageTaken = playerStat.Value.TeamContribs.OrderBy(x => x.DamageTaken).ElementAt((int)playerStat.Value.ScoreboardMatches / 2).DamageTaken;
                playerStat.Value.StatsMedianTeamContribution.HPRestored = playerStat.Value.TeamContribs.OrderBy(x => x.HPRestored).ElementAt((int)playerStat.Value.ScoreboardMatches / 2).HPRestored;
                playerStat.Value.StatsMedianTeamContribution.TimeOnCrystalDouble = playerStat.Value.TeamContribs.OrderBy(x => x.TimeOnCrystalDouble).ElementAt((int)playerStat.Value.ScoreboardMatches / 2).TimeOnCrystalDouble;
            }
        }
        try {
            RefreshLock.Wait();
            DataModel = playerStats.Keys.ToList();
            DataModelUntruncated = DataModel;
            Stats = playerStats;
            PlayerCount = DataModel.Count;
            _triggerSort = true;
        } finally {
            RefreshLock.Release();
        }
    }

    private void RemoveByMatchCount(uint minMatches) {
        List<PlayerAlias> DataModelTruncated = new();
        foreach(var player in DataModelUntruncated) {
            if(Stats[player].MatchesAll >= minMatches) {
                DataModelTruncated.Add(player);
            }
        }
        DataModel = DataModelTruncated;
        GoToPage(0);
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
            var fields = typeof(PlayerStats).GetFields();
            foreach(var field in fields) {
                var fieldId = field.Name.GetHashCode();
                if((uint)fieldId == columnId) {
                    //_plugin.Log.Debug($"Match found! {field.Name}");
                    comparator = (r) => field.GetValue(Stats[r]) ?? 0;
                    //if(field.FieldType == typeof(TimeSpan)) {
                    //    comparator = (r) => ((TimeSpan?)field.GetValue(Stats[r]))?.Ticks ?? 0;
                    //} else {
                    //    comparator = (r) => field.GetValue(Stats[r]) ?? 0;
                    //}
                }
                if(field.FieldType == typeof(ScoreboardDouble)) {
                    //iterate
                    var sFields = field.FieldType.GetFields();
                    foreach(var sField in sFields) {
                        var sFieldId = $"{field.Name}.{sField.Name}".GetHashCode();
                        if((uint)sFieldId == columnId) {
                            //_plugin.Log.Debug($"Match found! {sField.Name}");
                            comparator = (r) => sField.GetValue(field.GetValue(Stats[r])) ?? 0;
                        }
                    }
                }
            }
        }
        DataModel = direction == ImGuiSortDirection.Ascending ? DataModel.OrderBy(comparator).ToList() : DataModel.OrderByDescending(comparator).ToList();
        DataModelUntruncated = direction == ImGuiSortDirection.Ascending ? DataModelUntruncated.OrderBy(comparator).ToList() : DataModelUntruncated.OrderByDescending(comparator).ToList();
    }
}
