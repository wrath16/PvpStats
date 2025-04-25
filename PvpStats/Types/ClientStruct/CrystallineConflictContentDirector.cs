using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
internal struct CrystallineConflictContentDirector {
    public static int Offset = 0x1F38;

    [FieldOffset(0x000)] public int Unknown4;
    [FieldOffset(0x004)] public int Unknown5;
    [FieldOffset(0x008)] public int Unknown6;


    [FieldOffset(0x04C)] public int Unknown0;
    [FieldOffset(0x050)] public int Unknown1;
    [FieldOffset(0x054)] public int Unknown2;
    [FieldOffset(0x058)] public float CrystalUnbindTimeRemaining;   // in seconds
    [FieldOffset(0x05C)] public float Unknown3;                     // a timer of some kind related to kill feed
    [FieldOffset(0x060)] public int CrystalPosition;                //positive is Astra side, negative is Umbra side
    [FieldOffset(0x064)] public int AstraProgress;
    [FieldOffset(0x068)] public int UmbraProgress;

    [FieldOffset(0x06C)] public byte AstraOnPoint;
    [FieldOffset(0x070)] public byte UmbraOnPoint;
    [FieldOffset(0x074)] public int AstraMidpointProgress;
    [FieldOffset(0x078)] public int UmbraMidpointProgress;

}
