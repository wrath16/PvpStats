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
using PvpStats.Windows;
using PvpStats.Windows.Detail;
using PvpStats.Windows.Tracker;
using System;
using System.Data;
using System.Linq;
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
        Task.Delay(3000).ContinueWith((t) => {
            _plugin.DataQueue.QueueDataOperation(async () => {
                await _plugin.PlayerLinksService.BuildAutoLinksCache();
                await RefreshAll();
            });
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
                if(ImGui.InputTextWithHint("##TagsInput", "Enter tags...", ref tagsText, 100, ImGuiInputTextFlags.EnterReturnsTrue)) {
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

    public async Task RefreshAll() {
        _plugin.Log.Debug("refreshing windows...");
        Task.WaitAll(ConfigWindow.Refresh(), RefreshCCWindow(), RefreshFLWindow(), RefreshRWWindow());
        await Task.CompletedTask;
    }

    public async Task RefreshConfigWindow() {
        await ConfigWindow.Refresh();
    }

    public async Task RefreshCCWindow() {
        await CCTrackerWindow.Refresh();
    }

    public async Task RefreshFLWindow() {
        await FLTrackerWindow.Refresh();
    }

    public async Task RefreshRWWindow() {
        await RWTrackerWindow.Refresh();
    }

    public nint GetTextureHandle(uint iconId) {
        return _plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty().ImGuiHandle;
    }

    public nint GetTextureHandle(string path) {
        return _plugin.TextureProvider.GetFromGame(path).GetWrapOrEmpty().ImGuiHandle;
    }
}
