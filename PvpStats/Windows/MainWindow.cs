using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Windows.List;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows;

internal class MainWindow : Window {

    private Plugin _plugin;
    private CrystallineConflictList ccMatches;

    internal MainWindow(Plugin plugin) : base("Pvp Stats") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Always;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(750, 1500)
        };
        _plugin = plugin;
        ccMatches = new(plugin);
    }

    public override void OnClose() {
        base.OnClose();
    }

    public override void PreDraw() {
        base.PreDraw();
    }

    public void Refresh() {
        ccMatches.Refresh();
    }

    public override void Draw() {
        if (ImGui.BeginTabBar("TabBar", ImGuiTabBarFlags.None)) {
            if (ImGui.BeginTabItem("Match History")) {

                ccMatches.Draw();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Players")) {

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

    }
}
