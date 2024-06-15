using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PvpStats.Helpers;
internal static class PlayerJobHelper {

    internal static Dictionary<Job, string> AbbreviationNameMap = new Dictionary<Job, string>() {
        { Job.PLD, "Paladin" },
        { Job.WAR, "Warrior" },
        { Job.DRK, "Dark Knight" },
        { Job.GNB, "Gunbreaker" },
        { Job.MNK, "Monk" },
        { Job.DRG, "Dragoon" },
        { Job.NIN, "Ninja" },
        { Job.SAM, "Samurai" },
        { Job.RPR, "Reaper" },
        { Job.VPR, "Viper" },
        { Job.WHM, "White Mage" },
        { Job.SCH, "Scholar" },
        { Job.AST, "Astrologian" },
        { Job.SGE, "Sage" },
        { Job.BRD, "Bard" },
        { Job.MCH, "Machinist" },
        { Job.DNC, "Dancer" },
        { Job.BLM, "Black Mage" },
        { Job.SMN, "Summoner" },
        { Job.RDM, "Red Mage" },
        { Job.PIC, "Pictomancer" },
    };

    internal static Dictionary<JobRole, string> JobRoleName = new Dictionary<JobRole, string>() {
        { JobRole.TANK, "Tank" },
        { JobRole.HEALER, "Healer" },
        { JobRole.DPS, "DPS" },
    };

    internal static Dictionary<JobSubRole, string> JobSubRoleName = new Dictionary<JobSubRole, string>() {
        { JobSubRole.TANK, "Tank" },
        { JobSubRole.HEALER, "Healer" },
        { JobSubRole.MELEE, "Melee" },
        { JobSubRole.RANGED, "Ranged" },
        { JobSubRole.CASTER, "Caster" },
    };

    public static bool IsAbbreviatedAliasMatch(PlayerAlias abbreviatedPlayer, string fullName) {
        string pattern = "^" + abbreviatedPlayer.FirstName.Replace(".", @"[\w'-]*") + " " + abbreviatedPlayer.LastName.Replace(".", "");
        return Regex.IsMatch(fullName, pattern);
    }

    public static bool IsAbbreviatedAliasMatch(string abbreviatedPlayer, string fullName) {
        return IsAbbreviatedAliasMatch((PlayerAlias)$"{abbreviatedPlayer} whocares", fullName);
    }

    internal static Job? GetJobFromName(string jobName) {
        jobName = jobName.ToLower().Trim();
        foreach(var kvp in AbbreviationNameMap) {
            if(kvp.Value.Equals(jobName, StringComparison.OrdinalIgnoreCase)) {
                return kvp.Key;
            }
        }
        return null;
    }

    internal static string GetNameFromJob(Job job) {
        if(AbbreviationNameMap.ContainsKey(job)) {
            return AbbreviationNameMap[job];
        } else {
            return "";
        }
    }

    internal static JobRole? GetRoleFromJob(Job job) {
        switch(job) {
            case Job.PLD:
            case Job.WAR:
            case Job.DRK:
            case Job.GNB:
                return JobRole.TANK;
            case Job.WHM:
            case Job.SCH:
            case Job.AST:
            case Job.SGE:
                return JobRole.HEALER;
            case Job.MNK:
            case Job.DRG:
            case Job.NIN:
            case Job.SAM:
            case Job.RPR:
            case Job.BRD:
            case Job.MCH:
            case Job.DNC:
            case Job.BLM:
            case Job.SMN:
            case Job.RDM:
                return JobRole.DPS;
            default:
                return null;
        }
    }

    internal static JobSubRole? GetSubRoleFromJob(Job? job) {
        switch(job) {
            case Job.PLD:
            case Job.WAR:
            case Job.DRK:
            case Job.GNB:
                return JobSubRole.TANK;
            case Job.WHM:
            case Job.SCH:
            case Job.AST:
            case Job.SGE:
                return JobSubRole.HEALER;
            case Job.MNK:
            case Job.DRG:
            case Job.NIN:
            case Job.SAM:
            case Job.RPR:
            case Job.VPR:
                return JobSubRole.MELEE;
            case Job.BRD:
            case Job.MCH:
            case Job.DNC:
                return JobSubRole.RANGED;
            case Job.BLM:
            case Job.SMN:
            case Job.RDM:
            case Job.PIC:
                return JobSubRole.CASTER;
            default:
                return null;
        }
    }

    internal static Dictionary<uint, string> WorldNameMap = new();
}
