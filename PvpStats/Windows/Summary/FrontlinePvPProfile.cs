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

    public void Refresh() {

    }

    public unsafe void Draw() {
        var pvpProfile = PvPProfile.Instance();
        ImGuiHelper.HelpMarker("Uses game server-originating data from your PvP profile.", false);
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
        using(var table = ImRaii.Table($"t1###{matches.GetHashCode()}", 3, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                ImGui.TableSetupColumn("description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 158f);
                ImGui.TableSetupColumn($"value", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);
                ImGui.TableSetupColumn($"rate", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 45f);

                ImGui.TableNextColumn();
                ImGui.Text("Matches: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{matches.ToString("N0")}");
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                ImGui.Text("1st Place: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{firstPlace.ToString("N0")}");
                ImGui.TableNextColumn();
                if(matches > 0) {
                    ImGui.Text($"{string.Format("{0:P}%", (double)firstPlace / (matches))}");
                }

                ImGui.TableNextColumn();
                ImGui.Text("2nd Place: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{secondPlace.ToString("N0")}");
                ImGui.TableNextColumn();
                if(matches > 0) {
                    ImGui.Text($"{string.Format("{0:P}%", (double)secondPlace / (matches))}");
                }

                ImGui.TableNextColumn();
                ImGui.Text("3rd Place: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{thirdPlace.ToString("N0")}");
                ImGui.TableNextColumn();
                if(matches > 0) {
                    ImGui.Text($"{string.Format("{0:P}%", (double)thirdPlace / (matches))}");
                }
            }
        }
    }
}
