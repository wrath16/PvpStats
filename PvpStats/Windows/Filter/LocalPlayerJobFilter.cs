using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PvpStats.Windows.Filter;
public class LocalPlayerJobFilter : DataFilter {
    public override string Name => "Job";
    //public override string HelpMessage => "Comma-separate multiple party members.";
    public Job PlayerJob { get; set; }
    public JobSubRole? JobRole { get; set; }
    public bool AnyJob { get; set; } = true;

    private List<string> _jobCombo = new();
    private int _roleCount;

    public LocalPlayerJobFilter() { }

    internal LocalPlayerJobFilter(Plugin plugin, Action action, LocalPlayerJobFilter? filter = null) : base(plugin, action) {
        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        var allRoles = Enum.GetValues(typeof(JobSubRole)).Cast<JobSubRole>();
        _roleCount = allRoles.Count();
        _jobCombo.Add("All Jobs");
        foreach (var role in allRoles) {
            _jobCombo.Add(role.ToString());
        }
        foreach (var job in allJobs) {
            _jobCombo.Add(PlayerJobHelper.GetNameFromJob(job));
        }

        if (filter is not null) {
            AnyJob = filter.AnyJob;
            PlayerJob = filter.PlayerJob;
            JobRole = filter.JobRole;
        }
    }

    internal override void Draw() {
        int jobIndex = AnyJob ? 0 : JobRole != null ? (int)JobRole + 1 : (int)PlayerJob + _roleCount + 1;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2f);
        if (ImGui.Combo("###LocalPlayerJobCombo", ref jobIndex, _jobCombo.ToArray(), _jobCombo.Count)) {
            _plugin!.DataQueue.QueueDataOperation(() => {
                if (jobIndex == 0) {
                    AnyJob = true;
                    JobRole = null;
                }
                else if (jobIndex <= _roleCount + 1) {
                    AnyJob = false;
                    JobRole = (JobSubRole)jobIndex - 1;
                }
                else {
                    AnyJob = false;
                    JobRole = null;
                    PlayerJob = (Job)jobIndex - _roleCount - 1;
                }
                Refresh();
            });
        }

        //if (ImGui.InputText($"##playerFilter", ref playerName, 50, ImGuiInputTextFlags.None)) {
        //    if (playerName != _lastValue) {
        //        _lastValue = playerName;
        //        _plugin!.DataQueue.QueueDataOperation(() => {
        //            PlayerNamesRaw = playerName;
        //            //SetPartyMemberArray(partyMembers);
        //            //Refresh();
        //        });
        //    }
        //}
    }
}
