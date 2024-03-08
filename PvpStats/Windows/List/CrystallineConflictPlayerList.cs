using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class CrystallineConflictPlayerList : FilteredList<PlayerAlias> {

    private class PlayerStats {
        public Job FavoredJob;
        public uint MatchesAll, PlayerWinsAll, SelfWinsAll, PlayerLossesAll, SelfLossesAll, MatchesTeammate, SelfWinsTeammate, SelfLossesTeammate, MatchesOpponent, SelfWinsOpponent, SelfLossesOpponent;
        public int PlayerWinDiff, SelfAllWinDiff, SelfTeammateWinDiff, SelfOpponentWinDiff;
        public double PlayerWinrateAll, SelfWinrateAll, SelfWinrateTeammate, SelfWinrateOpponent;
        public Dictionary<Job, JobStats> JobStats = new();
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
        new ColumnParams{Name = "Name", Width = 200f * ImGuiHelpers.GlobalScale, Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoHide },
        new ColumnParams{Name = "Home World", Width = 110f * ImGuiHelpers.GlobalScale, Flags = ImGuiTableColumnFlags.WidthFixed },
        new ColumnParams{Name = "Favored Job" },
        new ColumnParams{Name = "Total Matches" },
        new ColumnParams{Name = "Player Wins", Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Player Losses", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Player Win Diff.", Flags = ImGuiTableColumnFlags.None },
        new ColumnParams{Name = "Player Win Rate", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Wins", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Losses", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Win Diff.", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Your Win Rate", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Matches", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Wins", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Losses", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Win Diff.", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Teammate Win Rate", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Matches", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Wins", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Losses", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Win Diff.", Flags = ImGuiTableColumnFlags.DefaultHide },
        new ColumnParams{Name = "Opponent Win Rate", Flags = ImGuiTableColumnFlags.DefaultHide },
    };

    private CrystallineConflictList ListModel { get;  init; }
    private Dictionary<PlayerAlias, PlayerStats> Stats = new();

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable 
        | ImGuiTableFlags.BordersInner | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;

    protected override bool ShowHeader {get; set;} = true;
    protected override bool ChildWindow { get; set; } = false;
    protected override string TableName => "##CCPlayerStatsTable";

    //public CrystallineConflictPlayerList(Plugin plugin) : base(plugin) {
    //}

    public CrystallineConflictPlayerList(Plugin plugin, CrystallineConflictList listModel) : base(plugin) {
        ListModel = listModel;
    }

    public override void DrawListItem(PlayerAlias item) {
        ImGui.Text($"{item.Name}");
        ImGui.TableNextColumn();
        ImGui.Text($"{item.HomeWorld}");
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
        ImGui.Text($"{Stats[item].MatchesAll}");
        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].PlayerWinsAll}");
        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].PlayerLossesAll}");
        ImGui.TableNextColumn();
        var playerWinDiff = Stats[item].PlayerWinDiff;
        var playerWinDiffColor = playerWinDiff > 0 ? ImGuiColors.HealerGreen : playerWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(playerWinDiffColor, $"{playerWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(playerWinDiffColor, $"{string.Format("{0:P1}%", Stats[item].PlayerWinrateAll)}");

        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].SelfWinsAll}");
        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].SelfLossesAll}");
        ImGui.TableNextColumn();
        var selfWinDiff = Stats[item].SelfAllWinDiff;
        var selfAllWinDiffColor = selfWinDiff > 0 ? ImGuiColors.HealerGreen : selfWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(selfAllWinDiffColor, $"{selfWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(selfAllWinDiffColor, $"{string.Format("{0:P1}%", Stats[item].SelfWinrateAll)}");


        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].MatchesTeammate}");
        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].SelfWinsTeammate}");
        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].SelfLossesTeammate}");
        ImGui.TableNextColumn();
        var teammateWinDiff = Stats[item].SelfTeammateWinDiff;
        var teammateWinDiffColor = teammateWinDiff > 0 ? ImGuiColors.HealerGreen : teammateWinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
        ImGui.TextColored(teammateWinDiffColor, $"{teammateWinDiff}");
        ImGui.TableNextColumn();
        ImGui.TextColored(teammateWinDiffColor, $"{string.Format("{0:P1}%", Stats[item].SelfWinrateTeammate)}");

        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].MatchesOpponent}");
        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].SelfWinsOpponent}");
        ImGui.TableNextColumn();
        ImGui.Text($"{Stats[item].SelfLossesOpponent}");
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
            foreach (var team in match.Teams) {
                foreach (var player in team.Value.Players) {
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
                }
            }
        }

        foreach(var playerStat in playerStats) {
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
        }
        try {
            RefreshLock.Wait();
            DataModel = playerStats.Keys.ToList();
            Stats = playerStats;
        } finally {
            RefreshLock.Release();
        }
    }
}
