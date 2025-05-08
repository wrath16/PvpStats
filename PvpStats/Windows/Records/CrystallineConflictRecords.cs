using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Windows.Records;
internal class CrystallineConflictRecords : MatchRecords<CrystallineConflictMatch> {

    public override string Name => "CC Records";

    internal CrystallineConflictRecords(Plugin plugin) : base(plugin) {
    }

    protected override Task RefreshInner(List<CrystallineConflictMatch> matches, List<CrystallineConflictMatch> additions, List<CrystallineConflictMatch> removals) {
        Dictionary<CrystallineConflictMatch, List<(string, string)>> superlatives = new();
        CrystallineConflictMatch? longestMatch = null, shortestMatch = null, highestLoserProgMatch = null, lowestWinnerProgMatch = null, longestWinStreakMatch = null, longestLossStreakMatch = null,
            mostKillsMatch = null, mostDeathsMatch = null, mostAssistsMatch = null, mostDamageDealtMatch = null, mostDamageTakenMatch = null, mostHPRestoredMatch = null, mostTimeOnCrystalMatch = null,
            highestKillsPerMinMatch = null, highestDeathsPerMinMatch = null, highestAssistsPerMinMatch = null, highestDamageDealtPerMinMatch = null, highestDamageTakenPerMinMatch = null, highestHPRestoredPerMinMatch = null, highestTimeOnCrystalPerMinMatch = null;
        int longestWinStreak = 0, longestLossStreak = 0, currentWinStreak = 0, currentLossStreak = 0;
        long mostKills = 0, mostDeaths = 0, mostAssists = 0, mostDamageDealt = 0, mostDamageTaken = 0, mostHPRestored = 0;
        TimeSpan mostTimeOnCrystal = TimeSpan.Zero;
        double mostKillsPerMin = 0, mostDeathsPerMin = 0, mostAssistsPerMin = 0, mostDamageDealtPerMin = 0, mostDamageTakenPerMin = 0, mostHPRestoredPerMin = 0, mostTimeOnCrystalPerMin = 0;

        MatchesTotal = matches.Count;

        foreach(var match in matches) {
            //track these for spectated matches as well
            if(longestMatch == null) {
                longestMatch = match;
                shortestMatch = match;
                highestLoserProgMatch = match;
            }
            if(longestMatch == null || match.MatchDuration > longestMatch.MatchDuration) {
                longestMatch = match;
            }
            if(shortestMatch == null || match.MatchDuration < shortestMatch.MatchDuration) {
                shortestMatch = match;
            }
            if(highestLoserProgMatch == null || match.LoserProgress > highestLoserProgMatch.LoserProgress) {
                highestLoserProgMatch = match;
            }
            if(lowestWinnerProgMatch == null || match.WinnerProgress < lowestWinnerProgMatch.WinnerProgress) {
                lowestWinnerProgMatch = match;
            }

            if(match.IsWin) {
                currentWinStreak++;
                if(currentWinStreak > longestWinStreak) {
                    longestWinStreakMatch = match;
                    longestWinStreak = currentWinStreak;
                }
            } else {
                currentWinStreak = 0;
            }
            if(match.IsLoss) {
                currentLossStreak++;
                if(currentLossStreak > longestLossStreak) {
                    longestLossStreakMatch = match;
                    longestLossStreak = currentLossStreak;
                }
            } else {
                currentLossStreak = 0;
            }

            if(match.IsSpectated) {
                //spectatedMatchCount++;
                //continue;
            } else {
                if(match.MatchDuration == null || match.PostMatch == null) {
                    continue;
                }

                CCScoreboardTally playerScoreboard = match.LocalPlayerStats.ToScoreboard();
                CCScoreboardDouble playerScoreboardPerMin = (CCScoreboardDouble)playerScoreboard / match.MatchDuration.Value.TotalMinutes;

                CompareValue(match, ref mostKillsMatch, playerScoreboard.Kills, ref mostKills);
                CompareValue(match, ref mostDeathsMatch, playerScoreboard.Deaths, ref mostDeaths);
                CompareValue(match, ref mostAssistsMatch, playerScoreboard.Assists, ref mostAssists);
                CompareValue(match, ref mostDamageDealtMatch, playerScoreboard.DamageDealt, ref mostDamageDealt);
                CompareValue(match, ref mostDamageTakenMatch, playerScoreboard.DamageTaken, ref mostDamageTaken);
                CompareValue(match, ref mostHPRestoredMatch, playerScoreboard.HPRestored, ref mostHPRestored);
                CompareValue(match, ref mostTimeOnCrystalMatch, playerScoreboard.TimeOnCrystal, ref mostTimeOnCrystal);

                CompareValue(match, ref highestKillsPerMinMatch, playerScoreboardPerMin.Kills, ref mostKillsPerMin);
                CompareValue(match, ref highestDeathsPerMinMatch, playerScoreboardPerMin.Deaths, ref mostDeathsPerMin);
                CompareValue(match, ref highestAssistsPerMinMatch, playerScoreboardPerMin.Assists, ref mostAssistsPerMin);
                CompareValue(match, ref highestDamageDealtPerMinMatch, playerScoreboardPerMin.DamageDealt, ref mostDamageDealtPerMin);
                CompareValue(match, ref highestDamageTakenPerMinMatch, playerScoreboardPerMin.DamageTaken, ref mostDamageTakenPerMin);
                CompareValue(match, ref highestHPRestoredPerMinMatch, playerScoreboardPerMin.HPRestored, ref mostHPRestoredPerMin);
                CompareValue(match, ref highestTimeOnCrystalPerMinMatch, playerScoreboardPerMin.TimeOnCrystal, ref mostTimeOnCrystalPerMin);
            }
            RefreshProgress = (float)MatchesProcessed++ / MatchesTotal;
        }

        Superlatives = new();
        AddSuperlative(longestWinStreakMatch, "Longest win streak", longestWinStreak.ToString());
        AddSuperlative(longestLossStreakMatch, "Longest loss streak", longestLossStreak.ToString());
        AddSuperlative(longestMatch, "Longest match", ImGuiHelper.GetTimeSpanString(longestMatch?.MatchDuration ?? TimeSpan.Zero));
        AddSuperlative(shortestMatch, "Shortest match", ImGuiHelper.GetTimeSpanString(shortestMatch?.MatchDuration ?? TimeSpan.Zero));
        AddSuperlative(highestLoserProgMatch, "Highest loser progress", highestLoserProgMatch?.LoserProgress.ToString() ?? "");
        AddSuperlative(lowestWinnerProgMatch, "Lowest winner progress", lowestWinnerProgMatch?.WinnerProgress.ToString() ?? "");
        AddSuperlative(mostKillsMatch, "Most kills", mostKills.ToString());
        AddSuperlative(mostDeathsMatch, "Most deaths", mostDeaths.ToString());
        AddSuperlative(mostAssistsMatch, "Most assists", mostAssists.ToString());
        AddSuperlative(mostDamageDealtMatch, "Most damage dealt", mostDamageDealt.ToString());
        AddSuperlative(mostDamageTakenMatch, "Most damage taken", mostDamageTaken.ToString());
        AddSuperlative(mostHPRestoredMatch, "Most HP restored", mostHPRestored.ToString());
        AddSuperlative(mostTimeOnCrystalMatch, "Longest time on crystal", ImGuiHelper.GetTimeSpanString(mostTimeOnCrystal));
        AddSuperlative(highestKillsPerMinMatch, "Most kills per min", mostKillsPerMin.ToString("0.00"));
        AddSuperlative(highestDeathsPerMinMatch, "Most deaths per min", mostDeathsPerMin.ToString("0.00"));
        AddSuperlative(highestAssistsPerMinMatch, "Most assists per min", mostAssistsPerMin.ToString("0.00"));
        AddSuperlative(highestDamageDealtPerMinMatch, "Most damage dealt per min", mostDamageDealtPerMin.ToString("0"));
        AddSuperlative(highestDamageTakenPerMinMatch, "Most damage taken per min", mostDamageTakenPerMin.ToString("0"));
        AddSuperlative(highestHPRestoredPerMinMatch, "Most HP restored per min", mostHPRestoredPerMin.ToString("0"));
        AddSuperlative(highestTimeOnCrystalPerMinMatch, "Longest time on crystal per min", ImGuiHelper.GetTimeSpanString(TimeSpan.FromSeconds(mostTimeOnCrystalPerMin)));
        return Task.CompletedTask;
    }

