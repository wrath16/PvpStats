using PvpStats.Types.Display;

namespace PvpStats.Windows.List;
internal abstract class FLStatsList<T> : StatsList<T, FLPlayerJobStats> where T : notnull {

    public FLStatsList(Plugin plugin) : base(plugin, null) {
    }
}
