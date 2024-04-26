using ImGuiNET;
using System;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public class LocalPlayerFilter : DataFilter {
    public override string Name => "Local Player";

    public override string HelpMessage => "Will only include matches using the currently logged-in character. Useful if you use multiple characters and want to view results separately.";
    public bool CurrentPlayerOnly { get; set; }
    public LocalPlayerFilter() { }

    internal LocalPlayerFilter(Plugin plugin, Func<Task> action, LocalPlayerFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            CurrentPlayerOnly = filter.CurrentPlayerOnly;
        }
    }

    internal override void Draw() {
        bool currentPlayerOnly = CurrentPlayerOnly;
        if(ImGui.Checkbox("Current player only", ref currentPlayerOnly)) {
            _plugin!.DataQueue.QueueDataOperation(async () => {
                CurrentPlayerOnly = currentPlayerOnly;
                await Refresh();
            });
        }
    }
}
