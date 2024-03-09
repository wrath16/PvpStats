using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PvpStats.Windows.List;
internal class CrystallineConflictPlayerList : FilteredList<PlayerAlias> {

    private class PlayerStats {
        public Job FavoredJob;
        public uint MatchesAll, PlayerWinsAll, SelfWinsAll, PlayerLossesAll, SelfLossesAll, MatchesTeammate, SelfWinsTeammate, SelfLossesTeammate, MatchesOpponent, SelfWinsOpponent, SelfLossesOpponent;
        public int PlayerWinDiff, SelfAllWinDiff, SelfTeammateWinDiff, SelfOpponentWinDiff;
        public double PlayerWinrateAll, SelfWinrateAll, SelfWinrateTeammate, SelfWinrateOpponent;
        public Dictionary<Job, JobStats> JobStats = new();
        public uint ScoreboardMatches; 
        public double AvgKills, AvgDeaths, AvgAssists, AvgDamageDealt, AvgDamageTaken, AvgHPRestored;
        public TimeSpan AvgTimeOnCrystal = TimeSpan.Zero;
        public ulong TotalKills, TotalDeaths, TotalAssists, TotalDamageDealt, TotalDamageTaken, TotalHPRestored;
        public TimeSpan TotalTimeOnCrystal = TimeSpan.Zero;
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

