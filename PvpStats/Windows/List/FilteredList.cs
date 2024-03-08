﻿using Dalamud.Interface.Utility;
using ImGuiNET;
using LiteDB;
using PvpStats.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace PvpStats.Windows.List;

public struct ColumnParams {
    public string Name;
    public ImGuiTableColumnFlags Flags;
    public float Width;
    public uint Id;
}

internal abstract class FilteredList<T> {

    protected SemaphoreSlim RefreshLock = new SemaphoreSlim(1);
    protected Plugin _plugin;

    public const int PageSize = 100;
    public int PageNumber { get; set; } = 0;

    public virtual List<T> DataModel { get; set; } = new();
    public List<T> CurrentPage { get; private set; } = new();
    public T? SelectedRow { get; set; }

    protected virtual List<ColumnParams> Columns { get; set; } = new();
    protected virtual ImGuiTableFlags TableFlags { get; set; }
    protected virtual ImGuiWindowFlags ChildFlags { get; set; } = ImGuiWindowFlags.None;
    protected virtual string TableName => $"##{GetHashCode()}-Table";
    protected virtual bool ShowHeader { get; set; } = false;
    protected virtual bool ChildWindow { get; set; } = true;

    public abstract void RefreshDataModel();
    protected abstract void PostColumnSetup();
    public abstract void DrawListItem(T item);
    public abstract void OpenItemDetail(T item);
    public abstract void OpenFullEditDetail(T item);

    public FilteredList(Plugin plugin) {
        _plugin = plugin;
        GoToPage();
    }

    internal void Refresh(List<T> dataModel) {
        DataModel = dataModel;
        RefreshDataModel();
        GoToPage();
    }

    public void GoToPage(int? pageNumber = null) {
        pageNumber ??= PageNumber;
        PageNumber = (int)pageNumber;
        CurrentPage = DataModel.Skip(PageNumber * PageSize).Take(PageSize).ToList();
    }

    public void Draw() {
        if(!RefreshLock.Wait(0)) {
            return;
        }

        try {
            if(ImGui.BeginChild($"##{GetHashCode()}-Child", new Vector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale), true, ChildFlags)) {
                DrawTable();
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
        } finally {
            RefreshLock.Release();
        }
    }

    private void DrawTable() {
        if(ImGui.BeginTable(TableName, Columns.Count, TableFlags)) {
            //setup columns
            foreach(var column in Columns) {
                ImGui.TableSetupColumn(column.Name, column.Flags, column.Width, column.Id);
            }
            PostColumnSetup();
            if(ShowHeader) {
                //ImGui.TableSetupScrollFreeze(1, 1);
                //ImGui.TableHeadersRow();
                foreach(var column in Columns) {
                    ImGui.TableNextColumn();
                    var tableHeader = ImGuiHelper.WrappedString(column.Name, 50f);
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 1f);
                    //this is stupid!
                    if(ImGui.GetColumnIndex() == 0) {
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4f * ImGuiHelpers.GlobalScale);
                    }
                    //ImGuiHelper.CenterAlignCursor(tableHeader);
                    ImGui.TableHeader(tableHeader);
                }
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
    }
}
