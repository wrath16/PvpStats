using PvpStats.Types.Display;

namespace PvpStats.Windows.List;
internal abstract class CCStatsList<T> : StatsList<T, CCPlayerJobStats> where T : notnull {

    public CCStatsList(Plugin plugin) : base(plugin, null) {
    }
}
