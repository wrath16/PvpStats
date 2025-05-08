using System.Runtime.InteropServices;

namespace PvpStats.Types.ClientStruct.Action;

//shamelessly copy-pasted from Death Recap

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ActionEffect {
    public ActionEffectType EffectType;
    public byte Param0;
    public byte Param1;
    public byte Param2;
    public byte Flags1;
    public byte Flags2;
    public ushort Value;
}

public enum ActionEffectType : byte {
    Nothing = 0,
    Miss = 1,
    FullResist = 2,
    Damage = 3,
    Heal = 4,
    BlockedDamage = 5,
    ParriedDamage = 6,
    Invulnerable = 7,
    NoEffectText = 8,
    MpLoss = 10,
    MpGain = 11,
    TpLoss = 12,
    TpGain = 13,
    ApplyStatusEffectTarget = 14,
    ApplyStatusEffectSource = 15,
    RecoveredFromStatusEffect = 16,
    LoseStatusEffectTarget = 17,
    LoseStatusEffectSource = 18,
    StatusNoEffect = 20,
    ThreatPosition = 24,
    EnmityAmountUp = 25,
    EnmityAmountDown = 26,
    StartActionCombo = 27,
    Knockback = 33,
    Mount = 40,
    FullResistStatus = 55,
    Vfx = 59,
    Gauge = 60,
    PartialInvulnerable = 74,
    Interrupt = 75,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ActionEffectHeader {
    public uint AnimationTargetId;
    public uint Unknown1;
    public uint ActionId;
    public uint GlobalEffectCounter;
    public float AnimationLockTime;
    public uint Unknown2;
    public ushort HiddenAnimation;
    public ushort Rotation;
    public ushort ActionAnimationId;
    public byte Variation;
    public ActionEffectDisplayType EffectDisplayType;
    public byte Unknown3;
    public byte EffectCount;
    public ushort Unknown4;
}

public enum ActionEffectDisplayType : byte {
    HideActionName = 0,
    ShowActionName = 1,
    ShowItemName = 2,
    MountName = 13
}

public enum ActorControlCategory : ushort {
    Death = 0x6,
    CancelAbility = 0xF,
    Cast = 0x11,
    GainEffect = 0x14,
    LoseEffect = 0x15,
    UpdateEffect = 0x16,
    TargetIcon = 0x22,
    Tether = 0x23,
    Targetable = 0x36,
    DirectorUpdate = 0x6D,
    SetTargetSign = 0x1F6,
    LimitBreak = 0x1F9,
    HoT = 0x604,
    DoT = 0x605
}