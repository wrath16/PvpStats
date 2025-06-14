using LiteDB;
using PvpStats.Types.Action;

namespace PvpStats.Types.Display;
internal class FlattenedActionAnalytics {
    public int Casts { get; set; }
    public int Targets { get; set; }
    public int StatusHits { get; set; }
    public int StatusMisses { get; set; }
    public long Damage { get; set; }
    public long ExemptDamage { get; set; }
    public long Heal { get; set; }
    public long ExemptHeal { get; set; }
    public long MPDrain { get; set; }
    public long MPGain { get; set; }

    public float AverageDamage => (float)Damage / Casts;
    public float AverageHeal => (float)Heal / Casts;
    public float AverageMPDrain => (float)MPDrain / Casts;
    public float AverageMPGain => (float)MPGain / Casts;
    public float StatusEffectiveness => (float)StatusHits / (StatusHits + StatusMisses);
    public float AverageTargets => (float)Targets / Casts;

    public static FlattenedActionAnalytics operator +(FlattenedActionAnalytics a, ActionAnalytics b) {
        //var c = (a as ActionAnalytics) + b;
        //return c as FlattenedActionAnalytics;

        return new FlattenedActionAnalytics() {
            //Impacts = a.Impacts + b.Impacts,
            StatusHits = a.StatusHits + b.StatusHits,
            StatusMisses = a.StatusMisses + b.StatusMisses,
            Damage = a.Damage + b.Damage,
            ExemptDamage = a.ExemptDamage + b.ExemptDamage,
            Heal = a.Heal + b.Heal,
            ExemptHeal = a.ExemptHeal + b.ExemptHeal,
            MPDrain = a.MPDrain + b.MPDrain,
            MPGain = a.MPGain + b.MPGain,
        };
    }
}
