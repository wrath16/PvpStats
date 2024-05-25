using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Types.Match;
public class RivalWingsMatch : PvpMatch {

    public RivalWingsMap? Arena { get; set; }
    public RivalWingsTeamName? MatchWinner { get; set; }
    public int PlayerCount { get; set; }
    public List<RivalWingsPlayer> Players { get; set; } = [];

    //have to use string so it can be deserialized correctly -_-
    public Dictionary<string, RivalWingsScoreboard>? PlayerScoreboards { get; set; } = [];
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsStructure, int>>? StructureHealth { get; set; } = [];
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsMech, double>>? TeamMechTime { get; set; } = [];
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsSupplies, int>>? Supplies { get; set; } = [];
    public Dictionary<RivalWingsTeamName, int>? Mercs { get; set; } = [];
    public Dictionary<string, Dictionary<RivalWingsMech, double>>? PlayerMechTime { get; set; } = [];
    public Dictionary<int, RivalWingsAllianceScoreboard>? AllianceStats { get; set; } = [];

    [BsonIgnore]
    public RivalWingsTeamName? LocalPlayerTeam => Players.FirstOrDefault(x => x.Name.Equals(LocalPlayer))?.Team;
    [BsonIgnore]
    public RivalWingsPlayer? LocalPlayerTeamMember => Players.FirstOrDefault(x => x.Name.Equals(LocalPlayer));
    [BsonIgnore]
    public bool IsWin => MatchWinner == LocalPlayerTeam!;
    [BsonIgnore]
    public bool IsLoss => !IsWin && MatchWinner != null;

    public RivalWingsMatch() : base() {
        Version = 0;
    }
}
