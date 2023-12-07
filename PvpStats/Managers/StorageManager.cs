using LiteDB;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Managers;
internal class StorageManager : IDisposable {

    private const string CCTable = "ccmatch";

    private Plugin _plugin;
    private SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
    private LiteDatabase Database { get; init; }

    internal StorageManager(Plugin plugin, string path) {
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
#if DEBUG
        _plugin.Log.Debug("disposing storage manager");
#endif
        Database.Dispose();
    }
    internal ILiteCollection<CrystallineConflictMatch> GetCCMatches() {
        return Database.GetCollection<CrystallineConflictMatch>(CCTable);
    }

    internal Task AddCCMatch(CrystallineConflictMatch match) {
        return AsyncWriteToDatabase(() => GetCCMatches().Insert(match));
    }

    internal Task AddCCMatches(IEnumerable<CrystallineConflictMatch> matches) {
        return AsyncWriteToDatabase(() => GetCCMatches().Insert(matches));
    }

    internal Task UpdateCCMatch(CrystallineConflictMatch match) {
        return AsyncWriteToDatabase(() => GetCCMatches().Update(match));
    }

    internal Task UpdateCCMatches(IEnumerable<CrystallineConflictMatch> matches) {
        return AsyncWriteToDatabase(() => GetCCMatches().Update(matches.Where(m => m.Id != null)));
    }

    //all writes are asynchronous for performance reasons
    private Task AsyncWriteToDatabase(Func<object> action, bool toSave = true) {
        return Task.Run(async () => {
            try {
                await _dbLock.WaitAsync();
                action.Invoke();
                if (toSave) {
                    _plugin.Refresh();
                }
            }
            finally {
                _dbLock.Release();
            }
        });
    }
}
