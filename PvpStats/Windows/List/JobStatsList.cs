using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal abstract class JobStatsList<T, U> : StatsList<Job, T, U> where T : PlayerJobStats where U : PvpMatch {

    internal OtherPlayerFilter PlayerFilter { get; private set; }
    public StatSourceFilter StatSourceFilter { get; protected set; }

    protected StatSourceFilter _lastJobStatSourceFilter = new();
    protected OtherPlayerFilter _lastPlayerFilter = new();

    public JobStatsList(Plugin plugin, StatSourceFilter? statSourceFilter, OtherPlayerFilter playerFilter) : base(plugin) {
        //StatSourceFilter = statSourceFilter;
        StatSourceFilter = new StatSourceFilter(plugin, Refresh, statSourceFilter);
        PlayerFilter = playerFilter;
    }

    protected override async Task RefreshInner(List<U> matches, List<U> additions, List<U> removals) {
        bool statFilterChange = !StatSourceFilter.Equals(_lastJobStatSourceFilter);
        bool playerFilterChange = StatSourceFilter!.InheritFromPlayerFilter && !PlayerFilter.Equals(_lastPlayerFilter);
        if(removals.Count * 2 >= _matches.Count || statFilterChange || playerFilterChange) {
            //force full build
            Reset();
            MatchesTotal = matches.Count;
            await ProcessMatches(matches);
        } else {
            MatchesTotal = removals.Count + additions.Count;
            var removeTask = ProcessMatches(removals, true);
            var addTask = ProcessMatches(additions);
            await Task.WhenAll([removeTask, addTask]);
        }
        PostRefresh(matches, additions, removals);
        _matches = matches;
    }

    protected override void PostRefresh(List<U> matches, List<U> additions, List<U> removals) {
        _lastJobStatSourceFilter = new(StatSourceFilter!);
        _lastPlayerFilter = new(PlayerFilter!);
        base.PostRefresh(matches, additions, removals);
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
