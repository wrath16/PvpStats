using LiteDB;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Services;
internal class StorageService {
    private const string CCTable = "ccmatch";
    private const string AutoPlayerLinksTable = "playerlinks_auto";
    private const string ManualPlayerLinksTable = "playerlinks_manual";

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

    internal async Task AddCCMatch(CrystallineConflictMatch match, bool toSave = true) {
        LogUpdate(match.Id.ToString());
        await WriteToDatabase(() => GetCCMatches().Insert(match), toSave);
    }

    internal async Task AddCCMatches(IEnumerable<CrystallineConflictMatch> matches, bool toSave = true) {
        LogUpdate(null, matches.Count());
        await WriteToDatabase(() => GetCCMatches().Insert(matches.Where(m => m.Id != null)), toSave);
    }

    internal async Task UpdateCCMatch(CrystallineConflictMatch match, bool toSave = true) {
        LogUpdate(match.Id.ToString());
        await WriteToDatabase(() => GetCCMatches().Update(match), toSave);
    }

    internal async Task UpdateCCMatches(IEnumerable<CrystallineConflictMatch> matches, bool toSave = true) {
        LogUpdate(null, matches.Count());
        await WriteToDatabase(() => GetCCMatches().Update(matches.Where(m => m.Id != null)), toSave);
    }

    internal ILiteCollection<PlayerAliasLink> GetAutoLinks() {
        return Database.GetCollection<PlayerAliasLink>(AutoPlayerLinksTable);
    }

    internal async Task SetAutoLinks(IEnumerable<PlayerAliasLink> links, bool toSave = false) {
        LogUpdate(null, links.Count());
        _plugin.Storage.GetAutoLinks().DeleteAll();
        await WriteToDatabase(() => GetAutoLinks().Insert(links.Where(x => x.Id != null)), toSave);
    }

    internal ILiteCollection<PlayerAliasLink> GetManualLinks() {
        return Database.GetCollection<PlayerAliasLink>(ManualPlayerLinksTable);
    }

    internal async Task SetManualLinks(IEnumerable<PlayerAliasLink> links, bool toSave = false) {
        LogUpdate(null, links.Count());
        //kind of hacky
        GetManualLinks().DeleteAll();
        await WriteToDatabase(() => GetManualLinks().Insert(links.Where(x => x.Id != null)), toSave);
    }

    private void LogUpdate(string? id = null, int count = 0) {
        var callingMethod = new StackFrame(2, true).GetMethod();
        var writeMethod = new StackFrame(1, true).GetMethod();

        _plugin.Log.Verbose(string.Format("Invoking {0,-25} {2,-30}{3,-30} Caller: {1,-70}",
            writeMethod?.Name, $"{callingMethod?.DeclaringType?.ToString() ?? ""}.{callingMethod?.Name ?? ""}", id != null ? $"ID: {id}" : "", count != 0 ? $"Count: {count}" : ""));
    }

    private async Task WriteToDatabase(Func<object> action, bool toSave = true) {
        try {
            await _dbLock.WaitAsync();
            action.Invoke();
            if(toSave) {
                await _plugin.WindowManager.Refresh();
            }
        } finally {
            _dbLock.Release();
        }
    }
}
