using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using LiteDB;
using PvpStats.Helpers;
using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal abstract class MatchList<T> : FilteredList<T, T> where T : PvpMatch {

    protected MatchCacheService<T> Cache;

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Hideable;
    protected override ImGuiWindowFlags ChildFlags { get; set; } = ImGuiWindowFlags.AlwaysVerticalScrollbar;
    protected override bool ContextMenu { get; set; } = true;
    protected override bool DynamicColumns { get; set; } = true;

    private Dictionary<ObjectId, uint> _popupIds = new();
    private Dictionary<ObjectId, bool> _popupStates = new();

    protected abstract string CSVRow(T match);

    public MatchList(Plugin plugin, MatchCacheService<T> cache, SemaphoreSlim? interlock = null) : base(plugin, interlock) {
        Cache = cache;
    }

    protected override Task RefreshInner(List<T> matches, List<T> additions, List<T> removals) {
        //PostRefresh(matches, additions, removals);
        _matches = matches;
        DataModel = matches;
        ListCSV = CSVHeader();
        GoToPage(0);
        return Task.CompletedTask;
    }

    protected override void Reset() {
        throw new System.NotImplementedException();
    }

    protected override void PostRefresh(List<T> matches, List<T> additions, List<T> removals) {
        throw new System.NotImplementedException();
    }

    protected override void ProcessMatch(T match, bool remove = false) {
        throw new System.NotImplementedException();
    }

    protected override void PreChildDraw() {
        ImGuiHelper.CSVButton(() => {
            Task.Run(() => {
                ListCSV = CSVHeader();
                foreach(var row in DataModel) {
                    ListCSV += CSVRow(row);
                }
                ImGui.SetClipboardText(ListCSV);
            });
        });
        ImGui.SameLine();
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Ban.ToIconString()}##CloseAllMatches")) {
                Task.Run(_plugin.WindowManager.CloseAllMatchWindows);
            }
        }
        ImGuiHelper.WrappedTooltip("Close all open match windows");
        ImGui.SameLine();
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            ImGuiHelper.RightAlignCursor(FontAwesomeIcon.Heart.ToIconString());
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X);
        }
        ImGuiHelper.DonateButton();

        //using(var popup = ImRaii.Popup("testPopup")) {
        //    if(popup) {
        //        ImGui.Text("test2");
        //        _plugin.Log.Debug("OPAIN!");
        //    }
        //}
        //_popupId = ImGui.GetID("testPopup");
    }

    public override void OpenItemDetail(T item) {
        _plugin.WindowManager.OpenMatchDetailsWindow(item);
    }

    public override void OpenFullEditDetail(T item) {
        _plugin.WindowManager.OpenFullEditWindow(item);
    }

    protected override void ContextMenuItems(T item) {
        bool isBookmarked = item.IsBookmarked;
        string tags = item.Tags;
        if(ImGui.MenuItem($"Favorite##{item!.GetHashCode()}--AddBookmark", null, isBookmarked)) {
            item.IsBookmarked = !item.IsBookmarked;
            _plugin.DataQueue.QueueDataOperation(async () => {
                await Cache.UpdateMatch(item);
            });
        }
        if(ImGui.MenuItem($"Set tags##{item!.GetHashCode()}--SetTags")) {
            //_plugin.Log.Debug($"Opening tags popup {item.Id}--TagsPopup");
            ImGui.OpenPopup(_popupIds[item.Id]);
        }

#if DEBUG
        if(ImGui.MenuItem($"Edit document##{item!.GetHashCode()}--FullEditContext")) {
            OpenFullEditDetail(item);
        }
#endif
    }

    protected override void PreListItemDraw(T item) {
        _popupStates.TryGetValue(item.Id, out var popupOpen);
        _plugin.WindowManager.SetTagsPopup(item, Cache, ref popupOpen);
        if(!_popupStates.TryAdd(item.Id, popupOpen)) {
            _popupStates[item.Id] = popupOpen;
        }
        _popupIds.TryAdd(item.Id, (ImGui.GetID($"{item.Id}--TagsPopup")));
    }
}
