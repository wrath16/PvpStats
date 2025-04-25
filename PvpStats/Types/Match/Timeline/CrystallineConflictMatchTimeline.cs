using PvpStats.Types.Event.CrystallineConflict;
using PvpStats.Types.Event.Frontline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.Match.Timeline;
internal class CrystallineConflictMatchTimeline : PvpMatchTimeline {

    List<ProgressEvent>? CrystalPosition { get; set; }
    public Dictionary<CrystallineConflictTeamName, List<ProgressEvent>>? TeamProgress { get; set; }
    public Dictionary<CrystallineConflictTeamName, List<ProgressEvent>>? TeamMidProgress { get; set; }
    public CrystallineConflictMatchTimeline() : base() {

    }

}
