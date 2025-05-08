namespace PvpStats.Types.Player;

public enum Job {
    PLD,
    WAR,
    DRK,
    GNB,
    WHM,
    SCH,
    AST,
    SGE,
    MNK,
    DRG,
    NIN,
    SAM,
    RPR,
    VPR,
    BRD,
    MCH,
    DNC,
    BLM,
    SMN,
    RDM,
    PCT
}

public enum JobRole {
    TANK,
    HEALER,
    DPS
}

public enum JobSubRole {
    TANK,
    HEALER,
    MELEE,
    RANGED,
    CASTER
}

public enum LimitBreak : uint {
    Phalanx = 29069,
    PrimalScream = 29083,
    Eventide = 29097,
    RelentlessRush = 29130,
    TerminalTrigger = 29131,
    TerminalTrigger2 = 29469,
    AfflatusPurgation = 29230,
    Seraphism = 41502,
    CelestialRiver = 29255,
    Mesotes = 29266,
    Mesotes2 = 29267,
    Meteodrive = 29485,
    SkyHigh = 29497,
    SkyShatter = 29498,
    SkyShatter2 = 29499,
    SeitonTenchu = 29515,
    SeitonTenchu2 = 29516,
    Zantetsuken = 29537,
    TenebraeLemurum = 29553,
    WorldSwallower = 39190,
    FinalFantasia = 29401,
    MarksmansSpite = 29415,
    Contradance = 29432,
    SoulResonance = 29662,
    SummonBahamut = 29673,
    Megaflare = 29675,
    SummonPhoenix = 29678,
    EverlastingFlight = 29680,
    SouthernCross = 41498,
    AdventOfChocobastion = 39215,
}

public enum KeyAction : uint {
    Guard = 29054,
    Guard2 = 29735,
    Purify = 29056,
    Guardian = 29066,
    PlentifulHarvest = 29546,
}

public enum MajorStatus : uint {
    //Status
    Invincible = 895,
    Stun = 1343,
    Heavy = 1344,
    Bind = 1345,
    Silence = 1347,
    Sleep = 1348,
    //Common
    Guard = 3054,
    GuardBroken = 3673,
    Purify = 3248,
    //PLD
    Covered = 2413,
    Covered2 = 4352,
    //need to find covering pld
    HallowedGround = 1302,
    Phalanx = 3210,
    //WAR
    InnerRelease = 1303,
    ThrillOfBattle = 3185,
    Unguarded = 3021,
    //DRK
    SoleSurvivor = 1306,
    UndeadRedemption = 3039,
    //GNB
    Nebula = 3051,
    RelentlessRush = 3052,
    Shrapnel = 3053,
    //WHM
    MiracleOfNature = 3085,

    CelestialRiver = 3105,
    CelestialTide = 3106,
    Mesotes = 3119,
    Meteodrive = 3174,
    Lype = 3120,
    Blackfeather = 2995,
    Mini = 3518
}

public enum MinorStatus : uint {
    //PLD
    HolySheltron = 3026,
    ShieldOath = 3188,
    SacredClaim = 3025,
    ShieldSmite = 4283,
    //WAR
    Onslaught = 3029,
    Bloodwhetting = 3030,
    Orogeny = 3256,
    //DRK
    SaltedEarthHeal = 3037,
    SaltedEarthDamage = 3038,
    //GNB
    NoMercy = 3042,
    HeartOfCorundum = 4295,
    CatharsisOfCorundum = 4296,
}

public enum UselessStatus : uint {
    WellFed = 48,
    Jackpot = 902,
    PrioritySeals = 1078,
    PreferredWorld = 1411,
    Nin1 = 3190,
    Vpr1 = 4086,
    Vpr2 = 4087,
}