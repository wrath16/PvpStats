using PvpStats.Types.Player;

namespace PvpStats.Types.Match;
public class CrystallineConflictPlayer {
    public PlayerAlias Alias { get; set; }
    public Job? Job { get; set; }
    //this is a fallback
    public uint? ClassJobId { get; set; }
    public PlayerRank? Rank { get; set; }
    public CrystallineConflictTeamName? Team { get; set; }
    public ulong? LodestoneId { get; set; }

    public CrystallineConflictPlayer() {
        //should never persist
        Alias = (PlayerAlias)"Sarah Montcroix Siren";
    }

    public CrystallineConflictPlayer(PlayerAlias alias, Job job, PlayerRank? rank = null) {
        Alias = alias;
        Job = job;
        Rank = rank;
    }
}
