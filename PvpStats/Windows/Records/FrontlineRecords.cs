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
            mostKillsPerMinMatch = null, mostDeathsPerMinMatch = null, mostAssistsPerMinMatch = null, mostDamageToPCsPerMinMatch = null, mostDamageToOtherPerMinMatch = null, mostDamageTakenPerMinMatch = null, mostHPRestoredPerMinMatch = null,
            fastestBHVMatch = null, battleHighSpikeMatch = null, momentaryPointAdvantageMatch = null, momentaryPointDeficitMatch = null;

        long mostKills = 0, mostDeaths = 0, mostAssists = 0, mostDamageToPCs = 0, mostDamageToOther = 0, mostDamageTaken = 0, mostHPRestored = 0, mostCombatPoints = 0, lowestCombatPoints = long.MaxValue;
        double mostKillsPerMin = 0, mostDeathsPerMin = 0, mostAssistsPerMin = 0, mostDamageToPCsPerMin = 0, mostDamageToOtherPerMin = 0, mostDamageTakenPerMin = 0, mostHPRestoredPerMin = 0, momentaryAdvantageSpan = 0, momentaryDeficitSpan = 0;
        TimeSpan fastestBHV = TimeSpan.MaxValue;
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

            //timeline only
            if(match.TimelineId != null) {
                FrontlineMatchTimeline? timeline = null;
                List<DateTime>? pointEventTimes = null;

                //fastest battle high V
                if(match.BattleHighVTime == null) {
                    //process value if not already pre-processed
                    timeline ??= Plugin.FLCache.GetTimeline(match);
                    if(timeline != null && timeline.SelfBattleHigh != null) {
                        var bhVEvent = timeline.SelfBattleHigh.FirstOrDefault(x => x.Count >= 100);
                        var bhVTime = bhVEvent?.Timestamp - match.MatchStartTime ?? TimeSpan.MaxValue;
                        match.BattleHighVTime = bhVTime;
                        _ = Plugin.FLCache.UpdateMatch(match);
                    }
                }
                if(match.BattleHighVTime != null && match.BattleHighVTime != TimeSpan.MaxValue) {
                    if(match.BattleHighVTime < fastestBHV) {
                        fastestBHV = (TimeSpan)match.BattleHighVTime;
                        fastestBHVMatch = match;
                    }
                }

                //battle high spike
                if(match.BattleHighSpike == null) {
                    timeline ??= Plugin.FLCache.GetTimeline(match);
                    if(timeline != null && timeline.SelfBattleHigh != null) {
                        //Plugin.Log2.Debug($"Match {match.DutyStartTime}");

                        int biggestSpike = 0, currentSpike = 0;
                        TimeSpan biggestSpikeTime = TimeSpan.MaxValue, currentSpikeTime = TimeSpan.Zero;

                        for(int i = 1; i < timeline.SelfBattleHigh.Count; i++) {
                            var bhEvent = timeline.SelfBattleHigh[i];
                            var lastEvent = timeline.SelfBattleHigh[i - 1];

                            //5 second max between events
                            if(bhEvent.Timestamp - lastEvent.Timestamp <= TimeSpan.FromSeconds(5) && bhEvent.Count > lastEvent.Count) {
                                currentSpike += bhEvent.Count - lastEvent.Count;
                                currentSpikeTime += bhEvent.Timestamp - lastEvent.Timestamp;
                            } else {
                                currentSpike = Math.Max(bhEvent.Count - lastEvent.Count, 0);
                                currentSpikeTime = TimeSpan.Zero;
                            }
                            if(currentSpike > biggestSpike || (currentSpike == biggestSpike && currentSpikeTime < biggestSpikeTime)) {
                                biggestSpike = currentSpike;
                                biggestSpikeTime = currentSpikeTime;
                            }
                            //Plugin.Log2.Debug($"Current Spike {currentSpike} Time {currentSpikeTime.TotalSeconds} Biggest Spike {biggestSpike}");
                        }

                        match.BattleHighSpike = biggestSpike;
                        match.BattleHighSpikeTime = biggestSpikeTime;
                        _ = Plugin.FLCache.UpdateMatch(match);
                    }
                }
                if(match.BattleHighSpike != null && match.BattleHighSpike != 0) {
                    if(match.BattleHighSpike > (battleHighSpikeMatch?.BattleHighSpike ?? 0)) {
                        battleHighSpikeMatch = match;
                    }
                }

                //momentary point advantage and deficit
                if(match.MomentaryPointAdvantage == null) {
                    timeline ??= Plugin.FLCache.GetTimeline(match);
                    pointEventTimes ??= timeline?.GetFlattenedTeamPoints()?.Select(x => x.Timestamp).ToList();
                    var playerTeam = match.LocalPlayerTeam;

                    if(timeline != null && timeline.TeamPoints != null && pointEventTimes != null && playerTeam != null) {
                        //Plugin.Log2.Debug($"Match {match.DutyStartTime}");

                        int biggestAdvantage = int.MinValue;
                        int biggestDeficit = int.MaxValue;

                        foreach(var pEvent in pointEventTimes) {
                            var teamPoints = timeline.GetTeamPoints(pEvent) ?? [];
                            var sortedPoints = new OrderedDictionary<FrontlineTeamName, int>(teamPoints.OrderByDescending(x => x.Value));
                            if(sortedPoints.First().Key == playerTeam) {
                                var advantage = sortedPoints.ElementAt(0).Value - sortedPoints.ElementAt(1).Value;
                                //Plugin.Log2.Debug($"{pEvent} {sortedPoints.ElementAt(0).Key} {sortedPoints.ElementAt(0).Value} {sortedPoints.ElementAt(1).Key} {sortedPoints.ElementAt(1).Value} {sortedPoints.ElementAt(2).Key} {sortedPoints.ElementAt(2).Value} advantage {advantage}");
                                if(advantage > biggestAdvantage) {
                                    biggestAdvantage = advantage;
                                }
                            } else {
                                var deficit = sortedPoints[(FrontlineTeamName)playerTeam] - sortedPoints.ElementAt(0).Value;
                                //Plugin.Log2.Debug($"{pEvent} {sortedPoints.ElementAt(0).Key} {sortedPoints.ElementAt(0).Value} {sortedPoints.ElementAt(1).Key} {sortedPoints.ElementAt(1).Value} {sortedPoints.ElementAt(2).Key} {sortedPoints.ElementAt(2).Value} deficit {deficit}");
                                if(deficit < biggestDeficit) {
                                    biggestDeficit = deficit;
                                }
                            }
                        }
                        match.MomentaryPointAdvantage = biggestAdvantage;
                        match.MomentaryPointDeficit = biggestDeficit;
                        _ = Plugin.FLCache.UpdateMatch(match);
                    }
                }
                if(match.MomentaryPointAdvantage != null && match.MomentaryPointAdvantage != int.MinValue) {
                    var spannedAdvantage = (double)match.MomentaryPointAdvantage / MatchHelper.GetFrontlineMaxPoints(match.Arena);
                    if(spannedAdvantage > momentaryAdvantageSpan) {
                        momentaryPointAdvantageMatch = match;
                        momentaryAdvantageSpan = spannedAdvantage;
                    }
                }
                if(match.MomentaryPointDeficit != null && match.MomentaryPointDeficit != int.MaxValue) {
                    var spannedDeficit = (double)match.MomentaryPointDeficit / MatchHelper.GetFrontlineMaxPoints(match.Arena);
                    if(spannedDeficit < momentaryDeficitSpan) {
                        momentaryPointDeficitMatch = match;
                        momentaryDeficitSpan = spannedDeficit;
                    }
                }
            }

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

        AddSuperlative(fastestBHVMatch, "* Fastest Battle High V", ImGuiHelper.GetTimeSpanString(fastestBHV));
        AddSuperlative(battleHighSpikeMatch, "* Highest Battle High chain", $"+{battleHighSpikeMatch?.BattleHighSpike.ToString() ?? ""} in {battleHighSpikeMatch?.BattleHighSpikeTime.Value.TotalSeconds:#.0}s");
        AddSuperlative(momentaryPointAdvantageMatch, "* Highest momentary point lead", $"{momentaryPointAdvantageMatch?.MomentaryPointAdvantage} ({momentaryAdvantageSpan:P0})");
        AddSuperlative(momentaryPointDeficitMatch, "* Highest momentary point deficit", $"{momentaryPointDeficitMatch?.MomentaryPointDeficit} ({Math.Abs(momentaryDeficitSpan):P0})");

        return Task.CompletedTask;
    }

    protected override void DrawMatchStat(FrontlineMatch match) {
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
