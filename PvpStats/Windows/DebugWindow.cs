using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace PvpStats.Windows;
internal class DebugWindow : Window {

    private Plugin _plugin;
    private string _addon = "";
    private string _idChain = "";
    private uint[] _idParams;

    private string _pname = "";

    internal DebugWindow(Plugin plugin) : base("Pvp Stats Debug") {
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

        if (ImGui.InputText($"Addon", ref _addon, 80)) {

        }

        if (ImGui.InputText($"ID Chain", ref _idChain, 80)) {
            List<uint> results = new();
            string[] splitString = _idChain.Split(",");
            foreach (string s in splitString) {
                uint result;
                if (uint.TryParse(s, out result)) {
                    results.Add(result);
                }
            }
            _idParams = results.ToArray();
        }

        if (ImGui.Button("Print Text Nodes")) {
            AtkNodeHelper.PrintTextNodes(_addon);
        }

        if (ImGui.Button("GetNodeByIDChain")) {
            unsafe {
                var x = AtkNodeHelper.GetNodeByIDChain(_addon, _idParams);
                _plugin.Log.Debug($"0x{new IntPtr(x).ToString("X8")}");
            }

        }

        if (ImGui.Button("Print ATKStage String data")) {
            AtkNodeHelper.PrintAtkStringArray();
        }
        ImGui.Separator();

        if (ImGui.Button("test obj table")) {
            _plugin.Log.Debug($"current player obj id: {_plugin.ClientState.LocalPlayer.ObjectId}");
            foreach (PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
                _plugin.Log.Debug(string.Format("name: {0,-30} world: {1,-15} id:{2}", pc.Name, pc.HomeWorld.GameData.Name.ToString(), pc.ObjectId));
            }
        }

        if (ImGui.InputText($"player name", ref _pname, 80)) {

        }

        if (ImGui.Button("check alias")) {
            PlayerAlias p = (PlayerAlias)_pname;
            bool result = PlayerJobHelper.IsAbbreviatedAliasMatch(p, "Sarah Montcroix");
            _plugin.Log.Debug($"{result}");
        }

    }

    private void TestParse() {
        string matchTimer = "1:35";
        DateTimeFormatInfo dt = new();
        //dt.
        bool parseResult = TimeSpan.TryParse(matchTimer, out TimeSpan ts);

        _plugin.Log.Debug($"parse result: {parseResult} minutes: {ts.Minutes} seconds: {ts.Seconds}");
    }
}
