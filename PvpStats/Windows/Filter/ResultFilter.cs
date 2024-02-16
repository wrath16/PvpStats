using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PvpStats.Windows.Filter;

public enum MatchResult {
    Any,
    Win,
    Loss,
    Other
}

public class ResultFilter : DataFilter {
    public override string Name => "Result";
    public MatchResult Result { get; set; }
    private List<string> _resultCombo = new();

    public ResultFilter() { }

    internal ResultFilter(Plugin plugin, Action action, ResultFilter? filter = null) : base(plugin, action) {
        Result = MatchResult.Any;
        if (filter is not null) {
            Result = filter.Result;
        }
        var results = Enum.GetValues(typeof(MatchResult)).Cast<MatchResult>();
        foreach (var result in results) {
            _resultCombo.Add(result.ToString());
        }
    }

    internal override void Draw() {
        int currentIndex = (int)Result;
        //bool allSelected = AllSelected;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if (ImGui.Combo($"##matchResultCombo", ref currentIndex, _resultCombo.ToArray(), _resultCombo.Count)) {
            _plugin!.DataQueue.QueueDataOperation(() => {
                Result = (MatchResult)currentIndex;
                Refresh();
            });
        }
    }
}
