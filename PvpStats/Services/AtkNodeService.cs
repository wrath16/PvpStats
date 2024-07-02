using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PvpStats.Services;
internal class AtkNodeService {

    private Plugin _plugin;

    internal AtkNodeService(Plugin plugin) {
        _plugin = plugin;
    }

    internal static unsafe AtkResNode* GetNodeByIDChain(string addon, params uint[] ids) {
        AtkUnitBase* addonNode = AtkStage.Instance()->RaptureAtkUnitManager->GetAddonByName(addon);
        if(addonNode == null || ids.Length <= 0) {
            return null;
        }
        return GetNodeByIDChain(addonNode->RootNode, ids);
    }

    internal static unsafe AtkResNode* GetNodeByIDChain(AtkUnitBase* addon, params uint[] ids) {
        if(addon == null) {
            return null;
        }
        return GetNodeByIDChain(addon->RootNode, ids);
    }

    internal static unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params uint[] ids) {
        if(node == null || ids.Length <= 0) {
            return null;
        }

        if(node->NodeId == ids[0]) {
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

    internal unsafe void PrintTextNodes(string addon) {
        AtkUnitBase* addonNode = AtkStage.Instance()->RaptureAtkUnitManager->GetAddonByName(addon);
        if(addonNode != null) {
            //Log.Debug($"addon name: {Marshal.PtrToStringUTF8((nint)addonNode->Name)} ptr: {string.Format("0x{0:X8}", new IntPtr(addonNode).ToString())}");
            _plugin.Log.Debug($"addon name: {addonNode->NameString} ptr: 0x{new nint(addonNode).ToString("X8")}");
            PrintTextNodes(addonNode->RootNode);
        } else {
            _plugin.Log.Debug($"{addon} is null!");
        }
    }

    internal unsafe void PrintTextNodes(AtkUnitBase* addon) {
        PrintTextNodes(addon->RootNode);
    }

    internal unsafe void PrintTextNodes(AtkResNode* node, bool checkSiblings = true, bool onlyVisible = true) {
        if(node == null) {
            return;
        }
        int type = (int)node->Type;
        int childCount = node->ChildCount;
        var parentNode = node->ParentNode;
        uint parentNodeId = parentNode != null ? parentNode->NodeId : 0;

        string parentNodeIDString = "";
        string parentNodeTypeString = "";
        while(parentNode != null) {
            parentNodeIDString += parentNode->NodeId;
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

        if(node->Type == NodeType.Text) {
            var tNode = node->GetAsAtkTextNode();
            if(tNode != null) {
                string nodeText = tNode->NodeText.ToString();
                if(!nodeText.IsNullOrEmpty() && (node->IsVisible() || !onlyVisible)) {
                    _plugin.Log.Debug(string.Format("Visible: {5,-6} ID: {0,-8} type: {1,-6} childCount: {2,-4} parentID: {3,-25} parentType: {4}", node->NodeId, type, childCount, parentNodeIDString, parentNodeTypeString, node->IsVisible));
                    _plugin.Log.Debug($"Text: {tNode->NodeText}");
                }
            }
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

            if(node->IsVisible() || !onlyVisible) {
                for(int i = 0; i < uldManager.NodeListCount; i++) {
                    var childNode = uldManager.NodeList[i];
                    PrintTextNodes(childNode, false);
                }
            }
        }

        //check child nodes
        var child = node->ChildNode;
        if(child != null || !node->IsVisible() && onlyVisible) {
            PrintTextNodes(child, checkSiblings);
        }

        //check sibling nodes
        var sibNode = node->PrevSiblingNode;
        if(sibNode != null && checkSiblings) {
            PrintTextNodes(sibNode, checkSiblings);
        }
    }

    internal unsafe void PrintAtkValues(AtkUnitBase* node) {
        if(node->AtkValues != null) {
            for(int i = 0; i < node->AtkValuesCount; i++) {
                string data = ConvertAtkValueToString(node->AtkValues[i]);
                _plugin.Log.Debug(string.Format("index: {0,-5} type: {1,-15} data: {2}", i, node->AtkValues[i].Type, data));
            }
        }
    }

    //internal static unsafe AtkValue* ValidatedAtkValue(AtkUnitBase* addon, int index) {
    //    var value = addon->AtkValues[index];
    //    //if(value == 0) {

    //    //}
    //}

    internal static unsafe string ConvertAtkValueToString(AtkValue value) {
        switch(value.Type) {
            case ValueType.Int:
            case ValueType.UInt:
                return value.Int.ToString();
            case ValueType.Bool:
                return (value.Int != 0).ToString();
            case ValueType.String:
            case ValueType.WideString:
            case ValueType.ManagedString:
            case ValueType.String8:
                return Marshal.PtrToStringUTF8((nint)value.String) ?? "";
            default:
                break;
        }
        return "";
    }

    internal unsafe void PrintAtkStringArray() {
        //int index = 0;
        //var stringArray = AtkStage.GetSingleton()->GetStringArrayData()[index];

        int count = 0;
        var stringArray = AtkStage.Instance()->GetStringArrayData()[0];
        while(stringArray != null) {

            int internalCount = 0;
            var internalArray = stringArray->StringArray[0];
            while(internalArray != null) {
                ////internalArray = stringArray[internalCount];
                //int secondInternalCount = 0;
                //var secondInternalArray = internalArray[0];

                //while(secondInternalArray != null) {
                //    Log.Debug(ReadString(secondInternalArray));
                //    //Log.Debug(Marshal.PtrToStringUTF8((nint)secondInternalArray));
                //    secondInternalCount++;
                //    secondInternalArray = internalArray[secondInternalCount];
                //}

                _plugin.Log.Debug($"{count} {internalCount}\t{MemoryService.ReadString(internalArray)}");

                internalCount++;
                internalArray = stringArray->StringArray[internalCount];
            }
            _plugin.Log.Debug($"{count} Total strings: {internalCount}");

            count++;
            stringArray = AtkStage.Instance()->GetStringArrayData()[count];
        }

        _plugin.Log.Debug($"Total AtkStageStringArrays: {count}");

        //for(int i = 0; i < string)

        //while(stringArray != null) {
        //    //stringArray->StringArray;
        //}
    }
}
