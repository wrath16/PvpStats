using Dalamud.Configuration;
using System;
using System.Threading;

namespace PvpStats.Settings;

[Serializable]
public class Configuration : IPluginConfiguration {
    public static int CurrentVersion = 0;
    public int Version { get; set; } = CurrentVersion;
    public FilterConfiguration MatchWindowFilters { get; set; } = new();

    [NonSerialized]
    private Plugin? _plugin;
    [NonSerialized]
    private SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

    public Configuration() {
    }

    public void Initialize(Plugin plugin) {
        _plugin = plugin;
    }

    public void Save() {
        try {
            _fileLock.WaitAsync();
            _plugin!.PluginInterface.SavePluginConfig(this);
        } finally {
            _fileLock.Release();
        }
    }
}
