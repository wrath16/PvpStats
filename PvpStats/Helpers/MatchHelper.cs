﻿using PvpStats.Types.Match;
using System.Collections.Generic;

namespace PvpStats.Helpers;
public static class MatchHelper {

    public static readonly Dictionary<uint, CrystallineConflictMap> CrystallineConflictMapLookup = new() {
        { 1032, CrystallineConflictMap.Palaistra },
        { 1033, CrystallineConflictMap.VolcanicHeart },
        { 1034, CrystallineConflictMap.CloudNine },
        { 1116, CrystallineConflictMap.ClockworkCastleTown },
        { 1138, CrystallineConflictMap.RedSands }
    };

    public static readonly Dictionary<ArenaTier, string> ArenaRankLookup = new() {
        { ArenaTier.Bronze, "bronze" },
        { ArenaTier.Silver, "silver" },
        { ArenaTier.Gold, "gold" },
        { ArenaTier.Platinum, "platinum" },
        { ArenaTier.Diamond, "diamond" },
        { ArenaTier.Crystal, "crystal" }
    };

    public static CrystallineConflictMatchType GetMatchType(uint dutyId) {
        switch (dutyId) {
            case 835: //palaistra
            case 836: //volcanic heart
            case 837: //cloud 9
            case 912: //clockwork castletown
            case 967: //red sands
                return CrystallineConflictMatchType.Casual;
            case 856: //palaistra
            case 857: //volcanic heart
            case 858: //cloud 9
            case 918: //clockwork castletown
            case 972: //red sands
                return CrystallineConflictMatchType.Ranked;
            case 862: //palaistra
            case 863: //volcanic heart
            case 864: //cloud 9
            case 923: //clockwork castletown
            case 978: //red sands
                return CrystallineConflictMatchType.Custom;
            default:
                return CrystallineConflictMatchType.Unknown;
        }
    }
    public static bool IsCrystallineConflictTerritory(uint territoryId) {
        return CrystallineConflictMapLookup.ContainsKey(territoryId);
        //foreach(var map in CrystallineConflictMaps) {
        //    if(map.TerritoryId == territoryId) {
        //        return true;
        //    }
        //}
        //return false;
    }

    public static string GetArenaName(CrystallineConflictMap map) {
        return map switch {
            CrystallineConflictMap.Palaistra => "The Palaistra",
            CrystallineConflictMap.VolcanicHeart => "The Volcanic Heart",
            CrystallineConflictMap.CloudNine => "Cloud Nine",
            CrystallineConflictMap.ClockworkCastleTown => "Clockwork Castletown",
            CrystallineConflictMap.RedSands => "The Red Sands",
            _ => "Unknown",
        };
    }

    public static CrystallineConflictTeamName GetTeamName(string name) {
        name = name.ToLower().Trim();
        switch (name) {
            case "astra": return CrystallineConflictTeamName.Astra;
            case "umbra": return CrystallineConflictTeamName.Umbra;
            default: return CrystallineConflictTeamName.Unknown;
        }
    }

    public static string GetTeamName(CrystallineConflictTeamName team) {
        switch (team) {
            case CrystallineConflictTeamName.Astra: return "Astra";
            case CrystallineConflictTeamName.Umbra: return "Umbra";
            default: return "Unknown Team Name";
        }
    }

    public static ArenaTier GetTier(string name) {
        name = name.ToLower().Trim();
        foreach (var kvp in ArenaRankLookup) {
            if (kvp.Value == name) {
                return kvp.Key;
            }
        }
        return ArenaTier.None;
    }
}