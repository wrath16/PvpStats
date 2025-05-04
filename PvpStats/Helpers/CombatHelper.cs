using PvpStats.Types.Player;
using System;
using System.Collections.Generic;

namespace PvpStats.Helpers;
internal static class CombatHelper {
    public static bool IsLimitBreak(uint actionId) {
        foreach(var value in Enum.GetValues(typeof(LimitBreak))) {
            if(actionId == (uint)value) {
                return true;
            }
        }
        return false;
    }

    public static bool IsImportantStatus(uint statusId) {
        //List<>
        foreach(var value in Enum.GetValues(typeof(MajorStatus))) {
            if(statusId == (uint)value) {
                return true;
            }
        }
        return false;
    }
}
