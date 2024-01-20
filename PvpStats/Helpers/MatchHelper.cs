using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            case 853: //palaistra
            case 856: //palaistra
            case 854: //volcanid heart
            case 857: //volcanic heart
            case 855: //cloud 9
            case 858: //cloud 9
            case 917: //clockwork castletown
            case 918: //clockwork castletown
            case 970: //red sands
            case 972: //red sands
            case 973: //red sands
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
        if (Regex.IsMatch(name, @"\bastra\b", RegexOptions.IgnoreCase)) {
            return CrystallineConflictTeamName.Astra;
        }
        else if (Regex.IsMatch(name, @"\bumbra\b", RegexOptions.IgnoreCase)) {
            return CrystallineConflictTeamName.Umbra;
        }
        return CrystallineConflictTeamName.Unknown;
    }

    public static string GetTeamName(CrystallineConflictTeamName team) {
        switch (team) {
            case CrystallineConflictTeamName.Astra: return "Astra";
            case CrystallineConflictTeamName.Umbra: return "Umbra";
            default: return "Unknown";
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

    public static float? ConvertProgressStringToFloat(string progress) {
        if (float.TryParse(progress.Replace("%", "").Replace(",", "."), out float parseResult)) {
            return parseResult;
        }
        else {
            return null;
        }
    }

    //public static readonly Dictionary<ClientLanguage, Regex> CrystalCreditBeforeRegex = new() {
    //        { ClientLanguage.English, new Regex(@"(?<=Crystal Credit\\n)[\d]*", RegexOptions.IgnoreCase) },
    //        { ClientLanguage.French, new Regex(@"", RegexOptions.IgnoreCase) },
    //        { ClientLanguage.German, new Regex(@"", RegexOptions.IgnoreCase) },
    //        { ClientLanguage.Japanese, new Regex(@"", RegexOptions.IgnoreCase) }
    //};

    //public static readonly Dictionary<ClientLanguage, Regex> CrystalCreditAfterRegex = new() {
    //        { ClientLanguage.English, new Regex(@"\b[\d]*$", RegexOptions.IgnoreCase) },
    //        { ClientLanguage.French, new Regex(@"", RegexOptions.IgnoreCase) },
    //        { ClientLanguage.German, new Regex(@"", RegexOptions.IgnoreCase) },
    //        { ClientLanguage.Japanese, new Regex(@"", RegexOptions.IgnoreCase) }
    //};

    public static readonly Regex CreditBeforeRegex = new Regex(@"^\d+(?= →)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    public static readonly Regex StarBeforeRegex = new Regex(@"(?<=^\w*\s*\d*\s*)★*(?=☆*\s*→)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    public static readonly Regex RiserBeforeRegex = new Regex(@"(?<=^\w*\s?)\d*(?=\s?(★|☆)* →)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    public static readonly Regex TierBeforeRegex = new Regex(@"^[^\d\s]+(?=\s?\d*\s?(★|☆)*\s?→)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    public static readonly Regex CreditAfterRegex = new Regex(@"(?<=→ )\d+", RegexOptions.IgnoreCase);
    public static readonly Regex StarAfterRegex = new Regex(@"★*(?=☆*$)", RegexOptions.IgnoreCase);
    public static readonly Regex RiserAfterRegex = new Regex(@"\d*(?=\s?(★|☆)*$)", RegexOptions.IgnoreCase);
    public static readonly Regex TierAfterRegex = new Regex(@"(?<=→\s)[^\d\s]+", RegexOptions.IgnoreCase);
}
