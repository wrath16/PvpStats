using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PvpStats.Services;

internal unsafe class GameFunctions {
    [Signature("48 8D 05 ?? ?? ?? ?? 48 89 06 48 8D 9E ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 86 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 86 ?? ?? ?? ?? 8D 7D ?? 48 8D 05", ScanType = ScanType.StaticAddress)]
    //[Signature("48 8D 0D ?? ?? ?? ?? BD ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 75", ScanType = ScanType.StaticAddress)]
    private readonly nint _ccDirector;

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

    [Signature("E8 ?? ?? ?? ?? 48 8B D8 E8 ?? ?? ?? ?? 48 8B F8")]
    private readonly delegate* unmanaged<EventFramework*, nint> _getInstanceContentCCDirector;

    //[Signature("E8 ?? ?? ?? ?? 84 C0 74 ?? 33 C0 38 87")]
    //private readonly delegate* unmanaged<>

    //p1 = data ref?
    //p2 = targetId
    //p3 = opcode (0x3ab as of patch 6.57)
    //p4 = dataPtr + 0x10 offset
    //p5 = data size (0x310)
    private delegate ulong ProcessPvPResultsFunc00Delegate(nint p1, uint p2, ushort p3, nint p4, long p5);

    //ProcessZonePacketDown
    //40 53 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 8B F2

    //sig scan1
    //E8 ?? ?? ?? ?? 84 C0 74 ?? 33 C0 38 87 ??
    //sig scan2
    //E8 ?? ?? ?? ?? 48 83 C4 ?? C3 41 81 FA 
    //me
    //40 53 41 54 41 55 41 57 48 83 EC ?? 44 8B EA 4D 8B F9 0F B6 91 1A ?? ?? ?? 45 0F B7 E0

    //[Signature("40 53 41 54 41 55 41 57 48 83 EC ?? 44 8B EA 4D 8B F9 0F B6 91 ?? ?? ?? ?? 45 0F B7 E0", DetourName = nameof(Func00Detour))]
    //private readonly Hook<ProcessPvPResultsFunc00Delegate> _func00Hook;

    ////p1 = data ref?
    ////p2 = targetId
    ////p3 = dataPtr + 0x10 offset
    //private delegate void ProcessPvPResultsFunc0Delegate(IntPtr p1, uint p2, IntPtr p3);
    //[Signature("48 83 EC ?? 4D 8B C8 48 C7 44 24 20 ?? ?? ?? ?? 41 B8 ?? ?? ?? ?? E8 E5 0C 00 00", DetourName = nameof(Func0Detour))]
    //private readonly Hook<ProcessPvPResultsFunc0Delegate> _func0Hook;

    //p1 = dataPtr + 0x10 offset
    //private delegate void ProcessPvPResultsFunc1Delegate(IntPtr p1);
    //[Signature("", DetourName = nameof(Func1Detour))]
    //private readonly Hook<ProcessPvPResultsFunc0Delegate> _func1Hook;

    private Plugin _plugin;

    internal GameFunctions(Plugin plugin) {
        _plugin = plugin;
        _plugin.InteropProvider.InitializeFromAttributes(this);
        //_plugin.Log.Debug($"func00 address: 0x{_func00Hook.Address.ToString("X2")}");
        //_plugin.Log.Debug($"func0 address: 0x{_func0Hook.Address.ToString("X2")}");
        //_plugin.Log.Debug($"func00 enabled? {_func00Hook.IsEnabled}");
        //_func00Hook.Enable();
        //_func0Hook.Enable();
    }

    //private unsafe ulong Func00Detour(IntPtr p1, uint p2, ushort p3, IntPtr p4, long p5) {
    //    //_plugin.Log.Information($"Func 00 detour occurred! opcode: {p3}");
    //    _func00Hook.Original(p1, p2, p3, p4, p5);
    //    return 0;
    //}

    //private unsafe void Func0Detour(IntPtr p1, uint p2, IntPtr p3) {
    //    _plugin.Log.Information("Func 0 detour occurred!");
    //    _func0Hook.Original(p1, p2, p3);
    //}

    internal int GetCurrentDutyId() {
        return GameMain.Instance()->CurrentContentFinderConditionId;
    }

