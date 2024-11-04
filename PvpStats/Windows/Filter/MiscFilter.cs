using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public class MiscFilter : DataFilter {

    public override string Name => "Misc";

    public bool MustHaveStats { get; set; }
    public bool IncludeSpectated { get; set; } = true;
    public bool ShowDeleted { get; set; }

    public MiscFilter() { }

    internal MiscFilter(Plugin plugin, Func<Task> action, MiscFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            MustHaveStats = filter.MustHaveStats;
            ShowDeleted = filter.ShowDeleted;
            IncludeSpectated = filter.IncludeSpectated;
        }
    }

    internal override void Draw() {
        using var table = ImRaii.Table("miscFilterTable", 3, ImGuiTableFlags.NoClip);
        ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 350f));
        ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 350f));
        ImGui.TableSetupColumn($"c3", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 350f));
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        //bool mustHaveStats = MustHaveStats;
        //if(ImGui.Checkbox("Must have post-game stats", ref mustHaveStats)) {
        //    Task.Run(async () => {
        //        MustHaveStats = mustHaveStats;
        //        await Refresh();
        //    });
        //}
        bool includeSpectated = IncludeSpectated;
        if(ImGui.Checkbox("Include spectated matches", ref includeSpectated)) {
            Task.Run(async () => {
                IncludeSpectated = includeSpectated;
                await Refresh();
            });
        }
        //bool showDeleted = ShowDeleted;
        //if (ImGui.Checkbox("Show deleted/incomplete", ref showDeleted)) {
        //    Task.Run(() => {
        //        ShowDeleted = showDeleted;
        //        Refresh();
        //    });
        //}
    }
}
