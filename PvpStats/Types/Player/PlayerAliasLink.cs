using LiteDB;
using System.Collections.Generic;

namespace PvpStats.Types.Player;
public class PlayerAliasLink {

    [BsonId]
    public ObjectId Id { get; init; }
    public bool IsAuto { get; set; }
    public bool IsUnlink { get; set; }
    public PlayerAlias? CurrentAlias { get; set; }
    public List<PlayerAlias> LinkedAliases { get; set; } = new();

    public PlayerAliasLink() {
        Id = new ObjectId();
    }

    //public override int GetHashCode() {
    //    return Id.GetHashCode();
    //}
}
