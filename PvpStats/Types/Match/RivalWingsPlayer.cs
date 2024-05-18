using PvpStats.Types.Player;

namespace PvpStats.Types.Match;
public class RivalWingsPlayer : PvpPlayer {
    public RivalWingsTeamName Team { get; set; }
    public int Alliance { get; set; }

    public RivalWingsPlayer() { }

    public RivalWingsPlayer(PlayerAlias name, Job? job, RivalWingsTeamName team) : base(name, job) {
        Team = team;
    }
}
