using ImGuiNET;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.Filter;
internal class TagFilter : DataFilter {
    public override string Name => "Tags";
    public override string HelpMessage => "Comma-separate multiple tags. 'AND' will include matches that have all tags. 'OR' will include matches that have at least one tag.";
    public string TagsRaw { get; set; } = "";
    public bool OrLogic { get; set; } = false;

    private string[] _logicCombo = { "AND", "OR" };
    private string _lastTextValue = "";
    private string _lastRefreshedValue = "";

    public TagFilter() { }

    internal TagFilter(Plugin plugin, Func<Task> action, TagFilter? filter = null) : base(plugin, action) {
        if(filter is not null) {
            TagsRaw = filter.TagsRaw;
            OrLogic = filter.OrLogic;
        }

        //refresh task
        Task.Run(async () => {
            PeriodicTimer periodicTimer = new(TimeSpan.FromMilliseconds(500));
            while(true) {
                await periodicTimer.WaitForNextTickAsync();
                if(_lastRefreshedValue != TagsRaw) {
                    _lastRefreshedValue = TagsRaw;
                    _ = plugin!.DataQueue.QueueDataOperation(async () => {
                        await Refresh();
                    });
                }
            }
        });
    }

    internal override void Draw() {
        string tags = TagsRaw;
        int orLogicInt = OrLogic ? 1 : 0;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if(ImGui.InputTextWithHint("##TagsInput", "Enter tags...", ref tags, 50)) {
            if(tags != _lastTextValue) {
                _lastTextValue = tags;
                Task.Run(() => {
                    TagsRaw = tags;
                    //await Refresh();
                });
            }
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if(ImGui.Combo("##TagLogic", ref orLogicInt, _logicCombo, _logicCombo.Length)) {
            Task.Run(async () => {
                OrLogic = Convert.ToBoolean(orLogicInt);
                await Refresh();
            });

        }
    }
}
