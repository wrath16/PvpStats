using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System.Collections.Generic;

namespace PvpStats.Helpers;
internal static class TextureHelper {

    internal static Dictionary<Job, uint> JobIcons => new() {
        { Job.PLD, 62119 },
        { Job.WAR, 62121 },
        { Job.DRK, 62132 },
        { Job.GNB, 62137 },
        { Job.MNK, 62120 },
        { Job.DRG, 62122 },
        { Job.NIN, 62130 },
        { Job.SAM, 62134 },
        { Job.RPR, 62139 },
        { Job.WHM, 62124 },
        { Job.SCH, 62128 },
        { Job.AST, 62133 },
        { Job.SGE, 62140 },
        { Job.BRD, 62123 },
        { Job.MCH, 62131 },
        { Job.DNC, 62138 },
        { Job.BLM, 62125 },
        { Job.SMN, 62127 },
        { Job.RDM, 62135 },
    };

    internal static uint GoblinMercIcon => 60976;
    internal static string RWTeamIconTexture => "ui/uld/PVPSimulationResult_hr1.tex";
    internal static string RWSuppliesTexture => "ui/uld/PVPSimulationHeader2_hr1.tex";
    internal static uint TrainIcon => 60980;

    internal static Dictionary<RivalWingsTeamName, uint> CoreIcons => new() {
        { RivalWingsTeamName.Falcons, 60947 },
        { RivalWingsTeamName.Ravens, 60948 },
    };
    internal static Dictionary<RivalWingsTeamName, uint> Tower1Icons => new() {
        { RivalWingsTeamName.Falcons, 60945 },
        { RivalWingsTeamName.Ravens, 60946 },
    };
    internal static Dictionary<RivalWingsTeamName, uint> Tower2Icons => new() {
        { RivalWingsTeamName.Falcons, 60956 },
        { RivalWingsTeamName.Ravens, 60957 },
    };
    internal static Dictionary<RivalWingsTeamName, uint> ChaserIcons => new() {
        { RivalWingsTeamName.Falcons, 60939 },
        { RivalWingsTeamName.Ravens, 60942 },
        { RivalWingsTeamName.Unknown, 60666 },
    };
    internal static Dictionary<RivalWingsTeamName, uint> OppressorIcons => new() {
        { RivalWingsTeamName.Falcons, 60940 },
        { RivalWingsTeamName.Ravens, 60943 },
        { RivalWingsTeamName.Unknown, 60667 },
    };
    internal static Dictionary<RivalWingsTeamName, uint> JusticeIcons => new() {
        { RivalWingsTeamName.Falcons, 60941 },
        { RivalWingsTeamName.Ravens, 60944 },
        { RivalWingsTeamName.Unknown, 60668 },
    };
    internal static Dictionary<FrontlineTeamName, uint> FrontlineTeamIcons => new() {
        { FrontlineTeamName.Maelstrom, 61526 },
        { FrontlineTeamName.Adders, 61527 },
        { FrontlineTeamName.Flames, 61528 },
    };

    internal static uint? GetSoaringIcon(uint level) {
        if(level > 0 && level <= 19) {
            return (level - 1) + 19181;
        } else if(level == 20) {
            return 14845;
        }
        return null;
    }

    internal static uint? GetBattleHighIcon(uint level) {
        if(level > 0 && level <= 5) {
            return (level - 1) + 61483;
        }
        return null;
    }
}
