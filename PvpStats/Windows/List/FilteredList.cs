using Dalamud.Interface.Utility;
using ImGuiNET;
using LiteDB;
using Newtonsoft.Json;
using PvpStats.Windows.Detail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;

public struct ColumnParams {
    public string Name;
    public ImGuiTableColumnFlags Flags;
    public float Width;
}

internal abstract class FilteredList<T> {

    private SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
    protected Plugin _plugin;

    public const int PageSize = 100;
    public int PageNumber { get; set; } = 0;

    protected virtual List<T> DataModel { get; set; } = new();
    public List<T> CurrentPage { get; private set; } = new();

    public T? SelectedRow { get; set; }
    protected virtual List<ColumnParams> Columns { get; set; } = new();
    protected virtual ImGuiTableFlags TableFlags { get; set; }

    public abstract void RefreshDataModel();
    public abstract void DrawListItem(T item);
    public abstract void OpenItemDetail(T item);
    public abstract void OpenFullEditDetail(T item);

    public FilteredList(Plugin plugin) {
        _plugin = plugin;
        Refresh();
    }

    public Task Refresh(int? pageNumber = null) {
        return Task.Run(async () => {
            try {
                await _refreshLock.WaitAsync();
                RefreshDataModel();
                //null page number = stay on same page
                pageNumber ??= PageNumber;
                PageNumber = (int)pageNumber;
                CurrentPage = DataModel.Skip(PageNumber * PageSize).Take(PageSize).ToList();
            }
            finally {
                _refreshLock.Release();
            }
        });
    }

    public void Draw() {
        //draw filters...

        ImGui.BeginChild("scrolling", new Vector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale), true);
        ImGui.BeginTable($"##{GetHashCode()}-Table", Columns.Count, TableFlags);
        //setup columns
        foreach (var column in Columns) {
            ImGui.TableSetupColumn(column.Name, column.Flags, column.Width);
        }
        ImGui.TableNextRow();
        foreach (var item in CurrentPage) {
            ImGui.TableNextColumn();
            if (ImGui.Selectable($"##{item!.GetHashCode()}-selectable", false, ImGuiSelectableFlags.SpanAllColumns)) {
                OpenItemDetail(item);
            }
#if DEBUG
            if (ImGui.BeginPopupContextItem($"##{item!.GetHashCode()}--ContextMenu", ImGuiPopupFlags.MouseButtonRight)) {
                if (ImGui.MenuItem($"Edit document##{item!.GetHashCode()}--FullEditContext")) {
                    //_plugin.Log.Debug($"{BsonMapper.Global.Serialize(typeof(T), item).ToString()}");
                    //var x = BsonMapper.Global.Serialize(typeof(T), item).ToString();
                    //var stringReader = new StringReader(x);
                    //var stringWriter = new StringWriter();
                    //var jsonReader = new JsonTextReader(stringReader);
                    //var jsonWriter = new JsonTextWriter(stringWriter) {
                    //    Formatting = Formatting.Indented
                    //};
                    //jsonWriter.WriteToken(jsonReader);
                    //_plugin.Log.Debug($"{stringWriter.ToString()}");

                    //_plugin.Log.Debug($"{BsonMapper.Global.ToDocument(typeof(T), item).ToString()}");
                    OpenFullEditDetail(item);
                }
                ImGui.EndPopup();
            }
#endif
            ImGui.SameLine();
            DrawListItem(item);
        }
        ImGui.EndTable();
        ImGui.EndChild();

        if (PageNumber > 0) {
            if (ImGui.Button("Previous 100")) {
                Refresh(PageNumber--);
            }
        }

        if ((PageNumber + 1) * PageSize < DataModel.Count) {
            if (ImGui.Button("Next 100")) {
                Refresh(PageNumber++);
            }
        }
    }
}
