using Dalamud.Interface.Colors;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Match;
using System.Linq;
using System.Numerics;
using System.Threading;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace PvpStats.Windows.Detail;

enum SortableColumn {
    Name,
    Kills,
    Deaths,
    Assists,
    DamageDealt,
    DamageTaken,
    HPRestored,
    TimeOnCrystal
}

internal class CrystallineConflictMatchDetail : Window {

    private Plugin _plugin;
    private CrystallineConflictMatch _dataModel;
    private SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

    internal CrystallineConflictMatchDetail(Plugin plugin, CrystallineConflictMatch match) : base($"Match Details: {match.Id}") {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Appearing;
        CollapsedCondition = ImGuiCond.Appearing;
        Position = new Vector2(0, 0);
        Collapsed = false;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(800, 1500)
        };
        _plugin = plugin;
        _dataModel = match;

        //sort team players
        foreach (var team in _dataModel.Teams) {
            team.Value.Players = team.Value.Players.OrderBy(p => p.Job).ToList();
        }
    }

    public void Open(CrystallineConflictMatch match) {
        _dataModel = match;
        IsOpen = true;
    }

    public override void OnClose() {
        _plugin.WindowSystem.RemoveWindow(this);
        foreach (var window in _plugin.WindowSystem.Windows) {
            _plugin.Log.Debug($"window name: {window.WindowName}");
        }
    }

    public override void PreDraw() {
        base.PreDraw();
    }

    public override void Draw() {
        ImGui.BeginTable("header", 2, ImGuiTableFlags.PadOuterX);
        ImGui.TableSetupColumn("arena", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("time", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        //ImGui.Indent();
        ImGui.Text($"{MatchHelper.GetArenaName(_dataModel.Arena)}");
        ImGui.TableNextColumn();
        var dutyStartTime = _dataModel.DutyStartTime.ToString();
        ImGuiHelper.RightAlignCursor(dutyStartTime);
        ImGui.Text($"{dutyStartTime}");
        ImGui.EndTable();

        ImGui.BeginTable("subheader", 3, ImGuiTableFlags.PadOuterX);
        ImGui.TableSetupColumn("queue", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("result", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("duration", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text($"{_dataModel.MatchType}");
        ImGui.TableNextColumn();
        bool noWinner = _dataModel.MatchWinner is null;
        bool isWin = _dataModel.MatchWinner == _dataModel.LocalPlayerTeam?.TeamName;
        var color = isWin ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
        color = noWinner ? ImGuiColors.DalamudGrey : color;
        string resultText = isWin ? "WIN" : "LOSS";
        resultText = noWinner ? "UNKNOWN" : resultText;
        ImGuiHelpers.CenterCursorForText(resultText);
        ImGui.TextColored(color, resultText);
        ImGui.TableNextColumn();
        string durationText = "";
        if (_dataModel.MatchStartTime != null && _dataModel.MatchEndTime != null) {
            var duration = _dataModel.MatchEndTime - _dataModel.MatchStartTime;
            durationText = $"{duration.Value.Minutes}{duration.Value.ToString(@"\:ss")}";
            ImGuiHelper.RightAlignCursor(durationText);
            ImGui.Text(durationText);
        }
        ImGui.EndTable();

        if (_dataModel.Teams.Count == 2) {
            var firstTeam = _dataModel.Teams.ElementAt(0).Value;
            var secondTeam = _dataModel.Teams.ElementAt(1).Value;

            ImGui.BeginTable("teams", 2, ImGuiTableFlags.PadOuterX);
            ImGui.TableSetupColumn("team1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("team2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var firstTeamName = MatchHelper.GetTeamName(firstTeam.TeamName);
            ImGuiHelper.CenterAlignCursor(firstTeamName);
            ImGui.Text($"{firstTeamName}");
            ImGui.TableNextColumn();
            var secondTeamName = MatchHelper.GetTeamName(secondTeam.TeamName);
            ImGuiHelper.CenterAlignCursor(secondTeamName);
            ImGui.Text($"{secondTeamName}");
            ImGui.TableNextColumn();
            var firstTeamProgress = string.Format("   {0:P1}%", firstTeam.Progress / 100f);
            ImGuiHelper.CenterAlignCursor(firstTeamProgress);
            ImGui.Text($"{firstTeamProgress}");
            ImGui.TableNextColumn();
            var secondTeamProgress = string.Format("   {0:P1}%", secondTeam.Progress / 100f);
            ImGuiHelper.CenterAlignCursor(secondTeamProgress);
            ImGui.Text($"{secondTeamProgress}");
            ImGui.EndTable();

            ImGui.BeginTable("players", 6, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoClip);
            ImGui.TableSetupColumn("rankteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
            ImGui.TableSetupColumn("playerteam1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("jobteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 26f);
            ImGui.TableSetupColumn("jobteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 26f);
            ImGui.TableSetupColumn("playerteam2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("rankteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
            ImGui.TableNextRow();

            int maxSize = int.Max(firstTeam.Players.Count, secondTeam.Players.Count);

            for (int i = 0; i < maxSize; i++) {
                if (i < firstTeam.Players.Count) {
                    ImGui.TableNextColumn();
                    string rank0 = firstTeam.Players[i].Rank?.Tier != ArenaTier.None ? firstTeam.Players[i].Rank!.ToString() : "";
                    ImGuiHelper.RightAlignCursor(rank0);
                    ImGui.Text(rank0);
                    ImGui.TableNextColumn();
                    var playerColor0 = _dataModel.LocalPlayerTeam is not null && firstTeam.TeamName == _dataModel.LocalPlayerTeam.TeamName ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                    playerColor0 = _dataModel.LocalPlayer.Equals(firstTeam.Players[i]) ? ImGuiColors.DalamudYellow : playerColor0;
                    string playerName0 = firstTeam.Players[i].Alias.Name;
                    ImGuiHelper.RightAlignCursor(playerName0);
                    ImGui.TextColored(playerColor0, playerName0);
                    ImGui.TableNextColumn();
                    //string playerJob0 = firstTeam.Players[i].Job.ToString();
                    //ImGui.Text(playerJob0);
                    ImGui.Image(_plugin.JobIcons[firstTeam.Players[i].Job].ImGuiHandle, new Vector2(24, 24));
                }
                else {
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                }

                if (i < secondTeam.Players.Count) {
                    ImGui.TableNextColumn();
                    //string playerJob1 = secondTeam.Players[i].Job.ToString();
                    //ImGui.Text(playerJob1);
                    ImGui.Image(_plugin.JobIcons[secondTeam.Players[i].Job].ImGuiHandle, new Vector2(24, 24));
                    ImGui.TableNextColumn();
                    var playerColor1 = _dataModel.LocalPlayerTeam is not null && secondTeam.TeamName == _dataModel.LocalPlayerTeam.TeamName ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                    playerColor1 = _dataModel.LocalPlayer.Equals(secondTeam.Players[i]) ? ImGuiColors.DalamudYellow : playerColor1;
                    string playerName1 = secondTeam.Players[i].Alias.Name;
                    ImGui.TextColored(playerColor1, $"     {playerName1}");
                    ImGui.TableNextColumn();
                    string rank1 = secondTeam.Players[i].Rank?.Tier != ArenaTier.None ? secondTeam.Players[i].Rank!.ToString() : "";
                    ImGui.Text(rank1);
                }
                else {
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                }

                //ImGui.TableNextColumn();
            }
            ImGui.EndTable();
        }
        ImGui.NewLine();
        if (_dataModel.PostMatch is null) {
            ImGui.Text("Post game statistics unavailable.");
        } else {
            ImGui.BeginTable("players", 8, ImGuiTableFlags.Sortable);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Kills", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f);
            ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f);
            ImGui.TableSetupColumn("Assists", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f);
            ImGui.TableSetupColumn("Damage\nDealt", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f);
            ImGui.TableSetupColumn("Damage\nTaken", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f);
            ImGui.TableSetupColumn("HP\nRestored", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f);
            ImGui.TableSetupColumn("Time on\nCrystal", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60f);

            //ImGui.TableHeadersRow();
            ImGui.TableNextColumn();
            ImGui.TableHeader("");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
            ImGui.TableHeader("Kills");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
            ImGui.TableHeader("Deaths");
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
            ImGui.TableHeader("Assists");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Damage\nDealt");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Damage\nTaken");
            ImGui.TableNextColumn();
            ImGui.TableHeader("HP\nRestored");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Time on\nCrystal");

            ImGuiTableSortSpecsPtr sortSpecs;

            //if(sortSpecs == ImGui.TableGetSortSpecs())


            ImGui.TableNextRow();


            foreach (var team in _dataModel.PostMatch.Teams) {
                //ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.7058824f, 0f, 0f, 1f));
                ImGui.TableNextColumn();
                var rowColor = team.Key == _dataModel.LocalPlayerTeam.TeamName ? new Vector4(0f, 0.6f, 1f, 0.5f) : new Vector4(0.7058824f, 0f, 0f, 0.5f);
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
                ImGui.Text($" {MatchHelper.GetTeamName(team.Value.TeamName)}");
                ImGui.TableNextColumn();
                ImGui.Text($"{team.Value.TeamStats.Kills}");
                ImGui.TableNextColumn();
                ImGui.Text($"{team.Value.TeamStats.Deaths}");
                ImGui.TableNextColumn();
                ImGui.Text($"{team.Value.TeamStats.Assists}");
                ImGui.TableNextColumn();
                ImGui.Text($"{team.Value.TeamStats.DamageDealt}");
                ImGui.TableNextColumn();
                ImGui.Text($"{team.Value.TeamStats.DamageTaken}");
                ImGui.TableNextColumn();
                ImGui.Text($"{team.Value.TeamStats.HPRestored}");
                ImGui.TableNextColumn();
                ImGui.Text($"{team.Value.TeamStats.TimeOnCrystal.Minutes}{team.Value.TeamStats.TimeOnCrystal.ToString(@"\:ss")}");
                //ImGui.PopStyleColor();
                foreach (var player in team.Value.PlayerStats) {
                    ImGui.TableNextColumn();
                    rowColor = team.Key == _dataModel.LocalPlayerTeam.TeamName ? new Vector4(0f, 0.6f, 1f, 0.2f) : new Vector4(0.7058824f, 0f, 0f, 0.2f);
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
                    //ImGui.Text($" {player.Player.Name}");
                    ImGui.TextColored(_dataModel.LocalPlayer.Equals(player.Player) ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite, $" {player.Player.Name}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.Kills}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.Deaths}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.Assists}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.DamageDealt}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.DamageTaken}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.HPRestored}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{player.TimeOnCrystal.Minutes}{player.TimeOnCrystal.ToString(@"\:ss")}");
                }
            }
            ImGui.EndTable();
        }
        //ImGui.Image(_plugin.TextureProvider.GetIcon(62123).ImGuiHandle, new Vector2(25,25));

        //ImGui.Text(_dataModel.Teams.ElementAt(0).Value.Players
    }

}
