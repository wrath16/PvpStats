using PvpStats.Types.Display;
using PvpStats.Windows.Filter;
using System.Collections.Generic;
using System.Reflection;

namespace PvpStats.Windows.List;
internal abstract class CCStatsList<T> : StatsList<T> {
    public CCStatsList(Plugin plugin) : base(plugin) {
    }

    public Dictionary<T, CCPlayerJobStats> StatsModel { get; protected set; }
    protected CrystallineConflictList ListModel { get; init; }
    protected OtherPlayerFilter? OtherPlayerFilter { get; init; }

    protected (PropertyInfo?, PropertyInfo?) GetStatsPropertyFromId(uint columnId) {
        var props = typeof(CCPlayerJobStats).GetProperties();
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
