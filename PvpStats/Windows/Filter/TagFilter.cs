using ImGuiNET;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;

public enum TagLogic {
    AND,
    OR,
    NAND,
    NOR
}


internal class TagFilter : DataFilter {
    public override string Name => "Tags";
    public override string HelpMessage => "Comma-separate multiple tags.\n'AND' will include matches that have all tags.\n'OR' will include matches that have at least one tag.\n'NAND' will include matches that don't have all tags.\n'NOR' will include matches that don't have any of the listed tags.";
    public string TagsRaw { get; set; } = "";
    public TagLogic Logic { get; set; } = TagLogic.AND;
    public bool AllowPartial {  get; set; } = false;

    private string[] _logicCombo = { "AND", "OR", "NAND", "NOR" };
    private string _lastTextValue = "";
    private string _lastRefreshedValue = "";

    public TagFilter() { }

    internal TagFilter(Plugin plugin, Func<Task> action, TagFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            TagsRaw = filter.TagsRaw;
            Logic = filter.Logic;
            AllowPartial = filter.AllowPartial;
        }

        //refresh task
        Task.Run(async () => {
            PeriodicTimer periodicTimer = new(TimeSpan.FromMilliseconds(500));
            while(true) {
                await periodicTimer.WaitForNextTickAsync();
                if(_lastRefreshedValue != TagsRaw) {
                    _lastRefreshedValue = TagsRaw;
                    _ = Task.Run(() => {
                        _ = Refresh();
                    });
                }
            }
        });
    }

    internal override void Draw() {
        string tags = TagsRaw;
        int logicIndex = (int)Logic;
        bool allowPartial = AllowPartial;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.InputTextWithHint("##TagsInput", "Enter tags...", ref tags, 100)) {
            if(tags != _lastTextValue) {
                _lastTextValue = tags;
                Task.Run(() => {
                    TagsRaw = tags;
                    //await Refresh();
                });
            }
        }
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 4);
        if(ImGui.Combo("##TagLogic", ref logicIndex, _logicCombo, _logicCombo.Length)) {
            Task.Run(async () => {
                Logic = (TagLogic)logicIndex;
                await Refresh();
            });
        }
        ImGui.SameLine();
        if(ImGui.Checkbox("Partial matches", ref allowPartial)) {
            Task.Run(async () => {
                AllowPartial = allowPartial;
                await Refresh();
            });
        }
    }
}
