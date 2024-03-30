using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.Player;
public class PlayerAliasLink {

    [BsonId]
    public ObjectId Id { get; init; }
    public bool IsAuto { get; set; }
    public bool IsUnlink { get; set; }
    public PlayerAlias CurrentAlias { get; set; }
    public List<PlayerAlias> LinkedAliases { get; set; } = new();

    public PlayerAliasLink() {
        Id = new ObjectId();
    }
}
