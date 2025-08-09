using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Windows.Records;
internal abstract class MatchRecords<T> : Refreshable<T> where T : PvpMatch {

    protected readonly Plugin Plugin;
    internal Dictionary<T, List<(string, string)>> Superlatives = new();

    protected abstract void DrawMatchStat(T match);

    internal MatchRecords(Plugin plugin) {
        this.Plugin = plugin;
    }

    protected override void Reset() {
        throw new NotImplementedException();
    }

    protected override void ProcessMatch(T match, bool remove = false) {
        throw new NotImplementedException();
    }

    protected override void PostRefresh(List<T> matches, List<T> additions, List<T> removals) {
        throw new NotImplementedException();
    }

    protected void CompareValue<U>(T newMatch, ref T? currentRecordMatch, U newValue, ref U currentRecord, bool invert = false) where U : IComparable<U> {
        var comparison = newValue.CompareTo(currentRecord);
        if(currentRecordMatch is null
            || !invert && (comparison > 0 || comparison == 0 && newMatch.MatchDuration < currentRecordMatch.MatchDuration)
            || invert && (comparison < 0 || comparison == 0 && newMatch.MatchDuration > currentRecordMatch.MatchDuration)) {
            currentRecordMatch = newMatch;
            currentRecord = newValue;
        }
    }

    protected void AddSuperlative(T? match, string sup, string val) {
        if(match == null) return;
        if(Superlatives.TryGetValue(match, out List<(string, string)>? value)) {
            value.Add((sup, val));
        } else {
            //Plugin.Log.Debug($"adding superlative {sup} {val} to {match.Id.ToString()}");
            Superlatives.Add(match, new() { (sup, val) });
        }
    }

    public void Draw() {
        ImGuiHelper.HelpMarker("Records marked with an asterisk (*) not all matches are eligible for.");
        ImGui.Separator();
        foreach(var match in Superlatives) {
            var x = match.Value;
            DrawStat(match.Key, match.Value.Select(x => x.Item1).ToArray(), match.Value.Select(x => x.Item2).ToArray());
            ImGui.Separator();
        }
    }

    protected void DrawStat(T match, string[] superlatives, string[] values) {
        using(var table = ImRaii.Table("headertable", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                var widthStyle = Plugin.Configuration.StretchScoreboardColumns ?? false ? ImGuiTableColumnFlags.WidthStretch : ImGuiTableColumnFlags.WidthFixed;
                ImGui.TableSetupColumn("title", widthStyle, ImGuiHelpers.GlobalScale * 185f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 80f);
                ImGui.TableSetupColumn($"examine", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 40f);

                for(int i = 0; i < superlatives.Length; i++) {
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(Plugin.Configuration.Colors.Header, superlatives[i] + ":");
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
                                Plugin.WindowManager.OpenMatchDetailsWindow(match);
                            }
                            var x = ImGui.CalcItemWidth();
                        }
                    }
                }
            }
        }

        DrawMatchStat(match);
    }
}
