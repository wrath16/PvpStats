using LiteDB;
using PvpStats.Types.Player;

namespace PvpStats.Types.Match;
public class FrontlinePlayer : PvpPlayer {
    public FrontlineTeamName Team { get; set; }
    public int Alliance { get; set; }

    [BsonIgnore]
    public int TeamAlliance => ((int)Team * 3) + Alliance;

    public FrontlinePlayer() { }

    public FrontlinePlayer(PlayerAlias name, Job? job, FrontlineTeamName team) : base(name, job) {
        Team = team;
    }
}
