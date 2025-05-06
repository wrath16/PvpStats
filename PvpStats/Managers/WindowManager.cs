using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using LiteDB;
using PvpStats.Helpers;
using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using PvpStats.Types.Player;
using PvpStats.Windows;
using PvpStats.Windows.Detail;
using PvpStats.Windows.Tracker;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Managers;
internal class WindowManager : IDisposable {

    private WindowSystem WindowSystem;
    private Plugin _plugin;
    internal CCTrackerWindow CCTrackerWindow { get; private set; }
    internal FLTrackerWindow FLTrackerWindow { get; private set; }
    internal RWTrackerWindow RWTrackerWindow { get; private set; }
    internal ConfigWindow ConfigWindow { get; private set; }
    internal SplashWindow SplashWindow { get; private set; }
#if DEBUG
    internal DebugWindow? DebugWindow { get; private set; }
#endif

    internal IFontHandle LargeFont { get; private set; }

    internal WindowManager(Plugin plugin) {
        _plugin = plugin;
        WindowSystem = new("PvP Stats");

        CCTrackerWindow = new(plugin);
        FLTrackerWindow = new(plugin);
        RWTrackerWindow = new(plugin);
        ConfigWindow = new(plugin);
        SplashWindow = new(plugin);
        WindowSystem.AddWindow(CCTrackerWindow);
        WindowSystem.AddWindow(FLTrackerWindow);
        WindowSystem.AddWindow(RWTrackerWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(SplashWindow);

#if DEBUG
        DebugWindow = new(plugin);
        WindowSystem.AddWindow(DebugWindow);
#endif

        LargeFont = _plugin.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 24f));
        _plugin.PluginInterface.UiBuilder.Draw += DrawUI;
        _plugin.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigWindow;
        _plugin.PluginInterface.UiBuilder.OpenMainUi += OpenSplashWindow;
        _plugin.ClientState.Login += OnLogin;
    }
    private void DrawUI() {
        WindowSystem.Draw();
    }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();
        _plugin.PluginInterface.UiBuilder.Draw -= DrawUI;
        _plugin.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigWindow;

        _plugin.ClientState.Login -= OnLogin;
    }

    private void OnLogin() {
        Task.Delay(3000).ContinueWith(async (t) => {
            if(_plugin.Configuration.EnableAutoPlayerLinking) {
                //don't need to put this here anymore but the delay is annoying so we want to do it after first refresh
                await _plugin.PlayerLinksService.BuildAutoLinksCache();
                _ = _plugin.WindowManager.RefreshAll(true);
            } else {
                _ = _plugin.WindowManager.RefreshAll();
            }
        });
    }

    internal void AddWindow(Window window) {
        WindowSystem.AddWindow(window);
    }

    internal void RemoveWindow(Window window) {
        WindowSystem.RemoveWindow(window);
    }

    internal void OpenCCWindow() {
        CCTrackerWindow.IsOpen = true;
    }

    internal void OpenFLWindow() {
        FLTrackerWindow.IsOpen = true;
    }

    internal void OpenRWWindow() {
        RWTrackerWindow.IsOpen = true;
    }

    internal void OpenConfigWindow() {
        ConfigWindow.IsOpen = true;
    }

    internal void OpenSplashWindow() {
        SplashWindow.IsOpen = true;
    }

#if DEBUG
    internal void OpenDebugWindow() {
        if(DebugWindow is not null) {
            DebugWindow.IsOpen = true;
        }
    }
