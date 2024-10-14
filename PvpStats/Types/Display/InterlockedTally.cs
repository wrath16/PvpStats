using System.Threading;

namespace PvpStats.Types.Display;
public class InterlockedTally {
    public int Tally = 0;

    public void Add(int value) {
        Interlocked.Add(ref Tally, value);
    }

    public void Subtract(int value) {
        Interlocked.Add(ref Tally, -value);
    }
}
