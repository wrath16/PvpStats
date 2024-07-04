using Dalamud.Game;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PvpStats.Services;
internal class LocalizationService {

    public class StringConfig {
        public uint RowId { get; set; }
        public string Value { get; set; } = "";
    }

    public class LanguageConfig {
        public Dictionary<string, StringConfig> RowVals { get; set; } = new();
    }

    public Dictionary<string, string> StringCache = new();

    public static readonly ClientLanguage[] SupportedLanguages = { ClientLanguage.English, ClientLanguage.French, ClientLanguage.German, ClientLanguage.Japanese };

    private Plugin _plugin;

    public LocalizationService(Plugin plugin) {
        _plugin = plugin;
        Initialize();
    }

    internal void Initialize() {
        StringCache = new();
        AddAddonString(StringCache, "AstraIntro", 14438);
        AddAddonString(StringCache, "UmbraIntro", 14439);
        AddAddonString(StringCache, "BronzeIntro", 14894);
        AddAddonString(StringCache, "SilverIntro", 14895);
        AddAddonString(StringCache, "GoldIntro", 14896);
        AddAddonString(StringCache, "PlatinumIntro", 14897);
        AddAddonString(StringCache, "DiamondIntro", 14898);
        AddAddonString(StringCache, "CrystalIntro", 14899);
    }

    private void AddAddonString(Dictionary<string, string> cache, string key, uint rowId) {
        var row = _plugin.DataManager.GetExcelSheet<Addon>().GetRow(rowId);
        cache.Add(key, row.Text.ToString());
    }

    public bool IsLanguageSupported(ClientLanguage? language = null) {
        language ??= _plugin.ClientState.ClientLanguage;
        return SupportedLanguages.Contains((ClientLanguage)language);
    }

    public List<uint?> GetRowId<T>(string data, string column, ClientLanguage? language = null) where T : ExcelRow {
        language ??= _plugin.ClientState.ClientLanguage;
        List<uint?> rowIds = new();
        Type type = typeof(T);

        //check to make sure column is string
        var columnProperty = type.GetProperty(column) ?? throw new InvalidOperationException($"No property of name: {column} on type {type.FullName}");
        if(!columnProperty.PropertyType.IsAssignableTo(typeof(Lumina.Text.SeString))) {
            throw new ArgumentException($"property {column} of type {columnProperty.PropertyType.FullName} on type {type.FullName} is not assignable to a SeString!");
        }

        foreach(var row in _plugin.DataManager.GetExcelSheet<T>((ClientLanguage)language)) {
            var rowData = columnProperty!.GetValue(row)?.ToString();
            if(data.Equals(rowData, StringComparison.OrdinalIgnoreCase)) {
                rowIds.Add(row.RowId);
            }
        }
        return rowIds;
    }

    public string TranslateDataTableEntry<T>(string data, string column, ClientLanguage destinationLanguage, ClientLanguage? originLanguage = null) where T : ExcelRow {
        originLanguage ??= _plugin.ClientState.ClientLanguage;
        uint? rowId = null;
        Type type = typeof(T);
        bool isPlural = column.Equals("Plural", StringComparison.OrdinalIgnoreCase);

        if(originLanguage == destinationLanguage) {
            return data;
        }

        //if(!IsLanguageSupported(destinationLanguage) || !IsLanguageSupported(originLanguage)) {
        //    throw new ArgumentException("Cannot translate to/from an unsupported client language.");
        //}

        //check to make sure column is string
        var columnProperty = type.GetProperty(column) ?? throw new InvalidOperationException($"No property of name: {column} on type {type.FullName}");
        if(!columnProperty.PropertyType.IsAssignableTo(typeof(Lumina.Text.SeString))) {
            throw new ArgumentException($"property {column} of type {columnProperty.PropertyType.FullName} on type {type.FullName} is not assignable to a SeString!");
        }

        //iterate over table to find rowId
        foreach(var row in _plugin.DataManager.GetExcelSheet<T>((ClientLanguage)originLanguage)!) {
            var rowData = columnProperty!.GetValue(row)?.ToString();

            //German declension placeholder replacement
            if(originLanguage == ClientLanguage.German && rowData != null) {
                var pronounProperty = type.GetProperty("Pronoun");
                if(pronounProperty != null) {
                    int pronoun = Convert.ToInt32(pronounProperty.GetValue(row))!;
                    rowData = ReplaceGermanDeclensionPlaceholders(rowData, pronoun, isPlural);
                }
            }
            if(data.Equals(rowData, StringComparison.OrdinalIgnoreCase)) {
                rowId = row.RowId; break;
            }
        }

        rowId = rowId ?? throw new ArgumentException($"'{data}' not found in table: {type.Name} for language: {originLanguage}.");

        //get data from destinationLanguage
        var translatedRow = _plugin.DataManager.GetExcelSheet<T>(destinationLanguage)!.Where(r => r.RowId == rowId).FirstOrDefault();
        string? translatedString = columnProperty!.GetValue(translatedRow)?.ToString() ?? throw new InvalidOperationException($"row id {rowId} not found in table {type.Name} for language: {destinationLanguage}");

        //add German declensions. Assume nominative case
        if(destinationLanguage == ClientLanguage.German) {
            var pronounProperty = type.GetProperty("Pronoun");
            if(pronounProperty != null) {
                int pronoun = Convert.ToInt32(pronounProperty.GetValue(translatedRow))!;
                translatedString = ReplaceGermanDeclensionPlaceholders(translatedString, pronoun, isPlural);
            }
        }

        return translatedString;
    }

    public string TranslateRankString(string rank, ClientLanguage destinationLanguage, ClientLanguage? originLanguage = null) {
        //if(!IsLanguageSupported(destinationLanguage) || !IsLanguageSupported(originLanguage)) {
        //    throw new ArgumentException("Cannot translate to/from an unsupported client language.");
        //}

        rank = rank.Trim();
        //Regex.Split(tierName, @"\s", RegexOptions.IgnoreCase);
        string[] splitString;

        switch(originLanguage) {
            default:
            case ClientLanguage.German:
            case ClientLanguage.French:
            case ClientLanguage.English:
                splitString = rank.Split(" ");
                break;
            case ClientLanguage.Japanese:
                //TODO
                splitString = Regex.Split(rank, @" ");
                break;
        }

        if(splitString.Length <= 0 || splitString.Length > 2) {
            throw new ArgumentException("Invalid rank string");
        }

        string tier = splitString[0];
        string? riser = null;
        if(splitString.Length == 2) {
            if(!int.TryParse(splitString[1], out int riserParsed)) {
                throw new ArgumentException("Cannot convert riser to integer");
            }
            riser = splitString[1];
        }

        var translatedTier = TranslateDataTableEntry<ColosseumMatchRank>(tier, "Unknown0", ClientLanguage.English);

        if(riser != null) {
            return $"{translatedTier} {riser}";
        } else {
            return $"{translatedTier}";
        }
    }

    //assumes nominative case
    //male = 0, female = 1, neuter = 2
    private static string ReplaceGermanDeclensionPlaceholders(string input, int gender, bool isPlural) {
        if(isPlural) {
            input = input.Replace("[a]", "e");
        }
        switch(gender) {
            default:
            case 0: //male
                input = input.Replace("[a]", "er").Replace("[t]", "der");
                break;
            case 1: //female
                input = input.Replace("[a]", "e").Replace("[t]", "die");
                break;
            case 2: //neuter
                input = input.Replace("[a]", "es").Replace("[t]", "das");
                break;
        }
        //remove possessive placeholder
        input = input.Replace("[p]", "");
        return input;
    }
}
