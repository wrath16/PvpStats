namespace PvpStats.Types.Match;

public class PvpDuty {
    public uint DutyId { get; init; }
    public string NameEnglish { get; init; }
    public Territory Territory { get; init; }
    public DutyType DutyType { get; init; }

    public PvpDuty(uint dutyId, string nameEnglish, Territory territory, DutyType dutyType) {
        DutyId = dutyId;
        NameEnglish = nameEnglish;
        Territory = territory;
        DutyType = dutyType;
    }
}
