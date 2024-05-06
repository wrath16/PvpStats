using PvpStats.Types.Player;
using System;

namespace PvpStats.Types.Match;
public class PvpPlayer : IEquatable<PvpPlayer>{
    public PlayerAlias Name { get; set; }
    public Job? Job { get; set; }

    public PvpPlayer(PlayerAlias name, Job? job) {
        Name = name;
        Job = job;
    }
    public bool Equals(PvpPlayer? other) {
        if(other is null) {
            return false;
        }
        return Name.Equals(other.Name);
    }

    public override int GetHashCode() {
        return Name.GetHashCode();
    }
}
