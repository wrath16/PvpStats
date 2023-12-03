using System.Collections.Generic;

namespace PvpStats.Types.Match;

public class CrystallineConflictTeam {
    public CrystallineConflictTeamName TeamName { get; set; }
    public List<CrystallineConflictPlayer> Players { get; set; } = new();
    public float Progress { get; set; } = 0;

    //public CrystallineConflictTeam(string teamName) {
    //    TeamName = MatchHelper.GetTeamName(teamName);
    //}
}
