using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public class FLStatSourceFilter : StatSourceFilter, IEquatable<FLStatSourceFilter> {

    public static new Dictionary<StatSource, string> FilterNames => new() {
        { StatSource.LocalPlayer, "Local Player" },
        { StatSource.Teammate, "Teammates" },
        { StatSource.Opponent, "Opponents" },
    };

    public FLStatSourceFilter() {
        Initialize();
    }

    public FLStatSourceFilter(FLStatSourceFilter filter) {
        Initialize(filter);
    }

    internal FLStatSourceFilter(Plugin plugin, Func<Task> action, FLStatSourceFilter? filter = null) : base(plugin, action) {
        Initialize(filter);
    }

    private void Initialize(FLStatSourceFilter? filter = null) {
        FilterState = new() {
                {StatSource.LocalPlayer, true },
                {StatSource.Teammate, true },
                {StatSource.Opponent, true },
        };
        if(filter is not null) {
            foreach(var category in filter.FilterState) {
                FilterState[category.Key] = category.Value;
            }
            InheritFromPlayerFilter = filter.InheritFromPlayerFilter;
        }
        UpdateAllSelected();
    }

    public bool Equals(FLStatSourceFilter? other) {
        return FilterState.All(x => x.Value == other?.FilterState[x.Key]) && (InheritFromPlayerFilter == other?.InheritFromPlayerFilter);
    }
}
