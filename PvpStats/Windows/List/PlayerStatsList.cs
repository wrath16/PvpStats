using Dalamud.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal abstract class PlayerStatsList<T, U> : StatsList<PlayerAlias, T, U> where T : PlayerJobStats where U : PvpMatch {

    internal OtherPlayerFilter PlayerFilter { get; private set; }
    public PlayerStatSourceFilter StatSourceFilter { get; protected set; }
    public MinMatchFilter MinMatchFilter { get; protected set; }
    public PlayerQuickSearchFilter PlayerQuickSearchFilter { get; protected set; }

    protected StatSourceFilter _lastStatSourceFilter = new();
    protected OtherPlayerFilter _lastPlayerFilter = new();

    public Dictionary<PlayerAlias, Dictionary<PlayerAlias, int>> ActiveLinks { get; protected set; } = new();

    public PlayerStatsList(Plugin plugin, StatSourceFilter? statSourceFilter, MinMatchFilter? minMatchFilter, PlayerQuickSearchFilter? quickSearchFilter, OtherPlayerFilter playerFilter) : base(plugin) {
        //note that draw and refresh are not utilized!
        StatSourceFilter = new PlayerStatSourceFilter(plugin, Refresh, statSourceFilter);
        MinMatchFilter = new MinMatchFilter(plugin, Refresh, minMatchFilter);
        PlayerQuickSearchFilter = new PlayerQuickSearchFilter(plugin, Refresh, quickSearchFilter);
        PlayerFilter = playerFilter;
        FullCSVRows = false;
        //Reset();
    }

    protected override async Task RefreshInner(List<U> matches, List<U> additions, List<U> removals) {
        bool statFilterChange = !StatSourceFilter.Equals(_lastStatSourceFilter);
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
        _lastStatSourceFilter = new(StatSourceFilter!);
        _lastPlayerFilter = new(PlayerFilter!);
        base.PostRefresh(matches, additions, removals);
    }

    protected override void PreTableDraw() {
        StatSourceFilter.Draw();

        int minMatches = (int)MinMatchFilter.MinMatches;
        ImGuiHelper.SetDynamicWidth(150f, 250f, 3f);
        if(ImGui.SliderInt("Min. matches", ref minMatches, 1, 100)) {
            MinMatchFilter.MinMatches = (uint)minMatches;
            RefreshQueue.QueueDataOperation(() => {
                ApplyQuickFilters(MinMatchFilter.MinMatches, PlayerQuickSearchFilter.SearchText);
            });
        }
        ImGui.SameLine();
        string quickSearch = PlayerQuickSearchFilter.SearchText;
        ImGuiHelper.SetDynamicWidth(150f, 250f, 3f);
        if(ImGui.InputTextWithHint("###playerQuickSearch", "Search...", ref quickSearch, 100)) {
            PlayerQuickSearchFilter.SearchText = quickSearch;
            RefreshQueue.QueueDataOperation(() => {
                ApplyQuickFilters(MinMatchFilter.MinMatches, PlayerQuickSearchFilter.SearchText);
            });
        }
        ImGuiHelper.HelpMarker("Comma separate multiple players.");

        ImGui.AlignTextToFramePadding();
        ImGuiHelper.HelpMarker("Right-click table header for column options.", false, true);
        ImGui.SameLine();
        //ImGuiHelper.CSVButton(ListCSV);
        CSVButton();

        ImGui.SameLine();
        ImGui.TextUnformatted($"Total players:   {DataModel.Count}");
    }

    protected void ApplyQuickFilters(uint minMatches, string searchText) {
        List<PlayerAlias> DataModelTruncated = new();
        var playerNames = searchText.Trim().Split(",").ToList();
        foreach(var player in DataModelUntruncated) {
            bool minMatchPass = StatsModel[player].TotalMatches >= minMatches;
            bool namePass = searchText.IsNullOrEmpty()
                || playerNames.Any(x => x.Length > 0 && player.FullName.Contains(x.Trim(), StringComparison.OrdinalIgnoreCase))
                || playerNames.Any(x => x.Length > 0 && ActiveLinks.Where(y => y.Key.Equals(player)).Any(y => y.Value.Any(z => z.Key.FullName.Contains(x.Trim(), StringComparison.OrdinalIgnoreCase))))
                ;
            if(minMatchPass && namePass) {
                DataModelTruncated.Add(player);
            }
        }
        DataModel = DataModelTruncated;
        GoToPage(0);
    }
}
