using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct FrontlineContentDirector {

    [FieldOffset(0x3E22)] public short MaelstromScore;
    [FieldOffset(0x3E42)] public short AddersScore;
    [FieldOffset(0x3E62)] public short FlamesScore;
    [FieldOffset(0x4D08)] public byte PlayerBattleHigh;
}

