using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PvpStats.Services;
internal class DataCacheService {

    private readonly Plugin _plugin;
    private readonly StorageService _storage;
    private List<CrystallineConflictMatch> _ccMatches = [];

    internal bool CachingEnabled { get; private set; }
    internal ReadOnlyCollection<CrystallineConflictMatch> CCMatches { get {
            if(CachingEnabled) {
                return _ccMatches.AsReadOnly();
            } else {
                return _storage.GetCCMatches().Query().ToList().AsReadOnly();
            }
        }
    } 

    internal DataCacheService(Plugin plugin) {
        _plugin = plugin;
        _storage = plugin.Storage;

        //temp
        EnableCaching();
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
        _ccMatches = [];
    }

    private void RebuildCache() {
        _ccMatches = _plugin.Storage.GetCCMatches().Query().ToList();
    }

    internal async Task AddCCMatch(CrystallineConflictMatch match) {
        if(CachingEnabled) {
            _ccMatches.Add(match);
        }
        await _storage.AddCCMatch(match);
    }

    internal async Task UpdateCCMatch(CrystallineConflictMatch match) {
        if(CachingEnabled && _ccMatches.RemoveAll(x => x.Id == match.Id) > 0) {
            _ccMatches.Add(match);
            //var cachedMatch = _ccMatches.First(x => x.Id == match.Id);
            //cachedMatch = match;
        }
        await _storage.UpdateCCMatch(match);
    }

    internal async Task UpdateCCMatches(IEnumerable<CrystallineConflictMatch> matches) {
        if(CachingEnabled) {
            foreach(var match in matches) {
                if(_ccMatches.RemoveAll(x => x.Id == match.Id) > 0) {
                    _ccMatches.Add(match);
                }
            }
        }
        await _storage.UpdateCCMatches(matches);
    }
}
