﻿using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

//[StructLayout(LayoutKind.Explicit, Size = 0x310)]
//public unsafe struct FrontlineTeamResultsPacket {
//    //in seconds
//    [FieldOffset(0x48)] public FrontlineTeamStat MaelStats;
//    [FieldOffset(0x55)] public FrontlineTeamStat AdderStats;
//    [FieldOffset(0x62)] public FrontlineTeamStat FlameStats;

//    [StructLayout(LayoutKind.Explicit, Size = 0x0D)]
//    public struct FrontlineTeamStat {
//        [FieldOffset(0x00)] public uint Unknown1;
//        [FieldOffset(0x02)] public uint Stat1;
//        [FieldOffset(0x04)] public uint TotalScore;
//        [FieldOffset(0x06)] public uint Stat2;
//        [FieldOffset(0x08)] public uint Unknown2;
//        [FieldOffset(0x0A)] public uint Unknown3;
//        [FieldOffset(0x0C)] public uint Stat3;
//    }
//}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct FrontlineResultsPacket {

    [FieldOffset(0x10)] public ushort MatchLength;              //in seconds

    [FieldOffset(0x16)] public ushort WolfMarks;
    [FieldOffset(0x18)] public ushort SeriesXP;
    //1A = pvp rank xp?
    [FieldOffset(0x1C)] public uint JobXP;
    [FieldOffset(0x20)] public ushort Unknown1;
    [FieldOffset(0x22)] public ushort Unknown2;
    [FieldOffset(0x24)] public ushort PlayerCount;

    [FieldOffset(0x28)] public TeamStat MaelStats;
    [FieldOffset(0x36)] public TeamStat AdderStats;
    [FieldOffset(0x44)] public TeamStat FlameStats;

    [StructLayout(LayoutKind.Explicit, Size = 0x0E)]
    public struct TeamStat {
        //0,1,2
        [FieldOffset(0x00)] public ushort Place;
        [FieldOffset(0x02)] public ushort OccupationPoints;     //ovoos, tomeliths
        [FieldOffset(0x04)] public ushort EnemyKillPoints;
        [FieldOffset(0x06)] public ushort Unknown1;
        [FieldOffset(0x08)] public ushort IcePoints;
        [FieldOffset(0x0A)] public ushort TotalPoints;
        [FieldOffset(0x0C)] public ushort KOPointLosses;
    }
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct FrontlinePlayerResultsPacket {
    [FieldOffset(0x00)] public uint DamageDealt;                //includes players+other
    [FieldOffset(0x04)] public uint DamageToOther;
    [FieldOffset(0x08)] public uint DamageTaken;
    [FieldOffset(0x0C)] public uint HPRestored;
    [FieldOffset(0x10)] public uint Unknown1;                   //believed to be HP received
    [FieldOffset(0x14)] public uint Occupations;
    [FieldOffset(0x18)] public ushort Unknown2;
    [FieldOffset(0x1A)] public ushort WorldId;
    [FieldOffset(0x1C)] public byte ClassJobId;
    [FieldOffset(0x1D)] public byte Kills;
    [FieldOffset(0x1E)] public byte Deaths;
    [FieldOffset(0x1F)] public byte Team;                       //0 = mael, 1 = adders, 2 = flames
    [FieldOffset(0x20)] public byte Alliance;                   //0 = mael A ... 8 = flames C
    [FieldOffset(0x21)] public byte Assists;
    [FieldOffset(0x22)] public fixed byte PlayerName[32];
}