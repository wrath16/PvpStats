using PvpStats.Types.Player;
using System;

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

    public static bool IsUselessStatus(uint statusId) {
        //non-combat crap
        if(statusId >= 360 && statusId <= 368) {
            return true;
        }

        foreach(var value in Enum.GetValues(typeof(UselessStatus))) {
            if(statusId == (uint)value) {
                return true;
            }
        }
        return false;
    }
}
