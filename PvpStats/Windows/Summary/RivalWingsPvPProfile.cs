using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using PvpStats.Helpers;

namespace PvpStats.Windows.Summary;
internal class RivalWingsPvPProfile {

    private Plugin _plugin;

    public RivalWingsPvPProfile(Plugin plugin) {
        _plugin = plugin;
    }

    public unsafe void Draw() {
        var pvpProfile = PvPProfile.Instance();
        ImGuiHelper.HelpMarker("This data comes from SE's game servers.", false);
        ImGui.TextColored(_plugin.Configuration.Colors.Header, "Overall Performance:");
        if(pvpProfile != null) {
            DrawTable(pvpProfile->RivalWingsTotalMatches, pvpProfile->RivalWingsTotalMatchesWon);
        }
        ImGui.Separator();
        ImGui.TextColored(_plugin.Configuration.Colors.Header, "Weekly Performance:");
        if(pvpProfile != null) {
            DrawTable(pvpProfile->RivalWingsWeeklyMatches, pvpProfile->RivalWingsWeeklyMatchesWon);
        }
    }

    private void DrawTable(uint matches, uint wins) {
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
                ImGui.Text("Wins: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{wins:N0}");
                ImGui.TableNextColumn();
                if(matches > 0) {
                    ImGui.Text($"{string.Format("{0:P}%", (double)wins / (matches))}");
                }
            }
        }
    }
}
