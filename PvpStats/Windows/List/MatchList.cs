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
internal abstract class MatchList<T> : FilteredList<T> where T : PvpMatch {

    protected MatchCacheService<T> Cache;

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Hideable;
    protected override ImGuiWindowFlags ChildFlags { get; set; } = ImGuiWindowFlags.AlwaysVerticalScrollbar;
    protected override bool ContextMenu { get; set; } = true;
    protected override bool DynamicColumns { get; set; } = true;

    private bool _tagPopupOpen = false;
    private Dictionary<ObjectId, uint> _popupIds = new();

    protected abstract string CSVRow(T match);

    public MatchList(Plugin plugin, MatchCacheService<T> cache, SemaphoreSlim? interlock = null) : base(plugin, interlock) {
        Cache = cache;
    }

    public async override Task RefreshDataModel() {
        await Task.CompletedTask;
    }

    protected override void PreChildDraw() {
        ImGuiHelper.CSVButton(() => {
            _plugin.DataQueue.QueueDataOperation(() => {
                ListCSV = CSVHeader();
                foreach(var row in DataModel) {
                    ListCSV += CSVRow(row);
                }
                Task.Run(() => {
                    ImGui.SetClipboardText(ListCSV);
                });
            });
        });
        ImGui.SameLine();
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Ban.ToIconString()}##CloseAllMatches")) {
                _plugin.DataQueue.QueueDataOperation(_plugin.WindowManager.CloseAllMatchWindows);
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
        _plugin.DataQueue.QueueDataOperation(() => {
            _plugin.WindowManager.OpenMatchDetailsWindow(item);
        });
    }

    public override void OpenFullEditDetail(T item) {
        _plugin.DataQueue.QueueDataOperation(() => {
            _plugin.WindowManager.OpenFullEditWindow(item);
        });
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
        _plugin.WindowManager.SetTagsPopup(item, Cache, ref _tagPopupOpen);
        _popupIds.TryAdd(item.Id, ImGui.GetID($"{item.Id}--TagsPopup"));
    }
}
