using ImGuiNET;
using System;

namespace PvpStats.Windows.Filter;
public class LocalPlayerFilter : DataFilter {
    public override string Name => "Local Player";
    public bool CurrentPlayerOnly { get; set; }
    public LocalPlayerFilter() { }

    internal LocalPlayerFilter(Plugin plugin, Action action, LocalPlayerFilter? filter = null) : base(plugin, action) {
        if (filter is not null) {
            CurrentPlayerOnly = filter.CurrentPlayerOnly;
        }
    }

    internal override void Draw() {
        bool currentPlayerOnly = CurrentPlayerOnly;
        if (ImGui.Checkbox("Current player only", ref currentPlayerOnly)) {
            _plugin!.DataQueue.QueueDataOperation(() => {
                CurrentPlayerOnly = currentPlayerOnly;
                Refresh();
            });
        }
    }
}
