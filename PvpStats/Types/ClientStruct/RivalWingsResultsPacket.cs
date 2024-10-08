﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct RivalWingsResultsPacket {
    [FieldOffset(0x10)] public short MatchLength;               //in seconds
    [FieldOffset(0x12)] public short Unknown10;                 //sample value: 0
    [FieldOffset(0x14)] public short Unknown11;                 //sample value: 0
    [FieldOffset(0x16)] public short WolfMarks;
    [FieldOffset(0x18)] public short SeriesXP;
    //1A = pvp rank xp?
    [FieldOffset(0x1C)] public int JobXP;
    [FieldOffset(0x20)] public short Unknown1;                  //sample value: 276(1,0x14), 299(1,0x2B), 516 (4,2), 513 (2, 1)
    [FieldOffset(0x22)] public byte Result;                     //0 = win, 1 = loss
    [FieldOffset(0x23)] public byte Unknown3;                   //sample value: 0, 1
    [FieldOffset(0x24)] public short Unknown4;                  //sample value: 21064 (0x52,0x48), 16595 (0x40, 0xD3)
    [FieldOffset(0x26)] public short Unknown5;                  //sample value: 115
    [FieldOffset(0x28)] public short PlayerCount;
    [FieldOffset(0x30)] private fixed byte Players[0x50 * 48];
    public unsafe Span<RivalWingsPlayer> PlayerSpan => new(Unsafe.AsPointer(ref Players[0]), 48);

    [StructLayout(LayoutKind.Explicit, Size = 0x50)]
    public struct RivalWingsPlayer {
        [FieldOffset(0x00)] public ulong AccountId;
        [FieldOffset(0x08)] public ulong ContentId;

        [FieldOffset(0x10)] public int DamageDealt;
        [FieldOffset(0x14)] public int DamageToOther;
        [FieldOffset(0x18)] public int DamageTaken;
        [FieldOffset(0x1C)] public int HPRestored;
        [FieldOffset(0x20)] public int Unknown1;                //related to healing
        [FieldOffset(0x24)] public ushort WorldId;
        [FieldOffset(0x26)] public byte ClassJobId;
        [FieldOffset(0x27)] public byte Kills;
        [FieldOffset(0x28)] public byte Deaths;
        [FieldOffset(0x29)] public byte Team;                   //0 = falcons, 1 = ravens
        [FieldOffset(0x2A)] public byte Alliance;               //0 = falcons A to 11 = ravens F
        [FieldOffset(0x2B)] public byte Assists;
        [FieldOffset(0x2C)] public ushort Ceruleum;
        [FieldOffset(0x2E)] public fixed byte PlayerName[32];
    }
}
