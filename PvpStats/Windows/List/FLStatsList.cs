using PvpStats.Types.Display;
using PvpStats.Windows.Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            var props2 = prop.PropertyType.GetProperties();
            foreach(var prop2 in props2) {
                var propId = $"{prop.Name}.{prop2.Name}".GetHashCode();
                if((uint)propId == columnId) {
                    return (prop, prop2);
                }
            }
        }
        return (null, null);
    }
}