    internal nint GetInstanceContentCrystallineConflictDirector() {
        //if (EventFramework.Instance() != null) {
        //    try {
        //        return SigScanner.Scan((IntPtr)EventFramework.Instance(), sizeof(EventFramework), "E8 ?? ?? ?? ?? 0F B6 98");
        //    } catch (KeyNotFoundException) {
        //        return 0;
        //    }

        //} else {
        //    return 0;
        //}
        return _getInstanceContentCCDirector(EventFramework.Instance());
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

    internal void CreateByteDump(nint ptr, int length, string name) {
        var bytes = new ReadOnlySpan<byte>((void*)ptr, length);
        var timeStamp = DateTime.Now;
        using(FileStream fs = File.Create($"{_plugin.PluginInterface.GetPluginConfigDirectory()}\\{name}_{timeStamp.Year}{timeStamp.Month}{timeStamp.Day}{timeStamp.Hour}{timeStamp.Minute}{timeStamp.Second}_dump.bin")) {
            fs.Write(bytes);
        }
    }

    internal void FindValueInContentDirector(string value) {
        var director = EventFramework.Instance()->GetInstanceContentDirector();
        _plugin.Log.Debug($"cc director: 0x{_ccDirector.ToString("X2")}");
        _plugin.Log.Debug($"instance content director: 0x{((nint)director).ToString("X2")}");
        Type[] types = { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong), typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(string) };
        foreach(var type in types) {
            object convertedValue;
            if(type != typeof(string)) {
                try {
                    convertedValue = TypeDescriptor.GetConverter(type).ConvertFromString(value);
                } catch(ArgumentException) {
                    //not a convertible type
                    //_plugin.Log.Debug($"{value} not convertible to {type.Name}");
                    continue;
                }
            } else {
                convertedValue = value;
            }

            for(int i = 0; type == typeof(string) && i == 0 || type != typeof(string) && i < Marshal.SizeOf(type); i++) {

                //var a = typeof(GameFunctions).GetMethod("FindValue");
                //_plugin.Log.Debug($"method found: {a != null}");

                //10 MB
                var matchedOffsets = (int[])typeof(GameFunctions).GetMethod("FindValue").MakeGenericMethod(type).Invoke(this, new object[] { convertedValue, (nint)director, 0xFFFFF, i });
                foreach(var offset in matchedOffsets) {
                    _plugin.Log.Debug($"{value} found at 0x{offset.ToString("X2")} for type: {type.Name} and byte offset: {i}");
                }
            }
        }
    }

    public void PrintAllStrings(nint ptr, int length) {
        using(UnmanagedMemoryStream memoryStream = new((byte*)ptr, length)) {
            using(var reader = new BinaryReader(memoryStream)) {
                try {
                    while(true) {
                        string result = reader.ReadString();
                        if(result.Length > 0) {
                            _plugin.Log.Verbose($"{result}");
                        }
                    }
                } catch(EndOfStreamException) {
                    return;
                }
            }
        }
    }
    public void PrintAllChars(nint ptr, int length) {
        var bytes = new ReadOnlySpan<byte>((void*)ptr, length);
        PrintAllChars(bytes.ToArray(), 8);
    }

    void PrintAllChars(byte[] ptr, int minLength = 1) {
        using(MemoryStream memoryStream = new(ptr)) {
            using(var reader = new BinaryReader(memoryStream, Encoding.ASCII)) {
                string curString = "";
                while(true) {
                    try {
                        char output = '\u0000';
                        try {
                            //output = reader.ReadChar();
                            output = (char)reader.PeekChar();
                            if(output != 0) {
                                curString += output;
                            } else if(curString.Length > 0) {
                                if(curString.Length >= minLength) {
                                    _plugin.Log.Verbose(curString);
                                }
                                curString = "";
                            }
                            reader.ReadChar();
                        } catch(ArgumentException) {
                            if(curString.Length >= minLength) {
                                _plugin.Log.Verbose(curString);
                            }
                            curString = "";
                            reader.ReadByte();
                        }
                    } catch(EndOfStreamException) {
                        _plugin.Log.Verbose(curString);
                        return;
                    }
                }
            }
        }
    }

    public void PrintAllPlayerObjects() {
        foreach(PlayerCharacter pc in _plugin.ObjectTable.Where(o => o.ObjectKind is ObjectKind.Player)) {
            _plugin.Log.Debug($"0x{pc.ObjectId.ToString("X2")} {pc.Name}");
            //_plugin.Log.Debug($"team null? {isPlayerTeam is null} player team? {isPlayerTeam} is p member? {pc.StatusFlags.HasFlag(StatusFlags.PartyMember)} isSelf? {isSelf}");
        }
    }

