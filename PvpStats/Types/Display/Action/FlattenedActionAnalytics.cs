using PvpStats.Types.Action;

namespace PvpStats.Types.Display.Action;
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

    public FlattenedActionAnalytics() {

    }

    public FlattenedActionAnalytics(ActionAnalytics actionAnalytics) {
        Casts = actionAnalytics.Impacts;
        StatusHits = actionAnalytics.StatusHits;
        StatusMisses = actionAnalytics.StatusMisses;
        Damage = actionAnalytics.Damage;
        ExemptDamage = actionAnalytics.ExemptDamage;
        Heal = actionAnalytics.Heal;
        ExemptHeal = actionAnalytics.ExemptHeal;
        MPDrain = actionAnalytics.MPDrain;
        MPGain = actionAnalytics.MPGain;
    }

    public FlattenedActionAnalytics(CCScoreboardTally scoreboard) {
        Damage = scoreboard.DamageDealt;
        Heal = scoreboard.HPRestored;
    }

    public static FlattenedActionAnalytics operator +(FlattenedActionAnalytics a, FlattenedActionAnalytics b) {
        a.Casts += b.Casts;
        a.Targets += b.Targets;
        a.StatusHits += b.StatusHits;
        a.StatusMisses += b.StatusMisses;
        a.Damage += b.Damage;
        a.ExemptDamage += b.ExemptDamage;
        a.Heal += b.Heal;
        a.ExemptHeal += b.ExemptHeal;
        a.MPDrain += b.MPDrain;
        a.MPGain += b.MPGain;
        return a;
    }

    public static FlattenedActionAnalytics operator -(FlattenedActionAnalytics a, FlattenedActionAnalytics b) {
        a.Casts -= b.Casts;
        a.Targets -= b.Targets;
        a.StatusHits -= b.StatusHits;
        a.StatusMisses -= b.StatusMisses;
        a.Damage -= b.Damage;
        a.ExemptDamage -= b.ExemptDamage;
        a.Heal -= b.Heal;
        a.ExemptHeal -= b.ExemptHeal;
        a.MPDrain -= b.MPDrain;
        a.MPGain -= b.MPGain;
        return a;
    }

    public static FlattenedActionAnalytics operator +(FlattenedActionAnalytics a, ActionAnalytics b) {
        return new FlattenedActionAnalytics() {
            Casts = a.Casts,
            Targets = a.Targets,
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

    public static FlattenedActionAnalytics operator -(FlattenedActionAnalytics a, ActionAnalytics b) {
        return new FlattenedActionAnalytics() {
            Casts = a.Casts,
            Targets = a.Targets,
            StatusHits = a.StatusHits - b.StatusHits,
            StatusMisses = a.StatusMisses - b.StatusMisses,
            Damage = a.Damage - b.Damage,
            ExemptDamage = a.ExemptDamage - b.ExemptDamage,
            Heal = a.Heal - b.Heal,
            ExemptHeal = a.ExemptHeal - b.ExemptHeal,
            MPDrain = a.MPDrain - b.MPDrain,
            MPGain = a.MPGain - b.MPGain,
        };
    }
}