    protected override void DrawMatchStat(CrystallineConflictMatch match) {
        using(var table = ImRaii.Table("match", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                var widthStyle = Plugin.Configuration.StretchScoreboardColumns ?? false ? ImGuiTableColumnFlags.WidthStretch : ImGuiTableColumnFlags.WidthFixed;
                ImGui.TableSetupColumn("Time", widthStyle, ImGuiHelpers.GlobalScale * 110f);
                ImGui.TableSetupColumn("Arena", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 155f);
                ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);
                ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);

                ImGui.TableNextColumn();
                ImGui.Text($"{match.DutyStartTime:yyyy-MM-dd HH:mm}");
                ImGui.TableNextColumn();
                if(match.Arena != null) {
                    ImGui.Text(MatchHelper.GetArenaName((CrystallineConflictMap)match.Arena));
                }
                ImGui.TableNextColumn();
                if(!match.IsSpectated) {
                    var localPlayerJob = match.LocalPlayerTeamMember!.Job;
                    ImGuiHelper.CenterAlignCursor(localPlayerJob.ToString() ?? "");
                    ImGui.TextColored(Plugin.Configuration.GetJobColor(localPlayerJob), localPlayerJob.ToString());
                    ImGui.TableNextColumn();
                    var color = match.IsWin ? Plugin.Configuration.Colors.Win : match.IsLoss ? Plugin.Configuration.Colors.Loss : Plugin.Configuration.Colors.Other;
                    var result = match.IsWin ? "WIN" : match.IsLoss ? "LOSS" : "???";
                    ImGuiHelper.CenterAlignCursor(result);
                    ImGui.TextColored(color, result);
                } else {
                    ImGui.Text($"Spectated");
                }
            }
        }
    }
}
