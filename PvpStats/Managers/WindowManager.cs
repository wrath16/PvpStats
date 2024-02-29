using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using LiteDB;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using PvpStats.Windows;
using PvpStats.Windows.Detail;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace PvpStats.Managers;
internal class WindowManager : IDisposable {

    private WindowSystem WindowSystem;
    private Plugin _plugin;
    private MainWindow MainWindow;
#if DEBUG
    private DebugWindow? DebugWindow;
#endif

    internal readonly Dictionary<Job, IDalamudTextureWrap> JobIcons = new();

    internal WindowManager(Plugin plugin) {
        _plugin = plugin;
        WindowSystem = new("PvP Stats");

        //MainWindow = new();
        _plugin.PluginInterface.UiBuilder.Draw += DrawUI;
        //_plugin.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        foreach(var icon in PlayerJobHelper.JobIcons) {
            JobIcons.Add(icon.Key, _plugin.TextureProvider.GetIcon(icon.Value));
        }

        MainWindow = new(plugin);
        WindowSystem.AddWindow(MainWindow);

#if DEBUG
        DebugWindow = new(plugin);
        WindowSystem.AddWindow(DebugWindow);
#endif
    }
    private void DrawUI() {
        WindowSystem.Draw();
    }

    private void DrawConfigUI() {
    }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();
        _plugin.PluginInterface.UiBuilder.Draw -= DrawUI;
        //_plugin.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
    }

    internal void AddWindow(Window window) {
        WindowSystem.AddWindow(window);
    }

    internal void RemoveWindow(Window window) {
        WindowSystem.RemoveWindow(window);
    }

    internal void OpenMainWindow() {
        MainWindow.IsOpen = true;
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

    internal void OpenFullEditWindow(CrystallineConflictMatch match) {
        var windowName = $"Full Edit: {match.GetHashCode()}";
        var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
        if(window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        } else {
            _plugin.Log.Debug($"Opening full edit details for...{match.DutyStartTime}");
            var itemDetail = new FullEditDetail<CrystallineConflictMatch>(_plugin, match);
            itemDetail.IsOpen = true;
            _plugin.WindowManager.AddWindow(itemDetail);
        }
    }

    public void Refresh() {
        MainWindow.Refresh();
    }

}
