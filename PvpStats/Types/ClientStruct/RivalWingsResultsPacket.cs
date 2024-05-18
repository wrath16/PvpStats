using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct RivalWingsResultsPacket {
    [FieldOffset(0x10)] public ushort MatchLength;              //in seconds

    [FieldOffset(0x16)] public ushort WolfMarks;
    [FieldOffset(0x18)] public ushort SeriesXP;
    //1A = pvp rank xp?
    [FieldOffset(0x1C)] public uint JobXP;
    [FieldOffset(0x20)] public ushort Unknown1;
    [FieldOffset(0x22)] public ushort Unknown2;

    [StructLayout(LayoutKind.Explicit)]
    public struct RivalWingsPlayer {
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
}
