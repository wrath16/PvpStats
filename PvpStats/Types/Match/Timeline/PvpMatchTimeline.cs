using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types.Match.Timeline;
public class PvpMatchTimeline {
    [BsonId]
    public ObjectId Id { get; init; }

    public DateTime Time { get; init; }

    public PvpMatchTimeline() {
        Id = new();
        Time = DateTime.Now;
    }
}
