using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Records;
internal class RivalWingsRecords : MatchRecords<RivalWingsMatch> {
    public override string Name => "RW Records";

    internal RivalWingsRecords(Plugin plugin) : base(plugin) {
    }

    protected override Task RefreshInner(List<RivalWingsMatch> matches, List<RivalWingsMatch> additions, List<RivalWingsMatch> removals) {
        Dictionary<RivalWingsMatch, List<(string, string)>> superlatives = new();
        RivalWingsMatch? shortestMatch = null, highestLoserProgMatch = null, lowestWinnerProgMatch = null, longestWinStreakMatch = null, longestLossStreakMatch = null,
            mostKillsMatch = null, mostDeathsMatch = null, mostAssistsMatch = null, mostDamageToPCsMatch = null, mostDamageToOtherMatch = null, mostDamageTakenMatch = null, mostHPRestoredMatch = null, mostCeruleumMatch = null,
            highestKillsPerMinMatch = null, highestDeathsPerMinMatch = null, highestAssistsPerMinMatch = null, highestDamageToPCsPerMinMatch = null, highestDamageToOtherPerMinMatch = null, highestDamageTakenPerMinMatch = null, highestHPRestoredPerMinMatch = null, highestCeruleumPerMinMatch = null,
            fastestFlyingHighMatch = null;
        int longestWinStreak = 0, longestLossStreak = 0, currentWinStreak = 0, currentLossStreak = 0;
        long mostKills = 0, mostDeaths = 0, mostAssists = 0, mostDamageToPCs = 0, mostDamageToOther = 0, mostDamageTaken = 0, mostHPRestored = 0, mostCeruleum = 0;
        double mostKillsPerMin = 0, mostDeathsPerMin = 0, mostAssistsPerMin = 0, mostDamageToPCsPerMin = 0, mostDamageToOtherPerMin = 0, mostDamageTakenPerMin = 0, mostHPRestoredPerMin = 0, mostCeruleumPerMin = 0;
        TimeSpan fastestFlyingHigh = TimeSpan.MaxValue;

        MatchesTotal = matches.Count;

        foreach(var match in matches) {
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

            if(match.MatchDuration != null && match.PlayerScoreboards != null && match.LocalPlayer != null) {
                var playerScoreboard = new RWScoreboardTally(match.PlayerScoreboards[match.LocalPlayer]);
                RWScoreboardDouble playerScoreboardPerMin = (RWScoreboardDouble)playerScoreboard / match.MatchDuration.Value.TotalMinutes;

                CompareValue(match, ref mostKillsMatch, playerScoreboard.Kills, ref mostKills);
                CompareValue(match, ref mostDeathsMatch, playerScoreboard.Deaths, ref mostDeaths);
                CompareValue(match, ref mostAssistsMatch, playerScoreboard.Assists, ref mostAssists);
                CompareValue(match, ref mostDamageToPCsMatch, playerScoreboard.DamageToPCs, ref mostDamageToPCs);
                CompareValue(match, ref mostDamageToOtherMatch, playerScoreboard.DamageToOther, ref mostDamageToOther);
                CompareValue(match, ref mostDamageTakenMatch, playerScoreboard.DamageTaken, ref mostDamageTaken);
                CompareValue(match, ref mostHPRestoredMatch, playerScoreboard.HPRestored, ref mostHPRestored);
                CompareValue(match, ref mostCeruleumMatch, playerScoreboard.Ceruleum, ref mostCeruleum);

                CompareValue(match, ref highestKillsPerMinMatch, playerScoreboardPerMin.Kills, ref mostKillsPerMin);
                CompareValue(match, ref highestDeathsPerMinMatch, playerScoreboardPerMin.Deaths, ref mostDeathsPerMin);
                CompareValue(match, ref highestAssistsPerMinMatch, playerScoreboardPerMin.Assists, ref mostAssistsPerMin);
                CompareValue(match, ref highestDamageToPCsPerMinMatch, playerScoreboardPerMin.DamageToPCs, ref mostDamageToPCsPerMin);
                CompareValue(match, ref highestDamageToOtherPerMinMatch, playerScoreboardPerMin.DamageToOther, ref mostDamageToOtherPerMin);
                CompareValue(match, ref highestDamageTakenPerMinMatch, playerScoreboardPerMin.DamageTaken, ref mostDamageTakenPerMin);
                CompareValue(match, ref highestHPRestoredPerMinMatch, playerScoreboardPerMin.HPRestored, ref mostHPRestoredPerMin);
                CompareValue(match, ref highestCeruleumPerMinMatch, playerScoreboardPerMin.Ceruleum, ref mostCeruleumPerMin);
            }

            //timeline only
            if(match.TimelineId != null) {
                RivalWingsMatchTimeline? timeline = null;

                //fastest flying high
                if(match.FlyingHighTime == null && !match.Flags.HasFlag(RWValidationFlag.InvalidSoaring)) {
                    //process value if not already pre-processed
                    timeline ??= Plugin.RWCache.GetTimeline(match);
                    var localPlayerAlliance = match.LocalPlayerTeamMember?.Alliance;
                    if(timeline != null && timeline.AllianceStacks != null && localPlayerAlliance != null) {
                        var flyingHighEvent = timeline.AllianceStacks[(int)localPlayerAlliance].FirstOrDefault(x => x.Count >= 20);
                        var flyingHighTime = flyingHighEvent?.Timestamp - match.MatchStartTime ?? TimeSpan.MaxValue;
                        match.FlyingHighTime = flyingHighTime;
                        _ = Plugin.RWCache.UpdateMatch(match);
                    }
                }
                if(match.FlyingHighTime != null && match.FlyingHighTime != TimeSpan.MaxValue && !match.Flags.HasFlag(RWValidationFlag.InvalidSoaring)) {
                    if(match.FlyingHighTime < fastestFlyingHigh) {
                        fastestFlyingHigh = (TimeSpan)match.FlyingHighTime;
                        fastestFlyingHighMatch = match;
                    }
                }
            }

            RefreshProgress = (float)MatchesProcessed++ / MatchesTotal;
        }

        Superlatives = new();
        AddSuperlative(longestWinStreakMatch, "Longest win streak", longestWinStreak.ToString());
        AddSuperlative(longestLossStreakMatch, "Longest loss streak", longestLossStreak.ToString());
        AddSuperlative(shortestMatch, "Shortest match", ImGuiHelper.GetTimeSpanString(shortestMatch?.MatchDuration ?? TimeSpan.Zero));
        AddSuperlative(highestLoserProgMatch, "Highest loser core health", highestLoserProgMatch?.LoserProgress.ToString() ?? "");
        AddSuperlative(lowestWinnerProgMatch, "Lowest winner core health", lowestWinnerProgMatch?.WinnerProgress.ToString() ?? "");
        AddSuperlative(mostKillsMatch, "Most kills", mostKills.ToString());
        AddSuperlative(mostDeathsMatch, "Most deaths", mostDeaths.ToString());
        AddSuperlative(mostAssistsMatch, "Most assists", mostAssists.ToString());
        AddSuperlative(mostDamageToPCsMatch, "Most damage to PCs", mostDamageToPCs.ToString());
        AddSuperlative(mostDamageToOtherMatch, "Most damage to other", mostDamageToOther.ToString());
        AddSuperlative(mostDamageTakenMatch, "Most damage taken", mostDamageTaken.ToString());
        AddSuperlative(mostHPRestoredMatch, "Most HP restored", mostHPRestored.ToString());
        AddSuperlative(mostCeruleumMatch, "Most ceruleum earned", mostCeruleum.ToString());
        AddSuperlative(highestKillsPerMinMatch, "Most kills per min", mostKillsPerMin.ToString("0.00"));
        AddSuperlative(highestDeathsPerMinMatch, "Most deaths per min", mostDeathsPerMin.ToString("0.00"));
        AddSuperlative(highestAssistsPerMinMatch, "Most assists per min", mostAssistsPerMin.ToString("0.00"));
        AddSuperlative(highestDamageToPCsPerMinMatch, "Most damage to PCs per min", mostDamageToPCsPerMin.ToString("0"));
        AddSuperlative(highestDamageToOtherPerMinMatch, "Most damage to other per min", mostDamageToOtherPerMin.ToString("0"));
        AddSuperlative(highestDamageTakenPerMinMatch, "Most damage taken per min", mostDamageTakenPerMin.ToString("0"));
        AddSuperlative(highestHPRestoredPerMinMatch, "Most HP restored per min", mostHPRestoredPerMin.ToString("0"));
        AddSuperlative(highestCeruleumPerMinMatch, "Most ceruleum earned per min", mostCeruleumPerMin.ToString("0.00"));
        AddSuperlative(fastestFlyingHighMatch, "* Fastest Flying High", ImGuiHelper.GetTimeSpanString(fastestFlyingHigh));
        return Task.CompletedTask;
    }

