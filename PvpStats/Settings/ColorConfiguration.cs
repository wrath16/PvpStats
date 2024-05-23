using Dalamud.Interface.Colors;
using System.Numerics;

namespace PvpStats.Settings;
public class ColorConfiguration {
    public Vector4 Header { get; set; } = ImGuiColors.DalamudYellow;
    public Vector4 CCLocalPlayer { get; set; } = ImGuiColors.DalamudYellow;
    public Vector4 Win { get; set; } = ImGuiColors.HealerGreen;
    public Vector4 Loss { get; set; } = ImGuiColors.DalamudRed;
    public Vector4 Other { get; set; } = ImGuiColors.DalamudGrey;
    public Vector4 CCPlayerTeam { get; set; } = ImGuiColors.TankBlue;
    public Vector4 CCEnemyTeam { get; set; } = ImGuiColors.DPSRed;

    public Vector4 Maelstrom { get; set; } = ImGuiColors.DPSRed;
    public Vector4 Adders { get; set; } = ImGuiColors.DalamudYellow;
    public Vector4 Flames { get; set; } = ImGuiColors.TankBlue;

    public Vector4 Tank { get; set; } = ImGuiColors.TankBlue;
    public Vector4 Healer { get; set; } = ImGuiColors.HealerGreen;
    public Vector4 Melee { get; set; } = ImGuiColors.DPSRed;
    public Vector4 Ranged { get; set; } = ImGuiColors.DalamudOrange;
    public Vector4 Caster { get; set; } = ImGuiColors.ParsedPink;

    public Vector4 StatHigh { get; set; } = new Vector4(0f, 1f, 0f, 1f);
    public Vector4 StatLow { get; set; } = new Vector4(1f, 0f, 0f, 1f);

    public Vector4 Favorite { get; set; } = ImGuiColors.DalamudYellow;
}
