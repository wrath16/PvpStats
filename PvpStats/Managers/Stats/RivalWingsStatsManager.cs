using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Managers.Stats;
internal class RivalWingsStatsManager : StatsManager<RivalWingsMatch> {
    public RivalWingsStatsManager(Plugin plugin) : base(plugin, plugin.RWCache) {
    }
}
