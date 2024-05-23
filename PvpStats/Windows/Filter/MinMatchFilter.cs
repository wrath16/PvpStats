using ImGuiNET;
using PvpStats.Helpers;
using System;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public class MinMatchFilter : DataFilter {
    public override string Name => "Min. Matches";

    public uint MinMatches { get; set; } = 1;

    public MinMatchFilter() { }

    public MinMatchFilter(Plugin plugin, Func<Task> action, MinMatchFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            MinMatches = filter.MinMatches;
        }
    }

    //not used!
    internal override void Draw() {
        int minMatches = (int)MinMatches;
        ImGuiHelper.SetDynamicWidth(150f, 250f, 3f);
        if(ImGui.SliderInt("Min. matches", ref minMatches, 1, 100)) {
            _plugin!.DataQueue.QueueDataOperation(async () => {
                MinMatches = (uint)minMatches;
                await Refresh();
            });
        }
    }
}
