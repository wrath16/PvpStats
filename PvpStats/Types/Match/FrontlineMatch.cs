using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Types.Match;
internal class FrontlineMatch : PvpMatch {

    public FrontlineMap? Arena { get; set; }
    public int PlayerCount { get; set; }

    public List<FrontlinePlayer> Players { get; set; } = new();

    //have to use string so it can be deserialized correctly -_-
    public Dictionary<string, FrontlineScoreboard> PlayerScoreboards { get; set; } = new();
    public Dictionary<FrontlineTeamName, FrontlineTeamScoreboard> Teams { get; set; } = new();

    [BsonIgnore]
    public FrontlinePlayer? LocalPlayerTeamMember => Players.FirstOrDefault(x => x.Name.Equals(LocalPlayer));

    [BsonIgnore]
    public FrontlineTeamName? LocalPlayerTeam => Players.First(x => x.Name.Equals(LocalPlayer)).Team;
    [BsonIgnore]
    public int? Result => LocalPlayerTeam != null ? Teams[(FrontlineTeamName)LocalPlayerTeam].Placement : null;

    public FrontlineMatch() : base() {
        Version = 0;
    }
}
