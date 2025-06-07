using LiteDB;

namespace PvpStats.Types.Action;
public class ActionAnalytics {

    public int Casts { get; set; }
    public int FriendlyTargets { get; set; }
    public int HostileTargets { get; set; }
    public int UnknownTargets { get; set; }
    public int StatusHits { get; set; }
    public int StatusMisses { get; set; }
    public long Damage { get; set; }
    public long ReflectDamage { get; set; }
    public long Overkill { get; set; }
    public long Heal { get; set; }
    public long GhostHeal { get; set; }
    public long Overheal { get; set; }
    public long MPDrain { get; set; }
    public long MPOverdrain { get; set; }
    public long MPGain { get; set; }
    public long MPOvergain { get; set; }

    [BsonIgnore]
    public int TotalTargets => FriendlyTargets + HostileTargets + UnknownTargets;
    [BsonIgnore]
    public float AverageTargets => (float)TotalTargets / Casts;
    [BsonIgnore]
    public float AverageFriendlyTargets => (float)FriendlyTargets / Casts;
    [BsonIgnore]
    public float AverageHostileTargets => (float)HostileTargets / Casts;
    [BsonIgnore]
    public float AverageDamage => (float)Damage / Casts;
    [BsonIgnore]
    public float AverageReflectDamage => (float)ReflectDamage / Casts;
    [BsonIgnore]
    public float AverageOverkill => (float)Overkill / Casts;
    [BsonIgnore]
    public float AverageHeal => (float)Heal / Casts;
    [BsonIgnore]
    public float AverageOverheal => (float)Overheal / Casts;
    [BsonIgnore]
    public float AverageMPDrain => (float)MPDrain / Casts;
    [BsonIgnore]
    public float AverageMPOverdrain => (float)MPOverdrain / Casts;
    [BsonIgnore]
    public float AverageMPGain => (float)MPGain / Casts;
    [BsonIgnore]
    public float AverageMPOvergain => (float)MPOvergain / Casts;
    [BsonIgnore]
    public float StatusEffectiveness => (float)StatusHits / (StatusHits + StatusMisses);

    public static ActionAnalytics operator +(ActionAnalytics a, ActionAnalytics b) {
        return new ActionAnalytics() {
            Casts = a.Casts + b.Casts,
            StatusHits = a.StatusHits + b.StatusHits,
            StatusMisses = a.StatusMisses + b.StatusMisses,
            FriendlyTargets = a.FriendlyTargets + b.FriendlyTargets,
            HostileTargets = a.HostileTargets + b.HostileTargets,
            UnknownTargets = a.UnknownTargets + b.UnknownTargets,
            Damage = a.Damage + b.Damage,
            ReflectDamage = a.ReflectDamage + b.ReflectDamage,
            Overkill = a.Overkill + b.Overkill,
            Heal = a.Heal + b.Heal,
            GhostHeal = a.GhostHeal + b.GhostHeal,
            Overheal = a.Overheal + b.Overheal,
            MPDrain = a.MPDrain + b.MPDrain,
            MPOverdrain = a.MPOverdrain + b.MPOverdrain,
            MPGain = a.MPGain + b.MPGain,
            MPOvergain = a.MPOvergain + b.MPOvergain,
        };
    }
}
