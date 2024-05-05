using PvpStats.Types.Player;

namespace PvpStats.Types.Match;
internal class PvpPlayer {
    public PlayerAlias Name { get; set; }
    public Job? Job { get; set; }

    public PvpPlayer(PlayerAlias name, Job? job) {
        Name = name;
        Job = job;
    }

    public override int GetHashCode() {
        return Name.GetHashCode();
    }
}
