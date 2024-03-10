using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using System.Numerics;

namespace PvpStats.Windows;
internal class ConfigWindow : Window {

    private Plugin _plugin;

    public ConfigWindow(Plugin plugin) : base("PvP Tracker Settings") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(300, 50),
            MaximumSize = new Vector2(400, 800)
        };
        _plugin = plugin;
    }

    public override void Draw() {
        if(ImGui.BeginTabBar("SettingsTabBar")) {
            if(ImGui.BeginTabItem("Interface")) {
                DrawInterfaceSettings();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawInterfaceSettings() {
        ImGui.TextColored(ImGuiColors.DalamudYellow, "Tracker Window");
        //var filterRatio = _plugin.Configuration.FilterRatio;
        //ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2f);
        //if(ImGui.SliderFloat("Filters height ratio", ref filterRatio, 2f, 5f)) {
        //    _plugin.Configuration.FilterRatio = filterRatio;
        //    _plugin.Configuration.Save();
        //}
        //ImGuiHelper.HelpMarker("Controls the denominator of the ratio of the window that will be occupied by the filters child.");
        //var sizeToFit = _plugin.Configuration.SizeFiltersToFit;
        //if(ImGui.Checkbox("Size to fit", ref sizeToFit)) {
        //    _plugin.Configuration.SizeFiltersToFit = sizeToFit;
        //    _plugin.Configuration.Save();
        //}

        var filterHeight = (int)_plugin.Configuration.FilterHeight;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2f);
        if(ImGui.SliderInt("Filter child height", ref filterHeight, 100, 500)) {
            _plugin.Configuration.FilterHeight = (uint)filterHeight;
            _plugin.Configuration.Save();
        }
        bool saveTabSize = _plugin.Configuration.PersistWindowSizePerTab;
        if(ImGui.Checkbox("Save window size per tab", ref saveTabSize)) {
            _plugin.Configuration.PersistWindowSizePerTab = saveTabSize;
            _plugin.Configuration.Save();
        }
        ImGui.Separator();

        ImGui.TextColored(ImGuiColors.DalamudYellow, "Match Details Window");

        bool resizeableWindow = _plugin.Configuration.ResizeableMatchWindow;
        if(ImGui.Checkbox("Make window resizeable", ref resizeableWindow)) {
            _plugin.Configuration.ResizeableMatchWindow = resizeableWindow;
            _plugin.Configuration.Save();
        }
        ImGuiHelper.HelpMarker("Reopen windows to reflect changes.");

        bool playerTeamLeft = _plugin.Configuration.LeftPlayerTeam;
        if(ImGui.Checkbox("Always show player team on left", ref playerTeamLeft)) {
            _plugin.Configuration.LeftPlayerTeam = playerTeamLeft;
            _plugin.Configuration.Save();
        }

        bool anchorTeamNames = _plugin.Configuration.AnchorTeamNames;
        if(ImGui.Checkbox("Anchor team stats", ref anchorTeamNames)) {
            _plugin.Configuration.AnchorTeamNames = anchorTeamNames;
            _plugin.Configuration.Save();
        }
        ImGuiHelper.HelpMarker("Team stat rows will not be affected by sorting.");
        ImGui.Separator();
    }
}
