﻿using PvpStats.Types.Event.RivalWings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.Match.Timeline;
public class RivalWingsMatchTimeline : PvpMatchTimeline {

    //polled
    public Dictionary<RivalWingsTeamName, Dictionary<RivalWingsStructure, List<StructureHealthEvent>>>? StructureHealths { get; set; }

    //triggered
    public List<MercClaimEvent>? MercClaims { get; set; }
    public List<MidClaimEvent>? MidClaims { get; set; }

    public RivalWingsMatchTimeline() : base() {

    }
}
