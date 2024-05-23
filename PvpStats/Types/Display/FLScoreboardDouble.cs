using PvpStats.Types.Match;

namespace PvpStats.Types.Display;
internal class FLScoreboardDouble : PvpScoreboardDouble {

    public double Occupations { get; set; }
    public double DamageToOther { get; set; }
    public double DamageToPCs { get; set; }
    public double Special1 { get; set; }

    public FLScoreboardDouble(PvpScoreboard playerScoreboard, PvpScoreboard teamScoreboard) : base(playerScoreboard, teamScoreboard) {
        if(playerScoreboard is FrontlineScoreboard && teamScoreboard is FrontlineScoreboard) {
            var playerFLScoreboard = playerScoreboard as FrontlineScoreboard;
            var teamFLScoreboard = teamScoreboard as FrontlineScoreboard;

            Occupations = playerFLScoreboard!.Occupations != 0 ? (double)playerFLScoreboard.Occupations / teamFLScoreboard!.Occupations : 0;
            DamageToOther = playerFLScoreboard!.DamageToOther != 0 ? (double)playerFLScoreboard.DamageToOther / teamFLScoreboard!.DamageToOther : 0;
            DamageToPCs = playerFLScoreboard!.DamageToPCs != 0 ? (double)playerFLScoreboard.DamageToPCs / teamFLScoreboard!.DamageToPCs : 0;
            Special1 = playerFLScoreboard!.Special1 != 0 ? (double)playerFLScoreboard.Special1 / teamFLScoreboard!.Special1 : 0;
        }
    }
}
