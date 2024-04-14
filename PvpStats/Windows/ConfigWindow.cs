using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Player;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows;
internal class ConfigWindow : Window {

    private Plugin _plugin;
    private PlayerAliasLink _newManualLink;
    private string[] _linksVerbCombo = new[] { "IS", "IS NOT" };
    private List<PlayerAliasLink> ManualLinks { get; set; } = new();
    private List<PlayerAliasLink> AutoLinks { get; set; } = new();
    protected SemaphoreSlim RefreshLock { get; init; } = new SemaphoreSlim(1);

    private float _saveOpacity = 0f;

    public ConfigWindow(Plugin plugin) : base("PvP Tracker Settings") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 500),
            MaximumSize = new Vector2(5000, 5000)
        };
        _plugin = plugin;
        _newManualLink = new();
        //_plugin.DataQueue.QueueDataOperation(Refresh);
    }

    internal async Task Refresh() {
        _newManualLink = new();
        List<PlayerAliasLink> flattenedList = new();
        foreach(var playerLink in _plugin.PlayerLinksService.ManualPlayerLinksCache) {
            foreach(var linkedAlias in playerLink.LinkedAliases) {
                flattenedList.Add(new() {
                    CurrentAlias = playerLink.CurrentAlias,
                    IsUnlink = playerLink.IsUnlink,
                    LinkedAliases = new() { linkedAlias }
                });
            }
        }
        try {
            await RefreshLock.WaitAsync();
            ManualLinks = flattenedList;
            AutoLinks = _plugin.PlayerLinksService.AutoPlayerLinksCache.OrderBy(x => x.CurrentAlias).ToList();
        } finally {
            RefreshLock.Release();
        }
        //ManualLinks = _plugin.Storage.GetManualLinks().Query().ToList();
    }

    public override void OnOpen() {
        _plugin.DataQueue.QueueDataOperation(Refresh);
        base.OnOpen();
    }

    public override void OnClose() {
        _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        base.OnClose();
    }

    public override void Draw() {
        //_plugin.Log.Verbose("draw config");
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
        //_plugin.Log.Verbose("draw config end");
    }

    private void DrawInterfaceSettings() {
        ImGui.TextColored(_plugin.Configuration.Colors.Header, "Tracker Window");
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
        bool filterHeightAdjust = _plugin.Configuration.CCWindowConfig.AdjustWindowHeightOnFilterCollapse;
        if(ImGui.Checkbox("Offset window size when filters hidden", ref filterHeightAdjust)) {
            _plugin.Configuration.CCWindowConfig.AdjustWindowHeightOnFilterCollapse = filterHeightAdjust;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGuiHelper.HelpMarker("The height of the window will be offset by the height of the filter child whenever filters are shown or hidden.", true, true);
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

        ImGui.TextColored(_plugin.Configuration.Colors.Header, "Match Details Window");

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
        ImGui.TextColored(_plugin.Configuration.Colors.Header, "Colors");
        DrawColorSettings();
    }

    private void DrawColorSettings() {
        if(ImGui.Button("Reset to Default")) {
            _plugin.Configuration.Colors = new();
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }

        using var table = ImRaii.Table("ColorSettingsTable", 3);
        if(!table) {
            return;
        }
        ImGui.TableSetupColumn("c1");
        ImGui.TableSetupColumn("c2");
        ImGui.TableSetupColumn("c3");

        ImGui.TableNextColumn();
        var headerColor = _plugin.Configuration.Colors.Header;
        if(ImGui.ColorEdit4("Headers", ref headerColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Header = headerColor;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var favoriteColor = _plugin.Configuration.Colors.Favorite;
        if(ImGui.ColorEdit4("Favorites", ref favoriteColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Favorite = favoriteColor;
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextRow();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var winColor = _plugin.Configuration.Colors.Win;
        if(ImGui.ColorEdit4("Wins", ref winColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Win = winColor;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var lossColor = _plugin.Configuration.Colors.Loss;
        if(ImGui.ColorEdit4("Losses", ref lossColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Loss = lossColor;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var otherColor = _plugin.Configuration.Colors.Other;
        if(ImGui.ColorEdit4("Draws", ref otherColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Other = otherColor;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextRow();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var localPlayerColor = _plugin.Configuration.Colors.CCLocalPlayer;
        if(ImGui.ColorEdit4("Local Player", ref localPlayerColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.CCLocalPlayer = localPlayerColor;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var playerTeamColor = _plugin.Configuration.Colors.CCPlayerTeam;
        if(ImGui.ColorEdit4("Player Team", ref playerTeamColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.CCPlayerTeam = playerTeamColor;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var enemyTeamColor = _plugin.Configuration.Colors.CCEnemyTeam;
        if(ImGui.ColorEdit4("Enemy Team", ref enemyTeamColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.CCEnemyTeam = enemyTeamColor;
            _plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextRow();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var tankColor = _plugin.Configuration.Colors.Tank;
        if(ImGui.ColorEdit4("Tank", ref tankColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Tank = tankColor;
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var healerColor = _plugin.Configuration.Colors.Healer;
        if(ImGui.ColorEdit4("Healer", ref healerColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Healer = healerColor;
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var meleeColor = _plugin.Configuration.Colors.Melee;
        if(ImGui.ColorEdit4("Melee", ref meleeColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Melee = meleeColor;
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var rangedColor = _plugin.Configuration.Colors.Ranged;
        if(ImGui.ColorEdit4("Ranged", ref rangedColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Ranged = rangedColor;
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var casterColor = _plugin.Configuration.Colors.Caster;
        if(ImGui.ColorEdit4("Caster", ref casterColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.Caster = casterColor;
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }

        ImGui.TableNextRow();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var statHighColor = _plugin.Configuration.Colors.StatHigh;
        if(ImGui.ColorEdit4("High stat", ref statHighColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.StatHigh = statHighColor;
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
        ImGui.TableNextColumn();
        var statLowColor = _plugin.Configuration.Colors.StatLow;
        if(ImGui.ColorEdit4("Low stat", ref statLowColor, ImGuiColorEditFlags.NoInputs)) {
            _plugin.Configuration.Colors.StatLow = statLowColor;
            //_plugin.DataQueue.QueueDataOperation(_plugin.Configuration.Save);
        }
    }

    private void ColorPicker(string name, ref Vector4 prop) {
        var color = prop;
        if(ImGui.ColorEdit4("Headers", ref color, ImGuiColorEditFlags.NoInputs)) {
            prop = color;
        }
    }

    private void DrawPlayerLinkSettings() {
        bool playerLinking = _plugin.Configuration.EnablePlayerLinking;
        if(ImGui.Checkbox("Enable player linking", ref playerLinking)) {
            _plugin.DataQueue.QueueDataOperation(async () => {
                _plugin.Configuration.EnablePlayerLinking = playerLinking;
                _plugin.Configuration.Save();
                await _plugin.WindowManager.Refresh();
            });
        }
        ImGuiHelper.HelpMarker("Enable combining of player stats with different aliases linked with the same unique character or player.");
        bool autoLinking = _plugin.Configuration.EnableAutoPlayerLinking;
        if(ImGui.Checkbox("Enable auto linking (requires PlayerTrack)", ref autoLinking)) {
            _plugin.DataQueue.QueueDataOperation(async () => {
                _plugin.Configuration.EnableAutoPlayerLinking = autoLinking;
                _plugin.Configuration.Save();
                await _plugin.WindowManager.Refresh();
            });
        }
        ImGuiHelper.HelpMarker("Use name change data from PlayerTrack to create player links.\n\n" +
            "Does not work on your own character (for now).");
        bool manualLinking = _plugin.Configuration.EnableManualPlayerLinking;
        if(ImGui.Checkbox("Enable manual linking", ref manualLinking)) {
            _plugin.DataQueue.QueueDataOperation(async () => {
                _plugin.Configuration.EnableManualPlayerLinking = manualLinking;
                _plugin.Configuration.Save();
                await _plugin.WindowManager.Refresh();
            });
        }
        ImGuiHelper.HelpMarker("Use the manual tab to create player links by hand or to track" +
            " un-covered auto link scenarios such as personal character alias changes, track known alt characters or to override mistakes.\n\nEnter format as <player name> <home world>");
        using(var tabBar = ImRaii.TabBar("LinksTabBar")) {
            using(var tab = ImRaii.TabItem("Auto")) {
                if(tab) {
                    if(ImGui.Button("Update Now")) {
                        _plugin.DataQueue.QueueDataOperation(async () => {
                            await _plugin.PlayerLinksService.BuildAutoLinksCache();
                            await _plugin.WindowManager.Refresh();
                        });
                    }
                    DrawAutoPlayerLinkSettings();
                }
            }
            using(var tab = ImRaii.TabItem("Manual")) {
                if(tab) {
                    DrawManualPlayerLinkSettings();
                }
            }
        }
    }

    private void DrawAutoPlayerLinkSettings() {
        ImGui.Text("Players with auto-linked aliases: ");

        using(var child = ImRaii.Child("AutoPlayerLinks", ImGui.GetContentRegionAvail(), true)) {
            if(child) {
                foreach(var playerLink in AutoLinks) {
                    DrawAutoPlayerLink(playerLink);
                }
            }
        }
    }

    private void DrawManualPlayerLinkSettings() {
        using(var child = ImRaii.Child("ManualLinksChild", new Vector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale), true)) {
            if(child) {
                using(var table = ImRaii.Table("ManualLinksTabke", 4, ImGuiTableFlags.NoSavedSettings)) {
                    if(table) {
                        ImGui.TableSetupColumn("LinkedAlias", ImGuiTableColumnFlags.WidthFixed, 180f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Verb", ImGuiTableColumnFlags.WidthFixed, 75f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("SourceAlias", ImGuiTableColumnFlags.WidthFixed, 180f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed, 50f * ImGuiHelpers.GlobalScale);
                        try {
                            foreach(var link in ManualLinks) {
                                DrawManualPlayerLink(link);
                            }
                            DrawManualPlayerLink(_newManualLink, true);
                        } catch {
                            //suppress remove elements
                        }

                    }
                }
            }
        }
        if(ImGui.Button("Save")) {
            _plugin.DataQueue.QueueDataOperation(async () => {
                //_plugin.Storage.SetManualLinks(ManualLinks, false);
                await _plugin.PlayerLinksService.SaveManualLinksCache(ManualLinks);
                if(_plugin.Configuration.EnablePlayerLinking && _plugin.Configuration.EnableManualPlayerLinking) {
                    await _plugin.WindowManager.Refresh();
                }
            }).ContinueWith((t) => {
                _saveOpacity = 1f;
            });
        }
        if(_saveOpacity > 0f) {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1f, 1f, 1f, _saveOpacity), "Saved!");
            _saveOpacity -= 0.002f;
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 60f * ImGuiHelpers.GlobalScale);
        if(ImGui.Button("Cancel")) {
            _plugin.DataQueue.QueueDataOperation(async () => {
                await Refresh();
            });
        }
    }

    private void DrawAutoPlayerLink(PlayerAliasLink playerLink) {
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

    private void DrawManualPlayerLink(PlayerAliasLink playerLink, bool isNew = false) {
        string inputText1 = "";
        string inputText2 = "";
        if(playerLink.LinkedAliases.Count > 0) {
            inputText2 = playerLink.LinkedAliases[0];
        }

        if(playerLink.CurrentAlias != null) {
            inputText1 = playerLink.CurrentAlias;
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.InputTextWithHint($"###{playerLink.GetHashCode()}--CurrentAlias", "Enter main player", ref inputText1, 60)) {
            try {
                playerLink.CurrentAlias = (PlayerAlias)inputText1;
            } catch {
                playerLink.CurrentAlias = null;
            }
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        int verbIndex = playerLink.IsUnlink ? 1 : 0;
        if(ImGui.Combo($"###{playerLink.GetHashCode()}--IsUnlink", ref verbIndex, _linksVerbCombo, _linksVerbCombo.Length)) {
            playerLink.IsUnlink = verbIndex == 1;
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.InputTextWithHint($"###{playerLink.GetHashCode()}--LinkedAlias", "Enter linked player", ref inputText2, 60)) {
            try {
                if(playerLink.LinkedAliases.Count <= 0) {
                    playerLink.LinkedAliases.Add((PlayerAlias)inputText2);
                } else {
                    playerLink.LinkedAliases[0] = (PlayerAlias)inputText2;
                }
            } catch {
                playerLink.LinkedAliases = new();
            }
        }
        ImGui.TableNextColumn();
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            if(isNew) {
                if(ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}###{playerLink.GetHashCode()}--Button")) {
                    ManualLinks.Add(playerLink);
                    _newManualLink = new();
                }

            } else {
                if(ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}###{playerLink.GetHashCode()}--Button")) {
                    ManualLinks.Remove(playerLink);
                }

            }
        }
        ImGuiHelper.WrappedTooltip(isNew ? "Add Link" : "Remove");
    }
}
