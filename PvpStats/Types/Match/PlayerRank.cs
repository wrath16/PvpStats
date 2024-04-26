using LiteDB;
using PvpStats.Helpers;
using System;
using System.Collections.Generic;

namespace PvpStats.Types.Match;

public class PlayerRank : IComparable<PlayerRank> {
    public static readonly Dictionary<ArenaTier, int> RisersPerTier = new() {
        { ArenaTier.None, 1 },
        { ArenaTier.Bronze, 3 },
        { ArenaTier.Silver, 3 },
        { ArenaTier.Gold, 4 },
        { ArenaTier.Platinum, 4 },
        { ArenaTier.Diamond, 5 },
        { ArenaTier.Crystal, 0 }
    };
    public static readonly int StarsPerRiser = 3;

    public static PlayerRank MaxRank => new() { Tier = ArenaTier.Crystal };
    public static PlayerRank MinRank => new() { Tier = ArenaTier.None, Riser = 1, Stars = 0 };

    public ArenaTier Tier { get; set; }
    public int? Riser { get; set; }
    public int? Stars { get; set; }
    public int? Credit { get; set; }

    [BsonIgnore]
    public int TotalStars {
        get {
            int starCount = 0;
            //add from previous tiers
            for(int i = (int)Tier - 1; i >= 0; i--) {
                starCount += RisersPerTier[(ArenaTier)i] * StarsPerRiser;
            }
            //add from previous risers
            if(Riser is not null) {
                starCount += (RisersPerTier[Tier] - (int)Riser) * StarsPerRiser;
            }
            starCount += Stars ?? 0;
            return starCount;
        }
    }

    [BsonIgnore]
    public int TotalCredit {
        get {
            if(Tier == ArenaTier.Crystal && Credit != null) {
                return (int)Credit;
            }
            return (MaxRank.TotalStars - TotalStars) * -100;
        }
    }

    public static explicit operator PlayerRank(string s) {
        var splitString = s.Trim().Split(" ");
        if(splitString is null || splitString.Length <= 0 || splitString.Length > 2) {
            throw new ArgumentException("Cannot convert string to player rank!");
        }

        ArenaTier tier = MatchHelper.GetTier(splitString[0]);
        int? riser = null;
        if(splitString.Length == 2) {
            if(!int.TryParse(splitString[1], out int riserParsed)) {
                throw new ArgumentException("Cannot convert riser to integer");
            }
            riser = riserParsed;
        }

        return new PlayerRank() {
            Tier = tier,
            Riser = riser
        };
    }

    public int CompareTo(PlayerRank? other) {
        return TotalCredit - (other ?? MinRank).TotalCredit;
    }

    public static bool operator >=(PlayerRank a, PlayerRank b) {
        return a.TotalCredit >= b.TotalCredit;
    }

    public static bool operator <=(PlayerRank a, PlayerRank b) {
        return a.TotalCredit <= b.TotalCredit;
    }

    public static bool operator >=(PlayerRank a, ArenaTier b) {
        return a.Tier >= b;
    }

    public static bool operator <=(PlayerRank a, ArenaTier b) {
        return a.Tier <= b;
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
        string rank = "";
        if(Tier != ArenaTier.None) {
            rank += $"{Tier}";
        }

        if(Tier == ArenaTier.Crystal) {
            if(Credit != null) {
                rank += $"   {Credit}";
            }
        } else {
            if(Riser != null) {
                rank += $" {Riser}   ";
            }
            if(Stars != null) {
                for(int i = 0; i < Stars; i++) {
                    rank += "★";
                }
                for(int i = 0; i < 3 - Stars; i++) {
                    rank += "☆";
                }
            }
        }
        return rank.Trim();
    }
}
