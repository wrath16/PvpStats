using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct FrontlineContentDirector {

    private const int Offset = 0x2522;

    [FieldOffset(Offset + 0x000)] public short MaelstromScore;
    [FieldOffset(Offset + 0x020)] public short AddersScore;
    [FieldOffset(Offset + 0x040)] public short FlamesScore;
    [FieldOffset(Offset + 0xEE5)] public byte PlayerBattleHigh;
}

