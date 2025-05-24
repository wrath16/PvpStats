using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct RivalWingsContentDirector {
    private const int Offset = 0x1E58;

    [FieldOffset(Offset + 0x008)] public fixed byte PlayerAlliances[0x08 * 6];
    public unsafe Span<AllianceStatus> AllianceSpan => new(Unsafe.AsPointer(ref PlayerAlliances[0]), 6);

    [FieldOffset(Offset + 0x060)] public Structure FalconCore;
    [FieldOffset(Offset + 0x100)] public Structure RavenCore;
    [FieldOffset(Offset + 0x1A0)] public Structure FalconTower1;
    [FieldOffset(Offset + 0x240)] public Structure FalconTower2;
    [FieldOffset(Offset + 0x2E0)] public Structure RavenTower1;
    [FieldOffset(Offset + 0x380)] public Structure RavenTower2;

    [FieldOffset(Offset + 0x680)] public short MercBalance;                 //50 to 100 for falcons, 0 to 50 for ravens
    [FieldOffset(Offset + 0x684)] public Team MercControl;

    [FieldOffset(Offset + 0x72C)] public Supplies MidType;
    [FieldOffset(Offset + 0x730)] public Team MidControl;
    [FieldOffset(Offset + 0x744)] public byte FalconMidScore;
    [FieldOffset(Offset + 0x748)] public byte RavenMidScore;
    //to find: mid timer, mid status, num players per team, control prog.

    [FieldOffset(Offset + 0x7B8)] public byte FalconChaserCount;
    [FieldOffset(Offset + 0x7BC)] public byte FalconOppressorCount;
    [FieldOffset(Offset + 0x7C0)] public byte FalconJusticeCount;

    [FieldOffset(Offset + 0x7C4)] public byte RavenChaserCount;
    [FieldOffset(Offset + 0x7C8)] public byte RavenOppressorCount;
    [FieldOffset(Offset + 0x7CC)] public byte RavenJusticeCount;

    [FieldOffset(Offset + 0x7D8)] public fixed byte FriendlyMechs[0x08 * 24];
    public unsafe Span<Mech> FriendlyMechSpan => new(Unsafe.AsPointer(ref FriendlyMechs[0]), 24);

    //[FieldOffset(0x2B40)] enemy mech hmm...

    [StructLayout(LayoutKind.Explicit, Size = 0x08)]
    public unsafe struct AllianceStatus {
        [FieldOffset(0x00)] public int Ceruleum;
        [FieldOffset(0x04)] public int SoaringStacks;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xA0)]
    public unsafe struct Structure {
        [FieldOffset(0x00)] public int Integrity;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x08)]
    public unsafe struct Mech {
        [FieldOffset(0x00)] public uint PlayerObjectId;
        [FieldOffset(0x04)] public MechType Type;
    }

    public enum MechType : byte {
        Chaser = 0,
        Oppressor = 1,
        Justice = 2,
        None = 3
    }

    public enum Supplies : byte {
        Gobtank = 0,
        Ceruleum = 1,
        Gobbiejuice = 2,
        Gobcrate = 3,
        None = 4,
    }

    public enum Team : byte {
        Falcons = 0,
        Ravens = 1,
        None = 2
    }
}

