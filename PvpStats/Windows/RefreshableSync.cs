using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PvpStats.Windows;
internal abstract class RefreshableSync<T> : Refreshable<T> where T : PvpMatch {
    protected override Task ProcessMatches(List<T> matches, bool remove = false) {
        try {
            matches.ForEach((x) => {
                ProcessMatch(x, remove);
                RefreshProgress = (float)MatchesProcessed++ / MatchesTotal;
            });
        } catch(Exception e) {
            Plugin.Log2.Error(e, "Process Match Error");
        }
        return Task.CompletedTask;
    }
}
