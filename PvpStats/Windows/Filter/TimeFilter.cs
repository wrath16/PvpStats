using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Types.Match;
using System;
using System.Linq;

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
    Custom
}

public class TimeFilter : DataFilter {
    public override string Name => "Time";

    public TimeRange StatRange { get; set; } = TimeRange.All;
    public static string[] Range = { "Past 24 hours", "Past 7 days", "This month", "Last month", "This year", "Last year", "All-time", "By season", "Custom" };

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Season { get; set; } = ArenaSeason.Season.Count - 1;
    private string _lastStartTime = "";
    private string _lastEndTime = "";

    public TimeFilter() { }

    internal TimeFilter(Plugin plugin, Action action, TimeFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            StatRange = filter.StatRange;
            StartTime = filter.StartTime;
            EndTime = filter.EndTime;
            Season = filter.Season;
        }
    }

    internal override void Draw() {
        int statRangeToInt = (int)StatRange;
        int seasonIndex = Season - 1;
        //ImGui.SetNextItemWidth(float.Min(ImGui.GetContentRegionAvail().X / 2f, ImGuiHelpers.GlobalScale * 125f));
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2f);
        if(ImGui.Combo($"##timeRangeCombo", ref statRangeToInt, Range, Range.Length)) {
            _plugin!.DataQueue.QueueDataOperation(() => {
                StatRange = (TimeRange)statRangeToInt;
                Refresh();
            });
        }
        if(StatRange == TimeRange.Custom) {
            if(ImGui.BeginTable("timeFilterTable", 2)) {
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
                            _plugin!.DataQueue.QueueDataOperation(() => {
                                StartTime = newStartTime;
                                Refresh();
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
                            _plugin!.DataQueue.QueueDataOperation(() => {
                                EndTime = newEndTime;
                                Refresh();
                            });
                        }
                    }
                }
                ImGui.EndTable();
            }
        } else if(StatRange == TimeRange.Season) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 50f);
            if(ImGui.Combo($"##seasonCombo", ref seasonIndex, ArenaSeason.Season.Keys.Select(x => x.ToString()).ToArray(), ArenaSeason.Season.Count)) {
                _plugin!.DataQueue.QueueDataOperation(() => {
                    Season = seasonIndex + 1;
                    Refresh();
                });
            }

            //if (ImGui.BeginCombo("##seasonCombo", season, ImGuiComboFlags.NoArrowButton)) {
            //    ImGui.EndCombo();
            //}
        }
    }
}
