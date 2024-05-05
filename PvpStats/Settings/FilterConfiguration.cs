using PvpStats.Windows.Filter;
using System.Collections.Generic;

namespace PvpStats.Settings;

//all because configuration doesn't like polymorphism
public class FilterConfiguration {
    public MatchTypeFilter? MatchTypeFilter { get; set; }
    public TimeFilter? TimeFilter { get; set; }
    public LocalPlayerFilter? LocalPlayerFilter { get; set; }
    public LocalPlayerJobFilter? LocalPlayerJobFilter { get; set; }
    public MiscFilter? MiscFilter { get; set; }
    public StatSourceFilter? StatSourceFilter { get; set; }
    public MinMatchFilter? MinMatchFilter { get; set; }

    public void SetFilters(List<DataFilter> filters) {
        var props = typeof(FilterConfiguration).GetProperties();
        foreach(var filter in filters) {
            foreach(var prop in props) {
                if(prop.PropertyType == filter.GetType()) {
                    prop.SetValue(this, filter);
                    break;
                }
            }
        }
    }
}
