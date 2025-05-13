using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using PvpStats.Helpers;

namespace PvpStats.Windows.Summary;
internal class FrontlinePvPProfile {

    private Plugin _plugin;

    public FrontlinePvPProfile(Plugin plugin) {
        _plugin = plugin;
    }

    public unsafe void Draw() {
        var pvpProfile = PvPProfile.Instance();
        ImGuiHelper.HelpMarker("This data comes from SE's game servers.", false);
        ImGui.TextColored(_plugin.Configuration.Colors.Header, "Overall Performance:");
        if(pvpProfile != null) {
            DrawTable(pvpProfile->FrontlineTotalMatches, pvpProfile->FrontlineTotalFirstPlace, pvpProfile->FrontlineTotalSecondPlace, pvpProfile->FrontlineTotalThirdPlace);
        }
        ImGui.Separator();
        ImGui.TextColored(_plugin.Configuration.Colors.Header, "Weekly Performance:");
        if(pvpProfile != null) {
            DrawTable(pvpProfile->FrontlineWeeklyMatches, pvpProfile->FrontlineWeeklyFirstPlace, pvpProfile->FrontlineWeeklySecondPlace, pvpProfile->FrontlineWeeklyThirdPlace);
        }
    }

    private void DrawTable(uint matches, uint firstPlace, uint secondPlace, uint thirdPlace) {
        var averagePlace = matches > 0 ? (double)(firstPlace + secondPlace * 2 + thirdPlace * 3) / matches : 0d;

        using(var table = ImRaii.Table($"t1###{matches.GetHashCode()}", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
                ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

                ImGui.TableNextColumn();
                ImGui.Text("Matches: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{matches:N0}");
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                ImGui.Text("First places: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{firstPlace:N0}");
                ImGui.TableNextColumn();
                if(matches > 0) {
                    ImGui.Text($"{string.Format("{0:P}%", (double)firstPlace / (matches))}");
                }

                ImGui.TableNextColumn();
                ImGui.Text("Second places: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{secondPlace:N0}");
                ImGui.TableNextColumn();
                if(matches > 0) {
                    ImGui.Text($"{string.Format("{0:P}%", (double)secondPlace / (matches))}");
                }

                ImGui.TableNextColumn();
                ImGui.Text("Third places: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{thirdPlace:N0}");
                ImGui.TableNextColumn();
                if(matches > 0) {
                    ImGui.Text($"{string.Format("{0:P}%", (double)thirdPlace / (matches))}");
                }

                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.Text("Average place: ");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawColorScale((float)averagePlace, _plugin.Configuration.Colors.StatHigh, _plugin.Configuration.Colors.StatLow, 1.5f, 2.5f, _plugin.Configuration.ColorScaleStats, averagePlace.ToString("0.00"));
            }
        }
    }
}
