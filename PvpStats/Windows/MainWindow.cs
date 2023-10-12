using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace PvpStats.Windows;

internal class MainWindow : Window {

    private Plugin _plugin;

    internal MainWindow(Plugin plugin) : base("Pvp Stats") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Always;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(200, 50),
            MaximumSize = new Vector2(500, 350)
        };
        _plugin = plugin;
    }

    public override void OnClose() {
        base.OnClose();
    }

    public override void PreDraw() {
        base.PreDraw();
    }

    public override void Draw() {
    }

}
