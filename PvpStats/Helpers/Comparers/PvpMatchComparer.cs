using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PvpStats.Helpers.Comparers;
internal class PvpMatchComparer<T> : IEqualityComparer<T> where T : PvpMatch {

    public bool Equals(T? x, T? y) {
        return x?.Equals(y) ?? false;
    }

    public int GetHashCode([DisallowNull] T obj) {
        return obj.GetHashCode();
    }
}
