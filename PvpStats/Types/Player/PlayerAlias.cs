﻿using Dalamud.Utility;
using LiteDB;
using PvpStats.Types.Match;
using System;
using System.Text.RegularExpressions;

namespace PvpStats.Types.Player;
public class PlayerAlias : IEquatable<PlayerAlias>, IEquatable<PvpPlayer>, IEquatable<CrystallineConflictPlayer>, IEquatable<string>, IComparable<PlayerAlias> {
    public string Name { get; set; } = "";
    public string HomeWorld { get; set; } = "";
    [BsonIgnore]
    public string FullName => $"{Name} {HomeWorld}";

    public static PlayerAlias Unknown => new("_UNKNOWN_", "_UNKNOWN_");

    public static explicit operator PlayerAlias(string s) {
        s = s.Trim();
        var homeWorld = Regex.Match(s, @"\b[\S]+$").Value.Trim();
        var playerName = Regex.Match(s, @".*(?=\s[\S]+$)").Value.Trim();
        if(s.IsNullOrEmpty()) {
            throw new InvalidCastException("Cannot convert empty string to player alias!");
        }
        if(homeWorld.IsNullOrEmpty() || playerName.IsNullOrEmpty()) {
            return new PlayerAlias(s, "");
        }
        return new PlayerAlias(playerName, homeWorld);
    }

    public static implicit operator string(PlayerAlias p) => $"{p.Name} {p.HomeWorld}";

    public static bool IsPlayersAlias(string s) {
        try {
            PlayerAlias alias = (PlayerAlias)s;
            return true;
        } catch(ArgumentException) {
            return false;
        }
    }

    [BsonIgnore]
    public string FirstName {
        get {
            if(Name.IsNullOrEmpty()) {
                return "";
            }
            string[] split = Name.Split(" ");
            if(split.Length > 0) {
                return split[0];
            }
            return "";
        }
    }

    [BsonIgnore]
    public string LastName {
        get {
            if(Name.IsNullOrEmpty()) {
                return "";
            }
            string[] split = Name.Split(" ");
            if(split.Length > 1) {
                return split[1];
            }
            return "";
        }
    }

    public PlayerAlias(string name, string world) {
        Name = name;
        HomeWorld = world;
    }

    public bool Equals(PlayerAlias? other) {
        if(other is null) {
            return false;
        }
        return FullName.Equals(other.FullName, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(PvpPlayer? other) {
        if(other is null) {
            return false;
        }
        return FullName.Equals(other.Name.FullName, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(CrystallineConflictPlayer? other) {
        if(other is null) {
            return false;
        }
        return FullName.Equals(other.Alias.FullName, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(string? other) {
        if(other is null) {
            return false;
        }
        return FullName.Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator ==(PlayerAlias? a, PlayerAlias? b) {
        if(a is null) {
            return b is null;
        }
        return a.Equals(b);
    }

    public static bool operator !=(PlayerAlias? a, PlayerAlias? b) {
        if(a is null) {
            return b is not null;
        }
        return !a.Equals(b);
    }

    public override string ToString() {
        return FullName;
    }

    public override int GetHashCode() {
        return string.GetHashCode(FullName);
    }

    public int CompareTo(PlayerAlias? other) {
        return FullName.CompareTo(other?.FullName);
    }
}
