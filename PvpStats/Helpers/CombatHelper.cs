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
}
