using Dalamud.Interface.Windowing;
using ImGuiNET;
using LiteDB;
using Newtonsoft.Json;
using PvpStats.Types.Match;
using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;

namespace PvpStats.Windows.Detail;
internal class FullEditDetail<T> : Window {

    private Plugin _plugin;
    private T _dataModel;
    private string _dataString;

    public FullEditDetail(Plugin plugin, T dataRow) : base("Full Edit") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(1200, 1500)
        };

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

    public override void Draw() {
        ImGui.InputTextMultiline("", ref _dataString, 999999, new Vector2(500, 500));
    }

    private void DrawProperty(PropertyInfo prop) {
        var data = prop.GetValue(_dataModel);
        string? stringData = "";

        //BsonMapper.Global.ToDocument(typeof(T), _dataModel).ToString();

        switch(prop.PropertyType) {
            case Type when prop.PropertyType == typeof(string):
                stringData = data as string;
                ImGui.InputText(prop.Name, ref stringData, 999, ImGuiInputTextFlags.None);
                break;
                case Type numType when prop.PropertyType.IsAssignableFrom(typeof(int)):
                //string? s = data as string;
                stringData = data.ToString();
                if (ImGui.InputText(prop.Name, ref stringData, 999, ImGuiInputTextFlags.CharsDecimal)) {
                    //tryparse
                    
                }
                break;

            default:
                break;
        }


        if(prop.PropertyType == typeof(string)) {

        }
    }
}
