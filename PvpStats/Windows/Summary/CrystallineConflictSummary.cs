using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictSummary {

    private Plugin _plugin;
    internal protected SemaphoreSlim RefreshLock { get; private set; } = new SemaphoreSlim(1);

    internal CCPlayerJobStats LocalPlayerStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> LocalPlayerJobStats { get; private set; } = new();
    internal Dictionary<CrystallineConflictMap, CCAggregateStats> ArenaStats { get; private set; } = new();
    internal Dictionary<PlayerAlias, CCAggregateStats> TeammateStats { get; private set; } = new();
    internal Dictionary<PlayerAlias, CCAggregateStats> OpponentStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> TeammateJobStats { get; private set; } = new();
    internal Dictionary<Job, CCAggregateStats> OpponentJobStats { get; private set; } = new();
    internal TimeSpan AverageMatchDuration { get; private set; } = new();

    public CrystallineConflictSummary(Plugin plugin) {
        _plugin = plugin;
    }

    internal async Task Refresh(List<CrystallineConflictMatch> matches) {
        Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> playerJobStatsLookup = new();
        Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> teammateJobStatsLookup = new();
        Dictionary<PlayerAlias, Dictionary<Job, CCAggregateStats>> opponentJobStatsLookup = new();
        Dictionary<PlayerAlias, List<CCScoreboardDouble>> playerTeamContributions = new();
        CCPlayerJobStats localPlayerStats = new();
        List<CCScoreboardDouble> localPlayerTeamContributions = new();
        Dictionary<CrystallineConflictMap, CCAggregateStats> arenaStats = new();
        Dictionary<PlayerAlias, CCAggregateStats> teammateStats = new();
        Dictionary<PlayerAlias, CCAggregateStats> opponentStats = new();
        Dictionary<Job, CCAggregateStats> localPlayerJobStats = new();
        Dictionary<Job, CCAggregateStats> teammateJobStats = new();
        Dictionary<Job, CCAggregateStats> opponentJobStats = new();
        Dictionary<PlayerAlias, List<PlayerAlias>> activeLinks = new();
        TimeSpan totalMatchTime = TimeSpan.Zero;

        foreach(var match in matches) {
            totalMatchTime += match.MatchDuration ?? TimeSpan.Zero;

            //local player stats
            if(!match.IsSpectated && match.PostMatch != null) {
                _plugin.CCStatsEngine.AddPlayerJobStat(localPlayerStats, localPlayerTeamContributions, match, match.LocalPlayerTeam!, match.LocalPlayerTeamMember!);
                if(match.LocalPlayerTeamMember!.Job != null) {
                    var job = (Job)match.LocalPlayerTeamMember!.Job;
                    if(!localPlayerJobStats.ContainsKey(job)) {
                        localPlayerJobStats.Add(job, new());
                    }
                    _plugin.CCStatsEngine.IncrementAggregateStats(localPlayerJobStats[job], match);
                }
            }

            //arena stats
            if(match.Arena != null) {
                var arena = (CrystallineConflictMap)match.Arena;
                if(!arenaStats.ContainsKey(arena)) {
                    arenaStats.Add(arena, new());
                }
                _plugin.CCStatsEngine.IncrementAggregateStats(arenaStats[arena], match);
            }

            //process player and job stats
            foreach(var team in match.Teams) {
                foreach(var player in team.Value.Players) {
                    bool isLocalPlayer = player.Alias.Equals(match.LocalPlayer);
                    bool isTeammate = !match.IsSpectated && !isLocalPlayer && team.Key == match.LocalPlayerTeam!.TeamName;
                    bool isOpponent = !match.IsSpectated && !isLocalPlayer && !isTeammate;
                    var job = (Job)player.Job!;

                    if(isTeammate) {
                        if(!teammateStats.ContainsKey(player.Alias)) {
                            teammateStats.Add(player.Alias, new());
                        }
                        _plugin.CCStatsEngine.IncrementAggregateStats(teammateStats[player.Alias], match);
                        if(player.Job != null) {
                            if(!teammateJobStats.ContainsKey(job)) {
                                teammateJobStats.Add(job, new());
                            }
                            _plugin.CCStatsEngine.IncrementAggregateStats(teammateJobStats[job], match);
                            if(!teammateJobStatsLookup.ContainsKey(player.Alias)) {
                                teammateJobStatsLookup.Add(player.Alias, new());
                            }
                            if(!teammateJobStatsLookup[player.Alias].ContainsKey(job)) {
                                teammateJobStatsLookup[player.Alias].Add(job, new());
                            }
                            _plugin.CCStatsEngine.IncrementAggregateStats(teammateJobStatsLookup[player.Alias][job], match);
                        }
                    } else if(isOpponent) {
                        if(!opponentStats.ContainsKey(player.Alias)) {
                            opponentStats.Add(player.Alias, new());
                        }
                        _plugin.CCStatsEngine.IncrementAggregateStats(opponentStats[player.Alias], match);
                        if(player.Job != null) {
                            if(!opponentJobStats.ContainsKey((Job)player.Job)) {
                                opponentJobStats.Add((Job)player.Job, new());
                            }
                            _plugin.CCStatsEngine.IncrementAggregateStats(opponentJobStats[(Job)player.Job], match);
                        }
                        if(!opponentJobStatsLookup.ContainsKey(player.Alias)) {
                            opponentJobStatsLookup.Add(player.Alias, new());
                        }
                        if(!opponentJobStatsLookup[player.Alias].ContainsKey(job)) {
                            opponentJobStatsLookup[player.Alias].Add(job, new());
                        }
                        _plugin.CCStatsEngine.IncrementAggregateStats(opponentJobStatsLookup[player.Alias][job], match);
                    }
                }
            }
        }

        //player linking
        if(_plugin.Configuration.EnablePlayerLinking) {
            //var manualLinks = _plugin.Storage.GetManualLinks().Query().ToList();
            var unLinks = _plugin.PlayerLinksService.ManualPlayerLinksCache.Where(x => x.IsUnlink).ToList();
            var checkPlayerLink = (PlayerAliasLink playerLink) => {
                if(playerLink.IsUnlink) return;
                foreach(var linkedAlias in playerLink.LinkedAliases) {
                    bool blocked = unLinks.Where(x => x.CurrentAlias.Equals(playerLink.CurrentAlias) && x.LinkedAliases.Contains(linkedAlias)).Any();
                    if(!blocked) {
                        bool anyMatch = false;
                        if(teammateStats.ContainsKey(linkedAlias)) {
                            anyMatch = true;
                            if(teammateStats.ContainsKey(playerLink.CurrentAlias)) {
                                teammateStats[playerLink.CurrentAlias] += teammateStats[linkedAlias];
                                foreach(var jobStat in teammateJobStatsLookup[linkedAlias]) {
                                    if(!teammateJobStatsLookup[playerLink.CurrentAlias].ContainsKey(jobStat.Key)) {
                                        teammateJobStatsLookup[playerLink.CurrentAlias].Add(jobStat.Key, new() {
                                            Matches = jobStat.Value.Matches,
                                        });
                                    } else {
                                        teammateJobStatsLookup[playerLink.CurrentAlias][jobStat.Key].Matches += jobStat.Value.Matches;
                                    }
                                }
                            } else {
                                teammateStats.Add(playerLink.CurrentAlias, teammateStats[linkedAlias]);
                                teammateJobStatsLookup.Add(playerLink.CurrentAlias, teammateJobStatsLookup[linkedAlias]);
                            }
                            teammateStats.Remove(linkedAlias);
                            teammateJobStatsLookup.Remove(linkedAlias);
                        }
                        if(opponentStats.ContainsKey(linkedAlias)) {
                            anyMatch = true;
                            if(opponentStats.ContainsKey(playerLink.CurrentAlias)) {
                                opponentStats[playerLink.CurrentAlias] += opponentStats[linkedAlias];
                                foreach(var jobStat in opponentJobStatsLookup[linkedAlias]) {
                                    if(!opponentJobStatsLookup[playerLink.CurrentAlias].ContainsKey(jobStat.Key)) {
                                        opponentJobStatsLookup[playerLink.CurrentAlias].Add(jobStat.Key, new() {
                                            Matches = jobStat.Value.Matches,
                                        });
                                    } else {
                                        opponentJobStatsLookup[playerLink.CurrentAlias][jobStat.Key].Matches += jobStat.Value.Matches;
                                    }
                                }
                            } else {
                                opponentStats.Add(playerLink.CurrentAlias, opponentStats[linkedAlias]);
                                opponentJobStatsLookup.Add(playerLink.CurrentAlias, opponentJobStatsLookup[linkedAlias]);
                            }
                            opponentStats.Remove(linkedAlias);
                            opponentJobStatsLookup.Remove(linkedAlias);
                        }
                        if(anyMatch) {
                            _plugin.Log.Verbose($"Coalescing {linkedAlias} into {playerLink.CurrentAlias}...");
                            if(activeLinks.ContainsKey(playerLink.CurrentAlias)) {
                                activeLinks[playerLink.CurrentAlias].Add(linkedAlias);
                            } else {
                                activeLinks.Add(playerLink.CurrentAlias, new() { linkedAlias });
                            }
                            if(activeLinks.ContainsKey(linkedAlias)) {
                                activeLinks[linkedAlias].Where(x => !x.Equals(playerLink.CurrentAlias)).ToList().ForEach(x => activeLinks[playerLink.CurrentAlias].Add(x));
                            }
                        }
                    }
                }
            };

            //auto links
            if(_plugin.Configuration.EnableAutoPlayerLinking) {
                foreach(var playerLink in _plugin.PlayerLinksService.AutoPlayerLinksCache) {
                    try {
                        checkPlayerLink(playerLink);
                    } catch(Exception e) {
                        _plugin.Log.Error($"Unable to add player link: {e.GetType()} {e.Message}\n {e.StackTrace}");
                    }
                }
            }

            //manual links
            if(_plugin.Configuration.EnableManualPlayerLinking) {
                foreach(var playerLink in _plugin.PlayerLinksService.ManualPlayerLinksCache) {
                    try {
                        checkPlayerLink(playerLink);
                    } catch(Exception e) {
                        _plugin.Log.Error($"Unable to add player link: {e.GetType()} {e.Message}\n {e.StackTrace}");
                    }
                }
            }
        }
        _plugin.CCStatsEngine.SetScoreboardStats(localPlayerStats, localPlayerTeamContributions);
        foreach(var teammateStat in teammateStats) {
            teammateStat.Value.Job = teammateJobStatsLookup[teammateStat.Key].OrderByDescending(x => x.Value.WinDiff).FirstOrDefault().Key;
        }
        foreach(var opponentStat in opponentStats) {
            opponentStat.Value.Job = opponentJobStatsLookup[opponentStat.Key].OrderBy(x => x.Value.WinDiff).FirstOrDefault().Key;
        }

        try {
            await RefreshLock.WaitAsync();
            LocalPlayerStats = localPlayerStats;
            LocalPlayerJobStats = localPlayerJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            TeammateStats = teammateStats.OrderBy(x => x.Value.Matches).OrderByDescending(x => x.Value.WinDiff).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            TeammateJobStats = teammateJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            OpponentStats = opponentStats.OrderBy(x => x.Value.Matches).OrderBy(x => x.Value.WinDiff).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            OpponentJobStats = opponentJobStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            ArenaStats = arenaStats.OrderByDescending(x => x.Value.Matches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            AverageMatchDuration = matches.Count > 0 ? totalMatchTime / matches.Count : TimeSpan.Zero;
        } finally {
            RefreshLock.Release();
        }
    }

    public void Draw() {
        if(!RefreshLock.Wait(0)) {
            return;
        }
        try {
            if(this.LocalPlayerStats.StatsAll.Matches > 0) {
                DrawResultTable();
            } else {
                ImGui.TextDisabled("No matches for given filters.");
            }

            if(this.LocalPlayerJobStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Jobs Played:");
                DrawJobTable(this.LocalPlayerJobStats);
            }

            if(this.LocalPlayerStats.StatsAll.Matches > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Average Performance:");
                ImGuiHelper.HelpMarker("1st row: average per match.\n2nd row: average per minute.\n3rd row: median team contribution per match.");
                DrawMatchStatsTable();
            }

            if(this.ArenaStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Arenas:");
                DrawArenaTable(this.ArenaStats);
            }

            if(this.TeammateJobStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Teammates' Jobs Played:");
                DrawJobTable(this.TeammateJobStats);
            }

            if(this.OpponentJobStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Opponents' Jobs Played:");
                DrawJobTable(this.OpponentJobStats);
            }

            if(this.TeammateStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Top Teammates:");
                DrawPlayerTable(this.TeammateStats);
            }

            if(this.OpponentStats.Count > 0) {
                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Top Opponents:");
                DrawPlayerTable(this.OpponentStats);
            }
        } finally {
            RefreshLock.Release();
        }
    }

    private void DrawResultTable() {
        using(var table = ImRaii.Table($"StatsSummary", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
                ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

                ImGui.TableNextColumn();
                ImGui.Text("Matches: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{this.LocalPlayerStats.StatsAll.Matches:N0}");
                ImGui.TableNextColumn();

                if(this.LocalPlayerStats.StatsAll.Matches > 0) {
                    ImGui.TableNextColumn();
                    ImGui.Text("Wins: ");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{this.LocalPlayerStats.StatsAll.Wins:N0}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{string.Format("{0:P}%", this.LocalPlayerStats.StatsAll.WinRate)}");

                    ImGui.TableNextColumn();
                    ImGui.Text("Losses: ");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{this.LocalPlayerStats.StatsAll.Losses:N0}");
                    ImGui.TableNextColumn();

                    if(this.LocalPlayerStats.StatsAll.OtherResult > 0) {
                        ImGui.TableNextColumn();
                        ImGui.Text("Other: ");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{this.LocalPlayerStats.StatsAll.OtherResult:N0}");
                        ImGui.TableNextColumn();
                    }
                    ImGui.TableNextRow();
                    ImGui.TableNextRow();
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Average match length: ");
                    ImGui.TableNextColumn();
                    ImGui.Text(ImGuiHelper.GetTimeSpanString(this.AverageMatchDuration));
                    ImGui.TableNextColumn();
                }
            }
        }
    }

    private void DrawArenaTable(Dictionary<CrystallineConflictMap, CCAggregateStats> arenaStats) {
        using(var table = ImRaii.Table($"ArenaTable", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Arena");
                ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableHeadersRow();
                foreach(var arena in arenaStats) {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{MatchHelper.GetArenaName(arena.Key)}");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{arena.Value.Matches}");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{arena.Value.Wins}");

                    ImGui.TableNextColumn();
                    if(arena.Value.Matches > 0) {
                        var diffColor = arena.Value.WinDiff > 0 ? ImGuiColors.HealerGreen : arena.Value.WinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
                        ImGui.TextColored(diffColor, $"{string.Format("{0:P}%", arena.Value.WinRate)}");
                        //ImGui.Text($"{string.Format("{0:P}%", arena.Value.WinRate)}");
                    }
                }
            }
        }
    }

    private void DrawJobTable(Dictionary<Job, CCAggregateStats> jobStats) {
        using(var table = ImRaii.Table($"JobTable", 5, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Job");
                ImGui.TableSetupColumn($"Role", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Win Rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableHeadersRow();
                foreach(var job in jobStats) {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{PlayerJobHelper.GetNameFromJob(job.Key)}");

                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiHelper.GetJobColor(job.Key), $"{PlayerJobHelper.GetSubRoleFromJob(job.Key)}");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{job.Value.Matches}");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{job.Value.Wins}");

                    ImGui.TableNextColumn();
                    if(job.Value.Matches > 0) {
                        var diffColor = job.Value.WinDiff > 0 ? ImGuiColors.HealerGreen : job.Value.WinDiff < 0 ? ImGuiColors.DPSRed : ImGuiColors.DalamudWhite;
                        ImGui.TextColored(diffColor, $"{string.Format("{0:P}%", job.Value.WinRate)}");
                    }
                }
            }
        }
    }

    private void DrawPlayerTable(Dictionary<PlayerAlias, CCAggregateStats> playerStats) {
        using(var table = ImRaii.Table($"PlayerTable", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Player");
                ImGui.TableSetupColumn($"Favored\nJob", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Matches", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableSetupColumn($"Wins", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                ImGui.TableHeadersRow();

                for(int i = 0; i < playerStats.Count && i < 5; i++) {
                    var player = playerStats.ElementAt(i);
                    ImGui.TableNextColumn();
                    ImGui.Text(player.Key.Name);

                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiHelper.GetJobColor(player.Value.Job), player.Value.Job.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.Value.Matches}");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.Value.Wins}");
                }
            }
        }
    }

    private void DrawMatchStatsTable() {
        using(var table = ImRaii.Table($"MatchStatsTable", 7, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Kills");
                ImGui.TableSetupColumn($"Deaths");
                ImGui.TableSetupColumn($"Assists");
                ImGui.TableSetupColumn("Damage\nDealt");
                ImGui.TableSetupColumn($"Damage\nTaken");
                ImGui.TableSetupColumn($"HP\nRestored");
                ImGui.TableSetupColumn("Time on\nCrystal");

                ImGui.TableHeadersRow();

                //per match
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMatch.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 1.0f, 4.5f, _plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMatch.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 1.5f, 3.5f, _plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMatch.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 5.0f, 8.0f, _plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMatch.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 400000f, 900000f, _plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMatch.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 400000f, 900000f, _plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMatch.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 300000f, 1000000f, _plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                var tcpa = this.LocalPlayerStats.ScoreboardPerMatch.TimeOnCrystal;
                if(_plugin.Configuration.ColorScaleStats) {
                    ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 30f, 120f, (float)tcpa.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpa));
                } else {
                    ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(tcpa));
                }

                //per min
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMin.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.7f, _plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMin.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 0.2f, 0.5f, _plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMin.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.7f, 1.5f, _plugin.Configuration.ColorScaleStats, "0.00");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMin.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 70000f, 150000f, _plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMin.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 70000f, 150000f, _plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardPerMin.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 50000f, 200000f, _plugin.Configuration.ColorScaleStats, "#");
                ImGui.TableNextColumn();
                var tcpm = this.LocalPlayerStats.ScoreboardPerMin.TimeOnCrystal;
                if(_plugin.Configuration.ColorScaleStats) {
                    ImGui.TextColored(ImGuiHelper.ColorScale(ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 4f, 25f, (float)tcpm.TotalSeconds), ImGuiHelper.GetTimeSpanString(tcpm));
                } else {
                    ImGui.TextUnformatted(ImGuiHelper.GetTimeSpanString(tcpm));
                }

                //team contrib
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardContrib.Kills, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardContrib.Deaths, ImGuiColors.HealerGreen, ImGuiColors.DPSRed, 0.15f, 0.25f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardContrib.Assists, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardContrib.DamageDealt, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardContrib.DamageTaken, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardContrib.HPRestored, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)this.LocalPlayerStats.ScoreboardContrib.TimeOnCrystalDouble, ImGuiColors.DPSRed, ImGuiColors.HealerGreen, 0.1f, 0.3f, _plugin.Configuration.ColorScaleStats, "{0:P1}%", true);
            }
        }
    }
}
