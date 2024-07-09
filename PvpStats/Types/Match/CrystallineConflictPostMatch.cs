﻿using PvpStats.Types.Display;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;

namespace PvpStats.Types.Match;
public class CrystallineConflictPostMatch {
    public CrystallineConflictTeamName MatchWinner { get; set; }
    public TimeSpan MatchDuration { get; set; }
    public Dictionary<CrystallineConflictTeamName, CrystallineConflictPostMatchTeam> Teams { get; set; } = new();
    public PlayerRank? RankBefore { get; set; }
    public PlayerRank? RankAfter { get; set; }
}

public class CrystallineConflictPostMatchTeam {
    public CrystallineConflictTeamName TeamName { get; set; }
    public float Progress { get; set; }
    public CrystallineConflictPostMatchRow TeamStats { get; set; } = new();
    public List<CrystallineConflictPostMatchRow> PlayerStats { get; set; } = new();
}

public class CrystallineConflictPostMatchRow {
    public PlayerAlias? Player { get; set; }
    public CrystallineConflictTeamName? Team { get; set; }
    public Job? Job { get; set; }
    public PlayerRank? PlayerRank { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }

    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int HPRestored { get; set; }

    public TimeSpan TimeOnCrystal { get; set; }

    public CCScoreboard ToScoreboard() {
        return new CCScoreboard() {
            Kills = Kills,
            Deaths = Deaths,
            Assists = Assists,
            DamageDealt = DamageDealt,
            DamageTaken = DamageTaken,
            HPRestored = HPRestored,
            TimeOnCrystal = TimeOnCrystal
        };
    }
}
