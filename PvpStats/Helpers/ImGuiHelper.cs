using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Types.Player;
using System;
using System.Numerics;

namespace PvpStats.Helpers;
internal static class ImGuiHelper {

    internal static void RightAlignCursor(string text) {
        var size = ImGui.CalcTextSize(text);
        var posX = ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X - ImGui.GetScrollX() - 2 * ImGui.GetStyle().ItemSpacing.X;
        if(posX > ImGui.GetCursorPosX()) {
            ImGui.SetCursorPosX(posX);
        }
    }

    internal static void CenterAlignCursor(string text) {
        var size = ImGui.CalcTextSize(text);
        var posX = ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X) / 2f;
        ImGui.SetCursorPosX(posX);
    }

    internal static void CenterAlignCursorVertical(string text) {
        var size = ImGui.CalcTextSize(text);
        var posY = ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - ImGui.CalcTextSize(text).Y) / 2f;
        ImGui.SetCursorPosY(posY);
    }

    internal static void HelpMarker(string text, bool sameLine = true) {
        if(sameLine) {
            ImGui.SameLine();
        }
        ImGui.TextDisabled("(?)");
        WrappedTooltip(text, 500f);
    }

    internal static void WrappedTooltip(string text, float width = 400f) {
        //width *= ImGuiHelpers.GlobalScale;
        //string[] splitStrings = text.Split(" ");
        //string wrappedString = "";
        //string currentLine = "";

        //foreach(var word in splitStrings) {
        //    if(ImGui.CalcTextSize($"{currentLine} {word}").X > width) {
        //        wrappedString += $"\n{word}";
        //        currentLine = word;
        //    } else {
        //        if(currentLine == "") {
        //            wrappedString += $"{word}";
        //            currentLine += $"{word}";
        //        } else {
        //            wrappedString += $" {word}";
        //            currentLine += $" {word}";
        //        }
        //    }
        //}

        if(ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            ImGui.Text(WrappedString(text, width));
            ImGui.EndTooltip();
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

    internal static Vector4 GetJobColor(Job? job) {
        var role = PlayerJobHelper.GetSubRoleFromJob(job);
        return role != null ? GetSubRoleColor((JobSubRole)role) : ImGuiColors.DalamudWhite;
    }

    internal static Vector4 GetSubRoleColor(JobSubRole role) {
        return role switch {
            JobSubRole.TANK => ImGuiColors.TankBlue,
            JobSubRole.HEALER => ImGuiColors.HealerGreen,
            JobSubRole.MELEE => ImGuiColors.DPSRed,
            JobSubRole.RANGED => ImGuiColors.DalamudOrange,
            JobSubRole.CASTER => ImGuiColors.ParsedPink,
            _ => ImGuiColors.DalamudWhite,
        };
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
            ImGui.TextColored(ImGuiHelper.ColorScale(colorMin, colorMax, minValue, maxValue, value), outputString);
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

    //internal static void IterateOverProps<T>(T item, int maxDepth, int depth = 0) {
    //    var props = typeof(T).GetProperties();
    //    foreach(var prop in props) {
    //        var value = prop.GetValue(item, null);
    //        if(depth + 1  < maxDepth) {
    //            IterateOverProps<object>(value, maxDepth, depth + 1);
    //        }
    //    }
    //}
}
