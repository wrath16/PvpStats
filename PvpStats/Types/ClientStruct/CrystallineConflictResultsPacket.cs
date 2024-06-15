using FFXIVClientStructs.Interop.Generated;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit, Size = 0x310)]
public unsafe struct CrystallineConflictResultsPacket {
    //in seconds
    [FieldOffset(0x00)] public ushort MatchLength;

    [FieldOffset(0x12)] public byte ColosseumMatchRankIdBefore;
    [FieldOffset(0x13)] public byte ColosseumMatchRankIdAfter;
    [FieldOffset(0x14)] public byte RiserBefore;
    [FieldOffset(0x15)] public byte RiserAfter;
    [FieldOffset(0x16)] public byte StarsBefore;
    [FieldOffset(0x17)] public byte StarsAfter;
    [FieldOffset(0x18)] public ushort CreditBefore;
    [FieldOffset(0x1A)] public ushort CreditAfter;

    //1=victory, 2=defeat   1=astra victory on spectating, 2=umbra victory?
    [FieldOffset(0x26)] public byte Result;
    //measured in 0.1%
    [FieldOffset(0x28)] public uint AstraProgress;
    [FieldOffset(0x2C)] public uint UmbraProgress;

    [FieldOffset(0x38)] private FixedSizeArray10<CrystallineConflictPlayer> Player;

    [StructLayout(LayoutKind.Explicit, Size = 0x48)]
    public struct CrystallineConflictPlayer {
        [FieldOffset(0x00)] public uint DamageDealt;
        [FieldOffset(0x04)] public uint DamageTaken;
        [FieldOffset(0x08)] public uint HPRestored;
        [FieldOffset(0x0C)] public ushort WorldId;
        [FieldOffset(0x0E)] public byte ClassJobId;
        [FieldOffset(0x0F)] public byte Kills;
        [FieldOffset(0x10)] public byte Deaths;
        [FieldOffset(0x11)] public byte Assists;
        //in seconds
        [FieldOffset(0x12)] public ushort TimeOnCrystal;
        [FieldOffset(0x14)] public byte ColosseumMatchRankId;
        //astra = 0, umbra = 1
        [FieldOffset(0x15)] public byte Team;
        [FieldOffset(0x16)] public fixed byte PlayerName[32];
    }

    public unsafe Span<CrystallineConflictPlayer> PlayerSpan => new Span<CrystallineConflictPlayer>(Unsafe.AsPointer(ref Player[0]), 10);
}