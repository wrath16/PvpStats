using PvpStats.Types.Event.RivalWings;
using System.Collections.Generic;

namespace PvpStats.Types.Match.Timeline;
public class RivalWingsMatchTimeline : PvpMatchTimeline {

    //polled
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsStructure, List<StructureHealthEvent>>>? StructureHealths { get; set; }
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsMech, List<MechCountEvent>>>? MechCounts { get; set; }
    public Dictionary<int, List<AllianceSoaringEvent>>? AllianceStacks { get; set; }

    //triggered
    public List<MercClaimEvent>? MercClaims { get; set; }
    public List<MidClaimEvent>? MidClaims { get; set; }

    public RivalWingsMatchTimeline() : base() {

    }
}
