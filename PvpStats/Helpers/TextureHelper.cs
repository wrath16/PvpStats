using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System.Collections.Generic;
using System.Numerics;

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
        { Job.VPR, 62141 },
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
        { Job.PCT, 62142 },
    };

    internal static uint TeamIcon => 60563;
    internal static uint EnemyIcon => 60562;
    internal static string CCTeamIconTexture => "ui/uld/PVPMKSResult_hr1.tex";
    internal static string RWTeamIconTexture => "ui/uld/PVPSimulationResult_hr1.tex";
    internal static string RWSuppliesTexture => "ui/uld/PVPSimulationHeader2_hr1.tex";
    internal static uint TrainIcon => 60980;

    internal static Dictionary<RivalWingsTeamName, uint> GoblinMercIcons => new() {
        { RivalWingsTeamName.Falcons, 60974 },
        { RivalWingsTeamName.Ravens, 60975 },
        { RivalWingsTeamName.Unknown, 60976 },
    };

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
            return (level - 1) + 219181;
        } else if(level == 20) {
            return 214845;
        }
        return null;
    }

    internal static uint? GetBattleHighIcon(uint level) {
        if(level > 0 && level <= 5) {
            return (level - 1) + 61483;
        }
        return null;
    }

    internal static (Vector2 UV0, Vector2 UV1) GetSuppliesUVs(RivalWingsSupplies supplies) {
        Vector2 uv0, uv1;
        switch(supplies) {
            case RivalWingsSupplies.Gobtank:
                uv0 = new Vector2(0);
                uv1 = new Vector2(0.2f, 1 / 3f);
                break;
            case RivalWingsSupplies.Ceruleum:
                uv0 = new Vector2(0.2f, 0);
                uv1 = new Vector2(0.4f, 1 / 3f);
                break;
            case RivalWingsSupplies.Gobbiejuice:
                uv0 = new Vector2(0.4f, 0);
                uv1 = new Vector2(0.6f, 1 / 3f);
                break;
            case RivalWingsSupplies.Gobcrate:
                uv0 = new Vector2(0.6f, 0);
                uv1 = new Vector2(0.8f, 1 / 3f);
                break;
            default:
                uv0 = new Vector2(0.8f, 0);
                uv1 = new Vector2(0.1f, 1 / 3f);
                break;
        };
        return (uv0, uv1);
    }
}
