using LiteDB;
using PvpStats.Types.Display;
using PvpStats.Types.Player;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Types.Match;
public class FrontlineMatch : PvpMatch {

    public FrontlineMap? Arena { get; set; }
    public int PlayerCount { get; set; }

    public List<FrontlinePlayer> Players { get; set; } = new();

    //have to use string so it can be deserialized correctly -_-
    public Dictionary<string, FrontlineScoreboard> PlayerScoreboards { get; set; } = new();
    public Dictionary<FrontlineTeamName, FrontlineTeamScoreboard> Teams { get; set; } = new();

    public Dictionary<string, int>? MaxBattleHigh { get; set; }

    [BsonIgnore]
    public FrontlinePlayer? LocalPlayerTeamMember => Players.FirstOrDefault(x => x.Name.Equals(LocalPlayer));

    [BsonIgnore]
    public FrontlineTeamName? LocalPlayerTeam => Players.FirstOrDefault(x => x.Name.Equals(LocalPlayer))?.Team;
    [BsonIgnore]
    public int? Result => LocalPlayerTeam != null ? Teams[(FrontlineTeamName)LocalPlayerTeam].Placement : null;

    public FrontlineMatch() : base() {
        Version = 0;
    }

    public Dictionary<FrontlineTeamName, FrontlineScoreboard> GetTeamScoreboards() {
        Dictionary<FrontlineTeamName, FrontlineScoreboard> scoreboards = [];
        foreach(var team in Teams) {
            scoreboards.Add(team.Key, new());
        }
        foreach(var player in Players) {
            var team = player.Team;
            var scoreboard = PlayerScoreboards[player.Name];
            scoreboards[team] += scoreboard;
            scoreboards[team].Size++;
        }
        return scoreboards;
    }

    public Dictionary<PlayerAlias, FLScoreboardDouble> GetPlayerContributions() {
        var teamScoreboards = GetTeamScoreboards();
        Dictionary<PlayerAlias, FLScoreboardDouble> contributions = [];
        foreach(var player in Players) {
            var scoreboard = PlayerScoreboards[player.Name];
            var team = player.Team;
            contributions.Add(player.Name, new(new FLScoreboardTally(scoreboard), new FLScoreboardTally(teamScoreboards[team])));
        }
        return contributions;
    }
}
