using LiteDB;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace PvpStats.Services;
internal class StorageService {
    private const string CCTable = "ccmatch";

    private Plugin _plugin;
    private SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
    private LiteDatabase Database { get; init; }

    internal StorageService(Plugin plugin, string path) {
        _plugin = plugin;
        Database = new LiteDatabase(path);

        //set mapper properties
        BsonMapper.Global.EmptyStringToNull = false;

        //create indices
        var ccMatchCollection = GetCCMatches();
        ccMatchCollection.EnsureIndex(m => m.DutyStartTime);
        ccMatchCollection.EnsureIndex(m => m.MatchType);
    }

    public void Dispose() {
        Database.Dispose();
    }
    internal ILiteCollection<CrystallineConflictMatch> GetCCMatches() {
        return Database.GetCollection<CrystallineConflictMatch>(CCTable);
    }

    internal void AddCCMatch(CrystallineConflictMatch match, bool toSave = true) {
        LogUpdate(match.Id.ToString());
        WriteToDatabase(() => GetCCMatches().Insert(match), toSave);
    }

    internal void AddCCMatches(IEnumerable<CrystallineConflictMatch> matches, bool toSave = true) {
        LogUpdate(null, matches.Count());
        WriteToDatabase(() => GetCCMatches().Insert(matches.Where(m => m.Id != null)), toSave);

    }

    internal void UpdateCCMatch(CrystallineConflictMatch match, bool toSave = true) {
        LogUpdate(match.Id.ToString());
        WriteToDatabase(() => GetCCMatches().Update(match), toSave);
    }

    internal void UpdateCCMatches(IEnumerable<CrystallineConflictMatch> matches, bool toSave = true) {
        LogUpdate(null, matches.Count());
        WriteToDatabase(() => GetCCMatches().Update(matches.Where(m => m.Id != null)), toSave);
    }

    private void LogUpdate(string? id = null, int count = 0) {
        var callingMethod = new StackFrame(2, true).GetMethod();
        var writeMethod = new StackFrame(1, true).GetMethod();

        _plugin.Log.Verbose(string.Format("Invoking {0,-25} {2,-30}{3,-30} Caller: {1,-70}",
            writeMethod?.Name, $"{callingMethod?.DeclaringType?.ToString() ?? ""}.{callingMethod?.Name ?? ""}", id != null ? $"ID: {id}" : "", count != 0 ? $"Count: {count}" : ""));
    }

    ////all writes are asynchronous for performance reasons
    //private Task AsyncWriteToDatabase(Func<object> action, bool toSave = true) {
    //    return Task.Run(async () => {
    //        try {
    //            await _dbLock.WaitAsync();
    //            action.Invoke();
    //        }
    //        finally {
    //            _dbLock.Release();
    //        }
    //    });
    //}

    //synchronous write
    private void WriteToDatabase(Func<object> action, bool toSave = true) {
        try {
            _dbLock.Wait();
            action.Invoke();
            if(toSave) {
                _plugin.WindowManager.Refresh();
            }
        } finally {
            _dbLock.Release();
        }
    }
}
