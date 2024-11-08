using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PvpStats.Types.Display;

namespace PvpStats.Windows.Records;
internal class FrontlineRecords : MatchRecords<FrontlineMatch> {

    public override string Name => "FL Records";

    internal int LongestWinStreak { get; private set; }
    internal int LongestLossStreak { get; private set; }

    public FrontlineRecords(Plugin plugin) : base(plugin) {
    }

    protected override Task RefreshInner(List<FrontlineMatch> matches, List<FrontlineMatch> additions, List<FrontlineMatch> removals) {
        Dictionary<FrontlineMatch, List<(string, string)>> superlatives = new();
        FrontlineMatch? longestMatch = null, shortestMatch = null, mostCombatPointsMatch = null, lowestCombatPointsMatch = null,
            mostKillsMatch = null, mostDeathsMatch = null, mostAssistsMatch = null, mostDamageToPCsMatch = null, mostDamageToOtherMatch = null, mostDamageTakenMatch = null, mostHPRestoredMatch = null,
            mostKillsPerMinMatch = null, mostDeathsPerMinMatch = null, mostAssistsPerMinMatch = null, mostDamageToPCsPerMinMatch = null, mostDamageToOtherPerMinMatch = null, mostDamageTakenPerMinMatch = null, mostHPRestoredPerMinMatch = null;

        long mostKills = 0, mostDeaths = 0, mostAssists = 0, mostDamageToPCs = 0, mostDamageToOther = 0, mostDamageTaken = 0, mostHPRestored = 0, mostCombatPoints = 0, lowestCombatPoints = long.MaxValue;
        double mostKillsPerMin = 0, mostDeathsPerMin = 0, mostAssistsPerMin = 0, mostDamageToPCsPerMin = 0, mostDamageToOtherPerMin = 0, mostDamageTakenPerMin = 0, mostHPRestoredPerMin = 0;

        int longestWinStreak = 0, longestLossStreak = 0, currentWinStreak = 0, currentLossStreak = 0;

        MatchesTotal = matches.Count;

        foreach(var match in matches.Where(x => x.LocalPlayer != null && x.PlayerScoreboards != null)) {
            FrontlineTeamScoreboard teamScoreboard = match.Teams[(FrontlineTeamName)match.LocalPlayerTeam!];
            FLScoreboardTally playerScoreboard = new FLScoreboardTally(match.PlayerScoreboards[match.LocalPlayer!]);
            FLScoreboardDouble playerScoreboardPerMin = (FLScoreboardDouble)playerScoreboard / match.MatchDuration!.Value.TotalMinutes;

            //track these for spectated matches as well
            if(longestMatch == null) {
                longestMatch = match;
                shortestMatch = match;
            }
            if(longestMatch == null || match.MatchDuration > longestMatch.MatchDuration) {
                longestMatch = match;
            }
            if(shortestMatch == null || match.MatchDuration < shortestMatch.MatchDuration) {
                shortestMatch = match;
            }

            if(mostCombatPointsMatch == null || teamScoreboard.KillPointsDiff > mostCombatPoints || (teamScoreboard.KillPointsDiff == mostCombatPoints && match.MatchDuration < mostCombatPointsMatch.MatchDuration)) {
                mostCombatPointsMatch = match;
                mostCombatPoints = teamScoreboard.KillPointsDiff;
            }

            if(lowestCombatPointsMatch == null || teamScoreboard.KillPointsDiff < lowestCombatPoints || (teamScoreboard.KillPointsDiff == lowestCombatPoints && match.MatchDuration < lowestCombatPointsMatch.MatchDuration)) {
                lowestCombatPointsMatch = match;
                lowestCombatPoints = teamScoreboard.KillPointsDiff;
            }

            //raw
            if(mostKillsMatch == null || playerScoreboard?.Kills > mostKills || (playerScoreboard?.Kills == mostKills && match.MatchDuration < mostKillsMatch.MatchDuration)) {
                mostKillsMatch = match;
                mostKills = playerScoreboard.Kills;
            }

            if(mostDeathsMatch == null || playerScoreboard?.Deaths > mostDeaths || (playerScoreboard?.Deaths == mostDeaths && match.MatchDuration < mostDeathsMatch.MatchDuration)) {
                mostDeathsMatch = match;
                mostDeaths = playerScoreboard.Deaths;
            }

            if(mostAssistsMatch == null || playerScoreboard?.Assists > mostAssists || (playerScoreboard?.Assists == mostAssists && match.MatchDuration < mostAssistsMatch.MatchDuration)) {
                mostAssistsMatch = match;
                mostAssists = playerScoreboard.Assists;
            }

            if(mostDamageToPCsMatch == null || playerScoreboard?.DamageToPCs > mostDamageToPCs || (playerScoreboard?.DamageToPCs == mostDamageToPCs && match.MatchDuration < mostDamageToPCsMatch.MatchDuration)) {
                mostDamageToPCsMatch = match;
                mostDamageToPCs = playerScoreboard.DamageToPCs;
            }

            if(mostDamageTakenMatch == null || playerScoreboard?.DamageTaken > mostDamageTaken || (playerScoreboard?.DamageTaken == mostDamageTaken && match.MatchDuration < mostDamageTakenMatch.MatchDuration)) {
                mostDamageTakenMatch = match;
                mostDamageTaken = playerScoreboard.DamageTaken;
            }

            if(mostDamageToOtherMatch == null || playerScoreboard?.DamageToOther > mostDamageToOther || (playerScoreboard?.DamageToOther == mostDamageToOther && match.MatchDuration < mostDamageToOtherMatch.MatchDuration)) {
                mostDamageToOtherMatch = match;
                mostDamageToOther = playerScoreboard.DamageToOther;
            }

            if(mostHPRestoredMatch == null || playerScoreboard?.HPRestored > mostHPRestored || (playerScoreboard?.HPRestored == mostHPRestored && match.MatchDuration < mostHPRestoredMatch.MatchDuration)) {
                mostHPRestoredMatch = match;
                mostHPRestored = playerScoreboard.HPRestored;
            }

            //per min
            if(mostKillsPerMinMatch == null || playerScoreboardPerMin?.Kills > mostKillsPerMin) {
                mostKillsPerMinMatch = match;
                mostKillsPerMin = playerScoreboardPerMin.Kills;
            }

            if(mostDeathsPerMinMatch == null || playerScoreboardPerMin?.Deaths > mostDeathsPerMin) {
                mostDeathsPerMinMatch = match;
                mostDeathsPerMin = playerScoreboardPerMin.Deaths;
            }

            if(mostAssistsPerMinMatch == null || playerScoreboardPerMin?.Assists > mostAssistsPerMin) {
                mostAssistsPerMinMatch = match;
                mostAssistsPerMin = playerScoreboardPerMin.Assists;
            }

            if(mostDamageToPCsPerMinMatch == null || playerScoreboardPerMin?.DamageToPCs > mostDamageToPCsPerMin) {
                mostDamageToPCsPerMinMatch = match;
                mostDamageToPCsPerMin = playerScoreboardPerMin.DamageToPCs;
            }

            if(mostDamageToOtherPerMinMatch == null || playerScoreboardPerMin?.DamageToOther > mostDamageToOtherPerMin) {
                mostDamageToOtherPerMinMatch = match;
                mostDamageToOtherPerMin = playerScoreboardPerMin.DamageToOther;
            }

            if(mostDamageTakenPerMinMatch == null || playerScoreboardPerMin?.DamageTaken > mostDamageTakenPerMin) {
                mostDamageTakenPerMinMatch = match;
                mostDamageTakenPerMin = playerScoreboardPerMin.DamageTaken;
            }

            if(mostHPRestoredPerMinMatch == null || playerScoreboardPerMin?.HPRestored > mostHPRestoredPerMin) {
                mostHPRestoredPerMinMatch = match;
                mostHPRestoredPerMin = playerScoreboardPerMin.HPRestored;
            }


            if(match.Result == 0) {
                currentWinStreak++;
                if(currentWinStreak > longestWinStreak) {
                    longestWinStreak = currentWinStreak;
                }
            } else {
                currentWinStreak = 0;
            }
            if(match.Result != 0) {
                currentLossStreak++;
                if(currentLossStreak > longestLossStreak) {
                    longestLossStreak = currentLossStreak;
                }
            } else {
                currentLossStreak = 0;
            }
            RefreshProgress = (float)MatchesProcessed++ / MatchesTotal;
        }

        LongestWinStreak = longestWinStreak;
        LongestLossStreak = longestLossStreak;

        Superlatives = new();
        if(longestMatch != null) {
            AddSuperlative(longestMatch, "Longest match", ImGuiHelper.GetTimeSpanString((TimeSpan)longestMatch.MatchDuration!));
            AddSuperlative(shortestMatch, "Shortest match", ImGuiHelper.GetTimeSpanString((TimeSpan)shortestMatch!.MatchDuration!));
            if(mostKillsMatch != null) {
                AddSuperlative(mostCombatPointsMatch, "Highest team combat point diff.", mostCombatPoints.ToString());
                AddSuperlative(lowestCombatPointsMatch, "Lowest team combat point diff.", lowestCombatPoints.ToString());

                AddSuperlative(mostKillsMatch, "Most kills", mostKills.ToString());
                AddSuperlative(mostDeathsMatch, "Most deaths", mostDeaths.ToString());
                AddSuperlative(mostAssistsMatch, "Most assists", mostAssists.ToString());
                AddSuperlative(mostDamageToPCsMatch, "Most damage to PCs", mostDamageToPCs.ToString());
                if(mostDamageToOther > 0) {
                    AddSuperlative(mostDamageToOtherMatch, "Most damage to other", mostDamageToOther.ToString());
                }
                AddSuperlative(mostDamageTakenMatch, "Most damage taken", mostDamageTaken.ToString());
                AddSuperlative(mostHPRestoredMatch, "Most HP restored", mostHPRestored.ToString());
                AddSuperlative(mostKillsPerMinMatch, "Highest kills per min", mostKillsPerMin.ToString("0.00"));
                AddSuperlative(mostDeathsPerMinMatch, "Highest deaths per min", mostDeathsPerMin.ToString("0.00"));
                AddSuperlative(mostAssistsPerMinMatch, "Highest assists per min", mostAssistsPerMin.ToString("0.00"));
                AddSuperlative(mostDamageToPCsPerMinMatch, "Highest damage to PCs per min", mostDamageToPCsPerMin.ToString("0"));
                if(mostDamageToOtherPerMin > 0) {
                    AddSuperlative(mostDamageToOtherPerMinMatch, "Highest damage to other per min", mostDamageToOtherPerMin.ToString("0"));
                }
                AddSuperlative(mostDamageTakenPerMinMatch, "Highest damage taken per min", mostDamageTakenPerMin.ToString("0"));
                AddSuperlative(mostHPRestoredPerMinMatch, "Highest HP restored per min", mostHPRestoredPerMin.ToString("0"));
            }
        }
        return Task.CompletedTask;
    }

