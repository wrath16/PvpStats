using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public abstract class DataFilter {
    protected Plugin? _plugin;
    [JsonIgnore]
    public virtual string Name => "";
    [JsonIgnore]
    public virtual string? HelpMessage { get; }
    private Func<Task>? RefreshData { get; init; }

    [JsonConstructor]
    public DataFilter() {
    }

    protected DataFilter(Plugin plugin, Func<Task> action) {
        _plugin = plugin;
        RefreshData = action;
    }

    internal async Task Refresh() {
        //_plugin.DataQueue.QueueDataOperation(() => RefreshData());
        if(RefreshData is null) {
            throw new InvalidOperationException("No refresh action initialized!");
        }
        await RefreshData();
    }

    internal abstract void Draw();
}
