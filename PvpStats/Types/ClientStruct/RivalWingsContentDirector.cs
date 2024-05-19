using FFXIVClientStructs.Interop.Attributes;
using System.Runtime.CompilerServices;
using System;
using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct RivalWingsContentDirector {
    [FixedSizeArray<AllianceStatus>(6)]
    [FieldOffset(0x1D20)] public fixed byte PlayerAlliances[0x08 * 6];

    [FieldOffset(0x1D78)] public Structure FalconCore;
    [FieldOffset(0x1E18)] public Structure RavenCore;
    [FieldOffset(0x1EB8)] public Structure FalconTower1;
    [FieldOffset(0x1F58)] public Structure FalconTower2;
    [FieldOffset(0x1FF8)] public Structure RavenTower1;
    [FieldOffset(0x2098)] public Structure RavenTower2;

    [FieldOffset(0x2444)] public Supplies MidType;
    [FieldOffset(0x2448)] public Team MidControl;
    [FieldOffset(0x245C)] public byte FalconMidScore;
    [FieldOffset(0x2460)] public byte RavenMidScore;

    [FieldOffset(0x24D0)] public byte FalconMech1;
    [FieldOffset(0x24D4)] public byte FalconMech2;
    [FieldOffset(0x24D8)] public byte FalconMech3;

    [FieldOffset(0x24DC)] public byte RavenMech1;
    [FieldOffset(0x24E0)] public byte RavenMech2;
    [FieldOffset(0x24E4)] public byte RavenMech3;

    [FixedSizeArray<Mech>(24)]
    [FieldOffset(0x24F0)] public fixed byte FriendlyMechs[0x08 * 24];

    //all dubious
    //[FieldOffset(0x2504)] public Mech MechAllianceB;
    //[FieldOffset(0x251C)] public Mech MechAllianceB_2;
    //[FieldOffset(0x2524)] public Mech MechAllianceA;
    //[FieldOffset(0x2534)] public Mech MechAllianceC;
    //[FieldOffset(0x253C)] public Mech MechAllianceD;
    //[FieldOffset(0x255C)] public Mech MechAllianceC_2;
    //[FieldOffset(0x2574)] public Mech MechAllianceA_2;
    //[FieldOffset(0x257C)] public Mech MechAllianceF;
    //[FieldOffset(0x2584)] public Mech MechAllianceE_2;
    //[FieldOffset(0x258C)] public Mech MechAllianceF_2;
    //[FieldOffset(0x2594)] public Mech MechAllianceE;
    //[FieldOffset(0x25AC)] public Mech MechAllianceD_2;

    //[FieldOffset(0x2B40)] enemy mech


    public unsafe Span<AllianceStatus> AllianceSpan => new(Unsafe.AsPointer(ref PlayerAlliances[0]), 6);
    public unsafe Span<Mech> FriendlyMechSpan => new(Unsafe.AsPointer(ref FriendlyMechs[0]), 24);

    [StructLayout(LayoutKind.Explicit, Size = 0x08)]
    public unsafe struct AllianceStatus {
        [FieldOffset(0x00)] public uint Ceruleum;
        [FieldOffset(0x04)] public uint SoaringStacks;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xA0)]
    public unsafe struct Structure {
        [FieldOffset(0x00)] public uint Integrity;
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



