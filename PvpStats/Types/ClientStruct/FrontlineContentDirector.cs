using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct FrontlineContentDirector {

    private const int Offset = 0x4022;

    [FieldOffset(Offset + 0x000)] public short MaelstromScore;
    [FieldOffset(Offset + 0x020)] public short AddersScore;
    [FieldOffset(Offset + 0x040)] public short FlamesScore;
    [FieldOffset(Offset + 0xEE6)] public byte PlayerBattleHigh;
}

