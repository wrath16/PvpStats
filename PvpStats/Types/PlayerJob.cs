using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types {

    public enum Job {
        PLD,
        WAR,
        DRK,
        GNB,
        WHM,
        SCH,
        AST,
        SGE,
        MNK,
        DRG,
        NIN,
        SAM,
        RPR,
        BRD,
        MCH,
        DNC,
        BLM,
        SMN,
        RDM
    }

    public enum JobRole {
        TANK,
        HEALER,
        DPS
    }
    internal static class PlayerJob {

        private static Dictionary<Job, string> AbbreviationNameMap = new Dictionary<Job, string>() {
            { Job.PLD, "Paladin" },
            { Job.WAR, "Warrior" },
            { Job.DRK, "Dark Knight" },
            { Job.GNB, "Gunbreaker" },
            { Job.MNK, "Monk" },
            { Job.DRG, "Dragoon" },
            { Job.NIN, "Ninja" },
            { Job.SAM, "Samurai" },
            { Job.RPR, "Reaper" },
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
        };

        static Job? GetJobFromName(string jobName) {
            jobName = jobName.ToLower();
            foreach(var kvp in AbbreviationNameMap) {
                if(kvp.Value.Equals(jobName, StringComparison.OrdinalIgnoreCase)) {
                    return kvp.Key;
                }
            }
            return null;
        }

        static string GetNameFromJob(Job job) {
            if(AbbreviationNameMap.ContainsKey(job)) {
                return AbbreviationNameMap[job];
            } else {
                return "";
            }
        }

        private static JobRole? GetRoleFromJob(Job job) {
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

        //public string Name { get; set; } = "";
        //public Job Abbreviation { get; set; }
        //public JobRole Role { get; set; }

        //public PlayerJob() {

        //}

        
    }

}
