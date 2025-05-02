using PvpStats.Types.Event;
using PvpStats.Types.Event.CrystallineConflict;
using System.Collections.Generic;

namespace PvpStats.Types.Match.Timeline;
internal class CrystallineConflictMatchTimeline : PvpMatchTimeline {

    public List<GenericMatchEvent>? MapEvents { get; set; }
    public List<ProgressEvent>? CrystalPosition { get; set; }
    public Dictionary<CrystallineConflictTeamName, List<ProgressEvent>>? TeamProgress { get; set; }
    public Dictionary<CrystallineConflictTeamName, List<ProgressEvent>>? TeamMidProgress { get; set; }
    public List<KnockoutEvent>? Kills {  get; set; }
    public List<ActionEvent>? LimitBreakCasts { get; set; }
    public List<ActionEvent>? LimitBreakEffects { get; set; }

    public CrystallineConflictMatchTimeline() : base() {

    }

}
