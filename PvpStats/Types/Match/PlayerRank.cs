using PvpStats.Helpers;
using System;

namespace PvpStats.Types.Match;

public class PlayerRank {

    public ArenaTier Tier { get; set; }
    public int? Riser { get; set; }
    public int? Credit { get; set; }

    public static explicit operator PlayerRank(string s) {
        var splitString = s.Trim().Split(" ");
        if (splitString is null || splitString.Length <= 0 || splitString.Length > 2) {
            throw new ArgumentException("Cannot convert string to player rank!");
        }

        ArenaTier tier = MatchHelper.GetTier(splitString[0]);
        int? riser = null;
        if (splitString.Length == 2) {
            if (!int.TryParse(splitString[1], out int riserParsed)) {
                throw new ArgumentException("Cannot convert riser to integer");
            }
            riser = riserParsed;
        }

        return new PlayerRank() {
            Tier = tier,
            Riser = riser
        };
    }

    //public static implicit operator string(PlayerRank p) {
    //    //if(Riser != null) {
    //    //    return $"{}";
    //    //}

    //}

    public PlayerRank() {

    }

    public PlayerRank(ArenaTier tier, int riser) {
        Tier = tier;
        Riser = riser;
    }

    public override string ToString() {
        if (Riser != null) {
            return $"{Tier} {Riser}";
        }
        return $"{Tier}";
    }
}
