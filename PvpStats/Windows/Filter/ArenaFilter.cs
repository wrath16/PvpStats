using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public class ArenaFilter : DataFilter {
    public override string Name => "Arena";
    [JsonIgnore]
    public bool AllSelected { get; set; }
    public Dictionary<CrystallineConflictMap, bool> FilterState { get; set; } = new();
    public int CurrentIndex { get; set; }
    private List<string> _range = new();

    public ArenaFilter() { }

    internal ArenaFilter(Plugin plugin, Func<Task> action, ArenaFilter? filter = null) : base(plugin, action) {
        FilterState = new();
        _range = new() {
            "All",
        };
        var allMaps = Enum.GetValues(typeof(CrystallineConflictMap)).Cast<CrystallineConflictMap>();
        foreach(var map in allMaps) {
            FilterState.Add(map, true);
            _range.Add(MatchHelper.GetArenaName(map));
        }
        CurrentIndex = 0;

        if(filter is not null) {
            foreach(var category in filter.FilterState) {
                FilterState[category.Key] = category.Value;
            }
            CurrentIndex = filter.CurrentIndex;
        }
        UpdateAllSelected();
    }

    private void UpdateAllSelected() {
        AllSelected = true;
        foreach(var category in FilterState) {
            AllSelected = AllSelected && category.Value;
        }
    }

    internal override void Draw() {
        int currentIndex = CurrentIndex;
        //bool allSelected = AllSelected;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if(ImGui.Combo($"##arenaRangeCombo", ref currentIndex, _range.ToArray(), _range.Count)) {
            _plugin!.DataQueue.QueueDataOperation(async () => {
                CurrentIndex = currentIndex;
                if(currentIndex == 0) {
                    foreach(var item in FilterState) {
                        FilterState[item.Key] = true;
                    }
                } else {
                    //cast as int
                    CrystallineConflictMap selectedMap = (CrystallineConflictMap)currentIndex - 1;
                    foreach(var item in FilterState) {
                        FilterState[item.Key] = false;
                    }
                    FilterState[selectedMap] = true;
                }
                UpdateAllSelected();
                await Refresh();
            });
        }
    }
}
