using Dalamud.Bindings.ImGui;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;

public class TierFilter : DataFilter {
    public override string Name => "Ranked Tier";
    public override string HelpMessage => "Ranked match tier is determined by the highest ranked player in each match.";
    public ArenaTier TierLow { get; set; } = ArenaTier.None;
    public ArenaTier TierHigh { get; set; } = ArenaTier.Ultima;
    private readonly List<string> _tierCombo = [];

    public TierFilter() { }

    internal TierFilter(Plugin plugin, Func<Task> action, TierFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            TierLow = filter.TierLow;
            TierHigh = filter.TierHigh;
        }
        var tiers = Enum.GetValues(typeof(ArenaTier)).Cast<ArenaTier>();
        foreach(var tier in tiers) {
            _tierCombo.Add(tier.ToString());
        }
    }

    internal override void Draw() {
        int indexLow = (int)TierLow;
        int indexHigh = (int)TierHigh;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        var width = ImGui.CalcItemWidth();
        if(ImGui.Combo($"##tierComboLow", ref indexLow, _tierCombo.ToArray(), _tierCombo.Count)) {
            Task.Run(async () => {
                TierLow = (ArenaTier)indexLow;
                await Refresh();
            });
        }
        ImGui.SameLine();
        ImGui.Text("to");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.Combo($"##tierComboHigh", ref indexHigh, _tierCombo.ToArray(), _tierCombo.Count)) {
            Task.Run(async () => {
                TierHigh = (ArenaTier)indexHigh;
                await Refresh();
            });
        }
    }
}
