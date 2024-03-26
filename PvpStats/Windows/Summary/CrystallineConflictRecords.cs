using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictRecords {

    private Plugin _plugin;
    private SemaphoreSlim _refreshLock = new SemaphoreSlim(1);
    private int _spectatedMatchCount = 0;

    internal CrystallineConflictMatch? LongestMatch { get; private set; }
    internal CrystallineConflictMatch? ShortestMatch { get; private set; }
    internal CrystallineConflictMatch? ClosestMatch { get; private set; }
    internal CrystallineConflictMatch? ClosestWin { get; private set; }
    internal CrystallineConflictMatch? ClosestLoss { get; private set; }
    internal CrystallineConflictMatch? MostKills { get; private set; }
    internal CrystallineConflictMatch? MostDeaths { get; private set; }
    internal CrystallineConflictMatch? MostAssists { get; private set; }
    internal CrystallineConflictMatch? MostDamageDealt { get; private set; }
    internal CrystallineConflictMatch? MostDamageTaken { get; private set; }
    internal CrystallineConflictMatch? MostHPRestored { get; private set; }
    internal CrystallineConflictMatch? MostTimeOnCrystal { get; private set; }

    private Dictionary<CrystallineConflictMatch, List<(string,string)>> Superlatives = new();

    internal int LongestWinStreak { get; private set; }
    internal int LongestLossStreak { get; private set; }


    internal CrystallineConflictRecords(Plugin plugin) {
        _plugin = plugin;
    }

    public void Refresh(List<CrystallineConflictMatch> matches) {
        CrystallineConflictMatch? longestMatch = null, shortestMatch = null, closestMatch = null, closestWin = null, closestLoss = null, 
            mostKills = null, mostDeaths = null, mostAssists = null, mostDamageDealt = null, mostDamageTaken = null, mostHPRestored = null, mostTimeOnCrystal = null;
        int longestWinStreak = 0, longestLossStreak = 0, spectatedMatchCount = 0;

        foreach(var match in matches) {
            //track these for spectated matches as well
            if(longestMatch == null) {
                longestMatch = match;
                shortestMatch = match;
                closestMatch = match;
            }
            if(longestMatch == null || match.MatchDuration > longestMatch.MatchDuration) {
                longestMatch = match;
            }
            if(shortestMatch == null || match.MatchDuration < shortestMatch.MatchDuration) {
                shortestMatch = match;
            }
            if(closestMatch == null || match.LoserProgress > longestMatch.LoserProgress) {
                closestMatch = match;
            }

            if(match.IsSpectated) {
                spectatedMatchCount++;
                continue;
            }

            if(mostKills == null || match.LocalPlayerStats?.Kills > mostKills.LocalPlayerStats?.Kills) {
                mostKills = match;
            }
            if(mostDeaths == null || match.LocalPlayerStats?.Deaths > mostDeaths.LocalPlayerStats?.Deaths) {
                mostDeaths = match;
            }
            if(mostAssists == null || match.LocalPlayerStats?.Assists > mostAssists.LocalPlayerStats?.Assists) {
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
            if(match.IsWin && (closestWin == null || match.LoserProgress > closestWin.LoserProgress)) {
                closestWin = match;
            }
            if(match.IsLoss && (closestLoss == null || match.LoserProgress > closestLoss.LoserProgress)) {
                closestLoss = match;
            }

            if(match.IsWin) {
                longestWinStreak++;
            } else {
                longestWinStreak = 0;
            }
            if(match.IsLoss) {
                longestLossStreak++;
            } else {
                longestLossStreak = 0;
            }
        }

        try {
            _refreshLock.WaitAsync();
            var addSuperlative = (CrystallineConflictMatch? match, string sup, string val) => {
                if(match == null) return;
                if(Superlatives.ContainsKey(match)) {
                    Superlatives[match].Add((sup, val));
                } else {
                    _plugin.Log.Debug($"adding superlative {sup} {val} to {match.Id.ToString()}");
                    Superlatives.Add(match, new() { (sup, val) });
                }
            };
            Superlatives = new();
            if(longestMatch != null) {
                addSuperlative(longestMatch, "Longest match", ImGuiHelper.GetTimeSpanString((TimeSpan)longestMatch!.MatchDuration));
                addSuperlative(shortestMatch, "Shortest match", ImGuiHelper.GetTimeSpanString((TimeSpan)shortestMatch.MatchDuration));
                addSuperlative(closestMatch, "Closest match", closestMatch.LoserProgress.ToString());
                if(mostKills != null) {
                    addSuperlative(mostKills, "Most kills", mostKills!.LocalPlayerStats!.Kills.ToString());
                    addSuperlative(mostDeaths, "Most deaths", mostDeaths!.LocalPlayerStats!.Deaths.ToString());
                    addSuperlative(mostAssists, "Most assists", mostAssists!.LocalPlayerStats!.Assists.ToString());
                    addSuperlative(mostDamageDealt, "Most damage dealt", mostDamageDealt!.LocalPlayerStats!.DamageDealt.ToString());
                    addSuperlative(mostDamageTaken, "Most damage taken", mostDamageTaken!.LocalPlayerStats!.DamageTaken.ToString());
                    addSuperlative(mostHPRestored, "Most HP restored", mostHPRestored!.LocalPlayerStats!.HPRestored.ToString());
                    addSuperlative(mostTimeOnCrystal, "Longest time on crystal", ImGuiHelper.GetTimeSpanString(mostTimeOnCrystal!.LocalPlayerStats!.TimeOnCrystal));
                }

            }


            LongestMatch = longestMatch;
            ShortestMatch = shortestMatch;
            ClosestMatch = closestMatch;
            ClosestWin = closestWin;
            ClosestLoss = closestLoss;
            MostKills = mostKills;
            MostDeaths = mostDeaths;
            MostAssists = mostAssists;
            MostDamageDealt = mostDamageDealt;
            MostDamageTaken = mostDamageTaken;
            MostHPRestored = mostHPRestored;
            MostTimeOnCrystal = mostTimeOnCrystal;
            LongestWinStreak = longestWinStreak;
            LongestLossStreak = longestLossStreak;
        } finally {
            _refreshLock.Release();
        }
    }

    public void Draw() {
        if(LongestMatch == null) {
            return;
        }

        foreach(var match in Superlatives) {
            var x = match.Value;
            DrawStat2(match.Key, match.Value.Select(x => x.Item1).ToArray(), match.Value.Select(x => x.Item2).ToArray());
        }


        //DrawStat("Longest match", ImGuiHelper.GetTimeSpanString((TimeSpan)LongestMatch.MatchDuration), LongestMatch);
        //DrawStat("Shortest match", ImGuiHelper.GetTimeSpanString((TimeSpan)ShortestMatch.MatchDuration), ShortestMatch);
        //DrawStat("Most kills", MostKills.LocalPlayerStats.Kills.ToString(), MostKills);
        //DrawStat("Most deaths", MostDeaths.LocalPlayerStats.Deaths.ToString(), MostDeaths);
        //DrawStat("Most assists", MostAssists.LocalPlayerStats.Assists.ToString(), MostAssists);
        //DrawStat("Most damage dealt", MostDamageDealt.LocalPlayerStats.DamageDealt.ToString(), MostDamageDealt);
        //DrawStat("Most damage taken", MostDamageTaken.LocalPlayerStats.DamageTaken.ToString(), MostDamageTaken);
        //DrawStat("Most HP restored", MostHPRestored.LocalPlayerStats.HPRestored.ToString(), MostHPRestored);
        //DrawStat("Longest time on crystal", ImGuiHelper.GetTimeSpanString((TimeSpan)MostTimeOnCrystal.LocalPlayerStats.TimeOnCrystal), MostTimeOnCrystal);
    }

    private void DrawStat2(CrystallineConflictMatch match, string[] superlatives, string[] values) {
        using(var table = ImRaii.Table("headertable", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("title", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f);
                ImGui.TableSetupColumn($"examine", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

                for(int i = 0; i < superlatives.Length; i++) {
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, superlatives[i] + ":");
                    ImGui.TableNextColumn();
                    ImGui.Text(values[i]);
                    ImGui.TableNextColumn();
                    if(i == superlatives.Length - 1) {
                        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
                            if(ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}##{match.GetHashCode()}--ViewDetails")) {
                                _plugin.WindowManager.OpenMatchDetailsWindow(match);
                            }
                        }
                    }
                }
            }
        }

        using(var table = ImRaii.Table("match", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 100f);
                ImGui.TableSetupColumn("Arena", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 150f);
                ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);
                ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);

                ImGui.TableNextColumn();
                ImGui.Text($"{match.DutyStartTime:MM/dd/yyyy HH:mm}");
                ImGui.TableNextColumn();
                if(match.Arena != null) {
                    ImGui.Text(MatchHelper.GetArenaName((CrystallineConflictMap)match.Arena));
                }
                ImGui.TableNextColumn();
                if(!match.IsSpectated) {
                    ImGui.TextColored(ImGuiHelper.GetJobColor(match.LocalPlayerTeamMember!.Job), $"{match.LocalPlayerTeamMember!.Job}");
                    ImGui.TableNextColumn();
                    var color = match.IsWin ? ImGuiColors.HealerGreen : match.IsLoss ? ImGuiColors.DalamudRed : ImGuiColors.DalamudGrey;
                    var result = match.IsWin ? "WIN" : match.IsLoss ? "LOSS" : "???";
                    ImGui.TextColored(color, result);
                } else {
                    ImGui.Text($"Spectated");
                }
            }
        }
    }

    private void DrawStat(string title, string value, CrystallineConflictMatch match, bool drawMatch = true) {
        using(var table = ImRaii.Table("title", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("title", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f);
                ImGui.TableSetupColumn($"examine", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, title + ":");
                ImGui.TableNextColumn();

                ImGui.Text(value);
                ImGui.TableNextColumn();
                if(drawMatch) {
                    using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
                        if(ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}##{match.GetHashCode()}--ViewDetails")) {
                            _plugin.WindowManager.OpenMatchDetailsWindow(match);
                        }
                    }
                }
            }
        }

        if(!drawMatch) return;

        using(var table = ImRaii.Table("match", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 100f);
                ImGui.TableSetupColumn("Arena", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 150f);
                ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);
                ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);

                ImGui.TableNextColumn();
                ImGui.Text($"{match.DutyStartTime:MM/dd/yyyy HH:mm}");
                ImGui.TableNextColumn();
                if(match.Arena != null) {
                    ImGui.Text(MatchHelper.GetArenaName((CrystallineConflictMap)match.Arena));
                }
                ImGui.TableNextColumn();
                if(!match.IsSpectated) {
                    ImGui.TextColored(ImGuiHelper.GetJobColor(match.LocalPlayerTeamMember!.Job), $"{match.LocalPlayerTeamMember!.Job}");
                    ImGui.TableNextColumn();
                    var color = match.IsWin ? ImGuiColors.HealerGreen : match.IsLoss ? ImGuiColors.DalamudRed : ImGuiColors.DalamudGrey;
                    var result = match.IsWin ? "WIN" : match.IsLoss ? "LOSS" : "???";
                    ImGui.TextColored(color, result);
                } else {
                    ImGui.Text($"Spectated");
                }
            }
        }
    }
}