    public int[] FindValue<T>(T toFind, nint ptr, int length, int offset = 0, bool printOnly = false) {
        if(toFind is not null) {
            _plugin.Log.Debug($"checking for value...{toFind.GetType().Name} offset: {offset}");
        }

        using(UnmanagedMemoryStream memoryStream = new((byte*)nint.Add(ptr, offset), length)) {
            using(var reader = new BinaryReader(memoryStream)) {
                List<int> matchingCursors = new();
                int cursor = 0;

                //Func<T> readMethod;

                try {
                    switch(typeof(T)) {
                        case Type _ when typeof(T) == typeof(string):
                            while(cursor < length) {
                                char output = '\u0000';
                                try {
                                    //output = reader.ReadChar();
                                    output = (char)reader.PeekChar();
                                } catch(ArgumentException e) {
                                    //_plugin.Log.Error($"{e.Message}\nCursor: 0x{cursor.ToString("X2")}\n");
                                    //return matchingCursors.ToArray();

                                    //skip the byte
                                    //reader.ReadByte();
                                    //cursor++;
                                }

                                if(!printOnly) {
                                    string match = "";
                                    int index = 0;
                                    string stringInput = (string)Convert.ChangeType(toFind, typeof(string));
                                    if(output == stringInput[index]) {
                                        match += output;
                                        index++;
                                        var byteCount = Encoding.UTF8.GetByteCount(new char[] { output });
                                        reader.ReadBytes(byteCount);
                                        if(match.Equals(toFind)) {
                                            matchingCursors.Add(cursor - Encoding.UTF8.GetByteCount(match.Remove(match.Length - 1)));
                                            match = "";
                                            index = 0;
                                        }
                                        cursor += byteCount;
                                    } else {
                                        match = "";
                                        index = 0;
                                        reader.ReadByte();
                                        cursor++;
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} Char {output}");
                                    reader.ReadByte();
                                    cursor++;
                                }
                            }
                            break;
                        case Type _ when typeof(T) == typeof(byte):
                            while(cursor < length) {
                                var output = reader.ReadByte();
                                if(!printOnly) {
                                    if(output == Convert.ToByte(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} Byte {output}");
                                }
                                cursor += sizeof(byte);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(ushort):
                            while(cursor < length) {
                                var output = reader.ReadUInt16();
                                if(!printOnly) {
                                    if(output == Convert.ToUInt16(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} UShort {output}");
                                }
                                cursor += sizeof(ushort);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(uint):
                            while(cursor < length) {
                                var output = reader.ReadUInt32();
                                if(!printOnly) {
                                    if(output == Convert.ToUInt32(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} UInt {output}");
                                }
                                cursor += sizeof(uint);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(ulong):
                            while(cursor < length) {
                                var output = reader.ReadUInt64();
                                if(!printOnly) {
                                    if(output == Convert.ToUInt64(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} ULong {output}");
                                }
                                cursor += sizeof(ulong);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(sbyte):
                            while(cursor < length) {
                                var output = reader.ReadSByte();
                                if(!printOnly) {
                                    if(output == Convert.ToSByte(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} SByte {output}");
                                }
                                cursor += sizeof(sbyte);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(short):
                            while(cursor < length) {
                                var output = reader.ReadInt16();
                                if(!printOnly) {
                                    if(output == Convert.ToInt16(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} Short {output}");
                                }
                                cursor += sizeof(short);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(int):
                            while(cursor < length) {
                                var output = reader.ReadInt32();
                                if(!printOnly) {
                                    if(output == Convert.ToInt32(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} Int {output}");
                                }
                                cursor += sizeof(int);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(long):
                            while(cursor < length) {
                                var output = reader.ReadInt64();
                                if(!printOnly) {
                                    if(output == Convert.ToInt64(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} Long {output}");
                                }
                                cursor += sizeof(long);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(float):
                            while(cursor < length) {
                                var output = reader.ReadSingle();
                                if(!printOnly) {
                                    if(output == Convert.ToSingle(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} Float {output}");
                                }
                                cursor += sizeof(float);
                            }
                            break;
                        case Type _ when typeof(T) == typeof(double):
                            while(cursor < length) {
                                var output = reader.ReadDouble();
                                if(!printOnly) {
                                    if(output == Convert.ToDouble(toFind)) {
                                        matchingCursors.Add(cursor);
                                    }
                                } else {
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} Double {output}");
                                }
                                cursor += sizeof(double);
                            }
                            break;
                        default:
                            throw new ArgumentException("Invalid type argument");
                    }
                } catch(AccessViolationException) {
                    _plugin.Log.Error($"Can't read memory at 0x{cursor.ToString("X2")}");
                }

                return matchingCursors.ToArray();
            }
        }
    }

    private void FindValue<T>(T toFind, byte[] bytes, int offset = 0) {

    }

    internal void ReadBytes(nint ptr, Type type, int length, int offset = 0) {
        //start low length
        using(UnmanagedMemoryStream memoryStream = new((byte*)nint.Add(ptr, offset), length)) {
            using(var reader = new BinaryReader(memoryStream)) {
                int cursor = 0;
                while(cursor < length) {
                    switch(type) {
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
                        case Type _ when type == typeof(byte):
                            var outputByte = reader.ReadByte();
                            _plugin.Log.Debug($"offset: 0x{cursor.ToString("X2")} Int64 {outputByte}");
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

    public static byte[] GetBytes(object str) {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        nint ptr = nint.Zero;
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
        if(b == null) return nullIsEmpty ? string.Empty : null;
        if(maxLength > 0) return Encoding.UTF8.GetString(b, maxLength).Split('\0')[0];
        var l = 0;
        while(b[l] != 0) l++;
        return Encoding.UTF8.GetString(b, l);
    }
}
