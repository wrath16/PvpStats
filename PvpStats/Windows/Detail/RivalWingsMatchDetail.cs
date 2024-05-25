using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using PvpStats.Types.Player;
using static Lumina.Data.Parsing.Layer.LayerCommon;
using System.Numerics;
using Dalamud.Interface.Internal;
using static PvpStats.Types.ClientStruct.RivalWingsContentDirector;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using Dalamud.Interface.Colors;

namespace PvpStats.Windows.Detail;
internal class RivalWingsMatchDetail : MatchDetail<RivalWingsMatch> {
    public RivalWingsMatchDetail(Plugin plugin, RivalWingsMatch match) : base(plugin, plugin.RWCache, match) {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(450, 500),
            MaximumSize = new Vector2(5000, 5000)
        };
    }

    public override void Draw() {
        using(var table = ImRaii.Table("header", 3, ImGuiTableFlags.PadOuterX)) {
            if(table) {
                ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                //ImGui.Indent();
                if(Match.Arena != null) {
                    ImGui.Text($"{MatchHelper.GetArenaName((RivalWingsMap)Match.Arena)}");
                }
                ImGui.TableNextColumn();
                DrawFunctions();
                ImGui.TableNextColumn();
                var dutyStartTime = Match.DutyStartTime.ToString();
                ImGuiHelper.RightAlignCursor(dutyStartTime);
                ImGui.Text($"{dutyStartTime}");
                ImGui.TableNextRow(ImGuiTableRowFlags.None, 5f * ImGuiHelpers.GlobalScale);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                bool isWin = Match.IsWin;
                bool isLoss = Match.IsLoss;
                var color = isWin ? Plugin.Configuration.Colors.Win : isLoss ? Plugin.Configuration.Colors.Loss : Plugin.Configuration.Colors.Other;
                string resultText = isWin ? "WIN" : isLoss ? "LOSS" : "???";
                ImGuiHelper.CenterAlignCursor(resultText);
                ImGui.TextColored(color, resultText);
                ImGui.TableNextColumn();
                if(Match.MatchDuration != null) {
                    string durationText = ImGuiHelper.GetTimeSpanString((TimeSpan)Match.MatchDuration);
                    ImGuiHelper.RightAlignCursor(durationText);
                    ImGui.TextUnformatted(durationText);
                }
            }
        }

        using(var table = ImRaii.Table("teamTables", 3, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoClip)) {
            if(table) {
                ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                DrawTeamHeader(RivalWingsTeamName.Falcons);
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                DrawTeamHeader(RivalWingsTeamName.Ravens);

                ImGui.TableNextColumn();
                DrawTeamTable(RivalWingsTeamName.Falcons, false);
                ImGui.TableNextColumn();
                if(Match.Mercs != null && Match.Supplies != null) {
                    var tableWidth = 55f * ImGuiHelpers.GlobalScale + ImGui.GetStyle().CellPadding.X * 3;
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - tableWidth / 2);
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 30f * ImGuiHelpers.GlobalScale);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 9f * ImGuiHelpers.GlobalScale);
                    DrawMidMercTable();
                }
                ImGui.TableNextColumn();
                DrawTeamTable(RivalWingsTeamName.Ravens, true);
                ////DrawTeamTable(RivalWingsTeamName.Falcons, false);
                //DrawTowerTable(RivalWingsTeamName.Falcons, RivalWingsStructure.Tower1, false);
                //DrawMechTable(RivalWingsTeamName.Falcons, false);
                //DrawTowerTable(RivalWingsTeamName.Falcons, RivalWingsStructure.Tower2, false);
                //ImGui.TableNextColumn();
                ////DrawTeamTable(RivalWingsTeamName.Ravens, true);
                //DrawTowerTable(RivalWingsTeamName.Ravens, RivalWingsStructure.Tower1, true);
                //DrawMechTable(RivalWingsTeamName.Ravens, true);
                //DrawTowerTable(RivalWingsTeamName.Ravens, RivalWingsStructure.Tower2, true);
            }
        }
    }

    private void DrawTeamTable(RivalWingsTeamName team, bool reverse) {
        using(var table = ImRaii.Table($"{team}--MainTable", 2, ImGuiTableFlags.NoClip)) {
            if(table) {
                ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                if(reverse) {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 49f * ImGuiHelpers.GlobalScale);
                    DrawTowerTable(team, RivalWingsStructure.Tower1, reverse);
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 49f * ImGuiHelpers.GlobalScale);
                    DrawMechTable(team, reverse);
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 49f * ImGuiHelpers.GlobalScale);
                    DrawTowerTable(team, RivalWingsStructure.Tower2, reverse);
                } else {
                    DrawCoreTable(team);
                }
                ImGui.TableNextColumn();
                if(reverse) {
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 50f * ImGuiHelpers.GlobalScale);
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 20f * ImGuiHelpers.GlobalScale);
                    var width = ImGui.GetStyle().CellPadding.X * 4f * 2  + 96f * ImGuiHelpers.GlobalScale;
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - width / 2);
                    DrawCoreTable(team);
                } else {
                    DrawTowerTable(team, RivalWingsStructure.Tower1, reverse);
                    //ImGui.SetNextItemWidth(40f * ImGuiHelpers.GlobalScale);
                    DrawMechTable(team, reverse);
                    DrawTowerTable(team, RivalWingsStructure.Tower2, reverse);
                }
            }
        }
    }

    private void DrawTeamHeader(RivalWingsTeamName team) {
        ImGuiHelper.CenterAlignCursor(team.ToString());
        bool isPlayerTeam = Match.LocalPlayerTeam == team;
        var color = isPlayerTeam ? Plugin.Configuration.Colors.CCLocalPlayer : ImGuiColors.DalamudWhite;
        ImGui.TextColored(color, team.ToString());
    }

    //private void DrawTeamTable2(RivalWingsTeamName team, bool reverse) {
    //    //using(var table = ImRaii.Table($"{team}--StructMechTable", 3, ImGuiTableFlags.Borders)) {
    //    //    ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
    //    //    ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
    //    //    ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthStretch);

    //    //    //team name
    //    //    ImGui.TableNextColumn();
    //    //    ImGui.TableNextColumn();
    //    //    var teamName = MatchHelper.GetTeamName(team);
    //    //    ImGuiHelper.CenterAlignCursor(teamName);
    //    //    ImGui.Text(teamName);
    //    //    ImGui.TableNextColumn();

    //    //    //icons
    //    //    using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
    //    //        ImGui.TableNextColumn();
    //    //        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - 40 / 2 * ImGuiHelpers.GlobalScale);
    //    //        if(!reverse) {
    //    //            ImGui.Image(Plugin.WindowManager.Tower1Icons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        } else {
    //    //            ImGui.Image(Plugin.WindowManager.CoreIcons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        }
    //    //        ImGui.TableNextColumn();
    //    //        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - 40 / 2 * ImGuiHelpers.GlobalScale);
    //    //        ImGui.Image(Plugin.WindowManager.Tower2Icons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        ImGui.TableNextColumn();
    //    //        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - 40 / 2 * ImGuiHelpers.GlobalScale);
    //    //        if(!reverse) {
    //    //            ImGui.Image(Plugin.WindowManager.CoreIcons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        } else {
    //    //            ImGui.Image(Plugin.WindowManager.Tower1Icons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        }
    //    //    }

    //    //    //health values
    //    //    ImGui.TableNextColumn();
    //    //    string structHealth1;
    //    //    if(!reverse) {
    //    //        structHealth1 = Match.StructureHealth[team][RivalWingsStructure.Tower1].ToString();
    //    //    } else {
    //    //        structHealth1 = Match.StructureHealth[team][RivalWingsStructure.Core].ToString();
    //    //    }
    //    //    ImGuiHelper.CenterAlignCursor(structHealth1);
    //    //    ImGui.Text(structHealth1);
    //    //    ImGui.TableNextColumn();
    //    //    string structHealth2 = Match.StructureHealth[team][RivalWingsStructure.Tower2].ToString();
    //    //    ImGuiHelper.CenterAlignCursor(structHealth2);
    //    //    ImGui.Text(structHealth2);
    //    //    ImGui.TableNextColumn();
    //    //    string structHealth3;
    //    //    if(!reverse) {
    //    //        structHealth3 = Match.StructureHealth[team][RivalWingsStructure.Core].ToString();
    //    //    } else {
    //    //        structHealth3 = Match.StructureHealth[team][RivalWingsStructure.Tower1].ToString();
    //    //    }
    //    //    ImGuiHelper.CenterAlignCursor(structHealth3);
    //    //    ImGui.Text(structHealth3);

    //    //    ImGui.TableNextRow();
    //    //    ImGui.TableNextRow();
    //    //    using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
    //    //        ImGui.TableNextColumn();
    //    //        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - 40 / 2 * ImGuiHelpers.GlobalScale);
    //    //        if(!reverse) {
    //    //            ImGui.Image(Plugin.WindowManager.ChaserIcons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        } else {
    //    //            ImGui.Image(Plugin.WindowManager.JusticeIcons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        }
    //    //        ImGui.TableNextColumn();
    //    //        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - 40 / 2 * ImGuiHelpers.GlobalScale);
    //    //        ImGui.Image(Plugin.WindowManager.OppressorIcons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        ImGui.TableNextColumn();
    //    //        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - 40 / 2 * ImGuiHelpers.GlobalScale);
    //    //        if(!reverse) {
    //    //            ImGui.Image(Plugin.WindowManager.JusticeIcons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        } else {
    //    //            ImGui.Image(Plugin.WindowManager.ChaserIcons[team].ImGuiHandle, new Vector2(40 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale));
    //    //        }
    //    //    }

    //    //    ImGui.TableNextColumn();
    //    //    string mechUptime1;
    //    //    if(!reverse) {
    //    //        mechUptime1 = (Match.TeamMechTime[team][RivalWingsMech.Chaser] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0");
    //    //    } else {
    //    //        mechUptime1 = (Match.TeamMechTime[team][RivalWingsMech.Justice] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0");
    //    //    }
    //    //    ImGuiHelper.CenterAlignCursor(mechUptime1);
    //    //    ImGui.Text(mechUptime1);
    //    //    ImGui.TableNextColumn();
    //    //    string mechUptime2 = (Match.TeamMechTime[team][RivalWingsMech.Oppressor] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0");
    //    //    ImGuiHelper.CenterAlignCursor(mechUptime2);
    //    //    ImGui.Text(mechUptime2);
    //    //    ImGui.TableNextColumn();
    //    //    string mechUptime3;
    //    //    if(!reverse) {
    //    //        mechUptime3 = (Match.TeamMechTime[team][RivalWingsMech.Justice] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0");
    //    //    } else {
    //    //        mechUptime3 = (Match.TeamMechTime[team][RivalWingsMech.Chaser] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0");
    //    //    }
    //    //    ImGuiHelper.CenterAlignCursor(mechUptime3);
    //    //    ImGui.Text(mechUptime3);
    //    //}


    //    using(var table = ImRaii.Table($"{team}--StructMechTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.NoClip)) {
    //        ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
    //        ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
    //        ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthStretch);

    //        var drawIcon = (nint image, float size) => {
    //            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
    //            ImGui.Image(image, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale));
    //        };
    //        var drawText = (string text) => {
    //            ImGuiHelper.CenterAlignCursor(text);
    //            ImGui.Text(text);
    //        };

    //        ImGui.TableNextColumn();
    //        if(reverse) {
    //            drawIcon(Plugin.WindowManager.Tower1Icons[team].ImGuiHandle, 40);
    //        }
    //        ImGui.TableNextColumn();
    //        ImGui.TableNextColumn();
    //        if(!reverse) {
    //            drawIcon(Plugin.WindowManager.Tower1Icons[team].ImGuiHandle, 40);
    //        }
    //        ImGui.TableNextColumn();
    //        if(reverse) {
    //            drawText(Match.StructureHealth[team][RivalWingsStructure.Tower1].ToString());
    //        }
    //        ImGui.TableNextColumn();
    //        ImGui.TableNextColumn();
    //        if(!reverse) {
    //            drawText(Match.StructureHealth[team][RivalWingsStructure.Tower1].ToString());
    //        }

    //        ImGui.TableNextRow();
    //        ImGui.TableNextRow();

    //        ImGui.TableNextColumn();
    //        if(reverse) {
    //            drawText((Match.TeamMechTime[team][RivalWingsMech.Chaser] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0"));
    //        }
    //        ImGui.TableNextColumn();
    //        //if(reverse) {
    //        //    drawText((Match.TeamMechTime[team][RivalWingsMech.Chaser] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0"));
    //        //    ImGui.SameLine();
    //        //    drawIcon(Plugin.WindowManager.ChaserIcons[team].ImGuiHandle, 40);
    //        //} else {
    //        //    drawIcon(Plugin.WindowManager.ChaserIcons[team].ImGuiHandle, 40);
    //        //    ImGui.SameLine();
    //        //    drawText((Match.TeamMechTime[team][RivalWingsMech.Chaser] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0"));
    //        //}
    //        drawIcon(Plugin.WindowManager.ChaserIcons[team].ImGuiHandle, 30);
    //        ImGui.TableNextColumn();
    //        if(!reverse) {
    //            drawText((Match.TeamMechTime[team][RivalWingsMech.Chaser] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0"));
    //        }
    //    }
    //}


    //private void DrawStructMechTable(RivalWingsTeamName team, bool reverse) {
    //    using(var table = ImRaii.Table($"{team}--MechTable", 2, ImGuiTableFlags.NoClip | ImGuiTableFlags.Borders)) {
    //        ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);
    //        ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);

    //        var drawIcon = (RivalWingsStructure structure, float size) => {
    //            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
    //            var image = structure switch {
    //                RivalWingsStructure.Core => Plugin.WindowManager.CoreIcons[team].ImGuiHandle,
    //                RivalWingsStructure.Tower1 => Plugin.WindowManager.Tower1Icons[team].ImGuiHandle,
    //                RivalWingsStructure.Tower2 => Plugin.WindowManager.Tower2Icons[team].ImGuiHandle,
    //                _ => Plugin.WindowManager.ChaserIcons[RivalWingsTeamName.Unknown].ImGuiHandle,
    //            };
    //            ImGui.Image(image, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0.1f), new Vector2(0.9f));
    //        };
    //        var drawText = (RivalWingsStructure structure) => {
    //            string text = Match.StructureHealth[team][structure].ToString();
    //            ImGuiHelper.CenterAlignCursor(text);
    //            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
    //            ImGui.Text(text);
    //        };
    //    }
    //}

    private void DrawCoreTable(RivalWingsTeamName team) {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 36f * ImGuiHelpers.GlobalScale);
        //Plugin.Log.Debug($"{ImGui.GetStyle().CellPadding.X * 2}");
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X * 4f, 0));
        using(var table = ImRaii.Table($"{team}--CoreTable", 1, ImGuiTableFlags.NoClip | ImGuiTableFlags.PadOuterX)) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 50f * ImGuiHelpers.GlobalScale);

            var drawIcon = (float size) => {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
                var image = Plugin.WindowManager.CoreIcons[team].ImGuiHandle;
                ImGui.Image(image, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0f), new Vector2(1f));
            };
            var drawText = (RivalWingsStructure structure) => {
                string text = Match.StructureHealth[team][structure].ToString();
                ImGuiHelper.CenterAlignCursor(text);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
                ImGui.Text(text);
            };

            ImGui.TableNextColumn();
            drawIcon(50);
            ImGui.TableNextColumn();
            drawText(RivalWingsStructure.Core);
        }
    }

    private void DrawTowerTable(RivalWingsTeamName team, RivalWingsStructure tower, bool reverse) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using(var table = ImRaii.Table($"{team}{tower}--Table", 2, ImGuiTableFlags.NoClip | ImGuiTableFlags.None, new Vector2(60f, 30f) * ImGuiHelpers.GlobalScale)) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);

            var drawIcon = (RivalWingsStructure structure, float size) => {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
                var image = structure switch {
                    RivalWingsStructure.Core => Plugin.WindowManager.CoreIcons[team].ImGuiHandle,
                    RivalWingsStructure.Tower1 => Plugin.WindowManager.Tower1Icons[team].ImGuiHandle,
                    RivalWingsStructure.Tower2 => Plugin.WindowManager.Tower2Icons[team].ImGuiHandle,
                    _ => Plugin.WindowManager.ChaserIcons[RivalWingsTeamName.Unknown].ImGuiHandle,
                };
                ImGui.Image(image, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0.1f), new Vector2(0.9f));
            };
            var drawText = (RivalWingsStructure structure) => {
                string text = Match.StructureHealth[team][structure].ToString();
                ImGuiHelper.CenterAlignCursor(text);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
                ImGui.Text(text);
            };

            if(!reverse) {
                ImGui.TableNextColumn();
                drawIcon(tower, 30);
                ImGui.TableNextColumn();
                drawText(tower);
            } else {
                ImGui.TableNextColumn();
                drawText(tower);
                ImGui.TableNextColumn();
                drawIcon(tower, 30);
            }
        }
    }

    private void DrawMechTable(RivalWingsTeamName team, bool reverse) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using(var table = ImRaii.Table($"{team}--MechTable", 2, ImGuiTableFlags.NoClip | ImGuiTableFlags.None, new Vector2(60f, 30f) * ImGuiHelpers.GlobalScale)) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);

            var drawIcon = (RivalWingsMech mech, float size) => {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
                var image = mech switch {
                    RivalWingsMech.Chaser => Plugin.WindowManager.ChaserIcons[team].ImGuiHandle,
                    RivalWingsMech.Oppressor => Plugin.WindowManager.OppressorIcons[team].ImGuiHandle,
                    RivalWingsMech.Justice => Plugin.WindowManager.JusticeIcons[team].ImGuiHandle,
                    _ => Plugin.WindowManager.ChaserIcons[RivalWingsTeamName.Unknown].ImGuiHandle,
                };
                ImGui.Image(image, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0.1f), new Vector2(0.9f));
            };
            var drawText = (RivalWingsMech mech) => {
                string text = (Match.TeamMechTime[team][mech] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0");
                ImGuiHelper.CenterAlignCursor(text);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
                ImGui.Text(text);
            };

            RivalWingsMech[] mechs = { RivalWingsMech.Chaser, RivalWingsMech.Oppressor, RivalWingsMech.Justice };
            foreach(var mech in mechs) {
                if(!reverse) {
                    ImGui.TableNextColumn();
                    drawIcon(mech, 25);
                    ImGui.TableNextColumn();
                    drawText(mech);
                } else {
                    ImGui.TableNextColumn();
                    drawText(mech);
                    ImGui.TableNextColumn();
                    drawIcon(mech, 25);
                }
            }
        }
        ImGuiHelper.WrappedTooltip("Average mechs deployed");
    }

    private void DrawMidMercTable() {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using(var table = ImRaii.Table($"MidMercTable", 3, ImGuiTableFlags.NoClip | ImGuiTableFlags.None, 
            new Vector2(55f * ImGuiHelpers.GlobalScale + ImGui.GetStyle().CellPadding.X * 3, (25f * 5 + 1) * ImGuiHelpers.GlobalScale))) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 15f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 25f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthFixed, 15f * ImGuiHelpers.GlobalScale);

            var drawImage = (nint image, float size) => {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
                ImGui.Image(image, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0.2f), new Vector2(0.8f));
            };
            var drawText = (string text) => {
                ImGuiHelper.CenterAlignCursor(text);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
                ImGui.Text(text);
            };

            ImGui.TableNextColumn();
            drawText(Match.Mercs[RivalWingsTeamName.Falcons].ToString());
            ImGui.TableNextColumn();
            drawImage(Plugin.WindowManager.GoblinMercIcon.ImGuiHandle, 25f);
            ImGui.TableNextColumn();
            drawText(Match.Mercs[RivalWingsTeamName.Ravens].ToString());

            RivalWingsSupplies[] supplies = { RivalWingsSupplies.Gobtank, RivalWingsSupplies.Ceruleum, RivalWingsSupplies.Gobbiejuice, RivalWingsSupplies.Gobcrate };
            foreach(var supply in supplies) {
                ImGui.TableNextColumn();
                drawText(Match.Supplies[RivalWingsTeamName.Falcons][supply].ToString());
                ImGui.TableNextColumn();
                //ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 2.5f * ImGuiHelpers.GlobalScale);
                DrawSuppliesIcon(supply, 25f);
                ImGui.TableNextColumn();
                drawText(Match.Supplies[RivalWingsTeamName.Ravens][supply].ToString());
            }
        }
    }
    private void DrawSuppliesIcon(RivalWingsSupplies supplies, float size) {
        Vector2 uv0, uv1;
        switch(supplies) {
            case RivalWingsSupplies.Gobtank:
                uv0 = new Vector2(0);
                uv1 = new Vector2(0.2f, 1 / 3f);
                break;
            case RivalWingsSupplies.Ceruleum:
                uv0 = new Vector2(0.2f, 0);
                uv1 = new Vector2(0.4f, 1 / 3f);
                break;
            case RivalWingsSupplies.Gobbiejuice:
                uv0 = new Vector2(0.4f, 0);
                uv1 = new Vector2(0.6f, 1 / 3f);
                break;
            case RivalWingsSupplies.Gobcrate:
                uv0 = new Vector2(0.6f, 0);
                uv1 = new Vector2(0.8f, 1 / 3f);
                break;
            default:
                uv0 = new Vector2(0.8f, 0);
                uv1 = new Vector2(0.1f, 1 / 3f);
                break;
        };
        ImGui.Image(Plugin.WindowManager.RWSuppliesTexture.ImGuiHandle, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), uv0, uv1);
    }

    protected override string BuildCSV() {
        throw new NotImplementedException();
    }
}
