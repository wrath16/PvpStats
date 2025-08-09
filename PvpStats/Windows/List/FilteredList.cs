using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using LiteDB;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;

public class ColumnParams {
    public string Name = "";
    public string? Header;
    public ImGuiTableColumnFlags Flags;
    public float Width;
    public uint Id;
    public int Priority;
    public int Alignment = 0;
}

public class NumericColumnParams : ColumnParams {
    public NumericColumnParams() {
        Alignment = 2;
    }
}

internal abstract class FilteredList<T, U> : Refreshable<U> where T : notnull where U : PvpMatch {

    protected SemaphoreSlim RefreshLock = new SemaphoreSlim(1);
    protected SemaphoreSlim? Interlock;
    private bool _refreshLockAcquired;
    private bool _interlockAcquired;

    protected Plugin _plugin;

    public const int PageSize = 100;
    public int PageNumber { get; set; } = 0;

    public virtual List<T> DataModel { get; set; } = new();
    public List<T> CurrentPage { get; private set; } = new();
    public T? SelectedRow { get; set; }
    public string ListCSV { get; set; } = "";

    protected virtual List<ColumnParams> Columns { get; set; } = new();
    protected virtual ImGuiTableFlags TableFlags { get; set; }
    protected virtual ImGuiWindowFlags ChildFlags { get; set; } = ImGuiWindowFlags.None;
    protected virtual string TableId => $"##{GetHashCode()}-Table";
    protected virtual bool ShowHeader { get; set; } = false;
    protected virtual bool ChildWindow { get; set; } = true;
    protected virtual bool ContextMenu { get; set; } = false;
    protected virtual bool DynamicColumns { get; set; } = false;

    public abstract void DrawListItem(T item);
    public abstract void OpenItemDetail(T item);
    public abstract void OpenFullEditDetail(T item);

    public FilteredList(Plugin plugin, SemaphoreSlim? interlock = null) {
        _plugin = plugin;
        Interlock = interlock;
        GoToPage();
    }

    protected override Task RefreshInner(List<U> matches, List<U> additions, List<U> removals) {
        PostRefresh(matches, additions, removals);
        _matches = matches;
        ListCSV = CSVHeader();
        GoToPage(0);
        return Task.CompletedTask;
    }

    public void GoToPage(int? pageNumber = null) {
        pageNumber ??= PageNumber;
        PageNumber = (int)pageNumber;
        CurrentPage = DataModel.Skip(PageNumber * PageSize).Take(PageSize).ToList();
    }

