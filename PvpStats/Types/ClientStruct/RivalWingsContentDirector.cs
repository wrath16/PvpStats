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

    [StructLayout(LayoutKind.Explicit, Size = 0x08)]
    public unsafe struct AllianceStatus {
        [FieldOffset(0x00)] public uint Ceruleum;
        [FieldOffset(0x04)] public uint SoaringStacks;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xA0)]
    public unsafe struct Structure {
        [FieldOffset(0x00)] public uint Integrity;
    }

    public unsafe Span<AllianceStatus> AllianceSpan => new(Unsafe.AsPointer(ref PlayerAlliances[0]), 6);
}



