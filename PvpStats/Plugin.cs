using Dalamud.Configuration;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PvpStats.Settings;
using PvpStats.Windows;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static Lumina.Data.Parsing.Uld.NodeData;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PvpStats {

    public sealed class Plugin : IDalamudPlugin {
        public string Name => "PvpStats";

        private const string DatabaseName = "data.db";

        private const string CommandName = "/pvpstats";
        private const string ConfigCommandName = "/pvpstatsconfig";

        //Dalamud services
        internal DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        internal IDataManager DataManager { get; init; }
        internal IClientState ClientState { get; init; }
        internal ICondition Condition { get; init; }
        internal IDutyState DutyState { get; init; }
        private IPartyList PartyList { get; init; }
        internal IChatGui ChatGui { get; init; }
        private IGameGui GameGui { get; init; }
        private IFramework Framework { get; init; }
        internal IPluginLog Log { get; init; }

        internal IAddonLifecycle AddonLifecycle { get; init; }

        public Configuration Configuration { get; init; }
        internal GameFunctions Functions { get; init; }

        //UI
        internal WindowSystem WindowSystem = new("Pvp Stats");
        private MainWindow MainWindow;

        private bool _matchInProgress;
        private DateTime _lastHeaderUpdateTime;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IDataManager dataManager,
            [RequiredVersion("1.0")] IClientState clientState,
            [RequiredVersion("1.0")] ICondition condition,
            [RequiredVersion("1.0")] IDutyState dutyState,
            [RequiredVersion("1.0")] IPartyList partyList,
            [RequiredVersion("1.0")] IChatGui chatGui,
            [RequiredVersion("1.0")] IGameGui gameGui,
            [RequiredVersion("1.0")] IFramework framework,
            [RequiredVersion("1.0")] IPluginLog log,
            [RequiredVersion("1.0")] IAddonLifecycle addonLifecycle) {
            try {
                PluginInterface = pluginInterface;
                CommandManager = commandManager;
                DataManager = dataManager;
                ClientState = clientState;
                Condition = condition;
                DutyState = dutyState;
                PartyList = partyList;
                ChatGui = chatGui;
                GameGui = gameGui;
                Framework = framework;
                Log = log;
                AddonLifecycle = addonLifecycle;

                Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
                Functions = new GameFunctions();

                Configuration.Initialize(this);

                MainWindow = new MainWindow(this);
                WindowSystem.AddWindow(MainWindow);
                CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
                    HelpMessage = "Opens something."
                });


                CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnConfigCommand) {
                    HelpMessage = "Open settings window."
                });

                PluginInterface.UiBuilder.Draw += DrawUI;
                PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

                Framework.Update += OnFrameworkUpdate;
                ChatGui.ChatMessage += OnChatMessage;
                ClientState.TerritoryChanged += OnTerritoryChanged;
                DutyState.DutyCompleted += OnDutyCompleted;
                DutyState.DutyStarted += OnDutyStarted;

                Log.Debug("starting up");
                AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "PvPMKSIntroduction", OnPvPIntro);
                AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "PvPMKSIntroduction", OnPvPIntroUpdate);
                //AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MKSRecord", OnPvPResults);
                AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "MKSRecord", OnPvPResults);
                AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "PvPMKSHeader", OnPvPHeaderUpdate);
                //AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "PvpProfileColosseum", OnPvPIntro);
                AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "MonsterNote", Monster);
            } catch(Exception e) {
                //remove handlers and release database if we fail to start
                Dispose();
                //it really shouldn't ever be null
                Log!.Error($"Failed to initialize plugin constructor: {e.Message}");
                //re-throw to prevent constructor from initializing
                throw;
            }

        }

        //Custom config loader. Unused
        public IPluginConfiguration? GetPluginConfig() {
            //string pluginName = PluginInterface.InternalName;
            FileInfo configFile = PluginInterface.ConfigFile;
            if(!configFile.Exists) {
                return null;
            }
            return JsonConvert.DeserializeObject<IPluginConfiguration>(File.ReadAllText(configFile.FullName), new JsonSerializerSettings {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        public void Dispose() {
#if DEBUG
            Log.Debug("disposing plugin");
#endif

            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(ConfigCommandName);

            Framework.Update -= OnFrameworkUpdate;
            ChatGui.ChatMessage -= OnChatMessage;
            ClientState.TerritoryChanged -= OnTerritoryChanged;
            DutyState.DutyCompleted -= OnDutyCompleted;
            DutyState.DutyStarted -= OnDutyStarted;

            AddonLifecycle.UnregisterListener(OnPvPIntro);
            AddonLifecycle.UnregisterListener(OnPvPIntroUpdate);
            AddonLifecycle.UnregisterListener(OnPvPResults);
            AddonLifecycle.UnregisterListener(OnPvPHeaderUpdate);
            AddonLifecycle.UnregisterListener(Monster);
        }

        private void OnCommand(string command, string args) {
            MainWindow.IsOpen = true;
        }

        private void OnConfigCommand(string command, string args) {
            DrawConfigUI();
        }

        private void DrawUI() {
            WindowSystem.Draw();
        }

        private void DrawConfigUI() {
        }

        private void OnFrameworkUpdate(IFramework framework) {
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
        }

        private void OnDutyStarted(object? sender, ushort param1) {
            if(_matchInProgress) {
                Log.Debug("Match has started.");
            }
            
        }

        private void OnDutyCompleted(object? sender, ushort param1) {
            if(_matchInProgress) {
                _matchInProgress = false;
                Log.Debug("Match ended.");
            }
        }

        private void OnTerritoryChanged(ushort territoryId) {
            var dutyId = GetCurrentDutyId();
            var duty = DataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow((uint)dutyId);
            //Log.Verbose($"Territory changed: {territoryId}, Current duty: {GetCurrentDutyId()}");
            //bool isCrystallineConflict = false;

            switch(dutyId) {
                case 835:
                case 836:
                case 837:
                case 856:
                case 857:
                case 858:
                case 912:
                case 918:
                    //isCrystallineConflict = true;
                    _matchInProgress = true;
                    Log.Debug($"Match has started on {DataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow((uint)dutyId).Name}");
                    break;
                default:
                    _matchInProgress = false;
                    break;
            }

            //if(isCrystallineConflict) {
            //    Log.Debug($"Match has started on {duty}");
            //}

        }

        private unsafe void Monster(AddonEvent type, AddonArgs args) {
            Log.Debug("MONSTAH!");
            var addon = (AtkUnitBase*)args.Addon;

            //PrintAtkValues(addon);
            PrintTextNodes(addon->GetNodeById(1));

            //var targetNode = addon->GetNodeById(22);

            //if(targetNode != null) {
            //    var textNode = targetNode->GetAsAtkTextNode();
            //    if(textNode != null) {
            //        Log.Debug($"{textNode->NodeText}");
            //    }
            //}
        }

        private unsafe void OnPvPHeaderUpdate(AddonEvent type, AddonArgs args) {
            //Log.Debug("pvp header refresh trig00red.");
            if((DateTime.Now - _lastHeaderUpdateTime).TotalSeconds > 30 && _matchInProgress) {
                var addon = (AtkUnitBase*)args.Addon;
                _lastHeaderUpdateTime = DateTime.Now;
                //PrintAtkValues(addon);
                var leftTeam = addon->GetNodeById(45)->GetAsAtkTextNode();
                var rightTeam = addon->GetNodeById(46)->GetAsAtkTextNode();
                var leftProgress = addon->GetNodeById(47)->GetAsAtkTextNode();
                var rightProgress = addon->GetNodeById(48)->GetAsAtkTextNode();
                var timerMins = addon->GetNodeById(25)->GetAsAtkTextNode();
                var timerSeconds = addon->GetNodeById(27)->GetAsAtkTextNode();

                bool isOvertime = addon->GetNodeById(23) != null ? addon->GetNodeById(23)->IsVisible : false;

                Log.Debug($"MATCH TIMER: {timerMins->NodeText}:{timerSeconds->NodeText}");
                Log.Debug($"OVERTIME: {isOvertime}");
                Log.Debug($"{leftTeam->NodeText}: {leftProgress->NodeText}");
                Log.Debug($"{rightTeam->NodeText}: {rightProgress->NodeText}");
                Log.Debug("--------");
            }
        }

        private unsafe void OnPvPResults(AddonEvent type, AddonArgs args) {
            Log.Debug("pvp record pre-setup.");
            //PrintAtkValues((AtkUnitBase*)args.Addon);

            //if((DateTime.Now - _lastHeaderUpdateTime).TotalSeconds > 10) {
            //    var addon = (AtkUnitBase*)args.Addon;
            //    _lastHeaderUpdateTime = DateTime.Now;
            //    PrintAtkValues(addon);
            //}
        }

        private unsafe void OnPvPResultsPreSetup(AddonEvent type, AddonArgs args) {
            Log.Debug("pvp record preSetup");
            PrintAtkValues((AtkUnitBase*)args.Addon);

            //if((DateTime.Now - _lastHeaderUpdateTime).TotalSeconds > 10) {
            //    var addon = (AtkUnitBase*)args.Addon;
            //    _lastHeaderUpdateTime = DateTime.Now;
            //    PrintAtkValues(addon);
            //}
        }

        private unsafe void OnPvPIntroUpdate(AddonEvent type, AddonArgs args) {
            //Log.Debug("intro prefinalize");
            var addon = (AtkUnitBase*)args.Addon;
            //PrintTextNodes(addon->GetNodeById(1));
        }


        private unsafe void OnPvPIntro(AddonEvent type, AddonArgs args) {
            Log.Debug("Pvp intro post setup!");
            var addon = (AtkUnitBase*)args.Addon;

            //PrintAtkValues(addon);
            PrintTextNodes(addon->GetNodeById(1), true, false);

            //team
            Log.Debug(ConvertAtkValueToString(addon->AtkValues[4]));
            for(int i = 0; i < 5; i++) {
                int offset = i * 16 + 6;
                if(offset >= addon->AtkValuesCount) {
                    break;
                }
                string player = ConvertAtkValueToString(addon->AtkValues[offset]);
                string world = ConvertAtkValueToString(addon->AtkValues[offset + 6]);
                string job = ConvertAtkValueToString(addon->AtkValues[offset + 5]);
                Log.Debug(string.Format("player: {0,-25} {1,-15} job: {2,-15}", player, world, job));
            }
            Log.Debug("");


            //var test = addon->GetNodeById(1);
            ////CheckNodes(test);

            //var listNode = addon->GetNodeById(4);
            //var listNodeComponent = listNode->GetAsAtkComponentNode();
            //var componentTree = (AtkComponentTreeList*)listNodeComponent->Component;

            //Log.Debug($"here");


            //Log.Debug($"Child count: {test->ChildCount}");
            //var test2 = test->ChildNode;
            //Log.Debug($"Child Id: {test2->NodeID}");
            //Log.Debug($"Child count: {test2->ChildCount}");

            //test = addon->GetNodeById(4);
            //Log.Debug($"Child count: {test->ChildCount}");
            //CheckNodes(addon->GetNodeById(1));
            //CheckNodes(addon->GetNodeById(11));
            //CheckNodes(addon->GetNodeById(3));
            ////get player list..
            //var player1 = addon->GetNodeById(17);
            //var child = player1->ChildNode;
            //for(int i = 0; i < player1->ChildCount; i++) {
            //    //get child node
            //    for(int j = 0; j < i; j++) {
            //        child = child->PrevSiblingNode;
            //    }

            //    try {
            //        var tNode = child->GetAsAtkTextNode();
            //        Log.Debug($"{tNode->NodeText}");
            //    } catch(InvalidOperationException e) {

            //    }

            //    //check children
            //    for(int j = 0; j < child->ChildCount; j++) {

            //    }
            //}

            ////17, 16, 15, 14, 13

        }

        internal unsafe AtkResNode* GetNodeByIDChain(string addon, params uint[] ids) {
            AtkUnitBase* addonNode = AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonByName(addon);
            if(addon == null || ids.Length <= 0) {
                return null;
            }

            return GetNodeByIDChain((AtkResNode*)addonNode->RootNode, ids);

            //var currentNode = addonNode->GetNodeById(ids[0]);
            //if(currentNode == null || ids.Length == 1) {
            //    return currentNode;
            //}

            //for(int i = 1; i < ids.Length; i++) {
            //    if((int)currentNode->Type < 1000) {
            //        return null;
            //    }

            //    var nextNode = currentNode->GetAsAtkComponentNode()->Component->UldManager.SearchNodeById(ids[i]);
            //    if(nextNode == null) {
            //        return null;
            //    }
            //    currentNode = nextNode;
            //}
            //return currentNode;
        }

        internal unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params uint[] ids) {
            if(node == null || ids.Length <= 0) {
                return null;
            }

            if(node->NodeID == ids[0]) {
                if(ids.Length == 1) {
                    return node;
                }

                var newList = new List<uint>(ids);
                newList.RemoveAt(0);

                var childNode = node->ChildNode;
                if(childNode != null) {
                    return GetNodeByIDChain(childNode, newList.ToArray());
                } else if((int)node->Type >= 1000) {
                    var componentNode = node->GetAsAtkComponentNode();
                    var component = componentNode->Component;
                    var uldManager = component->UldManager;
                    childNode = uldManager.NodeList[0];
                    if(childNode == null) {
                        return null;
                    } else {
                        return GetNodeByIDChain(childNode, newList.ToArray());
                    }
                } else {
                    return null;
                }
            } else {
                //check siblings
                var sibNode = node->PrevSiblingNode;
                if(sibNode != null) {
                    return GetNodeByIDChain(sibNode, ids);
                } else {
                    return null;
                }
            }
        }

        private unsafe void PrintAtkValues(AtkUnitBase* node) {
            if(node->AtkValues != null) {
                for(int i = 0; i < node->AtkValuesCount; i++) {
                    //node->AtkValues[i].Type
                    //string data = Marshal.PtrToStringUTF8((nint)node->AtkValues[i].String);
                    //string data = Marshal.(node->AtkValues[i].String);
                    string data = ConvertAtkValueToString(node->AtkValues[i]);

                    //switch(node->AtkValues[i].Type) {
                    //    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int:
                    //    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt:
                    //        data = node->AtkValues[i].Int.ToString(); 
                    //        break;
                    //    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool:
                    //        data = (node->AtkValues[i].Int != 0).ToString();
                    //        break;
                    //    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String:
                    //    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.AllocatedString:
                    //    case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String8:
                    //        data = Marshal.PtrToStringUTF8((nint)node->AtkValues[i].String);
                    //        break;
                    //    default:
                    //        break;
                    //}

                    Log.Debug(string.Format("index: {0,-5} type: {1,-15} data: {2}", i, node->AtkValues[i].Type, data));
                }
            }
        }

        private unsafe string ConvertAtkValueToString(AtkValue value) {
            switch(value.Type) {
                case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int:
                case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt:
                    return value.Int.ToString();
                case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool:
                    return (value.Int != 0).ToString();
                case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String:
                case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.AllocatedString:
                case FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String8:
                    return Marshal.PtrToStringUTF8((nint)value.String);
                default:
                    break;
            }
            return "";
        }

        private unsafe void PrintAtkStringArray() {
            int index = 0;
            var stringArray = AtkStage.GetSingleton()->GetStringArrayData()[index];
            while(stringArray != null) {
                //stringArray->StringArray;
            }
        }

        internal unsafe int GetCurrentDutyId() {
            return GameMain.Instance()->CurrentContentFinderConditionId;
        }

        internal unsafe void PrintTextNodes(string addon) {
            AtkUnitBase* addonNode = AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonByName(addon);
            if(addonNode != null) {
                //Log.Debug($"addon name: {Marshal.PtrToStringUTF8((nint)addonNode->Name)} ptr: {string.Format("0x{0:X8}", new IntPtr(addonNode).ToString())}");
                Log.Debug($"addon name: {Marshal.PtrToStringUTF8((nint)addonNode->Name)} ptr: 0x{new IntPtr(addonNode).ToString("X8")}");
                PrintTextNodes((AtkResNode*)addonNode->RootNode);
            } else {
                Log.Debug($"{addon} is null!");
            }
        }

        private unsafe void PrintTextNodes(AtkResNode* node, bool checkSiblings = true, bool onlyVisible = true) {
            if(node == null) {
                return;
            }
            int type = (int)node->Type;
            int childCount = node->ChildCount;
            var parentNode = node->ParentNode;
            uint parentNodeId = parentNode != null ? parentNode->NodeID : 0;

            string parentNodeIDString = "";
            string parentNodeTypeString = "";
            while(parentNode != null) {
                parentNodeIDString += parentNode->NodeID;
                parentNodeTypeString += parentNode->Type;
                parentNode = parentNode->ParentNode;
                if(parentNode != null) {
                    parentNodeIDString += "<-";
                    parentNodeTypeString += "<-";
                }
            }


            if(type >= 1000) {
                childCount = node->GetAsAtkComponentNode()->Component->UldManager.NodeListCount;
            }

            //Log.Debug($"Checking id: {node->NodeID} type: {type}");
            //if(type == 3) {
            //    Log.Debug(string.Format("ID: {0,-8} type: {1,-6} childCount: {2,-4} parentID: {3}", node->NodeID, type, childCount, parentNodeIDString));
            //}
            

            try {
                var tNode = node->GetAsAtkTextNode();
                if(tNode != null) {
                    string nodeText = tNode->NodeText.ToString();
                    if(!nodeText.IsNullOrEmpty() && (node->IsVisible || !onlyVisible)) {
                        Log.Debug(string.Format("Visible: {5,-6} ID: {0,-8} type: {1,-6} childCount: {2,-4} parentID: {3,-25} parentType: {4}", node->NodeID, type, childCount, parentNodeIDString, parentNodeTypeString, node->IsVisible));
                        Log.Debug($"Text: {tNode->NodeText}");
                    }
                }
            } catch(Exception e) {
                //not a text node!
            }

            //component
            //Window: 1004
            //DropDownList: 1010
            //List: 1019
            //RadioButton: 1021
            //RadioButton: 1013
            if(type >= 1000) {
                var componentNode = node->GetAsAtkComponentNode();
                var component = componentNode->Component;
                var uldManager = component->UldManager;

                if(node->IsVisible || !onlyVisible) {
                    for(int i = 0; i < uldManager.NodeListCount; i++) {
                        var childNode = uldManager.NodeList[i];
                        PrintTextNodes(childNode, false);
                    }
                }
            }

            //check child nodes
            var child = node->ChildNode;
            if(child != null || (!node->IsVisible && onlyVisible)) {
                PrintTextNodes(child);
            }

            //check sibling nodes
            var sibNode = node->PrevSiblingNode;
            if(sibNode != null && checkSiblings) {
                PrintTextNodes(sibNode);
            }
        }
    }
}
