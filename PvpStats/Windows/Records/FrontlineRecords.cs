using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.Records;
internal class FrontlineRecords : MatchRecords<FrontlineMatch> {

    public override string Name => "FL Records";

    public FrontlineRecords(Plugin plugin) : base(plugin) {
    }

    protected override Task RefreshInner(List<FrontlineMatch> matches, List<FrontlineMatch> additions, List<FrontlineMatch> removals) {
        Dictionary<FrontlineMatch, List<(string, string)>> superlatives = new();
        FrontlineMatch? longestMatch = null, shortestMatch = null, longestWinStreakMatch = null, longestLossStreakMatch = null, mostCombatPointsMatch = null, lowestCombatPointsMatch = null,
            mostKillsMatch = null, mostDeathsMatch = null, mostAssistsMatch = null, mostDamageToPCsMatch = null, mostDamageToOtherMatch = null, mostDamageTakenMatch = null, mostHPRestoredMatch = null,
            mostKillsPerMinMatch = null, mostDeathsPerMinMatch = null, mostAssistsPerMinMatch = null, mostDamageToPCsPerMinMatch = null, mostDamageToOtherPerMinMatch = null, mostDamageTakenPerMinMatch = null, mostHPRestoredPerMinMatch = null;

        long mostKills = 0, mostDeaths = 0, mostAssists = 0, mostDamageToPCs = 0, mostDamageToOther = 0, mostDamageTaken = 0, mostHPRestored = 0, mostCombatPoints = 0, lowestCombatPoints = long.MaxValue;
        double mostKillsPerMin = 0, mostDeathsPerMin = 0, mostAssistsPerMin = 0, mostDamageToPCsPerMin = 0, mostDamageToOtherPerMin = 0, mostDamageTakenPerMin = 0, mostHPRestoredPerMin = 0;

        int longestWinStreak = 0, longestLossStreak = 0, currentWinStreak = 0, currentLossStreak = 0;

        MatchesTotal = matches.Count;

        foreach(var match in matches.Where(x => x.LocalPlayer != null && x.PlayerScoreboards != null)) {
            FrontlineTeamScoreboard teamScoreboard = match.Teams[(FrontlineTeamName)match.LocalPlayerTeam!];
            FLScoreboardTally playerScoreboard = new FLScoreboardTally(match.PlayerScoreboards[match.LocalPlayer!]);
            FLScoreboardDouble playerScoreboardPerMin = (FLScoreboardDouble)playerScoreboard / match.MatchDuration.Value.TotalMinutes;

            if(match.Result == 0) {
                currentWinStreak++;
                if(currentWinStreak > longestWinStreak) {
                    longestWinStreakMatch = match;
                    longestWinStreak = currentWinStreak;
                }
            } else {
                currentWinStreak = 0;
            }
            if(match.Result != 0) {
                currentLossStreak++;
                if(currentLossStreak > longestLossStreak) {
                    longestLossStreakMatch = match;
                    longestLossStreak = currentLossStreak;
                }
            } else {
                currentLossStreak = 0;
            }

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

            CompareValue(match, ref mostCombatPointsMatch, teamScoreboard.KillPointsDiff, ref mostCombatPoints);
            CompareValue(match, ref lowestCombatPointsMatch, teamScoreboard.KillPointsDiff, ref lowestCombatPoints, true);

            CompareValue(match, ref mostKillsMatch, playerScoreboard.Kills, ref mostKills);
            CompareValue(match, ref mostDeathsMatch, playerScoreboard.Deaths, ref mostDeaths);
            CompareValue(match, ref mostAssistsMatch, playerScoreboard.Assists, ref mostAssists);
            CompareValue(match, ref mostDamageToPCsMatch, playerScoreboard.DamageToPCs, ref mostDamageToPCs);
            CompareValue(match, ref mostDamageToOtherMatch, playerScoreboard.DamageToOther, ref mostDamageToOther);
            CompareValue(match, ref mostDamageTakenMatch, playerScoreboard.DamageTaken, ref mostDamageTaken);
            CompareValue(match, ref mostHPRestoredMatch, playerScoreboard.HPRestored, ref mostHPRestored);

            CompareValue(match, ref mostKillsPerMinMatch, playerScoreboardPerMin.Kills, ref mostKillsPerMin);
            CompareValue(match, ref mostDeathsPerMinMatch, playerScoreboardPerMin.Deaths, ref mostDeathsPerMin);
            CompareValue(match, ref mostAssistsPerMinMatch, playerScoreboardPerMin.Assists, ref mostAssistsPerMin);
            CompareValue(match, ref mostDamageToPCsPerMinMatch, playerScoreboardPerMin.DamageToPCs, ref mostDamageToPCsPerMin);
            CompareValue(match, ref mostDamageToOtherPerMinMatch, playerScoreboardPerMin.DamageToOther, ref mostDamageToOtherPerMin);
            CompareValue(match, ref mostDamageTakenPerMinMatch, playerScoreboardPerMin.DamageTaken, ref mostDamageTakenPerMin);
            CompareValue(match, ref mostHPRestoredPerMinMatch, playerScoreboardPerMin.HPRestored, ref mostHPRestoredPerMin);

            RefreshProgress = (float)MatchesProcessed++ / MatchesTotal;
        }

        Superlatives = new();
        AddSuperlative(longestWinStreakMatch, "Longest win streak", longestWinStreak.ToString());
        AddSuperlative(longestLossStreakMatch, "Longest loss streak", longestLossStreak.ToString());
        AddSuperlative(longestMatch, "Longest match", ImGuiHelper.GetTimeSpanString(longestMatch?.MatchDuration ?? TimeSpan.Zero));
        AddSuperlative(shortestMatch, "Shortest match", ImGuiHelper.GetTimeSpanString(shortestMatch?.MatchDuration ?? TimeSpan.Zero));
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
        AddSuperlative(mostKillsPerMinMatch, "Most kills per min", mostKillsPerMin.ToString("0.00"));
        AddSuperlative(mostDeathsPerMinMatch, "Most deaths per min", mostDeathsPerMin.ToString("0.00"));
        AddSuperlative(mostAssistsPerMinMatch, "Most assists per min", mostAssistsPerMin.ToString("0.00"));
        AddSuperlative(mostDamageToPCsPerMinMatch, "Most damage to PCs per min", mostDamageToPCsPerMin.ToString("0"));
        if(mostDamageToOtherPerMin > 0) {
            AddSuperlative(mostDamageToOtherPerMinMatch, "Most damage to other per min", mostDamageToOtherPerMin.ToString("0"));
        }
        AddSuperlative(mostDamageTakenPerMinMatch, "Most damage taken per min", mostDamageTakenPerMin.ToString("0"));
        AddSuperlative(mostHPRestoredPerMinMatch, "Most HP restored per min", mostHPRestoredPerMin.ToString("0"));
        return Task.CompletedTask;
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
