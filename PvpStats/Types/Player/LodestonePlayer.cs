using System;
using System.Collections.Generic;

namespace PvpStats.Types.Player;
public class LodestonePlayer {
    public ulong LodestoneId { get; init; }
    public PlayerAlias? LastKnownAlias { get; set; }
    public List<PlayerAlias> Aliases { get; set; } = new();
    public DateTime LastUpdated { get; set; }

    public LodestonePlayer(ulong lodestoneId) {
        LodestoneId = lodestoneId;
    }
}
