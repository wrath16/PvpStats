using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;

namespace PvpStats.Windows.List;
internal abstract class JobStatsList<T, U> : StatsList<Job, T, U> where T : PlayerJobStats where U : PvpMatch {

    internal OtherPlayerFilter PlayerFilter { get; private set; }
    public StatSourceFilter StatSourceFilter { get; protected set; }

    public JobStatsList(Plugin plugin, StatSourceFilter statSourceFilter, OtherPlayerFilter playerFilter) : base(plugin) {
        StatSourceFilter = statSourceFilter;
        PlayerFilter = playerFilter;
    }

    protected override void PreTableDraw() {
        using(var filterTable = ImRaii.Table("jobListFilterTable", 2)) {
            if(filterTable) {
                ImGui.TableSetupColumn("filterName", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 110f);
                ImGui.TableSetupColumn($"filters", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Include stats from:");
                ImGui.TableNextColumn();
                StatSourceFilter.Draw();
            }
        }
        ImGui.AlignTextToFramePadding();
        ImGuiHelper.HelpMarker("Right-click table header for column options.", false, true);
        ImGui.SameLine();
        CSVButton();
    }
}
