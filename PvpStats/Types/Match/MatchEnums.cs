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

public enum CrystallineConflictMatchEvent {
    CrystalUnchained,
    OvertimeCommenced,
    MatchEnded,
    SpecialEvent
}

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

public enum RivalWingsMap {
    Astragalos,
    HiddenGorge,
}

public enum RivalWingsTeamName {
    Falcons = 0,
    Ravens = 1,
    Unknown = 2
}

public enum RivalWingsMech {
    Chaser = 0,
    Oppressor = 1,
    Justice = 2,
}

public enum RivalWingsSupplies {
    Gobtank = 0,
    Ceruleum = 1,
    Gobbiejuice = 2,
    Gobcrate = 3
}

public enum RivalWingsStructure {
    Core = 0,
    Tower1 = 1,
    Tower2 = 2
}
