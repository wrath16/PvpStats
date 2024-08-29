using PvpStats.Types.Display;
using PvpStats.Windows.Tracker;

namespace PvpStats.Windows.List;
internal abstract class FLStatsList<T> : StatsList<T, FLPlayerJobStats> where T : notnull {

    protected FLTrackerWindow Window;

    public FLStatsList(Plugin plugin, FLTrackerWindow window) : base(plugin, null) {
        Window = window;
    }
}
