using Dalamud.Interface.Utility;
using ImGuiNET;
using LiteDB;
using PvpStats.Helpers;
using System;
using System.Collections;
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
    protected virtual string TableId => $"##{GetHashCode()}-Table";
    protected virtual bool ShowHeader { get; set; } = false;
    protected virtual bool ChildWindow { get; set; } = true;

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
            if(ImGui.BeginChild(TableId, new Vector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale), true, ChildFlags)) {
                PreTableDraw();
                DrawTable();
            }
            ImGui.EndChild();

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

    protected virtual void PreTableDraw() {

    }

    protected virtual void PostColumnSetup() {

    }

    private void DrawTable() {
        if(ImGui.BeginTable(TableId, Columns.Count, TableFlags)) {
            try {
                //setup columns
                foreach(var column in Columns) {
                    ImGui.TableSetupColumn(column.Name, column.Flags, column.Width * ImGuiHelpers.GlobalScale, column.Id);
                }
                var clipper = new ListClipper(CurrentPage.Count, Columns.Count, true);
                PostColumnSetup();
                if(ShowHeader) {
                    //ImGui.TableSetupScrollFreeze(1, 1);
                    //ImGui.TableHeadersRow();
                    foreach(var i in clipper.Columns) {
                        var column = Columns[i];
                        ImGui.TableNextColumn();
                        //var tableHeader = ImGuiHelper.WrappedString(column.Name, 80f);
                        var tableHeader = ImGuiHelper.WrappedString(column.Name, 2);
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

                foreach(var i in clipper.Rows) {
                    var item = CurrentPage[i];
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
            } finally {
                ImGui.EndTable();
            }
        }
    }
}

//Shamelessly copy pasted from Submarine Tracker!
public unsafe class ListClipper : IEnumerable<(int, int)>, IDisposable {
    private ImGuiListClipperPtr Clipper;
    private readonly int CurrentRows;
    private readonly int CurrentColumns;
    private readonly bool TwoDimensional;
    private readonly int ItemRemainder;

    public int FirstRow { get; private set; } = -1;
    public int CurrentRow { get; private set; }
    public int DisplayEnd => Clipper.DisplayEnd;

    public IEnumerable<int> Rows {
        get {
            while(Clipper.Step()) // Supposedly this calls End()
            {
                if(Clipper.ItemsHeight > 0 && FirstRow < 0)
                    FirstRow = (int)(ImGui.GetScrollY() / Clipper.ItemsHeight);
                for(int i = Clipper.DisplayStart; i < Clipper.DisplayEnd; i++) {
                    CurrentRow = i;
                    yield return TwoDimensional ? i : i * CurrentColumns;
                }
            }
        }
    }

    public IEnumerable<int> Columns {
        get {
            var cols = (ItemRemainder == 0 || CurrentRows != DisplayEnd || CurrentRow != DisplayEnd - 1) ? CurrentColumns : ItemRemainder;
            for(int j = 0; j < cols; j++)
                yield return j;
        }
    }

    public ListClipper(int items, int cols = 1, bool twoD = false, float itemHeight = 0) {
        TwoDimensional = twoD;
        CurrentColumns = cols;
        CurrentRows = TwoDimensional ? items : (int)MathF.Ceiling((float)items / CurrentColumns);
        ItemRemainder = !TwoDimensional ? items % CurrentColumns : 0;
        Clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        Clipper.Begin(CurrentRows, itemHeight);
    }

    public IEnumerator<(int, int)> GetEnumerator() => (from i in Rows from j in Columns select (i, j)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose() {
        Clipper.Destroy(); // This also calls End() but I'm calling it anyway just in case
        GC.SuppressFinalize(this);
    }
}