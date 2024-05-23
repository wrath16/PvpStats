using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.Match;
public class RivalWingsMatch : PvpMatch {

    public RivalWingsMap? Arena { get; set; }
    public RivalWingsTeamName? MatchWinner { get; set; }
    public int PlayerCount { get; set; }
    public List<RivalWingsPlayer> Players { get; set; } = [];

    //have to use string so it can be deserialized correctly -_-
    public Dictionary<string, RivalWingsScoreboard>? PlayerScoreboards { get; set; } = [];
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsStructure, int>>? StructureHealth { get; set; } = [];
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsMech, float>>? TeamMechTime { get; set; } = [];
    public Dictionary<string, Dictionary<RivalWingsMech, float>>? PlayerMechTime { get; set; } = [];
    public Dictionary<int, RivalWingsAllianceScoreboard>? AllianceStats { get; set; } = [];
}
