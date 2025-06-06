using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
internal unsafe struct CrystallineConflictContentDirector {
    private const int Offset = 0x1FF0;

    public CCPlayer? GetPlayerByEntityId(uint entityId) {
        for(var i = 0; i < 10; i++) {
            var player = Players[i];
            if(player.EntityId == entityId) return player;
        }
        return null;
    }

    [FieldOffset(Offset + 0x050)] public CCPlayer* Players;

    [FieldOffset(Offset + 0x148)] public int Unknown4;
    [FieldOffset(Offset + 0x14C)] public int Unknown5;
    [FieldOffset(Offset + 0x150)] public int Unknown6;

    [FieldOffset(Offset + 0x194)] public int Unknown0;
    [FieldOffset(Offset + 0x198)] public int Unknown1;
    [FieldOffset(Offset + 0x19C)] public int Unknown2;
    [FieldOffset(Offset + 0x1A0)] public float CrystalUnbindTimeRemaining;   //in seconds
    [FieldOffset(Offset + 0x1A4)] public float Unknown3;                     //a timer of some kind related to kill feed
    [FieldOffset(Offset + 0x1A8)] public int CrystalPosition;                //positive is Astra side, negative is Umbra side
    [FieldOffset(Offset + 0x1AC)] public int AstraProgress;
    [FieldOffset(Offset + 0x1B0)] public int UmbraProgress;
    [FieldOffset(Offset + 0x1B4)] public int AstraOnPoint;
    [FieldOffset(Offset + 0x1B8)] public int UmbraOnPoint;
    [FieldOffset(Offset + 0x1BC)] public int AstraMidpointProgress;
    [FieldOffset(Offset + 0x1C0)] public int UmbraMidpointProgress;

    [FieldOffset(Offset + 0x414)] public float EventTimer;                  //used in all maps except palaistra
}

[StructLayout(LayoutKind.Explicit, Size = 0x138)]
internal unsafe struct CCPlayer {
    private const int Offset = 0x0;

    //public string Name => MemoryService.ReadString(_name, 64);

    [FieldOffset(Offset + 0x8A)] public fixed byte Name[64];
    [FieldOffset(Offset + 0xE0)] public uint EntityId;
    [FieldOffset(Offset + 0xE8)] public CCTeam Team;
    [FieldOffset(Offset + 0xE9)] public byte ClassJobId;
    [FieldOffset(Offset + 0x128)] public ushort WorldId;
    [FieldOffset(Offset + 0x12C)] public byte ColosseumMatchRankId;
    [FieldOffset(Offset + 0x12D)] public byte Riser;
}

public enum CCTeam : byte {
    Astra = 0,
    Umbra = 1,
}

