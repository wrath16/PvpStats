﻿#pragma warning disable
#if DEBUG
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.Sheets;
using PvpStats.Helpers;
using PvpStats.Managers.Game;
using PvpStats.Services;
using PvpStats.Types.ClientStruct;
using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using static Lumina.Data.Parsing.Uld.UldRoot;
using static PvpStats.Types.ClientStruct.RivalWingsContentDirector;

namespace PvpStats.Windows;
internal unsafe class DebugWindow : Window {

    private Plugin _plugin;
    private string _addon = "";
    private string _idChain = "";
    private uint[] _idParams;

    private string _pname = "";

    private string _toFind = "";
    private string _player = "";

    internal HashSet<PlayerAlias> CompetentPlayers { get; private set; } = [];

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

        using(var tabBar = ImRaii.TabBar("debugTabs")) {
            using(var tab = ImRaii.TabItem("Addon")) {
                if(tab) {
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


                    if(ImGui.Button("GetNodeById")) {
                        unsafe {
                            AtkUnitBase* addonNode = AtkStage.Instance()->RaptureAtkUnitManager->GetAddonByName(_addon);
                            if(uint.TryParse(_idChain, out uint result) && addonNode != null) {
                                var x = addonNode->GetNodeById(result);
                                _plugin.Log.Debug($"0x{new IntPtr(x).ToString("X8")}");
                            }

                        }

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
                }
            }

            using(var tab = ImRaii.TabItem("ContentDirector")) {
                if(tab) {
                    var instanceDirector = EventFramework.Instance()->GetInstanceContentDirector();
                    var directorAddress = &instanceDirector;
                    using(var table = ImRaii.Table("memoryStringTable", 2)) {
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
                    }
                    if(ImGui.Button("Copy ICD ptr")) {
                        ImGui.SetClipboardText(new IntPtr(instanceDirector).ToString("X2"));
                    }


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

                    if(ImGui.Button("Create ICD Byte Dump")) {
                        _plugin.Functions.CreateByteDump((nint)instanceDirector, 0x3000, "ICD");
                    }

                    if(ImGui.Button("Print Object Table")) {
                        foreach(IPlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
                            _plugin.Log.Debug($"0x{pc.GameObjectId.ToString("X2")} {pc.Name}");
                            //_plugin.Log.Debug($"team null? {isPlayerTeam is null} player team? {isPlayerTeam} is p member? {pc.StatusFlags.HasFlag(StatusFlags.PartyMember)} isSelf? {isSelf}");
                        }
                    }

                    ImGui.Separator();
                    if(instanceDirector != null && instanceDirector->InstanceContentType == FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentType.RivalWing) {
                        DrawRivalWingsDirector();
                    } else if(instanceDirector != null && instanceDirector->InstanceContentType == FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentType.Frontlines) {
                        DrawFrontlineDirector();
                    }
                    //ImGui.Text($"0x{new IntPtr(instanceDirector + RivalWingsMatchManager.RivalWingsContentDirectorOffset).ToString("X2")}");
                    //ImGui.Text($"0x{new IntPtr(instanceDirector + 0x1E58).ToString("X2")}");
                    //var x = (IntPtr)instanceDirector + 0x1E58;
                    //ImGui.Text($"0x{x.ToString("X2")}");
                    //var x = EventFramework.Instance()->GetInstanceContentDirector();
                    //ImGui.Text($"0x{((IntPtr)x).ToString("X2")}");
                    //var y = x + RivalWingsMatchManager.RivalWingsContentDirectorOffset;
                    //ImGui.Text($"0x{((IntPtr)y).ToString("X2")}");
                    //var z = (IntPtr)x + RivalWingsMatchManager.RivalWingsContentDirectorOffset;
                    //ImGui.Text($"0x{((IntPtr)z).ToString("X2")}");

                    ImGui.Separator();
                }
            }

            using(var tab = ImRaii.TabItem("Network Messages")) {
                if(tab) {
                    if(_plugin.CCMatchManager is not null) {
                        if(ImGui.Button("Clear opcodes")) {
                            _plugin.Functions._opCodeCount = new();
                        }

                        ImGui.Text($"Current match count: {_plugin.Functions._opcodeMatchCount}");

                        using(var table = ImRaii.Table("opcodetable", 2)) {

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
                        }
                    }
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
                            var mainAlias = _plugin.PlayerLinksService.GetMainAlias((PlayerAlias)_player);
                            _plugin.Log.Debug($"{_player} is {mainAlias}");

                        } catch(ArgumentException) {

                        }
                    }
                }
            }
            using(var tab = ImRaii.TabItem("Misc.")) {
                if(tab) {
                    if(ImGui.Button("Enable Duty Leave Button")) {
                        _plugin.RWMatchManager.EnableLeaveDutyButton();
                    }

                    if(ImGui.Button("Get competent players")) {
                        _plugin.DataQueue.QueueDataOperation(() => {
                            CompetentPlayers = [];
                            foreach(var match in _plugin.FLCache.Matches.Where(x => x.IsCompleted && !x.IsDeleted)) {
                                foreach(var scoreboard in match.PlayerScoreboards) {
                                    if(scoreboard.Value.KDA >= 20) {
                                        CompetentPlayers.Add((PlayerAlias)scoreboard.Key);
                                    }
                                }
                            }
                        });
                    }

                    if(ImGui.Button("Find shared accounts")) {
                        Dictionary<ulong, PlayerAlias> accounts = new();
                        Dictionary<ulong, HashSet<PlayerAlias>> linkedAliases = new();
                        System.Threading.Tasks.Task.Run(() => {
                            foreach(var match in _plugin.CCCache.Matches.Where(x => x.IsCompleted && !x.IsDeleted && x.PostMatch != null)) {
                                foreach(var team in match.Teams) {
                                    foreach(var player in team.Value.Players.Where(x => x.AccountId != null)) {
                                        var accountId = (ulong)player.AccountId;
                                        linkedAliases.TryAdd(accountId, new());
                                        if(!accounts.TryAdd(accountId, player.Alias)) {
                                            if(!player.Alias.Equals(accounts[accountId])) {
                                                if(linkedAliases[accountId].Add(player.Alias)) {
                                                    Plugin.Log2.Debug($"{accounts[accountId]} is {player.Alias}!");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }

                    if(ImGui.Button("Show all Duties")) {
                        foreach(var duty in _plugin.DataManager.GetExcelSheet<ContentFinderCondition>()) {
                            _plugin.Log.Debug($"id: {duty.RowId} name: {duty.Name}");
                        }
                    }

                    if(ImGui.Button("Show all Zones")) {
                        foreach(var zone in _plugin.DataManager.GetExcelSheet<TerritoryType>()) {
                            _plugin.Log.Debug($"id: {zone.RowId} name: {zone.PlaceName.Value.Name}");
                        }
                    }

                    if(ImGui.Button("First Mid wins...%")) {
                        _plugin.DataQueue.QueueDataOperation(() => {
                            int matchCount = 0;
                            int firstMidWins = 0;

                            var rwMatches = _plugin.RWCache.Matches.Where(x => x.IsCompleted && x.TimelineId != null);
                            var timelines = _plugin.Storage.GetRWTimelines().Query().ToList();
                            foreach(var rwMatch in rwMatches) {
                                var timeline = timelines.Where(x => x.Id.Equals(rwMatch.TimelineId)).FirstOrDefault();
                                if(timeline == null || timeline.MidClaims == null) {
                                    continue;
                                }
                                if(timeline.MidClaims.First().Kind == RivalWingsSupplies.Gobbiejuice) {
                                    continue;
                                }
                                matchCount++;
                                var firstMid = timeline.MidClaims.First();
                                if(firstMid.Team == rwMatch.MatchWinner) {
                                    firstMidWins++;
                                }
                            }

                            Plugin.Log2.Debug($"Matches: {matchCount}\nFirst Mid Winner Wins Match (no Gobbiejuice): {firstMidWins}\n{((float)firstMidWins / matchCount):P2}");
                        });

                    }

                    if(ImGui.Button("Test Timeline doohicky")) {

                    }


                    ImGui.Text(Framework.Instance()->GameVersionString);
                    ImGui.Text(Assembly.GetExecutingAssembly().GetName().Version.ToString());

                    ImGuiHelper.DrawRainbowTextByChar("Sarah Montcroix");
                    ImGui.NewLine();

                }
            }
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

    private void DrawRivalWingsDirector() {
        var instanceDirector = (RivalWingsContentDirector*)((IntPtr)EventFramework.Instance()->GetInstanceContentDirector() + RivalWingsMatchManager.RivalWingsContentDirectorOffset);
        using(var table = ImRaii.Table("core", 2)) {
            if(table) {
                ImGui.TableSetupColumn("falcons");
                ImGui.TableSetupColumn("ravens");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Falcon Core");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Raven Core");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->FalconCore.Integrity.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->RavenCore.Integrity.ToString());
            }
        }

        using(var table = ImRaii.Table("towers", 4)) {
            if(table) {
                ImGui.TableSetupColumn("ft1");
                ImGui.TableSetupColumn("ft2");
                ImGui.TableSetupColumn("rt1");
                ImGui.TableSetupColumn("rt2");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Tower 1");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Tower 2");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Tower 1");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Tower 2");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->FalconTower1.Integrity.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->FalconTower2.Integrity.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->RavenTower1.Integrity.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->RavenTower2.Integrity.ToString());
            }
        }

        using(var table = ImRaii.Table("mechs", 3)) {
            if(table) {
                ImGui.TableSetupColumn("mech");
                ImGui.TableSetupColumn("falcons");
                ImGui.TableSetupColumn("ravens");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Chasers:");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->FalconChaserCount.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->RavenChaserCount.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Oppressors:");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->FalconOppressorCount.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->RavenOppressorCount.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Justices:");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->FalconJusticeCount.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->RavenJusticeCount.ToString());
            }
        }

        using(var table = ImRaii.Table("mercs", 2)) {
            if(table) {
                ImGui.TableSetupColumn("desc");
                ImGui.TableSetupColumn("val");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Merc Control");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->MercControl.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Balance");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->MercBalance.ToString());
            }
        }

        using(var table = ImRaii.Table("mid", 2)) {
            if(table) {
                ImGui.TableSetupColumn("desc");
                ImGui.TableSetupColumn("val");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Mid Type");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->MidType.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Mid Control");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->MidControl.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Falcon Score");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->FalconMidScore.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Raven Score");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->RavenMidScore.ToString());
            }
        }

        using(var table = ImRaii.Table("playermechs", 2)) {
            if(table) {
                ImGui.TableSetupColumn("player");
                ImGui.TableSetupColumn("mech");

                for(int i = 0; i < instanceDirector->FriendlyMechSpan.Length; i++) {
                    var friendlyMechNative = instanceDirector->FriendlyMechSpan[i];
                    //var mechStats = _playerMechStats[i];
                    //add input bounds for sanity check in case of missing alliance
                    if(friendlyMechNative.Type != MechType.None) {
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(friendlyMechNative.PlayerObjectId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(friendlyMechNative.Type.ToString());
                    }
                }
            }
        }

        using(var table = ImRaii.Table("Alliances", 3)) {
            if(table) {
                ImGui.TableSetupColumn("alliance");
                ImGui.TableSetupColumn("soaring");
                ImGui.TableSetupColumn("ceruleum");

                for(int i = 0; i < instanceDirector->AllianceSpan.Length; i++) {
                    var allianceStat = instanceDirector->AllianceSpan[i];
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(i.ToString());
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(allianceStat.SoaringStacks.ToString());
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(allianceStat.Ceruleum.ToString());
                }
            }
        }
    }

    private void DrawFrontlineDirector() {
        var instanceDirector = (FrontlineContentDirector*)((IntPtr)EventFramework.Instance()->GetInstanceContentDirector());
        using(var table = ImRaii.Table("main", 2)) {
            if(table) {
                ImGui.TableSetupColumn("c1");
                ImGui.TableSetupColumn("c2");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Battle High");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->PlayerBattleHigh.ToString());

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Maelstrom");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->MaelstromScore.ToString());

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Adders");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->AddersScore.ToString());

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Flames");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(instanceDirector->FlamesScore.ToString());

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
#endif
#pragma warning restore