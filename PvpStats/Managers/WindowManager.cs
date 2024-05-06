using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using LiteDB;
using PvpStats.Helpers;
using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows;
using PvpStats.Windows.Detail;
using PvpStats.Windows.Tracker;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Managers;
internal class WindowManager : IDisposable {

    private WindowSystem WindowSystem;
    private Plugin _plugin;
    internal CCTrackerWindow CCTrackerWindow { get; private set; }
    internal FLTrackerWindow FLTrackerWindow { get; private set; }
    internal ConfigWindow ConfigWindow { get; private set; }
#if DEBUG
    internal DebugWindow? DebugWindow { get; private set; }
#endif

    internal readonly Dictionary<Job, IDalamudTextureWrap> JobIcons = new();
    internal IDalamudTextureWrap CCBannerImage { get; private set; }

    internal WindowManager(Plugin plugin) {
        _plugin = plugin;
        WindowSystem = new("PvP Stats");

        //MainWindow = new();
        _plugin.PluginInterface.UiBuilder.Draw += DrawUI;
        _plugin.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigWindow;

        foreach(var icon in PlayerJobHelper.JobIcons) {
            JobIcons.Add(icon.Key, _plugin.TextureProvider.GetIcon(icon.Value));
        }

        CCTrackerWindow = new(plugin);
        FLTrackerWindow = new(plugin);
        ConfigWindow = new(plugin);
        WindowSystem.AddWindow(CCTrackerWindow);
        WindowSystem.AddWindow(FLTrackerWindow);
        WindowSystem.AddWindow(ConfigWindow);

#if DEBUG
        DebugWindow = new(plugin);
        WindowSystem.AddWindow(DebugWindow);
#endif

        var imagePath = Path.Combine(_plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "cc_logo_full.png");
        CCBannerImage = _plugin.PluginInterface.UiBuilder.LoadImage(imagePath);

        _plugin.ClientState.Login += OnLogin;
    }
    private void DrawUI() {
        WindowSystem.Draw();
    }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();
        _plugin.PluginInterface.UiBuilder.Draw -= DrawUI;
        _plugin.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigWindow;
        CCBannerImage.Dispose();

        _plugin.ClientState.Login -= OnLogin;
    }

    private void OnLogin() {
        Task.Delay(3000).ContinueWith((t) => {
            _plugin.DataQueue.QueueDataOperation(async () => {
                await _plugin.PlayerLinksService.BuildAutoLinksCache();
                await Refresh();
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

    internal void OpenConfigWindow() {
        ConfigWindow.IsOpen = true;
    }

#if DEBUG
    internal void OpenDebugWindow() {
        if(DebugWindow is not null) {
            DebugWindow.IsOpen = true;
        }
    }
#endif

    internal void OpenMatchDetailsWindow(CrystallineConflictMatch match) {
        var windowName = $"Match Details: {match.Id}";
        var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
        if(window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        } else {
            _plugin.Log.Debug($"Opening item detail for...{match.DutyStartTime}");
            var itemDetail = new CrystallineConflictMatchDetail(_plugin, match);
            itemDetail.IsOpen = true;
            _plugin.WindowManager.AddWindow(itemDetail);
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
                default:
                    break;
            }
            var itemDetail = new FullEditDetail<T>(_plugin, matchCache, match);
            itemDetail.IsOpen = true;
            _plugin.WindowManager.AddWindow(itemDetail);
        }
    }

    public async Task Refresh() {
        _plugin.Log.Debug("refreshing windows...");
        Task.WaitAll(ConfigWindow.Refresh(), CCTrackerWindow.Refresh(), FLTrackerWindow.Refresh());
        await Task.CompletedTask;
    }
}
