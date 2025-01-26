using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.Event.RivalWings;
public class StructureHealthEvent : MatchEvent {

    public int Health { get; set; }

    public StructureHealthEvent(DateTime timestamp, int health) : base(timestamp) {
        Health = health;
    }

    public StructureHealthEvent(int health) : base(DateTime.Now) {
        Health = health;
    }
}
