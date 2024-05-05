namespace PvpStats.Types.Match;

public enum DutyType {
    Unknown,
    CrystallineConflict,
    Frontline,
    RivalWings
}

public enum CrystallineConflictTeamName {
    Unknown,
    Astra,
    Umbra
}

public enum CrystallineConflictMatchType {
    Unknown,
    Casual,
    Ranked,
    Custom
}

public enum CrystallineConflictMap {
    Palaistra,
    VolcanicHeart,
    CloudNine,
    ClockworkCastleTown,
    RedSands
}

//this matches the data sheet row IDs for ColosseumMatchRank
public enum ArenaTier {
    None = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4,
    Diamond = 5,
    Crystal = 6
}

//public enum MatchResult {
//    Win,
//    Loss,
//    Draw,
//    Unknown,
//}

public enum FrontlineMap {
    BorderlandRuins,
    SealRock,
    FieldsOfGlory,
    OnsalHakair,
}

public enum FrontlineTeamName {
    Maelstrom,
    Adders,
    Flames
}
