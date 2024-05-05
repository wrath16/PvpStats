using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Types.Match;
internal class FrontlineMatch : PvpMatch {

    FrontlineMap? Arena {  get; set; }

    public Dictionary<FrontlinePlayer, FrontlineScoreboard> Players { get; set; } = new();
    public Dictionary<FrontlineTeamName, FrontlineTeamStats> Teams { get; set; } = new();

    [BsonIgnore]
    public FrontlineTeamName? LocalPlayerTeam => Players.First(x => x.Key.Name.Equals(LocalPlayer)).Key.Team;
    [BsonIgnore]
    public int? Result => LocalPlayerTeam != null ? Teams[(FrontlineTeamName)LocalPlayerTeam].Placement : null;

    public FrontlineMatch() : base() {
        Version = 0;
    }
}
