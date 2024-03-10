using Dalamud.Configuration;
using System;
using System.Threading;

namespace PvpStats.Settings;

[Serializable]
public class Configuration : IPluginConfiguration {
    public static int CurrentVersion = 0;
    public int Version { get; set; } = CurrentVersion;
    public FilterConfiguration MatchWindowFilters { get; set; } = new();
    public bool LeftPlayerTeam { get; set; } = false;
    public bool AnchorTeamNames { get; set; } = true;
    public bool ResizeableMatchWindow { get; set; } = true;
    public bool SizeFiltersToFit { get; set; } = false;
    public bool PersistWindowSizePerTab { get; set; } = true;
    public float FilterRatio { get; set; } = 3.2f;
    public uint FilterHeight { get; set; } = 250;
    public WindowConfiguration CCWindowConfig { get; set; } = new();

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
