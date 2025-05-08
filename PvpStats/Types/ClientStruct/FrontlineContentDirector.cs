using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct FrontlineContentDirector {

    public static int Offset = 0x3E22;

    [FieldOffset(0x000)] public short MaelstromScore;
    [FieldOffset(0x020)] public short AddersScore;
    [FieldOffset(0x040)] public short FlamesScore;
    [FieldOffset(0xEE6)] public byte PlayerBattleHigh;
}

