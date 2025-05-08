using LiteDB;
using System;
using System.Collections.Generic;

namespace PvpStats.Types.Match.Timeline;
public class PvpMatchTimeline {
    [BsonId]
    public ObjectId Id { get; init; }

    public DateTime Time { get; init; }

    public Dictionary<uint, string>? ActionIdLookup { get; init; }
    public Dictionary<uint, (string Singular, sbyte Article)>? BNPCNameLookup { get; init; }

    public PvpMatchTimeline() {
        Id = new();
        Time = DateTime.Now;
    }
}
