using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
internal struct CrystallineConflictContentDirector {
    public const int Offset = 0x1F38;

    [FieldOffset(Offset + 0x000)] public int Unknown4;
    [FieldOffset(Offset + 0x004)] public int Unknown5;
    [FieldOffset(Offset + 0x008)] public int Unknown6;


    [FieldOffset(Offset + 0x04C)] public int Unknown0;
    [FieldOffset(Offset + 0x050)] public int Unknown1;
    [FieldOffset(Offset + 0x054)] public int Unknown2;
    [FieldOffset(Offset + 0x058)] public float CrystalUnbindTimeRemaining;   // in seconds
    [FieldOffset(Offset + 0x05C)] public float Unknown3;                     // a timer of some kind related to kill feed
    [FieldOffset(Offset + 0x060)] public int CrystalPosition;                //positive is Astra side, negative is Umbra side
    [FieldOffset(Offset + 0x064)] public int AstraProgress;
    [FieldOffset(Offset + 0x068)] public int UmbraProgress;

    [FieldOffset(Offset + 0x06C)] public byte AstraOnPoint;
    [FieldOffset(Offset + 0x070)] public byte UmbraOnPoint;
    [FieldOffset(Offset + 0x074)] public int AstraMidpointProgress;
    [FieldOffset(Offset + 0x078)] public int UmbraMidpointProgress;
}

[StructLayout(LayoutKind.Explicit)]
internal struct CCPlayer {
    public const int Offset = 0x0;

    [FieldOffset(Offset + 0xE0)] public uint EntityId;
    [FieldOffset(Offset + 0xE8)] public CCTeam Team;
    [FieldOffset(Offset + 0xE9)] public byte ClassJobId;
}

public enum CCTeam : byte {
    Astra = 0,
    Umbra = 1,
}