    public void Draw() {
        using(var table = ImRaii.Table("streaks", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("title", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 185f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);

                ImGui.TableNextColumn();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Longest win streak:");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(LongestWinStreak.ToString());
                ImGui.TableNextColumn();
                ImGui.TextColored(Plugin.Configuration.Colors.Header, "Longest loss streak:");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(LongestLossStreak.ToString());
            }
        }
        ImGui.Separator();
        foreach(var match in Superlatives) {
            var x = match.Value;
            DrawStat(match.Key, match.Value.Select(x => x.Item1).ToArray(), match.Value.Select(x => x.Item2).ToArray());
            ImGui.Separator();
        }
    }

    protected override void DrawMatchStat(FrontlineMatch match) {
        using(var table = ImRaii.Table("match", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 100f);
                ImGui.TableSetupColumn("Arena", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 145f);
                ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);
                ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);

                ImGui.TableNextColumn();
                ImGui.Text($"{match.DutyStartTime:yyyy-MM-dd HH:mm}");
                ImGui.TableNextColumn();
                if(match.Arena != null) {
                    ImGui.Text(MatchHelper.GetFrontlineArenaName(match.Arena));
                }
                ImGui.TableNextColumn();
                var localPlayerJob = match.LocalPlayerTeamMember!.Job;
                ImGuiHelper.CenterAlignCursor(localPlayerJob.ToString() ?? "");
                ImGui.TextColored(Plugin.Configuration.GetJobColor(localPlayerJob), localPlayerJob.ToString());
                ImGui.TableNextColumn();
                var color = match.Result switch {
                    0 => Plugin.Configuration.Colors.Win,
                    2 => Plugin.Configuration.Colors.Loss,
                    _ => Plugin.Configuration.Colors.Other,
                };
                string resultText = match.Result != null ? ImGuiHelper.AddOrdinal((int)match.Result + 1).ToUpper() : "???";
                ImGuiHelper.CenterAlignCursor(resultText);
                ImGui.TextColored(color, resultText);
            }
        }
    }
}
