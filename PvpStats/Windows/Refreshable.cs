﻿using PvpStats.Types.Match;
using PvpStats.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows;
internal abstract class Refreshable<T> where T : PvpMatch {

    public virtual string Name => "";

    //private readonly SemaphoreSlim _refreshProgressLock = new(1);
    protected DataQueue RefreshQueue { get; private set; } = new();

    public bool RefreshActive { get; set; }

    private double _refreshProgress;
    public double RefreshProgress {
        get {
            return _refreshProgress;
        }
        set {
            Interlocked.Exchange(ref _refreshProgress, value);
        }
    }

    private int _matchesProcessed;
    public int MatchesProcessed {
        get {
            return _matchesProcessed;
        }
        protected set {
            Interlocked.Exchange(ref _matchesProcessed, value);
        }
    }
    private int _matchesTotal;
    public int MatchesTotal {
        get {
            return _matchesTotal;
        }
        protected set {
            Interlocked.Exchange(ref _matchesTotal, value);
        }
    }

    protected List<T> _matches = [];

    protected abstract void Reset();
    protected abstract void ProcessMatch(T match, bool remove = false);
    protected abstract void PostRefresh(List<T> matches, List<T> additions, List<T> removals);

    public async Task Refresh(List<T> matches, List<T> additions, List<T> removals) {
        var task = RefreshQueue.QueueDataOperation(async () => {
            RefreshActive = true;
            MatchesProcessed = 0;
            RefreshProgress = 0;
            Stopwatch s1 = Stopwatch.StartNew();
            try {
                await RefreshInner(matches, additions, removals);
            } finally {
                s1.Stop();
                Plugin.Log2.Debug(string.Format("{0,-50}: {1,4} ms", $"{Name} Refresh", s1.ElapsedMilliseconds.ToString()));
                MatchesProcessed = 0;
                RefreshActive = false;
            }
        });
        await task.Result;
    }

    //full refresh
    public async Task Refresh() {
        await Refresh(_matches, _matches, _matches);
    }

    protected virtual async Task RefreshInner(List<T> matches, List<T> additions, List<T> removals) {
        if(removals.Count * 2 >= _matches.Count) {
            //force full build
            Reset();
            MatchesTotal = matches.Count;
            await ProcessMatches(matches);
        } else {
            MatchesTotal = removals.Count + additions.Count;
            var removeTask = ProcessMatches(removals, true);
            var addTask = ProcessMatches(additions);
            await Task.WhenAll([removeTask, addTask]);
        }
        PostRefresh(matches, additions, removals);
        _matches = matches;
    }

    protected virtual async Task ProcessMatches(List<T> matches, bool remove = false) {
        List<Task> matchTasks = [];
        matches.ForEach(x => {
            var t = new Task(() => {
                ProcessMatch(x, remove);
                RefreshProgress = (float)MatchesProcessed++ / MatchesTotal;
            });
            matchTasks.Add(t);
            t.Start();
        });
        try {
            await Task.WhenAll(matchTasks);
        } catch(Exception e) {
            Plugin.Log2.Error(e, "Process Match Error");
        }
    }
}
