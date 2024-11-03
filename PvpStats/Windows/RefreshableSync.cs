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

    //public override async Task Refresh(List<T> matches, List<T> additions, List<T> removals) {
    //    await RefreshQueue.QueueDataOperation(async () => {
    //        MatchesProcessed = 0;
    //        RefreshProgress = 0;
    //        Stopwatch s1 = Stopwatch.StartNew();
    //        try {
    //            if(removals.Count * 2 >= _matches.Count) {
    //                //force full build
    //                Reset();
    //                MatchesTotal = matches.Count;
    //                await ProcessMatches(matches);
    //            } else {
    //                MatchesTotal = removals.Count + additions.Count;
    //                await ProcessMatches(removals, true);
    //                await ProcessMatches(additions);
    //            }
    //            PostRefresh(matches, additions, removals);
    //            _matches = matches;
    //        } finally {
    //            s1.Stop();
    //            Plugin.Log2.Debug(string.Format("{0,-25}: {1,4} ms", $"{Name} Refresh", s1.ElapsedMilliseconds.ToString()));
    //            MatchesProcessed = 0;
    //        }
    //    });
    //}
}
