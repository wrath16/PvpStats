using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.Match;
public class FrontlineTeamStats {
    public int? Placement { get; set; } = null;
    public int TotalPoints { get; set; }
    public int KillPoints { get; set; }
    public int DeathPointLosses { get; set; }
    public int OvooPoints { get; set; }
}
