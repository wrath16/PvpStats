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
    Custom
}

public class TimeFilter : DataFilter {
    public override string Name => "Time";

    public TimeRange StatRange { get; set; } = TimeRange.All;
    public static string[] Range = { "Past 24 hours", "Past 7 days", "This month", "Last month", "This year", "Last year", "All-time", "By season", "By expansion", "Custom" };

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Season { get; set; } = GamePeriod.Season.Count - 1;
    public int Expansion { get; set; } = GamePeriod.Expansion.Last().Key;
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
        }
    }

    internal override void Draw() {
        int statRangeToInt = (int)StatRange;
        int seasonIndex = Season - 1;
        int expansionIndex = Expansion - 6;
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
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 50f);
            if(ImGui.Combo($"##seasonCombo", ref seasonIndex, GamePeriod.Season.Keys.Select(x => x.ToString()).ToArray(), GamePeriod.Season.Count)) {
                Task.Run(async () => {
                    Season = seasonIndex + 1;
                    await Refresh();
                });
            }
        } else if(StatRange == TimeRange.Expansion) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if(ImGui.Combo($"##expansionCombo", ref expansionIndex, GamePeriod.Expansion.Select(x => x.Value.Name).ToArray(), GamePeriod.Expansion.Count)) {
                Task.Run(async () => {
                    Expansion = expansionIndex + 6;
                    await Refresh();
                });
            }
        }
    }
}
