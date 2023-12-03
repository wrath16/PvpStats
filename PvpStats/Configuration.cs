using Dalamud.Configuration;
using System;

namespace PvpStats.Settings;

public enum ProgressTableRate {
    Total,
    Previous
}

public enum ProgressTableCount {
    All,
    Last
}

public enum ClearSequenceCount {
    All,
    Last
}

[Serializable]
public class Configuration : IPluginConfiguration {
    public static int CurrentVersion = 0;
    public int Version { get; set; } = CurrentVersion;

    [NonSerialized]
    private Plugin? _plugin;

    public Configuration() {
    }

    public void Initialize(Plugin plugin) {
        _plugin = plugin;
    }

    public void Save() {
        _plugin!.PluginInterface.SavePluginConfig(this);
    }
}
