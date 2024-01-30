using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PvpStats;

internal unsafe class GameFunctions {
    //[Signature("BA ?? ?? ?? ?? E8 ?? ?? ?? ?? 41 8B 4D 08", Offset = 1)]
    //private uint _agentId;

    //[Signature("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 33 ED 48 8D 15")]
    //private readonly delegate* unmanaged<uint, uint, float, float, uint> _setFlagMapMarker;
    ////(uint territoryId, uint mapId, float mapX, float mapY, uint iconId = 0xEC91)

    //[Signature("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? B0 ?? 48 8B B4 24")]
    //private readonly delegate* unmanaged<uint> _openMapByMapId;
    ////(uint mapId)

    //[Signature("E8 ?? ?? ?? ?? 84 C0 0F 94 C0 EB 19")]
    //private readonly delegate* unmanaged<nint, nint> _setWaymark;
    //(uint mapId)

    //private static AtkUnitBase* AddonToDoList => GetUnitBase<AtkUnitBase>("_ToDoList");

    private Plugin _plugin;

    internal GameFunctions(Plugin plugin) {
        _plugin = plugin;
    }

    internal void OpenMap(uint mapId) {
        //AgentMap* agent = AgentMap.Instance();
        //AgentMap.MemberFunctionPointers.OpenMapByMapId(agent, mapId);
        AgentMap.Instance()->OpenMapByMapId(mapId);
    }

    internal void SetFlagMarkers(uint territoryId, uint mapId, float mapX, float mapY) {
        //AgentMap.MemberFunctionPointers.SetFlagMapMarker(AgentMap.Instance(), territoryId, mapId, mapX, mapY, 60561u);
        AgentMap.Instance()->SetFlagMapMarker(territoryId, mapId, mapX, mapY);
    }

    internal int GetCurrentDutyId() {
        return GameMain.Instance()->CurrentContentFinderConditionId;
    }

    internal void Test() {
        var x = EventFramework.Instance()->GetContentDirector();
        var y = (InstanceContentDirector*)x;
        var z = y->ContentDirector.VTable;

    }

    internal InstanceContentType GetContentType() {
        var x = EventFramework.Instance()->GetInstanceContentDirector();
        return x->InstanceContentType;
    }

    internal byte[] GetRawInstanceContentDirector() {
        var x = EventFramework.Instance()->GetContentDirector();
        var y = (InstanceContentDirector*)x;
        var z = *y;
        return GetBytes(z);
    }

    internal byte[] GetRawDeepDungeonInstanceContentDirector() {
        var x = EventFramework.Instance()->GetInstanceContentDeepDungeon();
        return GetBytes(*x);
    }

    internal void AttemptToReadContentDirector() {
        var director = EventFramework.Instance()->GetContentDirector();
        ReadBytes((nint)director, typeof(ushort), 0x1CB0, 0);
    }

    internal void FindValueInContentDirector(int value) {
        var director = EventFramework.Instance()->GetContentDirector();

        for (int i = 0; i < sizeof(short); i++) {
            var int16 = FindInt16((short)value, (nint)director, 0x999999, i);
            if (int16 != null) {
                _plugin.Log.Debug($"{value} found at 0x{((int)int16).ToString("X2")} type: INT16");
            }
        }

        for (int i = 0; i < sizeof(int); i++) {
            var int32 = FindInt32(value, (nint)director, 0x999999, i);
            if (int32 != null) {
                _plugin.Log.Debug($"{value} found at 0x{((int)int32).ToString("X2")} type: INT32");
            }
        }

        for (int i = 0; i < sizeof(long); i++) {
            var int64 = FindInt64(value, (nint)director, 0x999999, i);
            if (int64 != null) {
                _plugin.Log.Debug($"{value} found at 0x{((int)int64).ToString("X2")} type: INT64");
            }
        }

        for (int i = 0; i < sizeof(ushort); i++) {
            var uint16 = FindUInt16((ushort)value, (nint)director, 0x999999, i);
            if (uint16 != null) {
                _plugin.Log.Debug($"{value} found at 0x{((int)uint16).ToString("X2")} type: UINT16");
            }
        }

        for (int i = 0; i < sizeof(uint); i++) {
            var uint32 = FindUInt32((uint)value, (nint)director, 0x999999);
            if (uint32 != null) {
                _plugin.Log.Debug($"{value} found at 0x{((int)uint32).ToString("X2")} type: UINT32");
            }
        }

        for (int i = 0; i < sizeof(ulong); i++) {
            var uint64 = FindUInt64((ulong)value, (nint)director, 0x999999);
            if (uint64 != null) {
                _plugin.Log.Debug($"{value} found at 0x{((int)uint64).ToString("X2")} type: UINT64");
            }
        }
    }

    int? FindInt16(short toFind, nint ptr, int length, int offset = 0) {
        using (UnmanagedMemoryStream memoryStream = new((byte*)IntPtr.Add(ptr, offset), length)) {
            using (var reader = new BinaryReader(memoryStream)) {
                int cursor = 0;
                while (cursor < length) {
                    var output = reader.ReadInt16();
                    if (output == toFind) {
                        return cursor;
                    }
                    cursor += sizeof(short);
                }
                return null;
            }
        }
    }

