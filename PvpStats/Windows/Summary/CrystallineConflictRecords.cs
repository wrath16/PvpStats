using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictRecords : Refreshable<CrystallineConflictMatch> {

    private readonly Plugin _plugin;

    public override string Name => "CC Records";

    internal Dictionary<CrystallineConflictMatch, List<(string, string)>> Superlatives = new();
    internal int LongestWinStreak { get; private set; }
    internal int LongestLossStreak { get; private set; }

    internal CrystallineConflictRecords(Plugin plugin) {
        _plugin = plugin;
    }

    protected override Task RefreshInner(List<CrystallineConflictMatch> matches, List<CrystallineConflictMatch> additions, List<CrystallineConflictMatch> removals) {
        Dictionary<CrystallineConflictMatch, List<(string, string)>> superlatives = new();
        CrystallineConflictMatch? longestMatch = null, shortestMatch = null, highestLoserProg = null, lowestWinnerProg = null,
            mostKills = null, mostDeaths = null, mostAssists = null, mostDamageDealt = null, mostDamageTaken = null, mostHPRestored = null, mostTimeOnCrystal = null,
            highestKillsPerMin = null, highestDeathsPerMin = null, highestAssistsPerMin = null, highestDamageDealtPerMin = null, highestDamageTakenPerMin = null, highestHPRestoredPerMin = null, highestTimeOnCrystalPerMin = null;
        int longestWinStreak = 0, longestLossStreak = 0, currentWinStreak = 0, currentLossStreak = 0;

        MatchesTotal = matches.Count;

        foreach(var match in matches) {
            //track these for spectated matches as well
            if(longestMatch == null) {
                longestMatch = match;
                shortestMatch = match;
                highestLoserProg = match;
            }
            if(longestMatch == null || match.MatchDuration > longestMatch.MatchDuration) {
                longestMatch = match;
            }
            if(shortestMatch == null || match.MatchDuration < shortestMatch.MatchDuration) {
                shortestMatch = match;
            }
            if(highestLoserProg == null || match.LoserProgress > highestLoserProg.LoserProgress) {
                highestLoserProg = match;
            }
            if(lowestWinnerProg == null || match.WinnerProgress < lowestWinnerProg.WinnerProgress) {
                lowestWinnerProg = match;
            }

            if(match.IsSpectated) {
                //spectatedMatchCount++;
                //continue;
            } else {
                if(mostKills == null || match.LocalPlayerStats?.Kills > mostKills.LocalPlayerStats?.Kills
                    || (match.LocalPlayerStats?.Kills == mostKills.LocalPlayerStats?.Kills && match.MatchDuration < mostKills.MatchDuration)) {
                    mostKills = match;
                }
                if(mostDeaths == null || match.LocalPlayerStats?.Deaths > mostDeaths.LocalPlayerStats?.Deaths
                    || (match.LocalPlayerStats?.Deaths == mostDeaths.LocalPlayerStats?.Deaths && match.MatchDuration < mostDeaths.MatchDuration)) {
                    mostDeaths = match;
                }
                if(mostAssists == null || match.LocalPlayerStats?.Assists > mostAssists.LocalPlayerStats?.Assists
                    || (match.LocalPlayerStats?.Assists == mostAssists.LocalPlayerStats?.Assists && match.MatchDuration < mostAssists.MatchDuration)) {
                    mostAssists = match;
                }
                if(mostDamageDealt == null || match.LocalPlayerStats?.DamageDealt > mostDamageDealt.LocalPlayerStats?.DamageDealt) {
                    mostDamageDealt = match;
                }
                if(mostDamageTaken == null || match.LocalPlayerStats?.DamageTaken > mostDamageTaken.LocalPlayerStats?.DamageTaken) {
                    mostDamageTaken = match;
                }
                if(mostHPRestored == null || match.LocalPlayerStats?.HPRestored > mostHPRestored.LocalPlayerStats?.HPRestored) {
                    mostHPRestored = match;
                }
                if(mostTimeOnCrystal == null || match.LocalPlayerStats?.TimeOnCrystal > mostTimeOnCrystal.LocalPlayerStats?.TimeOnCrystal) {
                    mostTimeOnCrystal = match;
                }
                if(match.MatchDuration != null && match.LocalPlayerStats != null) {
                    if(highestKillsPerMin == null || (float)match.LocalPlayerStats?.Kills! / match.MatchDuration.Value.TotalMinutes > (float)highestKillsPerMin.LocalPlayerStats?.Kills! / highestKillsPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestKillsPerMin = match;
                    }
                    if(highestDeathsPerMin == null || (float)match.LocalPlayerStats?.Deaths! / match.MatchDuration.Value.TotalMinutes > (float)highestDeathsPerMin.LocalPlayerStats?.Deaths! / highestDeathsPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestDeathsPerMin = match;
                    }
                    if(highestAssistsPerMin == null || (float)match.LocalPlayerStats?.Assists! / match.MatchDuration.Value.TotalMinutes > (float)highestAssistsPerMin.LocalPlayerStats?.Assists! / highestAssistsPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestAssistsPerMin = match;
                    }
                    if(highestDamageDealtPerMin == null || (float)match.LocalPlayerStats?.DamageDealt! / match.MatchDuration.Value.TotalMinutes > (float)highestDamageDealtPerMin.LocalPlayerStats?.DamageDealt! / highestDamageDealtPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestDamageDealtPerMin = match;
                    }
                    if(highestDamageTakenPerMin == null || (float)match.LocalPlayerStats?.DamageTaken! / match.MatchDuration.Value.TotalMinutes > (float)highestDamageTakenPerMin.LocalPlayerStats?.DamageTaken! / highestDamageTakenPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestDamageTakenPerMin = match;
                    }
                    if(highestHPRestoredPerMin == null || (float)match.LocalPlayerStats?.HPRestored! / match.MatchDuration.Value.TotalMinutes > (float)highestHPRestoredPerMin.LocalPlayerStats?.HPRestored! / highestHPRestoredPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestHPRestoredPerMin = match;
                    }
                    if(highestTimeOnCrystalPerMin == null || match.LocalPlayerStats?.TimeOnCrystal / match.MatchDuration.Value.TotalMinutes > highestTimeOnCrystalPerMin.LocalPlayerStats?.TimeOnCrystal / highestTimeOnCrystalPerMin.MatchDuration!.Value.TotalMinutes) {
                        highestTimeOnCrystalPerMin = match;
                    }
                }

                if(match.IsWin) {
                    currentWinStreak++;
                    if(currentWinStreak > longestWinStreak) {
                        longestWinStreak = currentWinStreak;
                    }
                } else {
                    currentWinStreak = 0;
                }
                if(match.IsLoss) {
                    currentLossStreak++;
                    if(currentLossStreak > longestLossStreak) {
                        longestLossStreak = currentLossStreak;
                    }
                } else {
                    currentLossStreak = 0;
                }
            }
            RefreshProgress = (float)MatchesProcessed++ / MatchesTotal;
        }

        LongestWinStreak = longestWinStreak;
        LongestLossStreak = longestLossStreak;

        Superlatives = new();
        if(longestMatch != null) {
            AddSuperlative(longestMatch, "Longest match", ImGuiHelper.GetTimeSpanString((TimeSpan)longestMatch.MatchDuration!));
            AddSuperlative(shortestMatch, "Shortest match", ImGuiHelper.GetTimeSpanString((TimeSpan)shortestMatch!.MatchDuration!));
            AddSuperlative(highestLoserProg, "Highest loser progress", highestLoserProg!.LoserProgress!.ToString()!);
            AddSuperlative(lowestWinnerProg, "Lowest winner progress", lowestWinnerProg!.WinnerProgress!.ToString()!);
            if(mostKills != null) {
                AddSuperlative(mostKills, "Most kills", mostKills!.LocalPlayerStats!.Kills.ToString());
                AddSuperlative(mostDeaths, "Most deaths", mostDeaths!.LocalPlayerStats!.Deaths.ToString());
                AddSuperlative(mostAssists, "Most assists", mostAssists!.LocalPlayerStats!.Assists.ToString());
                AddSuperlative(mostDamageDealt, "Most damage dealt", mostDamageDealt!.LocalPlayerStats!.DamageDealt.ToString());
                AddSuperlative(mostDamageTaken, "Most damage taken", mostDamageTaken!.LocalPlayerStats!.DamageTaken.ToString());
                AddSuperlative(mostHPRestored, "Most HP restored", mostHPRestored!.LocalPlayerStats!.HPRestored.ToString());
                AddSuperlative(mostTimeOnCrystal, "Longest time on crystal", ImGuiHelper.GetTimeSpanString(mostTimeOnCrystal!.LocalPlayerStats!.TimeOnCrystal));
                AddSuperlative(highestKillsPerMin, "Highest kills per min", (highestKillsPerMin!.LocalPlayerStats!.Kills / highestKillsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                AddSuperlative(highestDeathsPerMin, "Highest deaths per min", (highestDeathsPerMin!.LocalPlayerStats!.Deaths / highestDeathsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                AddSuperlative(highestAssistsPerMin, "Highest assists per min", (highestAssistsPerMin!.LocalPlayerStats!.Assists / highestAssistsPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0.00"));
                AddSuperlative(highestDamageDealtPerMin, "Highest damage dealt per min", (highestDamageDealtPerMin!.LocalPlayerStats!.DamageDealt / highestDamageDealtPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                AddSuperlative(highestDamageTakenPerMin, "Highest damage taken per min", (highestDamageTakenPerMin!.LocalPlayerStats!.DamageTaken / highestDamageTakenPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                AddSuperlative(highestHPRestoredPerMin, "Highest HP restored per min", (highestHPRestoredPerMin!.LocalPlayerStats!.HPRestored / highestHPRestoredPerMin!.MatchDuration!.Value.TotalMinutes).ToString("0"));
                AddSuperlative(highestTimeOnCrystalPerMin, "Longest time on crystal per min", ImGuiHelper.GetTimeSpanString(highestTimeOnCrystalPerMin!.LocalPlayerStats!.TimeOnCrystal / highestTimeOnCrystalPerMin!.MatchDuration!.Value.TotalMinutes));
            }
        }
        return Task.CompletedTask;
    }

    protected override void Reset() {
        throw new NotImplementedException();
    }

    protected override void ProcessMatch(CrystallineConflictMatch match, bool remove = false) {
        throw new NotImplementedException();
    }

    protected override void PostRefresh(List<CrystallineConflictMatch> matches, List<CrystallineConflictMatch> additions, List<CrystallineConflictMatch> removals) {
        throw new NotImplementedException();
    }

    private void AddSuperlative(CrystallineConflictMatch? match, string sup, string val) {
        if(match == null) return;
        if(Superlatives.TryGetValue(match, out List<(string, string)>? value)) {
            value.Add((sup, val));
        } else {
            //Plugin.Log.Debug($"adding superlative {sup} {val} to {match.Id.ToString()}");
            Superlatives.Add(match, new() { (sup, val) });
        }
    }

    public void Draw() {
        using(var table = ImRaii.Table("streaks", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("title", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 185f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);

                ImGui.TableNextColumn();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Longest win streak:");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell(LongestWinStreak.ToString());
                ImGui.TableNextColumn();
                ImGui.TextColored(_plugin.Configuration.Colors.Header, "Longest loss streak:");
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

    private void DrawStat(CrystallineConflictMatch match, string[] superlatives, string[] values) {
        using(var table = ImRaii.Table("headertable", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("title", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 185f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);
                ImGui.TableSetupColumn($"examine", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);

                for(int i = 0; i < superlatives.Length; i++) {
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(_plugin.Configuration.Colors.Header, superlatives[i] + ":");
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell(values[i]);
                    //ImGui.Text(values[i]);
                    ImGui.TableNextColumn();
                    if(i == superlatives.Length - 1) {
                        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
                            //ImGuiHelper.CenterAlignCursor(FontAwesomeIcon.Search.ToIconString());
                            var buttonWidth = ImGui.GetStyle().FramePadding.X * 2 + ImGui.CalcTextSize(FontAwesomeIcon.Search.ToIconString()).X;
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - buttonWidth) / 2f);
                            if(ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}##{match.GetHashCode()}--ViewDetails")) {
                                _plugin.WindowManager.OpenMatchDetailsWindow(match);
                            }
                            var x = ImGui.CalcItemWidth();
                        }
                    }
                }
            }
        }

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
                    ImGui.Text(MatchHelper.GetArenaName((CrystallineConflictMap)match.Arena));
                }
                ImGui.TableNextColumn();
                if(!match.IsSpectated) {
                    var localPlayerJob = match.LocalPlayerTeamMember!.Job;
                    ImGuiHelper.CenterAlignCursor(localPlayerJob.ToString() ?? "");
                    ImGui.TextColored(_plugin.Configuration.GetJobColor(localPlayerJob), localPlayerJob.ToString());
                    ImGui.TableNextColumn();
                    var color = match.IsWin ? _plugin.Configuration.Colors.Win : match.IsLoss ? _plugin.Configuration.Colors.Loss : _plugin.Configuration.Colors.Other;
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
