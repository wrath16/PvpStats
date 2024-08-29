using PvpStats.Types.Display;
using PvpStats.Windows.Tracker;

namespace PvpStats.Windows.List;
internal abstract class CCStatsList<T> : StatsList<T, CCPlayerJobStats> where T : notnull {

    protected CCTrackerWindow Window;

    public CCStatsList(Plugin plugin, CCTrackerWindow window) : base(plugin, null) {
        Window = window;
    }
}
