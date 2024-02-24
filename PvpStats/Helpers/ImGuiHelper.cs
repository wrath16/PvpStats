using Dalamud.Interface.Utility;
using ImGuiNET;

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

    //internal static void CenterAlignCursorVertical(string text) {
    //    var size = ImGui.CalcTextSize(text);
    //    var posY = ImGui.GetCursorPosY() + (ImGui.R - ImGui.CalcTextSize(text).Y) / 2f;
    //    ImGui.SetCursorPosY(posY);
    //}

    internal static void HelpMarker(string text) {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        WrappedTooltip(text, 500f);
    }

    internal static void WrappedTooltip(string text, float width = 400f) {
        width *= ImGuiHelpers.GlobalScale;
        string[] splitStrings = text.Split(" ");
        string wrappedString = "";
        string currentLine = "";

        foreach(var word in splitStrings) {
            if(ImGui.CalcTextSize($"{currentLine} {word}").X > width) {
                wrappedString += $"\n{word}";
                currentLine = word;
            } else {
                wrappedString += $" {word}";
                currentLine += $" {word}";
            }
        }

        if(ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            ImGui.Text(wrappedString);
            ImGui.EndTooltip();
        }
    }
}
