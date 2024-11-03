using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;

public class DurationFilter : DataFilter {
    public override string Name => "Duration";
    public int DirectionIndex { get; set; } = 0;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    private readonly List<string> _combo = ["≥", "＜"];
    private string _lastDuration = "";

    public DurationFilter() { }

    internal DurationFilter(Plugin plugin, Func<Task> action, DurationFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            DirectionIndex = filter.DirectionIndex;
            Duration = filter.Duration;
        }
    }

    internal override void Draw() {
        using var table = ImRaii.Table("###durationFilterTable", 2);
        if(!table) return;
        ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X / 2);
        ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();

        int currentIndex = DirectionIndex;
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 50f);
        if(ImGui.Combo($"##durationCombo", ref currentIndex, _combo.ToArray(), _combo.Count)) {
            Task.Run(async () => {
                DirectionIndex = currentIndex;
                await Refresh();
            });
        }
        var duration = ImGuiHelper.GetTimeSpanString(Duration);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.InputText($"##matchDuration", ref duration, 50, ImGuiInputTextFlags.None)) {
            if(duration != _lastDuration) {
                _lastDuration = duration;
                if(TimeSpan.TryParseExact(duration, @"m\:ss", CultureInfo.CurrentCulture, out TimeSpan y)) {
                    Task.Run(async () => {
                        Duration = y;
                        await Refresh();
                    });
                }
            }
        }
        ImGui.TableNextColumn();
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}")) {
                Task.Run(async () => {
                    DirectionIndex = 0;
                    Duration = TimeSpan.Zero;
                    await Refresh();
                });
            }
        }
        ImGuiHelper.WrappedTooltip("Reset");
    }
}
