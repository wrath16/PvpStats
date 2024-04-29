#pragma warning disable
#if DEBUG
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using ImGuiNET;
using PvpStats.Services;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace PvpStats.Windows;
internal unsafe class DebugWindow : Window {

    private Plugin _plugin;
    private string _addon = "";
    private string _idChain = "";
    private uint[] _idParams;

    private string _pname = "";

    private string _toFind = "";
    private string _player = "";

    internal DebugWindow(Plugin plugin) : base("Pvp Stats Debug") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Always;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(200, 200),
            MaximumSize = new Vector2(2000, 2000)
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
        ImGui.GetBackgroundDrawList().AddImage(_plugin.WindowManager.JobIcons[Job.AST].ImGuiHandle, ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize());

        if(ImGui.BeginTabBar("debugTabs")) {
            if(ImGui.BeginTabItem("Addon")) {
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
                    _plugin.AtkNodeService.PrintTextNodes(_addon);
                }

                if(ImGui.Button("GetNodeByIDChain")) {
                    unsafe {
                        var x = AtkNodeService.GetNodeByIDChain(_addon, _idParams);
                        _plugin.Log.Debug($"0x{new IntPtr(x).ToString("X8")}");
                    }

                }

                if(ImGui.Button("Print ATKStage String data")) {
                    _plugin.AtkNodeService.PrintAtkStringArray();
                }
                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("ContentDirector")) {
                var instanceDirector = EventFramework.Instance()->GetInstanceContentDirector();
                var directorAddress = &instanceDirector;

                //var crystallineConflictDirector = _plugin.Functions.GetInstanceContentCrystallineConflictDirector();

                if(ImGui.BeginTable("memoryStringTable", 2)) {
                    ImGui.TableNextColumn();
                    ImGui.Text($"Current Content Type:");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(instanceDirector != null ? instanceDirector->InstanceContentType : "")}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"ICD pointer address:");
                    ImGui.TableNextColumn();
                    ImGui.Text($"0x{new IntPtr(directorAddress).ToString("X2")}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"ICD pointer:");
                    ImGui.TableNextColumn();
                    ImGui.Text($"0x{new IntPtr(instanceDirector).ToString("X2")}");
                    //ImGui.TableNextColumn();
                    //ImGui.Text($"CC director pointer:");
                    //ImGui.TableNextColumn();
                    //ImGui.Text($"0x{new IntPtr(crystallineConflictDirector).ToString("X2")}");
                    ImGui.EndTable();
                }

                //ImGui.Text($"Current Content Type: {(instanceDirector != null ? instanceDirector->InstanceContentType : "" )}");
                //ImGui.Text($"instance content director pointer address: 0x{new IntPtr(directorAddress)}");
                //ImGui.Text($"instance content director pointer: 0x{new IntPtr(instanceDirector)}");

                //if (ImGui.Button("Get Content Type")) {
                //    var x = _plugin.Functions.GetContentType();
                //    _plugin.Log.Debug($"Content type: {x}");
                //}

                if(ImGui.Button("Print ICD Bytes")) {
                    _plugin.Functions.FindValue<byte>(0, (nint)instanceDirector, 0x2000, 0, true);
                    //var x = _plugin.Functions.GetRawInstanceContentDirector();
                    //_plugin.Log.Debug($"object size: {x.Length.ToString("X2")} bytes");
                    //int offset = 0x0;
                    //foreach(var b in x) {
                    //    string inHex = b.ToString("X2");
                    //    _plugin.Log.Debug($"offset: {offset.ToString("X2")} value: {inHex}");
                    //    offset++;
                    //}
                }

                //if (ImGui.Button("Print ICD Chars")) {
                //    _plugin.Functions.FindValue<string>("", (nint)instanceDirector, 0x2090, 0, true);
                //}

                //if (ImGui.Button("Print CC Director Bytes+")) {
                //    _plugin.Functions.FindValue(0, crystallineConflictDirector, 0x310, 0, true);
                //    _plugin.Functions.FindValue<short>(0, crystallineConflictDirector, 0x310, 0, true);
                //    //_plugin.Functions.FindValue<long>(0, dataPtr, 0x310, 0, true);
                //    _plugin.Functions.FindValue<byte>(0, crystallineConflictDirector, 0x310, 0, true);
                //}

                //if (ImGui.Button("Get DD Content Director Bytes")) {
                //    var x = _plugin.Functions.GetRawDeepDungeonInstanceContentDirector();
                //    _plugin.Log.Debug($"object size: {x.Length.ToString("X2")} bytes");
                //    int offset = 0x0;
                //    foreach (var b in x) {
                //        string inHex = b.ToString("X2");
                //        _plugin.Log.Debug($"offset: {offset.ToString("X2")} value: {inHex}");
                //        offset++;
                //    }

