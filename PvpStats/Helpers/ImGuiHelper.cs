using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Helpers;
internal static class ImGuiHelper {

    internal static void RightAlignCursor(string text) {
        var size = ImGui.CalcTextSize(text);
        var posX = ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X - ImGui.GetScrollX() - 2 * ImGui.GetStyle().ItemSpacing.X;
        if(posX > ImGui.GetCursorPosX()) {
            ImGui.SetCursorPosX(posX);
        }
    }

    internal static void RightAlignCursor2(string text, float extra) {
        var posX = ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X;
        if(posX > ImGui.GetCursorPosX()) {
            ImGui.SetCursorPosX(posX + extra);
        }
    }

    internal static void CenterAlignCursor(string text) {
        var posX = ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X) / 2f;
        ImGui.SetCursorPosX(posX);
    }

    internal static void CenterAlignCursorVertical(string text) {
        var size = ImGui.CalcTextSize(text);
        var posY = ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - ImGui.CalcTextSize(text).Y) / 2f;
        ImGui.SetCursorPosY(posY);
    }

    internal static void HelpMarker(string text, bool sameLine = true, bool alignToFrame = false) {
        if(sameLine) ImGui.SameLine();
        if(alignToFrame) ImGui.AlignTextToFramePadding();

        ImGui.TextDisabled("(?)");
        WrappedTooltip(text, 500f);
    }

    internal static void WrappedTooltip(string text, float width = 400f) {
        if(ImGui.IsItemHovered()) {
            using var tooltip = ImRaii.Tooltip();
            ImGui.Text(WrappedString(text, width));
        }
    }

    internal static string WrappedString(string text, float width) {
        width *= ImGuiHelpers.GlobalScale;
        string[] splitStrings = text.Split(" ");
        string wrappedString = "";
        string currentLine = "";

        foreach(var word in splitStrings) {
            if(ImGui.CalcTextSize($"{currentLine} {word}").X > width) {
                if(wrappedString == "") {
                    wrappedString = word;
                } else {
                    wrappedString += $"\n{word}";
                }
                currentLine = word;
            } else {
                if(currentLine == "") {
                    wrappedString += $"{word}";
                    currentLine += $"{word}";
                } else {
                    wrappedString += $" {word}";
                    currentLine += $" {word}";
                }
            }
        }
        return wrappedString;
    }

    internal static string WrappedString(string text, uint lines) {
        var size = ImGui.CalcTextSize(text).X;
        var sizePerLine = size / lines * 1.4; //add a margin of error
        string[] splitStrings = text.Split(" ");
        string wrappedString = "";
        string currentLine = "";
        int lineIndex = 0;

        foreach(var word in splitStrings) {
            if(ImGui.CalcTextSize($"{currentLine} {word}").X > sizePerLine && currentLine != "" && lineIndex + 1 < lines) {
                if(wrappedString == "") {
                    wrappedString = word;
                } else {
                    wrappedString += $"\n{word}";
                    lineIndex++;
                }
                currentLine = word;
            } else {
                if(currentLine == "") {
                    wrappedString += $"{word}";
                    currentLine += $"{word}";
                } else {
                    wrappedString += $" {word}";
                    currentLine += $" {word}";
                }
            }
        }
        return wrappedString;
    }

    internal static Vector4 ColorScale(Vector4 minColor, Vector4 maxColor, float minValue, float maxValue, float val) {
        float percentile = (val - minValue) / (maxValue - minValue);
        if(percentile >= 1) {
            return maxColor.Lighten(1f);
        } else if(percentile <= 0) {
            return minColor.Lighten(1f);
        }
        var pctlVector = new Vector4(percentile);
        var colorDiff = maxColor - minColor;
        var color = colorDiff * pctlVector + minColor;

        return color.Lighten(1f);
    }

    internal static void DrawPercentage(double val, Vector4 color) {
        if(val is not double.NaN) {
            ImGui.TextColored(color, string.Format("{0:P1}%", val));
        }
    }

    internal static void DrawPercentage(double val) {
        if(val is not double.NaN) {
            ImGui.TextUnformatted(string.Format("{0:P1}", val));
        }
    }

    internal static void DrawColorScale(float value, Vector4 colorMin, Vector4 colorMax, float minValue, float maxValue, bool colorEnabled, string format = "0.00", bool isPercent = false) {
        var outputString = isPercent ? string.Format(format, value) : value.ToString(format);
        if(colorEnabled) {
            ImGui.TextColored(ColorScale(colorMin, colorMax, minValue, maxValue, value), outputString);
        } else {
            ImGui.Text(outputString);
        }
    }

    internal static string GetTimeSpanString(TimeSpan time) {
        bool hasHours = time.TotalHours > 1;
        string display = $"{(hasHours ? $"{(int)time.TotalHours}:" : "")}";
        if(hasHours) {
            display += time.ToString(@"mm\:ss");
        } else {
            display += time.ToString(@"m\:ss");
        }
        return display;
    }

    internal static void CSVButton(string csv) {
        try {
            ImGui.PushFont(UiBuilder.IconFont);
            if(ImGui.Button($"{FontAwesomeIcon.Copy.ToIconString()}##--CopyCSV")) {
                Task.Run(() => {
                    ImGui.SetClipboardText(csv);
                });
            }
        } finally {
            ImGui.PopFont();
        }
        WrappedTooltip("Copy CSV to clipboard");
    }

    internal static void CSVButton(Action action) {
        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Copy.ToIconString()}##--CopyCSV")) {
                action.Invoke();
            }
        }
        WrappedTooltip("Copy CSV to clipboard");
    }

    internal static void DonateButton() {
        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
            var text = $"{FontAwesomeIcon.Star.ToIconString()}{FontAwesomeIcon.Copy.ToIconString()}";
            using(_ = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed)) {
                if(ImGui.Button($"{FontAwesomeIcon.Heart.ToIconString()}##--Donate")) {
                    Task.Run(() => {
                        Process.Start(new ProcessStartInfo() {
                            UseShellExecute = true,
                            FileName = "https://ko-fi.com/samoxiv"
                        });
                    });
                }
            }
        }
        WrappedTooltip("Support the dev");
    }

    internal static void FormattedCollapsibleHeader((string, float)[] columns, Action action) {
        List<string> formattedText = new();
        foreach(var column in columns) {
            float targetWidth = column.Item2 * ImGuiHelpers.GlobalScale;
            string text = column.Item1;
            while(ImGui.CalcTextSize(text).X < targetWidth) {
                text += " ";
            }
            formattedText.Add(text);
        }

        var headerText = "";
        for(int i = 0; i < columns.Length; i++) {
            headerText += "{" + i + "} ";
        }
        if(ImGui.CollapsingHeader(string.Format(headerText, formattedText.ToArray()))) {
            action.Invoke();
        }
    }

    internal static void SetDynamicWidth(float minWidth, float maxWidth, float factor) {
        float width = ImGui.GetContentRegionAvail().X / factor;
        minWidth = minWidth * ImGuiHelpers.GlobalScale;
        maxWidth = maxWidth * ImGuiHelpers.GlobalScale;
        ImGui.SetNextItemWidth(width < minWidth ? minWidth : width > maxWidth ? maxWidth : width);
    }

    internal static void DrawNumericCell(string value, float extra = 0f, Vector4? color = null) {
        RightAlignCursor2(value, extra);
        //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2 * ImGui.GetStyle().ItemSpacing.X);
        if(color != null) {
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, (Vector4)color);
            ImGui.TextUnformatted(value);
        } else {
            ImGui.TextUnformatted(value);
        }
    }

    internal static void DrawNumericCell(float value, Vector4 colorMin, Vector4 colorMax, float minValue, float maxValue, bool colorEnabled, string format = "0.00", float extra = 0f) {
        var outputString = value.ToString(format);
        RightAlignCursor2(outputString, extra);
        //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2 * ImGui.GetStyle().ItemSpacing.X);
        if(colorEnabled) {
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, ColorScale(colorMin, colorMax, minValue, maxValue, value));
            ImGui.TextUnformatted(outputString);
        } else {
            ImGui.TextUnformatted(outputString);
        }
    }

    internal static void DrawNumericCell(string display, float value, Vector4 colorMin, Vector4 colorMax, float minValue, float maxValue, bool colorEnabled, float extra = 0f) {
        RightAlignCursor2(display, extra);
        //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2 * ImGui.GetStyle().ItemSpacing.X);
        if(colorEnabled) {
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, ColorScale(colorMin, colorMax, minValue, maxValue, value));
            ImGui.TextUnformatted(display);
        } else {
            ImGui.TextUnformatted(display);
        }
    }

    //0 = left, 1 = center, 2 = right
    internal static void DrawTableHeader(string name, int alignment = 2, bool draw = true, bool multilineExpected = true, float extra = -11f) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 0f));
        //RightAlignCursor2(name, -11f);
        var cursorBefore = ImGui.GetCursorPos();
        if(multilineExpected) {
            ImGui.TableHeader($"\n\n##{name}");
        } else {
            ImGui.TableHeader($"##{name}");
        }
        ImGui.SetCursorPos(cursorBefore);

        if(!draw) return;

        void drawText(string text) {
            if(alignment == 2) {
                RightAlignCursor2(text, extra);
            } else if(alignment == 1) {
                //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14f * ImGuiHelpers.GlobalScale);
                CenterAlignCursor(text);
            }
            ImGui.TextUnformatted(text);
        }

        if(!name.Contains('\n')) {
            if(multilineExpected) {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
            }
            //ImGui.SameLine();

            drawText(name);
        } else {
            var splitName = name.Split('\n');
            //ImGui.TableHeader($"\n\n##{name}");
            //ImGui.SameLine();
            foreach(var s in splitName) {
                drawText(s);
            }
        }
    }

    public static string AddOrdinal(int num) {
        if(num <= 0) return num.ToString();

        switch(num % 100) {
            case 11:
            case 12:
            case 13:
                return num + "th";
        }

        switch(num % 10) {
            case 1:
                return num + "st";
            case 2:
                return num + "nd";
            case 3:
                return num + "rd";
            default:
                return num + "th";
        }
    }
}
