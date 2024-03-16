using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal class StatsList<T> : FilteredList<T> {
    public StatsList(Plugin plugin) : base(plugin) {
    }

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable
    | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;
    protected override bool ShowHeader { get; set; } = true;
    protected override bool ChildWindow { get; set; } = false;

    public override void DrawListItem(T item) {
        throw new NotImplementedException();
    }

    public override void OpenFullEditDetail(T item) {
        throw new NotImplementedException();
    }

    public override void OpenItemDetail(T item) {
        throw new NotImplementedException();
    }

    public override void RefreshDataModel() {
        throw new NotImplementedException();
    }
}
