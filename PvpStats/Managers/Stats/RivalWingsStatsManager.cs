using PvpStats.Types.Match;

namespace PvpStats.Managers.Stats;
internal class RivalWingsStatsManager : StatsManager<RivalWingsMatch> {
    public RivalWingsStatsManager(Plugin plugin) : base(plugin, plugin.RWCache) {
    }
}
