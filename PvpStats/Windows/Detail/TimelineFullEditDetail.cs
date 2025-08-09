using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using LiteDB;
using Newtonsoft.Json;
using PvpStats.Types.Match.Timeline;
using System.IO;
using System.Numerics;

namespace PvpStats.Windows.Detail;
internal class TimelineFullEditDetail<T> : Window where T : PvpMatchTimeline {

    private Plugin _plugin;
    private T _dataModel;
    private string _dataString;

    public TimelineFullEditDetail(Plugin plugin, T dataRow) : base($"Timeline Full Edit: {dataRow.Id}") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(800, 800)
        };
        Flags |= ImGuiWindowFlags.NoSavedSettings;

        _plugin = plugin;
        _dataModel = dataRow;

        var serializedObject = BsonMapper.Global.Serialize(typeof(T), _dataModel);
        var bytes = BsonSerializer.Serialize(BsonMapper.Global.ToDocument(typeof(T), _dataModel));
        Plugin.Log2.Debug($"Document size: {bytes.Length} bytes");
        var stringReader = new StringReader(serializedObject.ToString());
        var stringWriter = new StringWriter();
        var jsonReader = new JsonTextReader(stringReader);
        var jsonWriter = new JsonTextWriter(stringWriter) {
            Formatting = Formatting.Indented
        };
        jsonWriter.WriteToken(jsonReader);
        _dataString = stringWriter.ToString();
    }

    public override void OnClose() {
        _plugin.WindowManager.RemoveWindow(this);
    }

    public override void Draw() {
        using(var child = ImRaii.Child("scrolling", new Vector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale), false, ImGuiWindowFlags.AlwaysAutoResize)) {
            if(child) {
                ImGui.InputTextMultiline("", ref _dataString, 999999, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
            }
        }
        if(ImGui.Button("Save and close")) {
            _plugin.DataQueue.QueueDataOperation(async () => {
                var returnValue = LiteDB.JsonSerializer.Deserialize(_dataString);
                var x = BsonMapper.Global.Deserialize<T>(returnValue);
                if(_dataModel is CrystallineConflictMatchTimeline) {
                    await _plugin.Storage.UpdateCCTimeline(_dataModel as CrystallineConflictMatchTimeline);
                } else if(_dataModel is FrontlineMatchTimeline) {
                    await _plugin.Storage.UpdateFLTimeline(_dataModel as FrontlineMatchTimeline);
                } else if(_dataModel is RivalWingsMatchTimeline) {
                    await _plugin.Storage.UpdateRWTimeline(_dataModel as RivalWingsMatchTimeline);
                }

                await _plugin.WindowManager.RefreshAll();
                IsOpen = false;
            });
        }
    }
}