                //    var y = EventFramework.Instance()->GetInstanceContentDeepDungeon();
                //    foreach (var pMember in y->PartySpan) {
                //        _plugin.Log.Debug($"party member: {pMember.ObjectId.ToString("X2")}");
                //    }
                //}

                //if (ImGui.Button("Read Int32 from Content Director")) {
                //    _plugin.Functions.AttemptToReadContentDirector();
                //}

                if(ImGui.Button("Create ICD Byte Dump")) {
                    _plugin.Functions.CreateByteDump((nint)instanceDirector, 0x2030, "ICD");
                }

                if(ImGui.Button("Print Object Table")) {
                    foreach(PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
                        _plugin.Log.Debug($"0x{pc.ObjectId.ToString("X2")} {pc.Name}");
                        //_plugin.Log.Debug($"team null? {isPlayerTeam is null} player team? {isPlayerTeam} is p member? {pc.StatusFlags.HasFlag(StatusFlags.PartyMember)} isSelf? {isSelf}");
                    }
                }

                ImGui.Separator();

                //if(ImGui.InputText($"Value To Find##findvalue", ref _toFind, 80)) {

                //}

                //if(ImGui.Button("Find")) {
                //    _plugin.DataQueue.QueueDataOperation(() => _plugin.Functions.FindValueInContentDirector(_toFind));

                //    //if (int.TryParse(_toFind, out int result)) {
                //    //    _plugin.Functions.FindValueInContentDirector(result);
                //    //}

                //}

                ImGui.Separator();

                //if (ImGui.Button("Get instance content director pointer + address")) {
                //    //var director = EventFramework.Instance()->GetInstanceContentDirector();
                //    //var directorAddress = &director;
                //    //_plugin.Log.Debug($"instance content director pointer address: 0x{new IntPtr(directorAddress)}");
                //    //_plugin.Log.Debug($"instance content director pointer: 0x{((nint)director).ToString("X2")}");

                //    //int number = 27;
                //    //int* pointerToNumber = &number;

                //    //var x = &pointerToNumber;
                //}
                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("Network Messages")) {
                if(_plugin.MatchManager is not null) {
                    if(ImGui.Button("Clear opcodes")) {
                        _plugin.Functions._opCodeCount = new();
                    }

                    ImGui.Text($"Current match count: {_plugin.Functions._opcodeMatchCount}");

                    if(ImGui.BeginTable("opcodetable", 2)) {

                        ImGui.TableNextColumn();
                        ImGui.Text("Opcode");
                        ImGui.TableNextColumn();
                        ImGui.Text("Count");

                        foreach(var opcode in _plugin.Functions._opCodeCount) {

                            ImGui.TableNextColumn();
                            ImGui.Text($"{opcode.Key}");
                            ImGui.TableNextColumn();
                            ImGui.Text($"{opcode.Value}");
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }
            }

            using(var tab = ImRaii.TabItem("IPC")) {
                if(tab) {
                    var playerName = _player;
                    if(ImGui.InputTextWithHint("###PlayerNameInput", "Enter player name and world", ref playerName, 50, ImGuiInputTextFlags.EnterReturnsTrue)) {
                        _player = playerName;
                        try {
                            //var alias = (PlayerAlias)_player;
                            ////_plugin.Log.Debug(_plugin.Lodestone.GetPlayerCurrentNameWorld(alias));
                            ////var prevNames = _plugin.Lodestone.GetPlayerCurrentNameWorld(alias);
                            //_plugin.Log.Debug(_plugin.PlayerLinksService.GetPlayerLodestoneId(alias).ToString());
                            //var prevAliases = _plugin.PlayerLinksService.GetPreviousAliases(alias);
                            //foreach(var a in prevAliases) {
                            //    _plugin.Log.Debug(a);
                            //}

                        } catch(ArgumentException) {

                        }
                    }
                }
            }

            ImGui.EndTabBar();
        }

        //if (ImGui.Button("test obj table")) {
        //    _plugin.Log.Debug($"current player obj id: {_plugin.ClientState.LocalPlayer.ObjectId}");
        //    foreach (PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
        //        _plugin.Log.Debug(string.Format("name: {0,-30} world: {1,-15} id:{2}", pc.Name, pc.HomeWorld.GameData.Name.ToString(), pc.ObjectId));
        //    }
        //}

        //if (ImGui.InputText($"player name", ref _pname, 80)) {

        //}

    }

    private void TestParse() {
        string matchTimer = "1:35";
        DateTimeFormatInfo dt = new();
        //dt.
        bool parseResult = TimeSpan.TryParse(matchTimer, out TimeSpan ts);

        _plugin.Log.Debug($"parse result: {parseResult} minutes: {ts.Minutes} seconds: {ts.Seconds}");
    }
}
#endif
#pragma warning restore