    int? FindInt32(int toFind, nint ptr, int length, int offset = 0) {
        using (UnmanagedMemoryStream memoryStream = new((byte*)IntPtr.Add(ptr, offset), length)) {
            using (var reader = new BinaryReader(memoryStream)) {
                int cursor = 0;
                while (cursor < length) {
                    var output = reader.ReadInt32();
                    if (output == toFind) {
                        return cursor;
                    }
                    cursor += sizeof(int);
                }
                return null;
            }
        }
    }

    int? FindInt64(long toFind, nint ptr, int length, int offset = 0) {
        using (UnmanagedMemoryStream memoryStream = new((byte*)IntPtr.Add(ptr, offset), length)) {
            using (var reader = new BinaryReader(memoryStream)) {
                int cursor = 0;
                while (cursor < length) {
                    var output = reader.ReadInt64();
                    if (output == toFind) {
                        return cursor;
                    }
                    cursor += sizeof(long);
                }
                return null;
            }
        }
    }

    int? FindUInt16(ushort toFind, nint ptr, int length, int offset = 0) {
        using (UnmanagedMemoryStream memoryStream = new((byte*)IntPtr.Add(ptr, offset), length)) {
            using (var reader = new BinaryReader(memoryStream)) {
                int cursor = 0;
                while (cursor < length) {
                    var output = reader.ReadUInt16();
                    if (output == toFind) {
                        return cursor;
                    }
                    cursor += sizeof(ushort);
                }
                return null;
            }
        }
    }

    int? FindUInt32(uint toFind, nint ptr, int length, int offset = 0) {
        using (UnmanagedMemoryStream memoryStream = new((byte*)IntPtr.Add(ptr, offset), length)) {
            using (var reader = new BinaryReader(memoryStream)) {
                int cursor = 0;
                while (cursor < length) {
                    var output = reader.ReadUInt32();
                    if (output == toFind) {
                        return cursor;
                    }
                    cursor += sizeof(uint);
                }
                return null;
            }
        }
    }

    int? FindUInt64(ulong toFind, nint ptr, int length, int offset = 0) {
        using (UnmanagedMemoryStream memoryStream = new((byte*)IntPtr.Add(ptr, offset), length)) {
            using (var reader = new BinaryReader(memoryStream)) {
                int cursor = 0;
                while (cursor < length) {
                    var output = reader.ReadUInt64();
                    if (output == toFind) {
                        return cursor;
                    }
                    cursor += sizeof(ulong);
                }
                return null;
            }
        }
    }

    private void ReadBytes(nint ptr, Type type, int length, int offset = 0) {
        //start low length
        using (UnmanagedMemoryStream memoryStream = new((byte*)IntPtr.Add(ptr, offset), length)) {
            using (var reader = new BinaryReader(memoryStream)) {
                int cursor = 0;
                while (cursor < length) {
                    switch (type) {
                        case Type _ when type == typeof(short):
                            short outputShort = reader.ReadInt16();
                            _plugin.Log.Debug($"offset: 0x{cursor.ToString("X2")} Int16 {outputShort}");
                            cursor += sizeof(short);
                            break;
                        case Type _ when type == typeof(int):
                            int outputInt = reader.ReadInt32();
                            _plugin.Log.Debug($"offset: 0x{cursor.ToString("X2")} Int32 {outputInt}");
                            cursor += sizeof(int);
                            break;
                        case Type _ when type == typeof(long):
                            long outputLong = reader.ReadInt64();
                            _plugin.Log.Debug($"offset: 0x{cursor.ToString("X2")} Int64 {outputLong}");
                            cursor += sizeof(long);
                            break;
                        case Type _ when type == typeof(ushort):
                            uint outputUShort = reader.ReadUInt16();
                            _plugin.Log.Debug($"offset: 0x{cursor.ToString("X2")} UInt32 {outputUShort}");
                            cursor += sizeof(ushort);
                            break;
                        case Type _ when type == typeof(uint):
                            uint outputUint = reader.ReadUInt32();
                            _plugin.Log.Debug($"offset: 0x{cursor.ToString("X2")} UInt32 {outputUint}");
                            cursor += sizeof(uint);
                            break;
                        case Type _ when type == typeof(ulong):
                            ulong outputUlong = reader.ReadUInt64();
                            _plugin.Log.Debug($"offset: 0x{cursor.ToString("X2")} UInt64 {outputUlong}");
                            cursor += sizeof(ulong);
                            break;

                    }
                }
            }
        }
        //using(var reader = new BinaryReader(ptr))
    }

    static byte[] GetBytes(object str) {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = IntPtr.Zero;
        try {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
        return arr;
    }

    static string ReadString(byte* b, int maxLength = 0, bool nullIsEmpty = true) {
        if (b == null) return nullIsEmpty ? string.Empty : null;
        if (maxLength > 0) return Encoding.UTF8.GetString(b, maxLength).Split('\0')[0];
        var l = 0;
        while (b[l] != 0) l++;
        return Encoding.UTF8.GetString(b, l);
    }
}
