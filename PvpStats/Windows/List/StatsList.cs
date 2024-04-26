using ImGuiNET;
using System;
using System.Threading;

namespace PvpStats.Windows.List;
internal class StatsList<T> : FilteredList<T> {
    public StatsList(Plugin plugin, SemaphoreSlim? interlock = null) : base(plugin, interlock) {
    }

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable
    | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;
    protected override bool ShowHeader { get; set; } = true;
    protected override bool ChildWindow { get; set; } = false;
    protected bool TriggerSort { get; set; }

    public override void DrawListItem(T item) {
        throw new NotImplementedException();
    }

    public override void OpenFullEditDetail(T item) {
        throw new NotImplementedException();
    }

    public override void OpenItemDetail(T item) {
    }
}