    protected override List<ColumnParams> Columns { get; set; } = new() {
        new ColumnParams{Name = "Name", Id = 0, Width = 200f, Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{Name = "Home World", Id = 1, Width = 110f, Flags = ImGuiTableColumnFlags.WidthFixed },
        new ColumnParams{Name = "Favored Job", Id = (uint)typeof(PlayerStats).GetField("FavoredJob").Name.GetHashCode() },
        new ColumnParams{Name = "Total Matches", Id = (uint)typeof(PlayerStats).GetField("MatchesAll").Name.GetHashCode() },
        new ColumnParams{Name = "Player Wins", Id = (uint)typeof(PlayerStats).GetField("PlayerWinsAll").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Player Losses", Id = (uint)typeof(PlayerStats).GetField("PlayerLossesAll").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Player Win Diff.", Id = (uint)typeof(PlayerStats).GetField("PlayerWinDiff").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Player Win Rate", Id = (uint)typeof(PlayerStats).GetField("PlayerWinrateAll").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Wins", Id = (uint)typeof(PlayerStats).GetField("SelfWinsAll").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Losses", Id = (uint)typeof(PlayerStats).GetField("SelfLossesAll").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Win Diff.", Id = (uint)typeof(PlayerStats).GetField("SelfAllWinDiff").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Win Rate", Id = (uint)typeof(PlayerStats).GetField("SelfWinrateAll").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Matches", Id = (uint)typeof(PlayerStats).GetField("MatchesTeammate").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Wins", Id = (uint)typeof(PlayerStats).GetField("SelfWinsTeammate").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Losses", Id = (uint)typeof(PlayerStats).GetField("SelfLossesTeammate").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Win Diff.", Id = (uint)typeof(PlayerStats).GetField("SelfTeammateWinDiff").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Win Rate", Id = (uint)typeof(PlayerStats).GetField("SelfWinrateTeammate").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Matches", Id = (uint)typeof(PlayerStats).GetField("MatchesOpponent").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Wins", Id = (uint)typeof(PlayerStats).GetField("SelfWinsOpponent").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Losses", Id = (uint)typeof(PlayerStats).GetField("SelfLossesOpponent").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Win Diff.", Id = (uint)typeof(PlayerStats).GetField("SelfOpponentWinDiff").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Win Rate", Id = (uint)typeof(PlayerStats).GetField("SelfWinrateOpponent").Name.GetHashCode(), Flags = ImGuiTableColumnFlags.DefaultHide },
    };

    private CrystallineConflictList ListModel { get; init; }
    private OtherPlayerFilter OtherPlayerFilter { get; init; }
    private Dictionary<PlayerAlias, PlayerStats> Stats = new();
    private int PlayerCount { get; set; }
    private int MatchCount { get; set; }
    private uint MinMatches { get; set; } = 1;

    private bool _triggerSort = false;

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable
        | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;

    protected override bool ShowHeader { get; set; } = true;
    protected override bool ChildWindow { get; set; } = false;
    protected override string TableName => "##CCPlayerStatsTable";

    //public CrystallineConflictPlayerList(Plugin plugin) : base(plugin) {
    //}

    public CrystallineConflictPlayerList(Plugin plugin, CrystallineConflictList listModel, OtherPlayerFilter playerFilter) : base(plugin) {
        ListModel = listModel;
        OtherPlayerFilter = playerFilter;
    }

    protected override void PreTableDraw() {
        int minMatches = (int)MinMatches;
        ImGui.SetNextItemWidth(float.Max(ImGui.GetContentRegionAvail().X / 3f, 150f * ImGuiHelpers.GlobalScale));
        if(ImGui.SliderInt("Min. matches", ref minMatches, 1, 100)) {
            MinMatches = (uint)minMatches;
            _plugin.DataQueue.QueueDataOperation(() => {
                RefreshDataModel();
            });
        }

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
        var role = PlayerJobHelper.GetRoleFromJob(job);
        var jobColor = ImGuiColors.DalamudWhite;
        switch(role) {
            case JobRole.TANK:
                jobColor = ImGuiColors.TankBlue;
                break;
            case JobRole.HEALER:
                jobColor = ImGuiColors.HealerGreen;
                break;
            case JobRole.DPS:
                jobColor = ImGuiColors.DPSRed;
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
                            //handle draw logic!
                        }
                    } else {
                        //handle spectated logic here
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
                            playerStats[player.Alias].TotalKills += (ulong)playerScoreboard.Kills;
                            playerStats[player.Alias].TotalDeaths += (ulong)playerScoreboard.Deaths;
                            playerStats[player.Alias].TotalAssists += (ulong)playerScoreboard.Assists;
                            playerStats[player.Alias].TotalDamageDealt += (ulong)playerScoreboard.DamageDealt;
                            playerStats[player.Alias].TotalDamageTaken += (ulong)playerScoreboard.DamageTaken;
                            playerStats[player.Alias].TotalHPRestored += (ulong)playerScoreboard.HPRestored;
                            playerStats[player.Alias].TotalTimeOnCrystal += playerScoreboard.TimeOnCrystal;
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
                playerStat.Value.AvgKills = (double)playerStat.Value.TotalKills / playerStat.Value.ScoreboardMatches;
                playerStat.Value.AvgDeaths = (double)playerStat.Value.TotalDeaths / playerStat.Value.ScoreboardMatches;
                playerStat.Value.AvgAssists = (double)playerStat.Value.TotalAssists / playerStat.Value.ScoreboardMatches;
                playerStat.Value.AvgDamageDealt = (double)playerStat.Value.TotalDamageDealt / playerStat.Value.ScoreboardMatches;
                playerStat.Value.AvgDamageTaken = (double)playerStat.Value.TotalDamageTaken / playerStat.Value.ScoreboardMatches;
                playerStat.Value.AvgHPRestored = (double)playerStat.Value.TotalHPRestored / playerStat.Value.ScoreboardMatches;
                playerStat.Value.AvgTimeOnCrystal = playerStat.Value.TotalTimeOnCrystal / playerStat.Value.ScoreboardMatches;
            }
        }
        try {
            RefreshLock.Wait();
            DataModel = playerStats.Keys.ToList();
            Stats = playerStats;
            PlayerCount = DataModel.Count;
            _triggerSort = true;
        } finally {
            RefreshLock.Release();
        }
    }

    private void RemoveByMatchCount() {
        ////remove players who don't meet match threshold
        //if(playerStat.Value.MatchesAll < MinMatches) {
        //    playerStats.Remove(playerStat.Key);
        //}
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        _plugin.Log.Debug($"Sorting by {columnId}");
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
                }
            }
        }
        DataModel = direction == ImGuiSortDirection.Ascending ? DataModel.OrderBy(comparator).ToList() : DataModel.OrderByDescending(comparator).ToList();
    }
}
