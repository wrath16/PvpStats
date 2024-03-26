using LiteDB;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Types.Match;

public class CrystallineConflictMatch {

    [BsonId]
    public ObjectId Id { get; init; }
    public int Version { get; init; } = 0;
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsBookmarked { get; set; }

    public DateTime DutyStartTime { get; init; }
    public DateTime? MatchStartTime { get; set; }
    public DateTime? MatchEndTime { get; set; }
    [BsonIgnore]
    public TimeSpan? MatchDuration => MatchEndTime - MatchStartTime;
    public CrystallineConflictTeamName? MatchWinner { get; set; }
    public CrystallineConflictTeamName? OvertimeAdvantage { get; set; }

    public CrystallineConflictMatchType MatchType { get; set; }
    //should this be id only?
    //public PvpDuty Duty { get; set; }
    public CrystallineConflictMap? Arena { get; set; }
    public uint DutyId { get; set; }
    public uint TerritoryId { get; set; }

    public bool NeedsPlayerNameValidation { get; set; }
    public PlayerAlias? LocalPlayer { get; set; }
    public string? DataCenter { get; set; }
    //public CrystallineConflictTeam? FirstTeam { get; set; }
    //public CrystallineConflictTeam? SecondTeam { get; set; }
    public Dictionary<CrystallineConflictTeamName, CrystallineConflictTeam> Teams { get; set; } = new();

    //public bool IsCountdown { get; set; }
    public TimeSpan MatchTimer { get; set; }
    public bool IsOvertime { get; set; }

    //if this is PlayerAlias will not deserialize-_-
    public Dictionary<string, CrystallineConflictPlayer> IntroPlayerInfo { get; set; } = new();

    public List<ChatMessage> ChatLog { get; set; } = new();

    //stats results...
    public CrystallineConflictPostMatch? PostMatch { get; set; }

    //this might have performance impact if accessed frequently
    [BsonIgnore]
    public CrystallineConflictTeam? LocalPlayerTeam {
        get {
            foreach(var team in Teams) {
                foreach(var player in team.Value.Players) {
                    if(LocalPlayer is not null && LocalPlayer.Equals(player)) {
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
            foreach(var team in Teams) {
                foreach(var player in team.Value.Players) {
                    if(LocalPlayer is not null && LocalPlayer.Equals(player)) {
                        return player;
                    }
                }
            }
            return null;
        }
    }

    [BsonIgnore]
    public CrystallineConflictPostMatchRow? LocalPlayerStats {
        get {
            if(IsSpectated || PostMatch == null) return null;
            return PostMatch.Teams[LocalPlayerTeam!.TeamName].PlayerStats.Where(x => LocalPlayer!.Equals(x.Player)).First();
        }
    }

    [BsonIgnore]
    public bool IsWin => !IsSpectated && MatchWinner == LocalPlayerTeam!.TeamName;
    [BsonIgnore]
    public bool IsLoss => !IsWin && !IsSpectated && MatchWinner != null;

    [BsonIgnore]
    public bool IsSpectated => LocalPlayerTeam is null;

    [BsonIgnore]
    public bool IsCommenced => MatchStartTime != null;

    [BsonIgnore]
    public CrystallineConflictTeamName? MatchLoser {
        get {
            if(MatchWinner is null || Teams.Count > 2) {
                return null;
            }
            return Teams.Where(x => x.Key != MatchWinner).First().Key;
        }
    }

    [BsonIgnore]
    public float? WinnerProgress {
        get {
            if(MatchWinner is null) {
                return null;
            }
            return Teams[(CrystallineConflictTeamName)MatchWinner].Progress;
        }
    }

    [BsonIgnore]
    public float? LoserProgress {
        get {
            if(MatchLoser is null) {
                return null;
            }
            return Teams[(CrystallineConflictTeamName)MatchLoser].Progress;
        }
    }

    public CrystallineConflictMatch() {
        Id = new();
        DutyStartTime = DateTime.Now;
    }

    public override int GetHashCode() {
        return Id.GetHashCode();
    }
}