#endif

    //internal void OpenMatchDetailsWindow(CrystallineConflictMatch match) {
    //    var windowName = $"Match Details: {match.Id}";
    //    var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
    //    if(window is not null) {
    //        window.BringToFront();
    //        window.IsOpen = true;
    //    } else {
    //        _plugin.Log.Debug($"Opening item detail for...{match.DutyStartTime}");
    //        var itemDetail = new CrystallineConflictMatchDetail(_plugin, match);
    //        itemDetail.IsOpen = true;
    //        _plugin.WindowManager.AddWindow(itemDetail);
    //    }
    //}

    internal void OpenMatchDetailsWindow(PvpMatch match) {
        var windowName = $"Match Details: {match.Id}";
        var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
        if(window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        } else {
            _plugin.Log.Debug($"Opening item detail for...{match.DutyStartTime}");
            if(match.GetType() == typeof(CrystallineConflictMatch)) {
                var itemDetail = new CrystallineConflictMatchDetail(_plugin, (match as CrystallineConflictMatch)!);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            } else if(match.GetType() == typeof(FrontlineMatch)) {
                var itemDetail = new FrontlineMatchDetail(_plugin, (match as FrontlineMatch)!);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            } else if(match.GetType() == typeof(RivalWingsMatch)) {
                var itemDetail = new RivalWingsMatchDetail(_plugin, (match as RivalWingsMatch)!);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            }

            //var itemDetail = new CrystallineConflictMatchDetail(_plugin, match);
            //itemDetail.IsOpen = true;
            //_plugin.WindowManager.AddWindow(itemDetail);
        }
    }

    internal void CloseAllMatchWindows() {
        var windows = WindowSystem.Windows.Where(w => w.WindowName.StartsWith("Match Details: ", StringComparison.OrdinalIgnoreCase));
        foreach(var window in windows) {
            window.IsOpen = false;
        }
    }

    internal void OpenFullEditWindow<T>(T match) where T : PvpMatch {
        var windowName = $"Full Edit: {match.GetHashCode()}";
        var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
        if(window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        } else {
            _plugin.Log.Debug($"Opening full edit details for...{match.DutyStartTime}");
            var matchType = typeof(T);
            MatchCacheService<T>? matchCache = null;
            switch(matchType) {
                case Type _ when matchType == typeof(CrystallineConflictMatch):
                    matchCache = _plugin.CCCache as MatchCacheService<T>;
                    break;
                case Type _ when matchType == typeof(FrontlineMatch):
                    matchCache = _plugin.FLCache as MatchCacheService<T>;
                    break;
                case Type _ when matchType == typeof(RivalWingsMatch):
                    matchCache = _plugin.RWCache as MatchCacheService<T>;
                    break;
                default:
                    break;
            }
            var itemDetail = new FullEditDetail<T>(_plugin, matchCache, match);
            itemDetail.IsOpen = true;
            _plugin.WindowManager.AddWindow(itemDetail);
        }
    }

    internal void OpenTimelineFullEditWindow<T>(T timeline) where T : PvpMatchTimeline {
        var windowName = $"Timeline Full Edit: {timeline.Id}";
        var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
        if(window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        } else {
            _plugin.Log.Debug($"Opening timeline full edit details for...{timeline.Id}");
            var itemDetail = new TimelineFullEditDetail<T>(_plugin, timeline);
            itemDetail.IsOpen = true;
            _plugin.WindowManager.AddWindow(itemDetail);
        }
    }

    internal void OpenTimelineFullEditWindow(PvpMatchTimeline timeline) {
        var windowName = $"Timeline Full Edit: {timeline.Id}";
        var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
        if(window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        } else {
            _plugin.Log.Debug($"Opening timeline full edit details for...{timeline.Id}");

            //hacky time!
            if(timeline is CrystallineConflictMatchTimeline) {
                var itemDetail = new TimelineFullEditDetail<CrystallineConflictMatchTimeline>(_plugin, timeline as CrystallineConflictMatchTimeline);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            } else if(timeline is FrontlineMatchTimeline) {
                var itemDetail = new TimelineFullEditDetail<FrontlineMatchTimeline>(_plugin, timeline as FrontlineMatchTimeline);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            } else if(timeline is RivalWingsMatchTimeline) {
                var itemDetail = new TimelineFullEditDetail<RivalWingsMatchTimeline>(_plugin, timeline as RivalWingsMatchTimeline);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            }
        }
    }

    internal void SetTagsPopup<T>(T match, MatchCacheService<T> cache, ref bool opened) where T : PvpMatch {
        using(var popup = ImRaii.Popup($"{match.Id}--TagsPopup")) {
            if(popup) {
                string tagsText = match.Tags;
                ImGuiHelper.HelpMarker("Comma-separate tags. Hit enter to save and close.", true, true);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
                if(!opened) {
                    ImGui.SetKeyboardFocusHere(0);
                }
                if(ImGui.InputTextWithHint("##TagsInput", "Enter tags...", ref tagsText, 500, ImGuiInputTextFlags.EnterReturnsTrue)) {
                    match.Tags = tagsText;
                    ImGui.CloseCurrentPopup();
                    _plugin.DataQueue.QueueDataOperation(async () => {
                        match.Tags = match.Tags.Trim();
                        await cache.UpdateMatch(match);
                    });
                }
            } else if(opened) {
                //_plugin.DataQueue.QueueDataOperation(async () => {
                //    _plugin.Log.Debug("closing popup");
                //    match.Tags = match.Tags.Trim();
                //    await cache.UpdateMatch(match);
                //});
            }
        }
        opened = ImGui.IsPopupOpen($"{match.Id}--TagsPopup");
    }

    public async Task RefreshAll(bool fullRefresh = false) {
        Plugin.Log2.Debug("refreshing windows...");
        await Task.WhenAll(
            ConfigWindow.Refresh(),
            RefreshCCWindow(fullRefresh),
            RefreshFLWindow(fullRefresh),
            RefreshRWWindow(fullRefresh)
            );
    }

    public async Task RefreshConfigWindow() {
        await ConfigWindow.Refresh();
    }

    public async Task RefreshCCWindow(bool fullRefresh = false) {
        await CCTrackerWindow.Refresh(fullRefresh);
    }

    public async Task RefreshFLWindow(bool fullRefresh = false) {
        await FLTrackerWindow.Refresh(fullRefresh);
    }

    public async Task RefreshRWWindow(bool fullRefresh = false) {
        await RWTrackerWindow.Refresh(fullRefresh);
    }

    public nint GetTextureHandle(uint iconId) {
        return _plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty().ImGuiHandle;
    }

    public nint GetTextureHandle(string path) {
        return _plugin.TextureProvider.GetFromGame(path).GetWrapOrEmpty().ImGuiHandle;
    }

    public unsafe void DrawPlayerSnapshot(uint entityId) {
        var gameObj = _plugin.ObjectTable.SearchByEntityId(entityId);
        if(gameObj is null || gameObj is not IBattleChara) return;

        DrawPlayerSnapshot(new BattleCharaSnapshot(gameObj as IBattleChara));
    }

    public unsafe void DrawPlayerSnapshot(BattleCharaSnapshot snapshot) {
        DrawPlayerBars(snapshot.MaxHP, snapshot.CurrentHP, snapshot.ShieldPercents, snapshot.MaxMP, snapshot.CurrentMP);
        DrawStatuses(snapshot.Statuses);
    }

    private void DrawStatuses(List<StatusSnapshot> statusList, bool majorOnly = false) {
        //using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(1f, ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale);
        var sizeHeight = 30f * ImGuiHelpers.GlobalScale;
        List<(uint, uint, StatusSnapshot)> beneficialEffects = new();
        List<(uint, uint, StatusSnapshot)> detrimentalEffects = new();
        foreach(var status in statusList) {
            if(!_plugin.DebugMode && CombatHelper.IsUselessStatus(status.StatusId)) {
                continue;
            }
            var statusRow = _plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRow(status.StatusId);
            uint stackCount = status.Param;
            var iconId = statusRow.Icon;
            if(statusRow.MaxStacks > 0 && stackCount <= statusRow.MaxStacks) {
                iconId += stackCount - 1;
            }
            if(statusRow.StatusCategory == 1) {
                beneficialEffects.Add((status.StatusId, iconId, status));
            } else if(statusRow.StatusCategory == 2) {
                detrimentalEffects.Add((status.StatusId, iconId, status));
            }
        }

        foreach(var effect in beneficialEffects) {
            var texture = _plugin.TextureProvider.GetFromGameIcon(effect.Item2).GetWrapOrEmpty();
            if(_plugin.DebugMode) {
                ImGui.Text($"{effect.Item1}");
                ImGui.SameLine();
            }
            ImGui.Image(texture.ImGuiHandle, new Vector2(sizeHeight * texture.Width / texture.Height, sizeHeight));
            ImGui.SameLine();
        }
        ImGui.NewLine();
        foreach(var effect in detrimentalEffects) {
            var texture = _plugin.TextureProvider.GetFromGameIcon(effect.Item2).GetWrapOrEmpty();
            if(_plugin.DebugMode) {
                ImGui.Text($"{effect.Item1}");
                ImGui.SameLine();
            }
            ImGui.Image(texture.ImGuiHandle, new Vector2(sizeHeight * texture.Width / texture.Height, sizeHeight));
            ImGui.SameLine();
        }
    }

    public void DrawPlayerBars(uint maxHP, uint currentHP, uint shieldPercents, uint maxMP, uint currentMP) {
        float hpPercentage = (float)currentHP / maxHP;
        float mpPercentage = (float)currentMP / maxMP;
        float shieldPercentage = shieldPercents / 100f;
        float combinedHPShieldsPercentage = hpPercentage + shieldPercentage;

        Vector2 hpSize = new Vector2(200, 20) * ImGuiHelpers.GlobalScale;
        Vector2 mpSize = new Vector2(200, 10) * ImGuiHelpers.GlobalScale;

        Vector4 shieldColor = new Vector4(0f, 0.9f, 0.9f, 1f);

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 1f) * ImGuiHelpers.GlobalScale);
        style.Push(ImGuiStyleVar.FrameRounding, 2f * ImGuiHelpers.GlobalScale);
        //hp
        using var color = ImRaii.PushColor(ImGuiCol.PlotHistogram, new Vector4(0.51f, 0.71f, 0.22f, 1f));
        var startPosition = ImGui.GetCursorPos();
        var endPosition = new Vector2(startPosition.X + hpSize.X * hpPercentage, startPosition.Y);
        ImGui.ProgressBar(hpPercentage, hpSize, "");

        //shields
        if(shieldPercentage > 0) {
            color.Push(ImGuiCol.PlotHistogram, shieldColor);
            if(hpPercentage == 1f) {
                //case 1: full HP
                ImGui.SetCursorPos(startPosition);
                ImGui.ProgressBar(1f, new Vector2(hpSize.X * shieldPercentage, hpSize.Y), "");
            } else if(hpPercentage < 1f && combinedHPShieldsPercentage <= 1f) {
                ImGui.SetCursorPos(endPosition);
                ImGui.ProgressBar(1f, new Vector2(hpSize.X * shieldPercentage, hpSize.Y), "");
            } else if(hpPercentage < 1f && combinedHPShieldsPercentage > 1f) {
                ImGui.SetCursorPos(endPosition);
                ImGui.ProgressBar(1f, new Vector2(hpSize.X * (1f - hpPercentage), hpSize.Y), "");
                ImGui.SetCursorPos(startPosition);
                ImGui.ProgressBar(1f, new Vector2(hpSize.X * (shieldPercentage - (1f - hpPercentage)), hpSize.Y), "");
            }
        }

        //mp
        color.Push(ImGuiCol.PlotHistogram, new Vector4(0.74f, 0.25f, 0.47f, 1f));
        ImGui.ProgressBar(mpPercentage, mpSize, "");
    }
}
