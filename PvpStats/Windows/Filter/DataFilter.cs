using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
public abstract class DataFilter {
    protected Plugin? _plugin;
    [JsonIgnore]
    public virtual string Name => "";
    [JsonIgnore]
    public virtual string? HelpMessage { get; set; }
    [JsonIgnore]
    public virtual int FilterPriority { get; set; } = 0;
    private Func<Task>? RefreshData { get; init; }
    [JsonIgnore]
    protected Task<Task>? CurrentRefresh { get; set; }

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

    //must be invoked at point of input!
    protected void RateLimitRefresh(Action action) {
        if(CurrentRefresh is null || CurrentRefresh.Result.IsCompleted) {
            CurrentRefresh = (Task<Task>)Task.Run(async () => {
                action.Invoke();
                await Refresh();
            });
        }
    }

    internal abstract void Draw();
}
