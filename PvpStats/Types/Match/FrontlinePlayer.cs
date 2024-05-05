using PvpStats.Types.Player;

namespace PvpStats.Types.Match;
internal class FrontlinePlayer : PvpPlayer {
    public FrontlineTeamName Team { get; set; }
    public int Alliance { get; set; }

    public FrontlinePlayer(PlayerAlias name, Job? job, FrontlineTeamName team) : base(name, job) {
        Team = team;
    }
}
