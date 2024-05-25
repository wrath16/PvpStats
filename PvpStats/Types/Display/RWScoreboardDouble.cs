using PvpStats.Types.Match;

namespace PvpStats.Types.Display;
public class RWScoreboardDouble : PvpScoreboardDouble {

    public double Ceruleum { get; set; }
    public double DamageToOther { get; set; }
    public double DamageToPCs { get; set; }
    public double Special1 { get; set; }

    public RWScoreboardDouble(PvpScoreboard playerScoreboard, PvpScoreboard teamScoreboard) : base(playerScoreboard, teamScoreboard) {
        if(playerScoreboard is RivalWingsScoreboard && teamScoreboard is RivalWingsScoreboard) {
            var playerRWScoreboard = playerScoreboard as RivalWingsScoreboard;
            var teamRWScoreboard = teamScoreboard as RivalWingsScoreboard;

            Ceruleum = playerRWScoreboard!.Ceruleum != 0 ? (double)playerRWScoreboard.Ceruleum / teamRWScoreboard!.Ceruleum : 0;
            DamageToOther = playerRWScoreboard!.DamageToOther != 0 ? (double)playerRWScoreboard.DamageToOther / teamRWScoreboard!.DamageToOther : 0;
            DamageToPCs = playerRWScoreboard!.DamageToPCs != 0 ? (double)playerRWScoreboard.DamageToPCs / teamRWScoreboard!.DamageToPCs : 0;
            Special1 = playerRWScoreboard!.Special1 != 0 ? (double)playerRWScoreboard.Special1 / teamRWScoreboard!.Special1 : 0;
        }
    }
}
