﻿using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;

public enum TeamStatus {
    Any,
    Teammate,
    Opponent,
}

public class OtherPlayerFilter : DataFilter, IEquatable<OtherPlayerFilter> {
    public override string Name => "Player";
    //public override string HelpMessage => "Comma-separate multiple party members.";
    public string PlayerNamesRaw { get; set; } = "";
    public Job PlayerJob { get; set; }
    public bool AnyJob { get; set; } = true;
    public TeamStatus TeamStatus { get; set; }
    private string _lastTextValue = "";
    private string _lastRefreshedValue = "";

    private List<string> _jobCombo = new();
    private string[] _teamStatusCombo = { "Any Side", "Teammate", "Opponent" };

    public OtherPlayerFilter() { }

    public OtherPlayerFilter(OtherPlayerFilter filter) {
        PlayerNamesRaw = filter.PlayerNamesRaw;
        _lastRefreshedValue = PlayerNamesRaw;
        AnyJob = filter.AnyJob;
        PlayerJob = filter.PlayerJob;
        _lastTextValue = PlayerNamesRaw;
    }

    internal OtherPlayerFilter(Plugin plugin, Func<Task> action, OtherPlayerFilter? filter = null) : base(plugin, action) {
        var allJobs = Enum.GetValues(typeof(Job)).Cast<Job>();
        _jobCombo.Add("Any Job");
        foreach(var job in allJobs) {
            _jobCombo.Add(PlayerJobHelper.GetNameFromJob(job));
        }

        if(filter is not null) {
            PlayerNamesRaw = filter.PlayerNamesRaw;
            _lastRefreshedValue = PlayerNamesRaw;
            AnyJob = filter.AnyJob;
            PlayerJob = filter.PlayerJob;
            _lastTextValue = PlayerNamesRaw;
        }

        //refresh task
        Task.Run(async () => {
            PeriodicTimer periodicTimer = new(TimeSpan.FromMilliseconds(500));
            while(true) {
                await periodicTimer.WaitForNextTickAsync();
                if(_lastRefreshedValue != PlayerNamesRaw) {
                    _lastRefreshedValue = PlayerNamesRaw;
                    _ = Task.Run(() => {
                        _ = Refresh();
                    });
                }
            }
        });
    }

    internal override void Draw() {
        string playerName = PlayerNamesRaw;
        int jobIndex = AnyJob ? 0 : (int)PlayerJob + 1;
        int teamIndex = (int)TeamStatus;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.InputTextWithHint("###PlayerNameInput", "Enter player name and world", ref playerName, 50)) {
            if(playerName != _lastTextValue) {
                _lastTextValue = playerName;
                Task.Run(() => {
                    PlayerNamesRaw = playerName;
                    //await Refresh();
                });
            }
        }
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if(ImGui.Combo("###TeamStatusCombo", ref teamIndex, _teamStatusCombo, _teamStatusCombo.Length)) {
            Task.Run(async () => {
                TeamStatus = (TeamStatus)teamIndex;
                await Refresh();
            });
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.Combo("###JobCombo", ref jobIndex, _jobCombo.ToArray(), _jobCombo.Count)) {
            Task.Run(async () => {
                if(jobIndex == 0) {
                    AnyJob = true;
                } else {
                    AnyJob = false;
                    PlayerJob = (Job)jobIndex - 1;
                }
                await Refresh();
            });
        }
    }

    public bool Equals(OtherPlayerFilter? other) {
        return PlayerNamesRaw.Equals(other?.PlayerNamesRaw) && TeamStatus == other?.TeamStatus && AnyJob == other.AnyJob && (AnyJob || PlayerJob == other?.PlayerJob);
    }
}
