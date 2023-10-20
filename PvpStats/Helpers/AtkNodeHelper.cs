using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Helpers {
    internal class AtkNodeHelper {

        Plugin _plugin;

        internal AtkNodeHelper(Plugin plugin) {
            _plugin = plugin;
        }


        internal static unsafe AtkResNode* GetNodeByIDChain(string addon, params uint[] ids) {
            AtkUnitBase* addonNode = AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonByName(addon);
            if(addonNode == null || ids.Length <= 0) {
                return null;
            }
            return GetNodeByIDChain(addonNode->RootNode, ids);

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

        private static unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params uint[] ids) {
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

    }
}