    protected override void DrawMatchStat(RivalWingsMatch match) {
        using(var table = ImRaii.Table($"##{match.GetHashCode()}--Table", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
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
                    ImGui.Text(MatchHelper.GetArenaName((RivalWingsMap)match.Arena));
                }
                ImGui.TableNextColumn();
                var localPlayerJob = match.LocalPlayerTeamMember!.Job;
                ImGuiHelper.CenterAlignCursor(localPlayerJob.ToString() ?? "");
                ImGui.TextColored(Plugin.Configuration.GetJobColor(localPlayerJob), localPlayerJob.ToString());
                ImGui.TableNextColumn();
                var color = match.IsWin ? Plugin.Configuration.Colors.Win : match.IsLoss ? Plugin.Configuration.Colors.Loss : Plugin.Configuration.Colors.Other;
                var result = match.IsWin ? "WIN" : match.IsLoss ? "LOSS" : "???";
                ImGuiHelper.CenterAlignCursor(result);
                ImGui.TextColored(color, result);
            }
        }

        using(var table = ImRaii.Table($"##{match.GetHashCode()}--MechTable", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("CC", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 80f);
                ImGui.TableSetupColumn("OPP", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 80f);
                ImGui.TableSetupColumn("BJ", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 80f);

                ImGui.TableNextColumn();
                DrawMechTime(RivalWingsMech.Chaser, match);
                ImGui.TableNextColumn();
                DrawMechTime(RivalWingsMech.Oppressor, match);
                ImGui.TableNextColumn();
                DrawMechTime(RivalWingsMech.Justice, match);
            }
        }
    }

    private void DrawMechTime(RivalWingsMech mech, RivalWingsMatch match) {
        string text = "---";
        if(match.PlayerMechTime != null && match.LocalPlayer != null && match.MatchDuration != null) {
            match.PlayerMechTime.TryGetValue(match.LocalPlayer, out var playerMechTimes);
            double mechTime = 0d;
            if(playerMechTimes != null) {
                playerMechTimes.TryGetValue(mech, out var mechTimeRaw);
                mechTime = mechTimeRaw / match.MatchDuration.Value.TotalSeconds;
            }
            text = mechTime.ToString("P2");
        }
        var size = new Vector2(30f * ImGuiHelpers.GlobalScale, 30f * ImGuiHelpers.GlobalScale);
        var uv0 = new Vector2(0.1f);
        var uv1 = new Vector2(0.9f);

        using(var table = ImRaii.Table($"##{mech}--Table", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("mech", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 30f);
                ImGui.TableSetupColumn("time", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                switch(mech) {
                    default:
                    case RivalWingsMech.Chaser:
                        ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.ChaserIcons[RivalWingsTeamName.Unknown]), size, uv0, uv1);
                        ImGuiHelper.WrappedTooltip("Cruise Chaser");
                        break;
                    case RivalWingsMech.Oppressor:
                        ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.OppressorIcons[RivalWingsTeamName.Unknown]), size, uv0, uv1);
                        ImGuiHelper.WrappedTooltip("Oppressor");
                        break;
                    case RivalWingsMech.Justice:
                        ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.JusticeIcons[RivalWingsTeamName.Unknown]), size, uv0, uv1);
                        ImGuiHelper.WrappedTooltip("Brute Justice");
                        break;
                };
                ImGui.TableNextColumn();
                //ImGui.AlignTextToFramePadding();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
                ImGuiHelper.DrawNumericCell(text, -1f);
            }
        }

    }
}
