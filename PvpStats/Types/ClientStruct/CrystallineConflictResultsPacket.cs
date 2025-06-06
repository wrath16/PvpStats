using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct CrystallineConflictResultsPacket {
    //in seconds
    [FieldOffset(0x10)] public ushort MatchLength;

    [FieldOffset(0x22)] public byte ColosseumMatchRankIdBefore;
    [FieldOffset(0x23)] public byte ColosseumMatchRankIdAfter;
    [FieldOffset(0x24)] public byte RiserBefore;
    [FieldOffset(0x25)] public byte RiserAfter;
    [FieldOffset(0x26)] public byte StarsBefore;
    [FieldOffset(0x27)] public byte StarsAfter;
    [FieldOffset(0x28)] public ushort CreditBefore;
    [FieldOffset(0x2A)] public ushort CreditAfter;

    //1=victory, 2=defeat   1=astra victory on spectating, 2=umbra victory?
    [FieldOffset(0x36)] public byte Result;
    //measured in 0.1%
    [FieldOffset(0x38)] public uint AstraProgress;
    [FieldOffset(0x3C)] public uint UmbraProgress;

    [FieldOffset(0x40)] public fixed byte Players[0x50 * 10];
    public unsafe Span<CrystallineConflictPlayer> PlayerSpan => new(Unsafe.AsPointer(ref Players[0]), 10);

    [StructLayout(LayoutKind.Explicit, Size = 0x50)]
    public struct CrystallineConflictPlayer {
        [FieldOffset(0x00)] public ulong AccountId;                          //assumed
        [FieldOffset(0x08)] public ulong ContentId;

        [FieldOffset(0x10)] public int DamageDealt;
        [FieldOffset(0x14)] public int DamageTaken;
        [FieldOffset(0x18)] public int HPRestored;
        [FieldOffset(0x1C)] public ushort WorldId;
        [FieldOffset(0x1E)] public byte ClassJobId;
        [FieldOffset(0x1F)] public byte Kills;
        [FieldOffset(0x20)] public byte Deaths;
        [FieldOffset(0x21)] public byte Assists;
        [FieldOffset(0x22)] public ushort TimeOnCrystal;                     //in seconds
        [FieldOffset(0x24)] public byte ColosseumMatchRankId;
        [FieldOffset(0x25)] public CCTeam Team;
        [FieldOffset(0x26)] public fixed byte PlayerName[42];
    }
}