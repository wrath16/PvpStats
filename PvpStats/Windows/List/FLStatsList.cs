using PvpStats.Types.Display;
using PvpStats.Windows.Tracker;
using System.Reflection;

namespace PvpStats.Windows.List;
internal abstract class FLStatsList<T> : StatsList<T> {

    protected FLTrackerWindow Window;

    public FLStatsList(Plugin plugin, FLTrackerWindow window) : base(plugin, null) {
        Window = window;
    }

    protected (PropertyInfo?, PropertyInfo?) GetStatsPropertyFromId(uint columnId) {
        var props = typeof(FLPlayerJobStats).GetProperties();
        //iterate to two levels
        foreach(var prop in props) {
            var propId1 = $"{prop.Name}".GetHashCode();
            if((uint)propId1 == columnId) {
                return (prop, null);
            }

            var props2 = prop.PropertyType.GetProperties();
            foreach(var prop2 in props2) {
                var propId2 = $"{prop.Name}.{prop2.Name}".GetHashCode();
                if((uint)propId2 == columnId) {
                    return (prop, prop2);
                }
            }
        }
        return (null, null);
    }
}
