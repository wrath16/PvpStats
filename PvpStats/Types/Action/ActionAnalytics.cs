using LiteDB;

namespace PvpStats.Types.Action;
public class ActionAnalytics {

    public int Impacts { get; set; }
    //public int FriendlyTargets { get; set; }
    //public int HostileTargets { get; set; }
    //public int UnknownTargets { get; set; }
    public int StatusHits { get; set; }
    public int StatusMisses { get; set; }
    public long Damage { get; set; }
    //this damage is not counted in the scoreboard
    public long ExemptDamage { get; set; }
    //public long ReflectDamage { get; set; }
    public long Heal { get; set; }
    //this heal is not counted in the scoreboard
    public long ExemptHeal { get; set; }
    public long MPDrain { get; set; }
    public long MPGain { get; set; }

    //[BsonIgnore]
    //public int TotalTargets => FriendlyTargets + HostileTargets + UnknownTargets;
    //[BsonIgnore]
    //public float AverageTargets => (float)TotalTargets / Casts;
    //[BsonIgnore]
    //public float AverageFriendlyTargets => (float)FriendlyTargets / Casts;
    //[BsonIgnore]
    //public float AverageHostileTargets => (float)HostileTargets / Casts;
    [BsonIgnore]
    public float AverageDamage => (float)Damage / Impacts;
    //[BsonIgnore]
    //public float AverageReflectDamage => (float)ReflectDamage / Impacts;
    [BsonIgnore]
    public float AverageHeal => (float)Heal / Impacts;
    [BsonIgnore]
    public float AverageMPDrain => (float)MPDrain / Impacts;
    [BsonIgnore]
    public float AverageMPGain => (float)MPGain / Impacts;
    [BsonIgnore]
    public float StatusEffectiveness => (float)StatusHits / (StatusHits + StatusMisses);

    public static ActionAnalytics operator +(ActionAnalytics a, ActionAnalytics b) {
        return new ActionAnalytics() {
            Impacts = a.Impacts + b.Impacts,
            StatusHits = a.StatusHits + b.StatusHits,
            StatusMisses = a.StatusMisses + b.StatusMisses,
            //FriendlyTargets = a.FriendlyTargets + b.FriendlyTargets,
            //HostileTargets = a.HostileTargets + b.HostileTargets,
            //UnknownTargets = a.UnknownTargets + b.UnknownTargets,
            Damage = a.Damage + b.Damage,
            ExemptDamage = a.ExemptDamage + b.ExemptDamage,
            //ReflectDamage = a.ReflectDamage + b.ReflectDamage,
            Heal = a.Heal + b.Heal,
            ExemptHeal = a.ExemptHeal + b.ExemptHeal,
            MPDrain = a.MPDrain + b.MPDrain,
            MPGain = a.MPGain + b.MPGain,
        };
    }
}
