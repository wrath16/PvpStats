﻿using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PvpStats.Windows.Detail;
internal class RivalWingsMatchDetail : MatchDetail<RivalWingsMatch> {

    Dictionary<PlayerAlias, RWScoreboardDouble> _playerContributions = [];

    Dictionary<int, Dictionary<RivalWingsMech, double>>? _allianceMechTimes;

    public RivalWingsMatchDetail(Plugin plugin, RivalWingsMatch match) : base(plugin, plugin.RWCache, match) {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 500),
            MaximumSize = new Vector2(5000, 5000)
        };
        //Size = new Vector2(980,900);
        //SizeCondition = ImGuiCond.Appearing;

        _playerContributions = match.GetPlayerContributions();

        if(match.PlayerMechTime != null) {
            _allianceMechTimes = [];
            for(int i = 0; i < 6; i++) {
                _allianceMechTimes.Add(i, new() {
                    {RivalWingsMech.Chaser, 0 },
                    {RivalWingsMech.Oppressor, 0 },
                    {RivalWingsMech.Justice, 0 },
                });
            }
            foreach(var playerMechTime in match.PlayerMechTime) {
                var alliance = match.Players?.Where(x => x.Name.Equals(playerMechTime.Key)).FirstOrDefault()?.Alliance;
                if(alliance != null) {
                    _allianceMechTimes[(int)alliance][RivalWingsMech.Chaser] += playerMechTime.Value[RivalWingsMech.Chaser];
                    _allianceMechTimes[(int)alliance][RivalWingsMech.Oppressor] += playerMechTime.Value[RivalWingsMech.Oppressor];
                    _allianceMechTimes[(int)alliance][RivalWingsMech.Justice] += playerMechTime.Value[RivalWingsMech.Justice];
                    //Plugin.Log.Debug($"adding {playerMechTime.Key} to alliance {alliance}");
                }
            }
        }

        SortByColumn(0, ImGuiSortDirection.Ascending);
    }

    public override void Draw() {
        if(Plugin.Configuration.ShowBackgroundImage) {
            var cursorPosBefore = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - (250 / 2 + 0f) * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY((ImGui.GetCursorPos().Y + 40f * ImGuiHelpers.GlobalScale));
            ImGui.Image(Plugin.WindowManager.RWBannerImage.ImGuiHandle, new Vector2(250, 240) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, new Vector4(1, 1, 1, 0.1f));
            ImGui.SetCursorPos(cursorPosBefore);
        }

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
            }
        }

        using(var tabBar = ImRaii.TabBar("TabBar")) {

            if(Match.PlayerScoreboards != null) {
                using(var tab = ImRaii.TabItem("Players")) {
                    if(tab) {
                        ImGuiComponents.ToggleButton("##showPercentages", ref ShowPercentages);
                        ImGui.SameLine();
                        ImGui.Text("Show team contributions");
                        ImGuiHelper.HelpMarker("Right-click table header to show and hide columns including extra metrics.");
                        DrawPlayerStatsTable();
                    }
                }
            }

            if(Match.AllianceStats != null) {
                using(var tab = ImRaii.TabItem("Alliances")) {
                    if(tab) {
                        DrawAlliances();
                    }
                }
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
                    if(Match.TeamMechTime != null) {
                        DrawMechTable(team, Match.TeamMechTime[team], reverse);
                    } else {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 80f * ImGuiHelpers.GlobalScale);
                    }
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 49f * ImGuiHelpers.GlobalScale);
                    DrawTowerTable(team, RivalWingsStructure.Tower2, reverse);
                } else {
                    DrawCoreTable(team);
                }
                ImGui.TableNextColumn();
                if(reverse) {
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 50f * ImGuiHelpers.GlobalScale);
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 20f * ImGuiHelpers.GlobalScale);
                    var width = ImGui.GetStyle().CellPadding.X * 4f * 2 + 96f * ImGuiHelpers.GlobalScale;
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - width / 2);
                    DrawCoreTable(team);
                } else {
                    DrawTowerTable(team, RivalWingsStructure.Tower1, reverse);
                    //ImGui.SetNextItemWidth(40f * ImGuiHelpers.GlobalScale);
                    if(Match.TeamMechTime != null) {
                        DrawMechTable(team, Match.TeamMechTime[team], reverse);
                    } else {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 80f * ImGuiHelpers.GlobalScale);
                    }
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

    private void DrawCoreTable(RivalWingsTeamName team) {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 36f * ImGuiHelpers.GlobalScale);
        //Plugin.Log.Debug($"{ImGui.GetStyle().CellPadding.X * 2}");
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X * 0, 0));
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

    private void DrawMechTable(RivalWingsTeamName team, Dictionary<RivalWingsMech, double> mechTime, bool reverse) {
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
                string text = (mechTime[mech] / Match.MatchDuration.Value.TotalSeconds).ToString("0.0");
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

    private void DrawPlayerStatsTable() {
        using var table = ImRaii.Table($"postmatchplayers##{Match.Id}", 19,
            ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.PadOuterX
            , new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
        //new Vector2(ImGui.GetContentRegionAvail().X, 550f * ImGuiHelpers.GlobalScale)
        if(!table) return;
        ImGui.TableSetupColumn("Alliance", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 10f, 3);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 200f, 0);
        ImGui.TableSetupColumn("Home World", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 110f, 1);
        ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 50f, 2);
        ImGui.TableSetupColumn("Kills", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Kills".GetHashCode());
        ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Deaths".GetHashCode());
        ImGui.TableSetupColumn("Assists", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 52f, (uint)"Assists".GetHashCode());
        ImGui.TableSetupColumn("Damage to PCs", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToPCs".GetHashCode());
        ImGui.TableSetupColumn("Damage to Other", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToOther".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageTaken".GetHashCode());
        ImGui.TableSetupColumn("HP Restored", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"HPRestored".GetHashCode());
        ImGui.TableSetupColumn("Special", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 60f, (uint)"Special1".GetHashCode());
        ImGui.TableSetupColumn("Ceruleum", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f, (uint)"Ceruleum".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Kill/Assist", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerKA".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerLife".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageTakenPerLife".GetHashCode());
        ImGui.TableSetupColumn("HP Restored per Life", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"HPRestoredPerLife".GetHashCode());
        ImGui.TableSetupColumn("KDA Ratio", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"KDA".GetHashCode());

        ImGui.TableSetupScrollFreeze(2, 1);

        ImGui.TableNextColumn();
        ImGui.TableHeader("Alliance");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        //ImGuiHelper.CenterAlignCursor("Name");
        ImGui.TableHeader("Name");
        ImGui.TableNextColumn();
        //ImGuiHelper.CenterAlignCursor("Home World");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeader("Home World");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        //ImGuiHelper.CenterAlignCursor("Job");
        ImGui.TableHeader("Job");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeader("Kills");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeader("Deaths");
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeader("Assists");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage\nto PCs");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage\nto Other");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage\nDealt");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage\nTaken");
        ImGui.TableNextColumn();
        ImGui.TableHeader("HP\nRestored");
        ImGui.TableNextColumn();
        ImGui.TableHeader("");
        ImGuiHelper.HelpMarker("Not sure what this is. It's related to healing.");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Ceru-\nleum");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage Dealt\nper Kill/Assist");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage Dealt\nper Life");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Damage Taken\nper Life");
        ImGui.TableNextColumn();
        ImGui.TableHeader("HP Restored\nper Life");
        ImGui.TableNextColumn();
        ImGui.TableHeader("KDA\nRatio");

        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(sortSpecs.SpecsDirty) {
            SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
            sortSpecs.SpecsDirty = false;
        }

        foreach(var row in Match.PlayerScoreboards!) {
            var player = Match.Players!.Where(x => x.Name.Equals(row.Key)).First();
            var playerAlias = (PlayerAlias)row.Key;
            ImGui.TableNextColumn();
            //bool isPlayer = row.Key.Player != null;
            //bool isPlayerTeam = row.Key.Team == _dataModel.LocalPlayerTeam?.TeamName;
            var rowColor = Plugin.Configuration.GetRivalWingsTeamColor(player.Team) - new Vector4(0f, 0f, 0f, 0.7f);
            var textColor = Match.LocalPlayer is not null && Match.LocalPlayer.Equals(playerAlias) ? Plugin.Configuration.Colors.CCLocalPlayer : ImGuiColors.DalamudWhite;
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            string alliance = GetAllianceLetter(player.Alliance);
            ImGui.TextColored(textColor, $" {alliance} ");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $" {playerAlias.Name} ");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{player.Name.HomeWorld}");
            ImGui.TableNextColumn();
            //ImGuiHelper.CenterAlignCursor(player.Job?.ToString() ?? "");
            ImGui.TextColored(textColor, $"{player.Job}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Kills) : row.Value.Kills)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Deaths) : row.Value.Deaths)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Assists) : row.Value.Assists)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].DamageToPCs) : row.Value.DamageToPCs)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].DamageToOther) : row.Value.DamageToOther)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].DamageDealt) : row.Value.DamageDealt)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].DamageTaken) : row.Value.DamageTaken)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].HPRestored) : row.Value.HPRestored)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Special1) : row.Value.Special1)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{(ShowPercentages ? string.Format("{0:P1}%", _playerContributions[player.Name].Ceruleum) : row.Value.Ceruleum)}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{row.Value.DamageDealtPerKA}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{row.Value.DamageDealtPerLife}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{row.Value.DamageTakenPerLife}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{row.Value.HPRestoredPerLife}");
            ImGui.TableNextColumn();
            ImGui.TextColored(textColor, $"{string.Format("{0:0.00}", row.Value.KDA)}");
        }
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        Func<KeyValuePair<string, RivalWingsScoreboard>, object> comparator = (r) => 0;

        //0 = name
        //1 = homeworld
        //2 = job
        //3 = alliance
        if(columnId == 0) {
            comparator = (r) => Match.Players.First(x => x.Name.Equals(r.Key)).Name.Name ?? "";
        } else if(columnId == 1) {
            comparator = (r) => Match.Players.First(x => x.Name.Equals(r.Key)).Name.HomeWorld ?? "";
        } else if(columnId == 2) {
            comparator = (r) => Match.Players.First(x => x.Name.Equals(r.Key)).Job ?? 0;
        } else if(columnId == 3) {
            comparator = (r) => {
                var player = Match.Players.First(x => x.Name.Equals(r.Key));
                return ((int)player.Team * 6) + player.Alliance;
            };
        } else {
            bool propFound = false;
            if(ShowPercentages) {
                var props = typeof(RWScoreboardDouble).GetProperties();
                foreach(var prop in props) {
                    var propId = prop.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        Plugin.Log.Debug($"sorting by {prop.Name}");
                        comparator = (r) => prop.GetValue(_playerContributions[(PlayerAlias)r.Key]) ?? 0;
                        propFound = true;
                        break;
                    }
                }
            }
            if(!propFound) {
                var props = typeof(RivalWingsScoreboard).GetProperties();
                foreach(var prop in props) {
                    var propId = prop.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        Plugin.Log.Debug($"sorting by {prop.Name}");
                        comparator = (r) => prop.GetValue(r.Value) ?? 0;
                        break;
                    }
                }
            }
        }
        Match.PlayerScoreboards = direction == ImGuiSortDirection.Ascending ? Match.PlayerScoreboards.OrderBy(comparator).ToDictionary()
            : Match.PlayerScoreboards.OrderByDescending(comparator).ToDictionary();
    }

    private void DrawAlliances() {
        if(Match.LocalPlayerTeam == null) {
            return;
        }

        using(var table = ImRaii.Table($"AllianceTable", 3, ImGuiTableFlags.NoClip | ImGuiTableFlags.Borders)) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthStretch);
            for(int i = 0; i < 6; i++) {
                ImGui.TableNextColumn();
                DrawAllianceStatTable((RivalWingsTeamName)Match.LocalPlayerTeam, i);
            }
        }

    }

    private void DrawAllianceStatTable(RivalWingsTeamName team, int alliance) {
        var allianceStats = Match.AllianceStats?[alliance];
        if(allianceStats == null) {
            return;
        }

        //using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));

        var drawText = (string text) => {
            ImGuiHelper.CenterAlignCursor(text);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
            ImGui.Text(text);
        };

        var drawImage = (nint image, float size) => {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
            ImGui.Image(image, new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0f), new Vector2(1));
        };

        using(var table = ImRaii.Table($"{team}{alliance}--AllianceTable", 2, ImGuiTableFlags.NoClip)) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 50f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch, 50f * ImGuiHelpers.GlobalScale);

            ImGui.TableNextColumn();
            using(var table2 = ImRaii.Table($"{team}{alliance}--AllianceHeaderTable", 1)) {
                ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                //using( var font = ImRaii.PushFont(UiBuilder.DefaultFont)) {
                //    UiBuilder.
                //}
                drawText(GetAllianceLetter(alliance));
                //ImGui.Text(GetAllianceLetter(alliance));
                ImGui.TableNextColumn();
                var size = 30f;
                var handle = Plugin.WindowManager.SoaringIcons[allianceStats.SoaringStacks].ImGuiHandle;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14f * ImGuiHelpers.GlobalScale);
                ImGui.Image(handle, new Vector2(size * ImGuiHelpers.GlobalScale * 0.75f, size * ImGuiHelpers.GlobalScale), new Vector2(0f), new Vector2(1));
                //ImGui.Image(Plugin.WindowManager.SoaringIcons[allianceStats.SoaringStacks].ImGuiHandle, new Vector2(25f * ImGuiHelpers.GlobalScale, 25f * ImGuiHelpers.GlobalScale));
                //ImGui.TableNextColumn();
            }

            //draw mech stats
            if(Match.LocalPlayerTeam != null && _allianceMechTimes != null) {
                ImGui.TableNextColumn();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 49f * ImGuiHelpers.GlobalScale);
                DrawMechTable((RivalWingsTeamName)Match.LocalPlayerTeam, _allianceMechTimes[alliance], false);
            }
        }
    }

    private string GetAllianceLetter(int alliance) {
        return alliance switch {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            4 => "E",
            5 => "F",
            _ => "?",
        };
    }

    protected override string BuildCSV() {
        throw new NotImplementedException();
    }
}
