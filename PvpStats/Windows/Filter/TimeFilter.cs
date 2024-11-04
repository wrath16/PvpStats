using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Types;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;

public enum TimeRange {
    PastDay,
    PastWeek,
    ThisMonth,
    LastMonth,
    ThisYear,
    LastYear,
    All,
    Season,
    Expansion,
    Patch,
    Custom
}

public enum TimeRelation {
    Before,
    Since,
    During,
    After
}

public class TimeFilter : DataFilter {
    public override string Name => "Time";

    public TimeRange StatRange { get; set; } = TimeRange.All;
    public static string[] Range = { "Past 24 hours", "Past 7 days", "This month", "Last month", "This year", "Last year", "All-time", "By ranked season", "By expansion", "By patch", "Custom" };
    public static string[] RelationRange = { "BEFORE", "SINCE", "DURING", "AFTER" };

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Season { get; set; } = GamePeriod.Season.Count - 1;
    public TimeRelation SeasonRelation { get; set; } = TimeRelation.During;
    public int Expansion { get; set; } = GamePeriod.Expansion.Last().Key;
    public TimeRelation ExpansionRelation { get; set; } = TimeRelation.During;
    public int Patch { get; set; } = GamePeriod.Patch.Last().Key;
    public TimeRelation PatchRelation { get; set; } = TimeRelation.Since;


    private string _lastStartTime = "";
    private string _lastEndTime = "";

    public TimeFilter() { }

    internal TimeFilter(Plugin plugin, Func<Task> action, TimeFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            StatRange = filter.StatRange;
            StartTime = filter.StartTime;
            EndTime = filter.EndTime;
            Season = filter.Season;
            Expansion = filter.Expansion;
            Patch = filter.Patch;
        }
    }

    internal override void Draw() {
        int statRangeToInt = (int)StatRange;
        int seasonIndex = Season - 1;
        int seasonRelationIndex = (int)SeasonRelation;
        int expansionIndex = Expansion - 6;
        int expansionRelationIndex = (int)ExpansionRelation;
        int patchIndex = Patch;
        int patchRelationIndex = (int)PatchRelation;
        //ImGui.SetNextItemWidth(float.Min(ImGui.GetContentRegionAvail().X / 2f, ImGuiHelpers.GlobalScale * 125f));
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2f);
        if(ImGui.Combo($"##timeRangeCombo", ref statRangeToInt, Range, Range.Length)) {
            Task.Run(async () => {
                StatRange = (TimeRange)statRangeToInt;
                await Refresh();
            });
        }
        if(StatRange == TimeRange.Custom) {
            using var table = ImRaii.Table("timeFilterTable", 2);
            if(table) {
                ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Start:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                var startTime = StartTime.ToString();
                if(ImGui.InputText($"##startTime", ref startTime, 50, ImGuiInputTextFlags.None)) {
                    if(startTime != _lastStartTime) {
                        _lastStartTime = startTime;
                        if(DateTime.TryParse(startTime, out DateTime newStartTime)) {
                            Task.Run(async () => {
                                StartTime = newStartTime;
                                await Refresh();
                            });
                        }
                    }
                }
                ImGui.TableNextColumn();
                ImGui.Text("End:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                var endTime = EndTime.ToString();
                if(ImGui.InputText($"##endTime", ref endTime, 50, ImGuiInputTextFlags.None)) {
                    if(endTime != _lastEndTime) {
                        _lastEndTime = endTime;
                        if(DateTime.TryParse(endTime, out DateTime newEndTime)) {
                            Task.Run(async () => {
                                EndTime = newEndTime;
                                await Refresh();
                            });
                        }
                    }
                }
            }
        } else if(StatRange == TimeRange.Season) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
            if(ImGui.Combo($"##seasonRelationCombo", ref seasonRelationIndex, RelationRange, RelationRange.Length)) {
                Task.Run(async () => {
                    SeasonRelation = (TimeRelation)seasonRelationIndex;
                    await Refresh();
                });
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 50f);
            if(ImGui.Combo($"##seasonCombo", ref seasonIndex, GamePeriod.Season.Keys.Select(x => x.ToString()).ToArray(), GamePeriod.Season.Count)) {
                Task.Run(async () => {
                    Season = seasonIndex + 1;
                    await Refresh();
                });
            }
        } else if(StatRange == TimeRange.Expansion) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
            if(ImGui.Combo($"##expansionRelationCombo", ref expansionRelationIndex, RelationRange, RelationRange.Length)) {
                Task.Run(async () => {
                    ExpansionRelation = (TimeRelation)expansionRelationIndex;
                    await Refresh();
                });
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if(ImGui.Combo($"##expansionCombo", ref expansionIndex, GamePeriod.Expansion.Select(x => x.Value.Name).ToArray(), GamePeriod.Expansion.Count)) {
                Task.Run(async () => {
                    Expansion = expansionIndex + 6;
                    await Refresh();
                });
            }
        } else if(StatRange == TimeRange.Patch) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
            if(ImGui.Combo($"##patchRelationCombo", ref patchRelationIndex, RelationRange, RelationRange.Length)) {
                Task.Run(async () => {
                    PatchRelation = (TimeRelation)patchRelationIndex;
                    await Refresh();
                });
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if(ImGui.Combo($"##milestoneCombo", ref patchIndex, GamePeriod.Patch.Select(x => x.Value.Name).ToArray(), GamePeriod.Patch.Count)) {
                Task.Run(async () => {
                    Patch = patchIndex;
                    await Refresh();
                });
            }
        }
    }
}
