using LiteDB;
using PvpStats.Types.Display;
using PvpStats.Types.Player;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Types.Match;
public class RivalWingsMatch : PvpMatch {

    public RivalWingsMap? Arena { get; set; }
    public RivalWingsTeamName? MatchWinner { get; set; }
    public int PlayerCount { get; set; }
    public List<RivalWingsPlayer>? Players { get; set; }

    //have to use string so it can be deserialized correctly -_-
    public Dictionary<string, RivalWingsScoreboard>? PlayerScoreboards { get; set; }
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsStructure, int>>? StructureHealth { get; set; }
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsMech, double>>? TeamMechTime { get; set; }
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsSupplies, int>>? Supplies { get; set; }
    public Dictionary<RivalWingsTeamName, int>? Mercs { get; set; }
    public Dictionary<string, Dictionary<RivalWingsMech, double>>? PlayerMechTime { get; set; }
    public Dictionary<int, RivalWingsAllianceScoreboard>? AllianceStats { get; set; }

    [BsonIgnore]
    public RivalWingsTeamName? LocalPlayerTeam => Players?.FirstOrDefault(x => x.Name.Equals(LocalPlayer))?.Team;
    [BsonIgnore]
    public RivalWingsPlayer? LocalPlayerTeamMember => Players?.FirstOrDefault(x => x.Name.Equals(LocalPlayer));
    [BsonIgnore]
    public bool IsWin => MatchWinner == LocalPlayerTeam!;
    [BsonIgnore]
    public bool IsLoss => !IsWin && MatchWinner != null;

    public RivalWingsMatch() : base() {
        Version = 0;
    }

    public Dictionary<RivalWingsTeamName, RivalWingsScoreboard>? GetTeamScoreboards() {
        Dictionary<RivalWingsTeamName, RivalWingsScoreboard> scoreboards = [];
        if(StructureHealth is null || Players is null) {
            return null;
        }
        foreach(var team in StructureHealth.Keys) {
            scoreboards.Add(team, new());
        }
        foreach(var player in Players) {
            var team = player.Team;
            var scoreboard = PlayerScoreboards[player.Name];
            scoreboards[team] += scoreboard;
            scoreboards[team].Size++;
        }
        return scoreboards;
    }

    public Dictionary<PlayerAlias, RWScoreboardDouble>? GetPlayerContributions() {
        var teamScoreboards = GetTeamScoreboards();
        if(teamScoreboards is null || Players is null || PlayerScoreboards is null) {
            return null;
        }
        Dictionary<PlayerAlias, RWScoreboardDouble> contributions = [];
        foreach(var player in Players) {
            var scoreboard = PlayerScoreboards[player.Name];
            var team = player.Team;
            contributions.Add(player.Name, new(scoreboard, teamScoreboards[team]));
        }
        return contributions;
    }
}
