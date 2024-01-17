using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using LiteDB;
using PvpStats.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Managers;
internal class WindowManager : IDisposable {

    internal WindowSystem WindowSystem;
    private Plugin _plugin;
    private MainWindow MainWindow;
    private DebugWindow? DebugWindow;

    internal WindowManager(Plugin plugin) {
        _plugin = plugin;
        WindowSystem = new("PvP Stats");

        //MainWindow = new();
        _plugin.PluginInterface.UiBuilder.Draw += DrawUI;
        _plugin.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

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
        _plugin.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
    }

    internal void AddWindow(Window window) {
        WindowSystem.AddWindow(window);
    }

    internal void RemoveWindow(Window window) {

    }

    internal void OpenMainWindow() {
        MainWindow.IsOpen = true;
    }

#if DEBUG
    internal void OpenDebugWindow() {
        if (DebugWindow is not null) {
            DebugWindow.IsOpen = true;
        }
    }
#endif

    internal void OpenMatchDetailsWindow(ObjectId id) {
        var window = WindowSystem.Windows.Where(w => w.WindowName == $"Match Details: {id}").FirstOrDefault();
        if (window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        }
    }

    public void Refresh() {
        MainWindow.Refresh();
    }

}
