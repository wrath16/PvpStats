using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using LiteDB;
using Newtonsoft.Json;
using PvpStats.Types.Match;
using System.IO;
using System.Numerics;

namespace PvpStats.Windows.Detail;
internal class FullEditDetail<T> : Window {

    private Plugin _plugin;
    private T _dataModel;
    private string _dataString;

    public FullEditDetail(Plugin plugin, T dataRow) : base($"Full Edit: {dataRow.GetHashCode()}") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(800, 800)
        };
        //Flags |= ImGuiWindowFlags.NoScrollbar;

        _plugin = plugin;
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
        if(ImGui.Button("Save")) {
            _plugin.DataQueue.QueueDataOperation(async () => {
                var returnValue = LiteDB.JsonSerializer.Deserialize(_dataString);
                var x = BsonMapper.Global.Deserialize<CrystallineConflictMatch>(returnValue);
                //make this generic for type
                await _plugin.CCCache.UpdateMatch(x);
                await _plugin.WindowManager.Refresh();
            });
            //using(var reader = new StreamReader(_dataString)) {
            //    var returnValue = LiteDB.JsonSerializer.Deserialize(_dataString);
            //    var x = BsonMapper.Global.Deserialize<CrystallineConflictMatch>(returnValue);
            //    _plugin.Storage.UpdateCCMatch(x);
            //    //var ccMatch = returnValue.AsDocument.;
            //}

            //var ccMatch = (CrystallineConflictMatch)BsonMapper.Global.Deserialize(typeof(CrystallineConflictMatch), _dataString);
            //_plugin.Storage.UpdateCCMatch(ccMatch);
        }
    }

    //private void DrawProperty(PropertyInfo prop) {
    //    var data = prop.GetValue(_dataModel);
    //    string? stringData = "";

    //    //BsonMapper.Global.ToDocument(typeof(T), _dataModel).ToString();

    //    switch(prop.PropertyType) {
    //        case Type when prop.PropertyType == typeof(string):
    //            stringData = data as string;
    //            ImGui.InputText(prop.Name, ref stringData, 999, ImGuiInputTextFlags.None);
    //            break;
    //        case Type numType when prop.PropertyType.IsAssignableFrom(typeof(int)):
    //            //string? s = data as string;
    //            stringData = data.ToString();
    //            if(ImGui.InputText(prop.Name, ref stringData, 999, ImGuiInputTextFlags.CharsDecimal)) {
    //                //tryparse

    //            }
    //            break;

    //        default:
    //            break;
    //    }

    //    if(prop.PropertyType == typeof(string)) {

    //    }
    //}
}
