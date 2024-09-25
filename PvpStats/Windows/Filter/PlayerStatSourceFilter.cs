using ImGuiNET;
using PvpStats.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;

public class PlayerStatSourceFilter : StatSourceFilter, IEquatable<PlayerStatSourceFilter> {

    public override string Name => "Stat Source";

    public PlayerStatSourceFilter() {
        Initialize();
    }

    public PlayerStatSourceFilter(StatSourceFilter filter) {
        Initialize(filter);
    }

    internal PlayerStatSourceFilter(Plugin plugin, Func<Task> action, StatSourceFilter? filter = null) : base(plugin, action) {
        Initialize(filter);
    }

    private void Initialize(StatSourceFilter? filter = null) {
        FilterState = new() {
                {StatSource.LocalPlayer, true },
                {StatSource.Teammate, true },
                {StatSource.Opponent, true },
                {StatSource.Spectated, true },
            };

        if(filter is not null) {
            foreach(var category in filter.FilterState) {
                FilterState[category.Key] = category.Value;
            }
            InheritFromPlayerFilter = filter.InheritFromPlayerFilter;
        }
        UpdateAllSelected();
    }

    internal override void Draw() {
        bool inheritFromPlayerFilter = InheritFromPlayerFilter;
        if(ImGui.Checkbox($"Inherit from player filter##{GetHashCode()}", ref inheritFromPlayerFilter)) {
            _plugin!.DataQueue.QueueDataOperation(async () => {
                InheritFromPlayerFilter = inheritFromPlayerFilter;
                await Refresh();
            });
        }
        ImGuiHelper.HelpMarker("Will only include stats for players who match all conditions of the player filter.");
    }

    public bool Equals(PlayerStatSourceFilter? other) {
        return FilterState.All(x => x.Value == other?.FilterState[x.Key]) && (InheritFromPlayerFilter == other?.InheritFromPlayerFilter);
    }
}