    public void Draw() {
        //if(!RefreshLock.Wait(0) || Interlock != null && !Interlock.Wait(0)) {
        //    _plugin.Log.Debug("not drawing due to refresh lock!");
        //    return;
        //}
        //if(!AcquireLocks()) {
        //    _plugin.Log.Debug("not all locks acquired!");
        //    ReleaseLocks();
        //    return;
        //}

        try {
            PreChildDraw();
            using(var child = ImRaii.Child(TableId, new Vector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale), true, ChildFlags)) {
                if(child) {
                    PreTableDraw();
                    DrawTable();
                }
            }

            ImGui.Text("");
            if(PageNumber > 0) {
                ImGui.SameLine();
                if(ImGui.Button($"Previous {PageSize}")) {
                    RefreshQueue.QueueDataOperation(() => {
                        PageNumber--;
                        GoToPage();
                    });
                }
            }

            if((PageNumber + 1) * PageSize < DataModel.Count) {
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 65f * ImGuiHelpers.GlobalScale);
                if(ImGui.Button($"Next {PageSize}")) {
                    RefreshQueue.QueueDataOperation(() => {
                        PageNumber++;
                        GoToPage();
                    });
                }
            }
        } finally {
            ReleaseLocks();
        }
    }

    private bool AcquireLocks() {
        _refreshLockAcquired = RefreshLock.Wait(0);
        _interlockAcquired = Interlock != null && Interlock.Wait(0);
        return _refreshLockAcquired && (Interlock == null || _interlockAcquired);
    }

    private void ReleaseLocks() {
        if(_refreshLockAcquired) {
            RefreshLock.Release();
        }
        if(_interlockAcquired) {
            Interlock?.Release();
        }
    }

    protected virtual void PreChildDraw() { }

    protected virtual void PreTableDraw() { }

    protected virtual void PostColumnSetup() { }

    protected virtual void ContextMenuItems(T item) { }

    protected virtual void PreListItemDraw(T item) { }

    private void DrawTable() {
        using var table = ImRaii.Table(TableId, Columns.Count, TableFlags);
        if(!table) {
            return;
        }
        //setup columns
        foreach(var column in Columns) {
            var columnWidth = column.Width * ImGuiHelpers.GlobalScale;
            var tableHeader = column.Header ?? ImGuiHelper.WrappedString(column.Name, 2);
            var headerSize = ImGui.CalcTextSize(tableHeader);
            var sizeDiff = headerSize.X - columnWidth;
            if(sizeDiff >= -5f * ImGuiHelpers.GlobalScale) {
                columnWidth = headerSize.X + 5f * ImGuiHelpers.GlobalScale;
            }
            ImGui.TableSetupColumn(column.Name, column.Flags, columnWidth, column.Id);
        }
        var clipper = new ListClipper(CurrentPage.Count, Columns.Count, true);
        PostColumnSetup();
        if(ShowHeader) {
            //ImGui.TableSetupScrollFreeze(1, 1);
            //ImGui.TableHeadersRow();
            foreach(var i in clipper.Columns) {
                if(ImGui.TableNextColumn()) {
                    var column = Columns[i];
                    var tableHeader = column.Header ?? ImGuiHelper.WrappedString(column.Name, 2);
                    //ImGui.TableHeader(tableHeader);
                    ImGuiHelper.DrawTableHeader(tableHeader, column.Alignment, true, true, -5f);
                }
            }
        }
        //ImGui.TableNextRow();
        //must also set table flags to hideable for this to work
        if(DynamicColumns) {
            int prioLevel = GetLowestPrio();
            for(int i = 0; i < Columns.Count; i++) {
                if(Columns[i].Priority > prioLevel) {
                    ImGui.TableSetColumnEnabled(i, false);
                } else {
                    ImGui.TableSetColumnEnabled(i, true);
                }
            }
        }

        foreach(var i in clipper.Rows) {
            T? item = CurrentPage[0];
            try {
                item = CurrentPage[i];
            } catch(ArgumentOutOfRangeException) {
                //suppress: this will happen during refreshes
            }
            ImGui.TableNextColumn();
            if(ImGui.Selectable($"##{item.GetHashCode()}-selectable", false, ImGuiSelectableFlags.SpanAllColumns)) {
                OpenItemDetail(item);
            }

            if(ContextMenu) {
                using var contextMenu = ImRaii.ContextPopupItem($"##{item!.GetHashCode()}--ContextMenu", ImGuiPopupFlags.MouseButtonRight);
                if(contextMenu) {
                    ContextMenuItems(item);
                }
            }
            PreListItemDraw(item);
            DrawListItem(item);
        }
    }

    protected int GetLowestPrio() {
        float width = ImGui.GetContentRegionAvail().X - 15f * ImGuiHelpers.GlobalScale; //for scrollbar...
        float currentWidth = 0f;
        int i = 0;
        //this should not be hard-coded
        for(i = 0; i < Columns.Count; i++) {
            foreach(var column in Columns.Where(x => x.Priority == i)) {
                currentWidth += column.Width * ImGuiHelpers.GlobalScale;
                if(currentWidth > width) {
                    return int.Max(0, i - 1);
                }
            }
        }
        return int.Max(0, i - 1);
    }

    protected string CSVHeader() {
        string header = "";
        foreach(var col in Columns) {
            header += col.Name + ",";
        }
        header += "\n";
        return header;
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
        Clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
        Clipper.Begin(CurrentRows, itemHeight);
    }

    public IEnumerator<(int, int)> GetEnumerator() => (from i in Rows from j in Columns select (i, j)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose() {
        Clipper.Destroy(); // This also calls End() but I'm calling it anyway just in case
        GC.SuppressFinalize(this);
    }
}