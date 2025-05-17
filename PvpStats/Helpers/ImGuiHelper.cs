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
        //var posX = ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X) / 2f;
        //ImGui.SetCursorPosX(posX);
        CenterAlignCursor(ImGui.CalcTextSize(text).X);
    }

    internal static void CenterAlignCursor(float width) {
        var posX = ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - width) / 2f;
        ImGui.SetCursorPosX(posX);
    }

    internal static void CenterAlignCursorVertical(string text) {
        //var size = ImGui.CalcTextSize(text);
        //var posY = ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - ImGui.CalcTextSize(text).Y) / 2f;
        //ImGui.SetCursorPosY(posY);
        CenterAlignCursorVertical(ImGui.CalcTextSize(text).Y);
    }

    internal static void CenterAlignCursorVertical(float height) {
        var posY = ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - height) / 2f;
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

    internal static void DrawColorScale(float value, Vector4 colorMin, Vector4 colorMax, float minValue, float maxValue, bool colorEnabled, string? customString = null) {
        var outputString = customString ?? value.ToString();
        if(colorEnabled) {
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, ColorScale(colorMin, colorMax, minValue, maxValue, value));
            ImGui.TextUnformatted(outputString);
        } else {
            ImGui.TextUnformatted(outputString);
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

    public static void DrawRefreshProgressBar(float value) {
        var cursorBefore = ImGui.GetCursorPos();
        Vector2 barSize = new Vector2(200f, 100f);
        ImGui.SetCursorPos(new Vector2((ImGui.GetWindowSize().X - barSize.X) / 2, ImGui.GetWindowSize().Y / 2));
        ImGui.GetWindowDrawList();
        using(var child = ImRaii.Child("ProgressBar")) {
            using var color = ImRaii.PushColor(ImGuiCol.PlotHistogram, ImGuiColors.DalamudGrey2);
            using var color2 = ImRaii.PushColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg] + new Vector4(0f, 0f, 0f, 1f));
            ImGui.ProgressBar(value, barSize * ImGuiHelpers.GlobalScale);
        }
        ImGui.SetCursorPos(cursorBefore);
    }

    public static void DrawRainbowText(string text, int period = 30, float offset = 0) {
        var index = (ImGui.GetFrameCount() + offset * period) % period;
        var minColor = new Vector4(0f, 0f, 0f, 1f);
        var maxColor = new Vector4(0f, 0f, 0f, 1f);
        var percentile = 0f;
        if(index >= 0 && index < period / 3) {
            minColor = new Vector4(1f, 0f, 0f, 1f);
            maxColor = new Vector4(0f, 1f, 0f, 1f);
            percentile = index / (period / 3);
        } else if(index >= period / 3 && index < 2 * period / 3) {
            minColor = new Vector4(0f, 1f, 0f, 1f);
            maxColor = new Vector4(0f, 0f, 1f, 1f);
            percentile = (index - (period / 3)) / (period / 3);
        } else {
            minColor = new Vector4(0f, 0f, 1f, 1f);
            maxColor = new Vector4(1f, 0f, 0f, 1f);
            percentile = (index - (2 * period / 3)) / (period / 3);
        }

        var textColor = ColorScale(minColor, maxColor, 0f, 1f, percentile);
        ImGui.TextColored(textColor, text);
    }

    public static void DrawRainbowTextByChar(string text) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        for(int i = 0; i < text.Length; i++) {
            char c = text[i];
            float offset = (float)i / text.Length;
            DrawRainbowText(c.ToString(), 50, offset);
            ImGui.SameLine();
        }
    }

    public static void DrawRotatedImage(nint image, Vector2 size, Vector2 pos, Vector2 pivot, float angleDeg) {
        var drawList = ImGui.GetWindowDrawList();
        var angle = angleDeg * (float)Math.PI / 180;
        Vector2 halfSize = size / 2;
        Vector2[] corners = new Vector2[4];
        corners[0] = pos;                               // Top-left
        corners[1] = pos + new Vector2(size.X, 0);     // Top-right
        corners[2] = pivot;                            // Bottom-right
        corners[3] = pos + new Vector2(0, size.Y);     // Bottom-left

        // Rotate each corner around the pivot
        Vector2[] rotated = new Vector2[4];
        for(int i = 0; i < 4; i++) {
            Vector2 offset = corners[i] - pivot;

            float rotatedX = offset.X * MathF.Cos(angle) - offset.Y * MathF.Sin(angle);
            float rotatedY = offset.X * MathF.Sin(angle) + offset.Y * MathF.Cos(angle);

            rotated[i] = pivot + new Vector2(rotatedX, rotatedY);
        }

        drawList.AddImageQuad(
            image,
            rotated[0], // top-left
            rotated[1], // top-right
            rotated[2], // bottom-right
            rotated[3], // bottom-left
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
            ImGui.GetColorU32(Vector4.One) // Tint color (white = no tint)
        );
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
