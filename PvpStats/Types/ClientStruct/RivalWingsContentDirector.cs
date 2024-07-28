using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct RivalWingsContentDirector {
    [FieldOffset(0x1E08)] public fixed byte PlayerAlliances[0x08 * 6];
    public unsafe Span<AllianceStatus> AllianceSpan => new(Unsafe.AsPointer(ref PlayerAlliances[0]), 6);

    [FieldOffset(0x1E68)] public Structure FalconCore;
    [FieldOffset(0x1F08)] public Structure RavenCore;
    [FieldOffset(0x1FA8)] public Structure FalconTower1;
    [FieldOffset(0x2048)] public Structure FalconTower2;
    [FieldOffset(0x20E8)] public Structure RavenTower1;
    [FieldOffset(0x2188)] public Structure RavenTower2;

    [FieldOffset(0x2488)] public short MercBalance;                 //50 to 100 for falcons, 0 to 50 for ravens
    [FieldOffset(0x248C)] public Team MercControl;

    [FieldOffset(0x2534)] public Supplies MidType;
    [FieldOffset(0x2538)] public Team MidControl;
    [FieldOffset(0x254C)] public byte FalconMidScore;
    [FieldOffset(0x2550)] public byte RavenMidScore;
    //to find: mid timer, mid status, num players per team, control prog.

    [FieldOffset(0x25C0)] public byte FalconChaserCount;
    [FieldOffset(0x25C4)] public byte FalconOppressorCount;
    [FieldOffset(0x25C8)] public byte FalconJusticeCount;

    [FieldOffset(0x25CC)] public byte RavenChaserCount;
    [FieldOffset(0x25D0)] public byte RavenOppressorCount;
    [FieldOffset(0x25D4)] public byte RavenJusticeCount;

    [FieldOffset(0x25E0)] public fixed byte FriendlyMechs[0x08 * 24];
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

