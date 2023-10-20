using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PvpStats.Windows;

internal class MainWindow : Window {

    private Plugin _plugin;
    private string _addon = "";
    private string _idChain = "";
    private uint[] _idParams;

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

        if(ImGui.InputText($"Addon", ref _addon, 80)) {
            
        }

        if(ImGui.InputText($"ID Chain", ref _idChain, 80)) {
            List<uint> results = new();
            string[] splitString = _idChain.Split(",");
            foreach(string s in splitString) {
                uint result;
                if(uint.TryParse(s, out result)) {
                    results.Add(result);
                }
            }
            _idParams = results.ToArray();
        }

        if(ImGui.Button("Print Text Nodes")) {
            _plugin.PrintTextNodes(_addon);
        }

        if(ImGui.Button("GetNodeByIDChain")) {
            unsafe {
                var x = _plugin.GetNodeByIDChain(_addon, _idParams);
                _plugin.Log.Debug($"0x{new IntPtr(x).ToString("X8")}");
            }

        }
    }

}
