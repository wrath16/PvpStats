using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using LiteDB;
using Newtonsoft.Json;
using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using System.IO;
using System.Numerics;

namespace PvpStats.Windows.Detail;
internal class FullEditDetail<T> : Window where T : PvpMatch {

    private Plugin _plugin;
    private MatchCacheService<T>? _matchCache;
    private T _dataModel;
    private string _dataString;

    public FullEditDetail(Plugin plugin, MatchCacheService<T>? matchCache, T dataRow) : base($"Full Edit: {dataRow.GetHashCode()}") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(800, 800)
        };
        Flags |= ImGuiWindowFlags.NoSavedSettings;

        _plugin = plugin;
        _matchCache = matchCache;
        _dataModel = dataRow;

        var serializedObject = BsonMapper.Global.Serialize(typeof(T), _dataModel).ToString();
        var stringReader = new StringReader(serializedObject);
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
                if(_matchCache != null) {
                    await _matchCache.UpdateMatch(x);
                }
                await _plugin.WindowManager.Refresh();
                IsOpen = false;
            });
        }
    }
}
