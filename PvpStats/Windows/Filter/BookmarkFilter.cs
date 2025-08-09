using Dalamud.Bindings.ImGui;
using System;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;

public class BookmarkFilter : DataFilter {
    public override string Name => "Favorites Only";
    public bool BookmarkedOnly { get; set; } = false;

    public BookmarkFilter() { }

    internal BookmarkFilter(Plugin plugin, Func<Task> action, BookmarkFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            BookmarkedOnly = filter.BookmarkedOnly;
        }
    }

    internal override void Draw() {
        bool bookMarkedOnly = BookmarkedOnly;
        if(ImGui.Checkbox("", ref bookMarkedOnly)) {
            Task.Run(async () => {
                BookmarkedOnly = bookMarkedOnly;
                await Refresh();
            });
        }
    }
}
