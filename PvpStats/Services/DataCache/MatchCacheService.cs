using LiteDB;
using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Services.DataCache;
internal abstract class MatchCacheService<T> where T : PvpMatch {
    protected readonly Plugin _plugin;
    //private readonly StorageService _storage;
    private List<T> _matches = [];

    internal bool CachingEnabled { get; private set; }
    internal ReadOnlyCollection<T> Matches {
        get {
            if(CachingEnabled) {
                return _matches.AsReadOnly();
            } else {
                return GetFromStorage().ToList().AsReadOnly();
            }
        }
    }

    protected abstract IEnumerable<T> GetFromStorage();
    protected abstract Task AddToStorage(T match);
    protected abstract Task UpdateToStorage(T match);
    protected abstract Task UpdateManyToStorage(IEnumerable<T> matches);

    internal MatchCacheService(Plugin plugin) {
        _plugin = plugin;
    }

    internal void EnableCaching() {
        RebuildCache();
        CachingEnabled = true;
    }

    internal void DisableCaching() {
        ClearCache();
        CachingEnabled = false;
    }

    private void ClearCache() {
        _matches = [];
    }

    private void RebuildCache() {
        _matches = GetFromStorage().ToList().ToList();
    }

    internal async Task AddMatch(T match) {
        if(CachingEnabled) {
            _matches.Add(match);
        }
        await AddToStorage(match);
    }

    internal async Task UpdateMatch(T match) {
        if(CachingEnabled && _matches.RemoveAll(x => x.Id == match.Id) > 0) {
            _matches.Add(match);
        }
        await UpdateToStorage(match);
    }

    internal async Task UpdateMatches(IEnumerable<T> matches) {
        if(CachingEnabled) {
            foreach(var match in matches) {
                if(_matches.RemoveAll(x => x.Id == match.Id) > 0) {
                    _matches.Add(match);
                }
            }
        }
        await UpdateManyToStorage(matches);
    }
}
