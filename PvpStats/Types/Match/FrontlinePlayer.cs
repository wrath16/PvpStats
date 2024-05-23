using PvpStats.Types.Player;

namespace PvpStats.Types.Match;
public class FrontlinePlayer : PvpPlayer {
    public FrontlineTeamName Team { get; set; }
    public int Alliance { get; set; }

    public FrontlinePlayer() { }

    public FrontlinePlayer(PlayerAlias name, Job? job, FrontlineTeamName team) : base(name, job) {
        Team = team;
    }
}
