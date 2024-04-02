using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Player;
using System.Linq;
using System.Numerics;

namespace PvpStats.Windows;
internal class ConfigWindow : Window {

    private Plugin _plugin;

    public ConfigWindow(Plugin plugin) : base("PvP Tracker Settings") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(300, 300),
            MaximumSize = new Vector2(800, 800)
        };
        _plugin = plugin;
    }

    public override void Draw() {
        if(ImGui.BeginTabBar("SettingsTabBar")) {
            if(ImGui.BeginTabItem("Interface")) {
                DrawInterfaceSettings();
                ImGui.EndTabItem();
            }
            if(ImGui.BeginTabItem("Player Links")) {
                DrawPlayerLinkSettings();
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

        var filterHeight = (int)_plugin.Configuration.CCWindowConfig.FilterHeight;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2f);
        if(ImGui.SliderInt("Filter child height", ref filterHeight, 100, 500)) {
            _plugin.Configuration.CCWindowConfig.FilterHeight = (uint)filterHeight;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        bool minimize = _plugin.Configuration.MinimizeWindow;
        if(ImGui.Checkbox("Shrink window on collapse", ref minimize)) {
            _plugin.Configuration.MinimizeWindow = minimize;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        bool minimizeDir = _plugin.Configuration.MinimizeDirectionLeft;
        if(ImGui.Checkbox("Anchor left-side of window on shrink", ref minimizeDir)) {
            _plugin.Configuration.MinimizeDirectionLeft = minimizeDir;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGuiHelper.HelpMarker("Only applies to previous setting. Otherwise anchors on the right-side.", true, true);
        bool saveTabSize = _plugin.Configuration.PersistWindowSizePerTab;
        if(ImGui.Checkbox("Save window size per tab", ref saveTabSize)) {
            _plugin.Configuration.PersistWindowSizePerTab = saveTabSize;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        bool resizeLeft = _plugin.Configuration.ResizeWindowLeft;
        if(ImGui.Checkbox("Resize window leftwards on tab switch", ref resizeLeft)) {
            _plugin.Configuration.ResizeWindowLeft = resizeLeft;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        bool colorScale = _plugin.Configuration.ColorScaleStats;
        if(ImGui.Checkbox("Color scale stat values", ref colorScale)) {
            _plugin.Configuration.ColorScaleStats = colorScale;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.Separator();

        ImGui.TextColored(ImGuiColors.DalamudYellow, "Match Details Window");

        bool resizeableWindow = _plugin.Configuration.ResizeableMatchWindow;
        if(ImGui.Checkbox("Make window resizeable", ref resizeableWindow)) {
            _plugin.Configuration.ResizeableMatchWindow = resizeableWindow;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGuiHelper.HelpMarker("Reopen windows to reflect changes.", true, true);

        bool showBackgroundImage = _plugin.Configuration.ShowBackgroundImage;
        if(ImGui.Checkbox("Show background image", ref showBackgroundImage)) {
            _plugin.Configuration.ShowBackgroundImage = showBackgroundImage;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        bool playerTeamLeft = _plugin.Configuration.LeftPlayerTeam;
        if(ImGui.Checkbox("Always show player team on left", ref playerTeamLeft)) {
            _plugin.Configuration.LeftPlayerTeam = playerTeamLeft;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }

        bool anchorTeamNames = _plugin.Configuration.AnchorTeamNames;
        if(ImGui.Checkbox("Anchor team stats", ref anchorTeamNames)) {
            _plugin.Configuration.AnchorTeamNames = anchorTeamNames;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGuiHelper.HelpMarker("Team stat rows will not be affected by sorting.", true, true);
        ImGui.Separator();
    }

    private void DrawPlayerLinkSettings() {

        //enable setting...
        bool playerLinking = _plugin.Configuration.EnablePlayerLinking;
        if(ImGui.Checkbox("Enable player linking", ref playerLinking)) {
            _plugin.Configuration.EnablePlayerLinking = playerLinking;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGuiHelper.HelpMarker("Enable/disable combining of player stats with different aliases linked with the same unique character or player.");
        bool autoLinking = _plugin.Configuration.EnableAutoPlayerLinking;
        if(ImGui.Checkbox("Enable auto linking (requires PlayerTrack)", ref autoLinking)) {
            _plugin.Configuration.EnableAutoPlayerLinking = autoLinking;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGuiHelper.HelpMarker("Use data from PlayerTrack to correlate players across name changes/world transfers.\n\n" +
            "Currently does not consider the point in time at which an alias was observed as a variable and applies all known names to all known home worlds. Does not work on your own character (for now).");

        if(_plugin.PlayerLinksService == null) return;

        if(ImGui.Button("Update Now")) {
            _plugin.DataQueue.QueueDataOperation(() => {
                _plugin.PlayerLinksService.BuildAutoLinksCache();
                _plugin.WindowManager.Refresh();
            });
        }

        ImGui.Text("Players with auto-linked aliases: ");

        using(var child = ImRaii.Child("autoPlayerLinks", ImGui.GetContentRegionAvail(), true)) {
            if(child) {
                foreach(var playerLink in _plugin.PlayerLinksService.AutoPlayerLinksCache.OrderBy(x => x.CurrentAlias)) {
                    DrawPlayerLink(playerLink);
                }
            }
        }
    }

    private void DrawPlayerLink(PlayerAliasLink playerLink) {
        //if(ImGui.CollapsingHeader(playerLink.CurrentAlias)) {
        //    foreach(var prevAlias in playerLink.LinkedAliases) {
        //        ImGui.Text(prevAlias);
        //    }
        //}
        ImGuiHelper.FormattedCollapsibleHeader(new[] { (playerLink.CurrentAlias.Name, 200f), (playerLink.CurrentAlias.HomeWorld, 200f), }, () => {
            foreach(var prevAlias in playerLink.LinkedAliases) {
                using(var table = ImRaii.Table($"{playerLink.Id}--AliasTable", 2)) {
                    if(!table) {
                        return;
                    }
                    ImGui.TableSetupColumn("name", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("homeworld");
                    ImGui.TableNextColumn();
                    ImGui.Text(prevAlias.Name);
                    ImGui.TableNextColumn();
                    ImGui.Text(prevAlias.HomeWorld);
                }
            }
        });
    }
}
