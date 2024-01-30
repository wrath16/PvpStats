using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace PvpStats.Windows;
internal class DebugWindow : Window {

    private Plugin _plugin;
    private string _addon = "";
    private string _idChain = "";
    private uint[] _idParams;

    private string _pname = "";

    private string _toFind = "";

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

        //if (ImGui.Button("test obj table")) {
        //    _plugin.Log.Debug($"current player obj id: {_plugin.ClientState.LocalPlayer.ObjectId}");
        //    foreach (PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
        //        _plugin.Log.Debug(string.Format("name: {0,-30} world: {1,-15} id:{2}", pc.Name, pc.HomeWorld.GameData.Name.ToString(), pc.ObjectId));
        //    }
        //}

        //if (ImGui.InputText($"player name", ref _pname, 80)) {

        //}

        if (ImGui.Button("Get Content Type")) {
            var x = _plugin.Functions.GetContentType();
            _plugin.Log.Debug($"Content type: {x}");
        }

        if (ImGui.Button("Get Content Director Bytes")) {
            var x = _plugin.Functions.GetRawInstanceContentDirector();
            _plugin.Log.Debug($"object size: {x.Length.ToString("X2")} bytes");
            //int offset = 0x0;
            //foreach (var b in x) {
            //    string inHex = b.ToString("X2");
            //    _plugin.Log.Debug($"offset: {offset.ToString("X2")} value: {inHex}");
            //    offset++;
            //}
        }

        if (ImGui.Button("Get DD Content Director Bytes")) {
            var x = _plugin.Functions.GetRawDeepDungeonInstanceContentDirector();
            _plugin.Log.Debug($"object size: {x.Length.ToString("X2")} bytes");
            int offset = 0x0;
            foreach (var b in x) {
                string inHex = b.ToString("X2");
                _plugin.Log.Debug($"offset: {offset.ToString("X2")} value: {inHex}");
                offset++;
            }
        }

        if (ImGui.Button("Read Int32 from Content Director")) {
            _plugin.Functions.AttemptToReadContentDirector();
        }

        if (ImGui.InputText($"##findvalue", ref _toFind, 80, ImGuiInputTextFlags.CharsDecimal)) {

        }

        if (ImGui.Button("Find value")) {
            if (int.TryParse(_toFind, out int result)) {
                _plugin.Functions.FindValueInContentDirector(result);
            }

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
