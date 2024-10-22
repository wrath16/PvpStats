using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using System.Numerics;

namespace PvpStats.Windows;
internal class SplashWindow : Window {

    private Plugin _plugin;

    public SplashWindow(Plugin plugin) : base("PvP Tracker") {
        _plugin = plugin;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(185, 175),
            MaximumSize = new Vector2(185, 175)
        };
        Flags |= ImGuiWindowFlags.NoResize;
    }

    public override void Draw() {
        ImGui.TextUnformatted("Trackers:");
        if(ImGui.Button("Crystalline Conflict")) {
            _plugin.WindowManager.OpenCCWindow();

        }
        if(ImGui.Button("Frontline")) {
            _plugin.WindowManager.OpenFLWindow();
        }
        if(ImGui.Button("Rival Wings")) {
            _plugin.WindowManager.OpenRWWindow();
        }

        //ImGui.NewLine();
        ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 25f * ImGuiHelpers.GlobalScale);
        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Cog.ToIconString()}##--OpenSettings")) {
                _plugin.WindowManager.OpenConfigWindow();
            }
        }
        ImGuiHelper.WrappedTooltip("Settings");
        ImGui.SameLine();
        //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 30f * ImGuiHelpers.GlobalScale);
        ImGuiHelper.DonateButton();
    }
}
