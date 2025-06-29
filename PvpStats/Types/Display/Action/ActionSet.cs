using PvpStats.Types.Match.Timeline;
using System.Collections.Generic;

namespace PvpStats.Types.Display.Action;
internal class ActionSet {

    public static readonly List<ActionSet> Sets = [
        //PLD
        new("Royal Authority Combo", 9123, new() { { 29058, new(true) }, { 29059, new(true) }, { 29060, new(true) }, }),
        new("Sepulchre Combo", 9761, new() { { 29061, new(true) }, { 41428, new(true) }, { 41429, new(true) }, }),
        new("Blade of Valor Combo", 9589, new() { { 29071, new(true) }, { 29072, new(true) }, { 29073, new(true) }, }),
        //WAR
        new("Storm's Path Combo", 9135, new() { { 29074, new(true) }, { 29075, new(true) }, { 29076, new(true) }, }),
        //DRK
        new("Souleater Combo", 9146, new() { { 29085, new(true) }, { 29086, new(true) }, { 29087, new(true) }, }),
        new("Shadowbringer", 9594, new() { { 29091, new(true) }, { 29738, new(true) }, }),
        new("Torcleaver Combo", 9768, new() { { 41434, new(true) }, { 41435, new(true) }, { 41436, new(true) }, }),
        new("Salt and Darkness", 9596, new() { { 29095, new(false) }, { 29096, new(true) }, }),
        //GNB
        new("Burst Strike Combo", 9357, new() { { 29098, new(true) }, { 29099, new(true) }, { 29100, new(true) }, { 29101, new(true) }, }),
        new("Wicked Talon Combo", 9366, new() { { 29102, new(true) }, { 29103, new(true) }, { 29104, new(true) }, }),
        new("Continuation", 9361, new() { { 29107, new(true) }, { 29108, new(true) }, { 29109, new(true) }, { 29110, new(true) }, }),
        new("Relentless Rush", 9603, new() { { 29130, new(true, false) }, { 29557, new(false, true) }, }),
        new("Terminal Trigger", 9604, new() { { 29131, new(true) }, { 29469, new(false) }, }),
        //WHM
        //SCH
        //AST
        new("Fall Malefic", 9616, new() { { 29242, new(true) }, { 29246, new(true) }, }),
        new("Aspected Benefic", 9420, new() { { 29243, new(true) }, { 29247, new(true) }, }),
        new("Gravity II", 9617, new() { { 29244, new(true) }, { 29248, new(true) }, }),
        //SGE
        new("Pneuma", 9575, new() { { 29260, new(false, true) }, { 29706, new(true) }, }),
        //MNK
        new("Pouncing Coeurl Combo", 9778, new() { { 29475, new(true) }, { 29476, new(true) }, { 29477, new(true) }, { 41444, new(true) }, { 41445, new(true) }, { 41446, new(true) }, }),
        new("Meteodrive", 9646, new() { { 29485, new(true) }, { CrystallineConflictMatchTimeline.StatusIdOffset + 3175, new(false) }, }),
        //DRG
        new("Drakesbane Combo", 9782, new() { { 29486, new(true) }, { 29487, new(true) }, { 29488, new(true) }, { 41449, new(true) }, }),
        new("Sky Shatter", 9653, new() { { 29498, new(true) }, { 29499, new(false) }, }),
        //NIN
        new("Aeolian Edge Combo", 9182, new() { { 29500, new(true) }, { 29501, new(true) }, { 29502, new(true) }, { 29517, new(false) }, { 29518, new(false) }, { 29519, new(false) }, }),
        new("Fuma Shuriken", 9654, new() { { 29505, new(true) }, { 29521, new(false) }, }),
        new("Assassinate", 9187, new() { { 29503, new(true) }, { 29520, new(false) }, }),
        new("Zesho Meppo", 9784, new() { { 41452, new(true) }, { 41453, new(false) }, }),
        new("Fleeting Raiju Combo", 9693, new() { { 29510, new(true) }, { 29707, new(true) }, { 29522, new(false) }, { 29708, new(false) }, }),
        new("Seiton Tenchu", 9661, new() { { 29515, new(true) }, { 29516, new(true) }, }),
        //SAM
        new("Kasha Combo", 9199, new() { { 29523, new(true) }, { 29524, new(true) }, { 29525, new(true) }, }),
        new("Kaiten", 9495, new() { { 29526, new(true) }, { 29527, new(true) }, { 29528, new(true) }, }),
        new("Setsugekka", 9786, new() { { 41454, new(true) }, { 41455, new(true) }, }),
        new("Namikiri", 9663, new() { { 29530, new(true) }, { 29531, new(true) }, }),
        //RPR
        new("Infernal Slice Combo", 9542, new() { { 29538, new(true) }, { 29539, new(true) }, { 29540, new(true) }, }),
        new("Cross Reaping Combo", 9546, new() { { 29543, new(true) }, { 29544, new(true) }, }),
        new("Death Warrant", 9669, new() { { 29549, new(true) }, { 29603, new(false) }, { 41457, new(false) }, }),
        //VPR
        new("Ravenous Bite Combo", 9704, new() { { 39157, new(true) }, { 39159, new(true) }, { 39161, new(true) }, { 39158, new(true) }, { 39160, new(true) }, { 39163, new(true) }, }),
        new("Sanguine Feast Combo", 9708, new() { { 39166, new(true) }, { 39167, new(true) } }),
        new("Fourth Generation Combo", 9712, new() { { 39169, new(true) }, { 39170, new(true) }, { 39171, new(true) }, { 39172, new(true) } }),
        new("Serpent's Tail", 9726, new() { { 39174, new(true) }, { 39175, new(true) }, { 39176, new(true) }, }),
        new("Uncoiled Serpent's Tail", 9723, new() { { 39177, new(true) }, { 39178, new(true) } }),
        new("Serpent's Legacy", 9721, new() { { 39179, new(true) }, { 39180, new(true) }, { 39181, new(true) }, { 39182, new(true) } }),
        new("Backlash", 9728, new() { { 39186, new(true) }, { 39187, new(true) }, { 39188, new(true) } }),
        //BRD
        new("Harmonic Arrow", 9792, new() { { 41464, new(true) }, { 41465, new(true) }, { 41466, new(true) }, { 41964, new(true) } }),
        //MCH
        new("Wildfire", 9231, new() { { 29409, new(true, false) }, { 29410, new(false, true) }, { 29411, new(false, true) }, { 41470, new(false) } }),
        //DNC
        new("Honing Ovation", 9639, new() { { 29423, new(true) }, { 29424, new(true) }, { 29425, new(true) }, { 29426, new(true) }, { 29427, new(true) } }),
        new("Fountainfall Combo", 9445, new() { { 29416, new(true) }, { 29417, new(true) }, { 29418, new(true) }, { 29419, new(true) }, }),
        new("Honing Dance", 9638, new() { { 29422, new(true, false) }, { 29558, new(false, true) } }),
        new("Saber Dance", 9457, new() { { 29420, new(true) }, { CrystallineConflictMatchTimeline.StatusIdOffset + 2022, new(false) }, }),
        new("Starfall Dance", 9637, new() { { 29421, new(true) }, { CrystallineConflictMatchTimeline.StatusIdOffset + 3161, new(false) }, }),
        new("Fan Dance", 9640, new() { { 29428, new(true) }, { CrystallineConflictMatchTimeline.StatusIdOffset + 2052, new(false) }, }),
        new("Dance of the Dawn", 9798, new() { { 41472, new(true) }, { CrystallineConflictMatchTimeline.StatusIdOffset + 4314, new(false) }, }),
        //BLM
        new("Fire IV Combo", 9239, new() { { 29649, new(true) }, { 30896, new(true) }, { 29650, new(true) }, }),
        new("Blizzard IV Combo", 9240, new() { { 29653, new(true) }, { 30897, new(true) }, { 29654, new(true) }, }),
        //SMN
        new("Primal's Ruin", 9675, new() { { 29665, new(true) }, { 29666, new(true) }, }),
        //RDM
        new("Enchanted Redoublement Combo", 9265, new() { { 41488, new(true) }, { 41489, new(true) }, { 41490, new(true) }, }),
        //PCT
        new("Water in Blue Combo", 9734, new() { { 39191, new(true) }, { 39192, new(true) }, { 39193, new(true) }, }),
        new("Thunder in Magenta Combo", 9737, new() { { 39194, new(true) }, { 39195, new(true) }, { 39196, new(true) }, }),
        new("Creature Motif", 9754, new() { { 39200, new(true) }, { 39201, new(true) }, { 39202, new(true) }, { 39203, new(true) }, }),
        new("Living Muse", 9755, new() { { 39205, new(true) }, { 39206, new(true) }, { 39207, new(true) }, { 39208, new(true) }, }),
        new("Living Portrait", 9759, new() { { 39782, new(true) }, { 39783, new(true) }, }),
        new("Star Prism", 9748, new() { { 39216, new(true) }, { 39217, new(false, true) }, }),
        ];

    public uint IconId { get; set; }
    public string Name { get; set; }
    public Dictionary<uint, ActionSetParams> Actions { get; set; }

    public ActionSet(string name, uint iconId, Dictionary<uint, ActionSetParams> actions) {
        Name = name;
        IconId = iconId;
        Actions = actions;
    }
}

internal class ActionSetParams {
    public bool IncludeCasts {  get; set; }
    public bool IncludeTargets { get; set; }

    public ActionSetParams(bool includeCasts, bool? includeTargets = null) {
        IncludeCasts = includeCasts;
        if(includeTargets == null) {
            IncludeTargets = includeCasts;
        } else {
            IncludeTargets = includeTargets.Value;
        }
    }
}
