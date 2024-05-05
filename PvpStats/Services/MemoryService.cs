using Dalamud.Game.Network;
using PvpStats.Types.ClientStruct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PvpStats.Services;

internal unsafe class MemoryService : IDisposable {
    private Plugin _plugin;

    //debug fields
    internal Dictionary<ushort, uint> _opCodeCount = new();
    internal int _opcodeMatchCount = 0;
    private DateTime _lastSortTime;
    internal bool _qPopped = false;

    private ushort[] _blacklistedOpcodes = [949,
        819,
        273,
        241,
        337,
        458,
        397,
        110,
        193,
        378,
        913,
        993,
        799,
        246,
        402,
        768,
        836,
        210,
        788,
        295,
        367,
        194,
        577,
        401,
        482,
        701,
        780,
        365,
        579,
        737,
        653,
        487,
        494,
        690,
        172,
        862,
        117,
        396,
        171,
        291,
        888,
        941,
        726,
        183,
        501,
        370];

    internal MemoryService(Plugin plugin) {
        _plugin = plugin;

#if DEBUG
        _plugin.GameNetwork.NetworkMessage += OnNetworkMessage;
#endif
    }

    public void Dispose() {
#if DEBUG
        _plugin.GameNetwork.NetworkMessage -= OnNetworkMessage;
#endif
    }

    private unsafe void OnNetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
        if(direction != NetworkMessageDirection.ZoneDown) {
            return;
        }

        if(_opCodeCount.ContainsKey(opCode)) {
            _opCodeCount[opCode]++;
        } else {
            _opCodeCount.Add(opCode, 1);
        }

        if(!_blacklistedOpcodes.Contains(opCode)) {
            _plugin.Log.Verbose($"OPCODE: {opCode} {opCode:X2} DATAPTR: 0x{dataPtr.ToString("X2")} SOURCEACTORID: {sourceActorId} TARGETACTORID: {targetActorId}");
            //_plugin.Functions.PrintAllChars(dataPtr, 0x2000, 8);
            //_plugin.Functions.PrintAllStrings(dataPtr, 0x500);
        }

        //770...
        //235...
        //347 = cc packet
        if(opCode == 235) {
            _plugin.Log.Debug("235 occurred!");

            FrontlineResultsPacket resultsPacket = *(FrontlineResultsPacket*)(dataPtr);
            //var printTeamStats = (FrontlineTeamResultsPacket.FrontlineTeamStat team, string name) => {
            //    _plugin.Log.Debug($"{name}\nUnknown1 {team.Unknown1}\nStat1 {team.Stat1}\nTotalScore {team.TotalScore}\nStat2 {team.Stat2}\nUnknown2 {team.Unknown2}\nUnknown3 {team.Unknown3}\nStat3 {team.Stat3}");
            //};

            var printTeamStats = (FrontlineResultsPacket.TeamStat team, string name) => {
                _plugin.Log.Debug($"{name}\nPlace {team.Place}\nOvooPoints {team.OccupationPoints}\nKillPoints {team.EnemyKillPoints}\nDeathLosses {team.KOPointLosses}\nUnknown1 {team.Unknown1}\nUnknown2 {team.Unknown2}\nTotalRating {team.TotalPoints}");
            };

            //printTeamStats(resultsPacket.MaelStats, "Maelstrom");
            //printTeamStats(resultsPacket.AdderStats, "Adders");
            //printTeamStats(resultsPacket.FlameStats, "Flames");
            FindValue<ushort>(0, dataPtr, 0x300, 0, true);

            //try {
            //    FindValue<byte>(0, dataPtr, 0x2000, 0, true);
            //} catch {
            //}
            //try {
            //    FindValue<int>(0, dataPtr, 0x2000, 0, true);
            //} catch {
            //}
            //try {
            //    FindValue<short>(0, dataPtr, 0x2000, 0, true);
            //} catch {
            //}
            //try {
            //    FindValue<string>("", dataPtr, 0x2000, 0, true);
            //} catch {
            //}

            //PrintAllChars(dataPtr, 0x2000, 8);
        }

        if(opCode == 552) {
            //player payload
            //FrontlinePlayerResultsPacket resultsPacket = *(FrontlinePlayerResultsPacket*)(dataPtr);
            //var playerName = (PlayerAlias)$"{MemoryService.ReadString(resultsPacket.PlayerName, 32)} {_plugin.DataManager.GetExcelSheet<World>()?.GetRow(resultsPacket.WorldId)?.Name}";
            //var team = resultsPacket.Team == 0 ? "Maelstrom" : resultsPacket.Team == 1 ? "Adders" : "Flames";
            //var job = PlayerJobHelper.GetJobFromName(_plugin.DataManager.GetExcelSheet<ClassJob>()?.GetRow(resultsPacket.ClassJobId)?.NameEnglish ?? "");
            //_plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-15}", "NAME", "TEAM", "ALLIANCE", "JOB", "KILLS", "DEATHS", "ASSISTS", "DAMAGE DEALT", "DAMAGE OTHER", "DAMAGE TAKEN", "HP RESTORED", "??? 1", "??? 2"));
            //_plugin.Log.Debug(string.Format("{0,-32} {1,-15} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-15} {8,-15} {9,-15} {10,-15} {11,-15} {12,-15}", playerName, team, resultsPacket.Alliance, job, resultsPacket.Kills, resultsPacket.Deaths, resultsPacket.Assists, resultsPacket.DamageDealt, resultsPacket.DamageToOther, resultsPacket.DamageTaken, resultsPacket.HPRestored, resultsPacket.Unknown1, resultsPacket.Unknown2));

            //FindValue<byte>(0, dataPtr, 0x50, 0, true);
            FindValue<ushort>(0, dataPtr, 0x40, 0, true);
            //FindValue<uint>(0, dataPtr, 0x50, 0, true);
        }

        ////643 has promise...
        //if (opCode == 945 || opCode == 560) {
        //    _plugin.Functions.FindValue<string>("", dataPtr, 0x500, 0, true);
        //}

        if(DateTime.Now - _lastSortTime > TimeSpan.FromSeconds(60)) {
            _lastSortTime = DateTime.Now;
            _opCodeCount = _opCodeCount.OrderBy(x => x.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    internal void CreateByteDump(nint ptr, int length, string name) {
        var bytes = new ReadOnlySpan<byte>((void*)ptr, length);
        var timeStamp = DateTime.Now;
        using(FileStream fs = File.Create($"{_plugin.PluginInterface.GetPluginConfigDirectory()}\\{name}_{timeStamp.Year}{timeStamp.Month}{timeStamp.Day}{timeStamp.Hour}{timeStamp.Minute}{timeStamp.Second}{timeStamp.Millisecond}_dump.bin")) {
            fs.Write(bytes);
        }
    }

    public void PrintAllChars(nint ptr, int length, int minLengthString) {
        var bytes = new ReadOnlySpan<byte>((void*)ptr, length);
        PrintAllChars(bytes.ToArray(), minLengthString);
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
                                } catch(ArgumentException) {
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
                                    _plugin.Log.Debug($"offset: 0x{(cursor + offset).ToString("X2")} Byte {output:X2}");
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

    public static string? ReadString(byte* b, int maxLength = 0, bool nullIsEmpty = true) {
        if(b == null) return nullIsEmpty ? string.Empty : null;
        if(maxLength > 0) return Encoding.UTF8.GetString(b, maxLength).Split('\0')[0];
        var l = 0;
        while(b[l] != 0) l++;
        return Encoding.UTF8.GetString(b, l);
    }
}
