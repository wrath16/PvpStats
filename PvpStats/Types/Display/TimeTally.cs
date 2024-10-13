using System;
using System.Threading;

namespace PvpStats.Types.Display;
internal class TimeTally {
    public long Ticks;

    public void AddTime(TimeSpan time) {
        Interlocked.Add(ref Ticks, time.Ticks);
    }

    public void RemoveTime(TimeSpan time) {
        Interlocked.Add(ref Ticks, -time.Ticks);
    }

    public TimeSpan ToTimeSpan() {
        return TimeSpan.FromTicks(Ticks);
    }

}
