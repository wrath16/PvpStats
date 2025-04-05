using PvpStats.Types.Event.Frontline;
using System.Collections.Generic;

namespace PvpStats.Types.Match.Timeline;
internal class FrontlineMatchTimeline : PvpMatchTimeline {

    public Dictionary<FrontlineTeamName, List<TeamPointsEvent>>? TeamPoints { get; set; }
    public List<BattleHighLevelEvent>? SelfBattleHigh { get; set; }
    public FrontlineMatchTimeline() : base() {

    }
}
