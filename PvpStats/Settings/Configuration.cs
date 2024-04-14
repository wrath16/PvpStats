using Dalamud.Configuration;
using Dalamud.Interface.Colors;
using PvpStats.Helpers;
using PvpStats.Types.Player;
using System;
using System.Numerics;
using System.Threading;

namespace PvpStats.Settings;

[Serializable]
public class Configuration : IPluginConfiguration {
    public static int CurrentVersion = 0;
    public int Version { get; set; } = CurrentVersion;
    public bool EnablePlayerLinking { get; set; } = true;
    public bool EnableAutoPlayerLinking { get; set; } = true;
    public bool EnableManualPlayerLinking { get; set; } = true;
    public bool LeftPlayerTeam { get; set; } = false;
    public bool AnchorTeamNames { get; set; } = true;
    public bool ResizeableMatchWindow { get; set; } = true;
    public bool ShowBackgroundImage { get; set; } = true;
    public bool SizeFiltersToFit { get; set; } = false;
    public bool PersistWindowSizePerTab { get; set; } = true;
    public bool MinimizeWindow { get; set; } = true;
    public bool MinimizeDirectionLeft { get; set; } = false;
    public bool ResizeWindowLeft { get; set; } = false;
    public bool ColorScaleStats { get; set; } = true;
    public WindowConfiguration CCWindowConfig { get; set; } = new();
    public FilterConfiguration MatchWindowFilters { get; set; } = new();
    public ColorConfiguration Colors { get; set; } = new();

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
        //try {
        //    await _fileLock.WaitAsync();
        //    _plugin!.PluginInterface.SavePluginConfig(this);
        //} finally {
        //    _fileLock.Release();
        //}
        _plugin!.PluginInterface.SavePluginConfig(this);
    }

    public Vector4 GetJobColor(Job? job) {
        return PlayerJobHelper.GetSubRoleFromJob(job) switch {
            JobSubRole.TANK => Colors.Tank,
            JobSubRole.HEALER => Colors.Healer,
            JobSubRole.MELEE => Colors.Melee,
            JobSubRole.RANGED => Colors.Ranged,
            JobSubRole.CASTER => Colors.Caster,
            _ => ImGuiColors.DalamudWhite,
        };
    }
}
