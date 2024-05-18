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
    public List<RivalWingsPlayer> Players { get; set; } = new();

    //have to use string so it can be deserialized correctly -_-
    public Dictionary<string, RivalWingsPlayer> PlayerScoreboards { get; set; } = new();
}
