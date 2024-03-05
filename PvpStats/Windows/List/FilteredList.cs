using Dalamud.Interface.Utility;
using ImGuiNET;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PvpStats.Windows.List;

public struct ColumnParams {
    public string Name;
    public ImGuiTableColumnFlags Flags;
    public float Width;
}

internal abstract class FilteredList<T> {

    //private SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
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
        GoToPage();
    }

    internal void Refresh(List<T> dataModel) {
        DataModel = dataModel;
        GoToPage();
    }

    public void GoToPage(int? pageNumber = null) {
        pageNumber ??= PageNumber;
        PageNumber = (int)pageNumber;
        CurrentPage = DataModel.Skip(PageNumber * PageSize).Take(PageSize).ToList();
    }

    public void Draw() {
        if(ImGui.BeginChild("scrolling", new Vector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale), true)) {
            if(ImGui.BeginTable($"##{GetHashCode()}-Table", Columns.Count, TableFlags)) {
                //setup columns
                foreach(var column in Columns) {
                    ImGui.TableSetupColumn(column.Name, column.Flags, column.Width);
                }
                ImGui.TableNextRow();
                foreach(var item in CurrentPage) {
                    ImGui.TableNextColumn();
                    if(ImGui.Selectable($"##{item!.GetHashCode()}-selectable", false, ImGuiSelectableFlags.SpanAllColumns)) {
                        OpenItemDetail(item);
                    }
#if DEBUG
                    if(ImGui.BeginPopupContextItem($"##{item!.GetHashCode()}--ContextMenu", ImGuiPopupFlags.MouseButtonRight)) {
                        if(ImGui.MenuItem($"Edit document##{item!.GetHashCode()}--FullEditContext")) {
                            OpenFullEditDetail(item);
                        }
                        ImGui.EndPopup();
                    }
#endif
                    ImGui.SameLine();
                    DrawListItem(item);
                }
                ImGui.EndTable();
            }
            ImGui.EndChild();
        }

        ImGui.Text("");
        if(PageNumber > 0) {
            ImGui.SameLine();
            if(ImGui.Button($"Previous {PageSize}")) {
                _plugin.DataQueue.QueueDataOperation(() => {
                    PageNumber--;
                    GoToPage();
                });
            }
        }

        if((PageNumber + 1) * PageSize < DataModel.Count) {
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 65f * ImGuiHelpers.GlobalScale);
            if(ImGui.Button($"Next {PageSize}")) {
                _plugin.DataQueue.QueueDataOperation(() => {
                    PageNumber++;
                    GoToPage();
                });
            }
        }
    }
}
