using LiteDB;
using System;

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
