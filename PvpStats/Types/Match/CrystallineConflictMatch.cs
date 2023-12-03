using LiteDB;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;

namespace PvpStats.Types.Match;

public class CrystallineConflictMatch {

    [BsonId]
    public ObjectId Id { get; init; }
    public int Version { get; init; } = 0;
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }

    public DateTime DutyStartTime { get; init; }
    public DateTime? MatchStartTime { get; set; }
    public DateTime? MatchEndTime { get; set; }

    public CrystallineConflictMatchType MatchType { get; set; }
    //should this be id only?
    //public PvpDuty Duty { get; set; }
    public CrystallineConflictMap Arena { get; set; }
    public uint DutyId { get; set; }
    public uint TerritoryId { get; set; }

    public bool NeedsPlayerNameValidation { get; set; }
    public PlayerAlias? LocalPlayer { get; set; }
    //public CrystallineConflictTeam? FirstTeam { get; set; }
    //public CrystallineConflictTeam? SecondTeam { get; set; }
    public Dictionary<CrystallineConflictTeamName, CrystallineConflictTeam> Teams { get; set; } = new();
    public CrystallineConflictTeamName? MatchWinner { get; set; }

    //public bool IsCountdown { get; set; }
    public TimeSpan MatchTimer { get; set; }
    public bool IsOvertime { get; set; }

    public List<ChatMessage> ChatLog { get; set; } = new();

    //stats results...

    //this might have performance impact if accessed frequently
    [BsonIgnore]
    public CrystallineConflictTeam? LocalPlayerTeam {
        get {
            foreach (var team in Teams) {
                foreach (var player in team.Value.Players) {
                    if (LocalPlayer.Equals(player)) {
                        return team.Value;
                    }
                }
            }
            return null;
        }
    }

    [BsonIgnore]
    public CrystallineConflictPlayer? LocalPlayerTeamMember {
        get {
            foreach (var team in Teams) {
                foreach (var player in team.Value.Players) {
                    if (player.Equals(LocalPlayer)) {
                        return player;
                    }
                }
            }
            return null;
        }
    }

    [BsonIgnore]
    public bool IsCommenced => MatchStartTime != null;

    public CrystallineConflictMatch() {
        Id = new();
        DutyStartTime = DateTime.Now;
    }

}
