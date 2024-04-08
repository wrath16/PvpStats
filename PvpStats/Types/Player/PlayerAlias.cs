using Dalamud.Utility;
using LiteDB;
using PvpStats.Types.Match;
using System;

namespace PvpStats.Types.Player;
public class PlayerAlias : IEquatable<PlayerAlias>, IEquatable<CrystallineConflictPlayer>, IEquatable<string>, IComparable<PlayerAlias> {
    public string Name { get; set; } = "";
    public string HomeWorld { get; set; } = "";
    //[BsonId]
    public string FullName => $"{Name} {HomeWorld}";

    public static explicit operator PlayerAlias(string s) {
        s = s.Trim();
        var splitString = s.Split(" ");
        if(splitString is null || splitString.Length != 3) {
            throw new ArgumentException($"Cannot convert string to player alias: {s}");
        }
        return new PlayerAlias(splitString[0] + " " + splitString[1], splitString[2]);
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
