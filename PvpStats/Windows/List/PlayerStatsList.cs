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

namespace PvpStats.Windows.List;
internal abstract class PlayerStatsList<T, U> : StatsList<PlayerAlias, T> where T : PlayerJobStats where U : PvpMatch {

    internal OtherPlayerFilter PlayerFilter { get; private set; }
    public PlayerStatSourceFilter StatSourceFilter { get; protected set; }
    public MinMatchFilter MinMatchFilter { get; protected set; }
    public PlayerQuickSearchFilter PlayerQuickSearchFilter { get; protected set; }

    public Dictionary<PlayerAlias, Dictionary<PlayerAlias, int>> ActiveLinks { get; protected set; } = new();

    protected List<U> Matches = new();

    public PlayerStatsList(Plugin plugin, PlayerStatSourceFilter statSourceFilter, MinMatchFilter minMatchFilter, PlayerQuickSearchFilter quickSearchFilter, OtherPlayerFilter playerFilter) : base(plugin) {

        //note that draw and refresh are not utilized!
        StatSourceFilter = statSourceFilter;
        MinMatchFilter = minMatchFilter;
        PlayerQuickSearchFilter = quickSearchFilter;
        PlayerFilter = playerFilter;
        //Reset();
    }

    protected override void PreTableDraw() {
        StatSourceFilter.Draw();

        int minMatches = (int)MinMatchFilter.MinMatches;
        ImGuiHelper.SetDynamicWidth(150f, 250f, 3f);
        if(ImGui.SliderInt("Min. matches", ref minMatches, 1, 100)) {
            MinMatchFilter.MinMatches = (uint)minMatches;
            ApplyQuickFilters(MinMatchFilter.MinMatches, PlayerQuickSearchFilter.SearchText);
        }
        ImGui.SameLine();
        string quickSearch = PlayerQuickSearchFilter.SearchText;
        ImGuiHelper.SetDynamicWidth(150f, 250f, 3f);
        if(ImGui.InputTextWithHint("###playerQuickSearch", "Search...", ref quickSearch, 100)) {
            PlayerQuickSearchFilter.SearchText = quickSearch;
            ApplyQuickFilters(MinMatchFilter.MinMatches, PlayerQuickSearchFilter.SearchText);
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

    protected override void PostColumnSetup() {
        ImGui.TableSetupScrollFreeze(1, 1);
        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(sortSpecs.SpecsDirty || TriggerSort) {
            TriggerSort = false;
            sortSpecs.SpecsDirty = false;
            //this causes conflicts when multiple tracker windows refresh at once
            _plugin.DataQueue.QueueDataOperation(() => {
                SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
                GoToPage(0);
            });
        }
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
