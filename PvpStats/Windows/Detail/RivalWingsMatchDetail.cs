using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Event;
using PvpStats.Types.Event.RivalWings;
using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace PvpStats.Windows.Detail;
internal class RivalWingsMatchDetail : MatchDetail<RivalWingsMatch> {

    private RivalWingsMatchTimeline? _timeline;
    private List<MatchEvent> _consolidatedEvents = new();

    private RWTeamQuickFilter _teamQuickFilter;
    private Dictionary<PlayerAlias, RWScoreboardDouble>? _playerContributions = [];
    private Dictionary<RivalWingsTeamName, RivalWingsScoreboard>? _teamScoreboard;
    private Dictionary<string, RivalWingsScoreboard>? _scoreboard;
    private Dictionary<string, RivalWingsScoreboard>? _unfilteredScoreboard;
    private Dictionary<int, (Dictionary<RivalWingsMech, double> MechTime, List<PlayerAlias> Pilots)>? _allianceMechTimes;

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
                _allianceMechTimes.Add(i, (new() {
                    { RivalWingsMech.Chaser, 0 },
                    { RivalWingsMech.Oppressor, 0 },
                    { RivalWingsMech.Justice, 0 },
                }, new()));
            }
            foreach(var playerMechTime in match.PlayerMechTime) {
                var alliance = match.Players?.Where(x => x.Name.Equals(playerMechTime.Key)).FirstOrDefault()?.Alliance;
                var alias = (PlayerAlias)playerMechTime.Key;
                if(alliance != null) {
                    _allianceMechTimes[(int)alliance].MechTime[RivalWingsMech.Chaser] += playerMechTime.Value[RivalWingsMech.Chaser];
                    _allianceMechTimes[(int)alliance].MechTime[RivalWingsMech.Oppressor] += playerMechTime.Value[RivalWingsMech.Oppressor];
                    _allianceMechTimes[(int)alliance].MechTime[RivalWingsMech.Justice] += playerMechTime.Value[RivalWingsMech.Justice];
                    if(!_allianceMechTimes[(int)alliance].Pilots.Contains(alias)) {
                        _allianceMechTimes[(int)alliance].Pilots.Add(alias);
                    }
                    //Plugin.Log.Debug($"adding {playerMechTime.Key} to alliance {alliance}");
                }
            }
        }

        if(match.TimelineId != null) {
            _timeline = Plugin.Storage.GetRWTimelines().Query().Where(x => x.Id.Equals(match.TimelineId)).FirstOrDefault();
            if(_timeline != null) {
                _consolidatedEvents = [.. _timeline.MercClaims, .. _timeline.MidClaims];
                //get structure destroyed events
                foreach(var teamStructHealths in _timeline.StructureHealths ?? []) {
                    foreach(var structHealths in teamStructHealths.Value) {
                        var lastHealth = structHealths.Value.Last();
                        if(lastHealth.Health <= 0) {
                            _consolidatedEvents.Add(new StructureHealthEvent(lastHealth.Timestamp, lastHealth.Health) {
                                Structure = structHealths.Key,
                                Team = teamStructHealths.Key,
                            });
                        }
                    }
                }
                //get soaring high events
                foreach(var allianceSoaringStacks in _timeline.AllianceStacks ?? []) {
                    var flyingHighEvent = allianceSoaringStacks.Value.FirstOrDefault(x => x.Count == 20);
                    if(flyingHighEvent != null) {
                        _consolidatedEvents.Add(new AllianceSoaringEvent(flyingHighEvent.Timestamp, flyingHighEvent.Count) {
                            Alliance = allianceSoaringStacks.Key,
                        });
                    }
                }

                //get first mech event
                foreach(var teamMechCountEvents in _timeline.MechCounts ?? []) {
                    MechCountEvent? teamFirstDeployEvent = null;
                    RivalWingsMech? teamFirstDeployMech = null;
                    foreach(var mechCountEvents in teamMechCountEvents.Value) {
                        var firstDeployEvent = mechCountEvents.Value.FirstOrDefault(x => x.Count > 0);
                        if(firstDeployEvent != null && (teamFirstDeployEvent == null || firstDeployEvent.Timestamp < teamFirstDeployEvent.Timestamp)) {
                            teamFirstDeployEvent = firstDeployEvent;
                            teamFirstDeployMech = mechCountEvents.Key;
                        }
                    }
                    if(teamFirstDeployEvent != null) {
                        _consolidatedEvents.Add(new MechCountEvent(teamFirstDeployEvent.Timestamp, teamFirstDeployEvent.Count) {
                            Mech = teamFirstDeployMech,
                            Team = teamMechCountEvents.Key,
                        });
                    }
                }

                if(match.MatchEndTime != null) {
                    _consolidatedEvents.Add(new MatchEndEvent((DateTime)match.MatchEndTime, match.MatchWinner));
                }
                _consolidatedEvents.Sort();
            }
        }

        CSV = BuildCSV();
        _teamQuickFilter = new(plugin, ApplyTeamFilter);
        _unfilteredScoreboard = match.PlayerScoreboards;
        _scoreboard = _unfilteredScoreboard;
        _teamScoreboard = match.GetTeamScoreboards();
        SortByColumn(0, ImGuiSortDirection.Ascending);
    }

    public override void Draw() {
        base.Draw();
        if(Plugin.Configuration.ShowBackgroundImage) {
            var cursorPosBefore = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - (250 / 2 + 0f) * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY((ImGui.GetCursorPos().Y + 40f * ImGuiHelpers.GlobalScale));
            bool flipImage = Match.LocalPlayerTeam == RivalWingsTeamName.Ravens && Plugin.Configuration.LeftPlayerTeam;
            var uv0 = flipImage ? new Vector2(1f, 0f) : Vector2.Zero;
            var uv1 = flipImage ? new Vector2(0f, 1f) : Vector2.One;
            ImGui.Image(Plugin.TextureProvider.GetFromFile(Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "rw_logo.png")).GetWrapOrEmpty().ImGuiHandle,
                new Vector2(250, 240) * ImGuiHelpers.GlobalScale, uv0, uv1, new Vector4(1, 1, 1, 0.1f));
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
                var firstTeam = (Plugin.Configuration.LeftPlayerTeam ? Match.LocalPlayerTeam ?? RivalWingsTeamName.Falcons : RivalWingsTeamName.Falcons);
                var secondTeam = (RivalWingsTeamName)((int)(firstTeam + 1) % 2);

                ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                DrawTeamHeader(firstTeam);
                ImGui.TableNextColumn();

                if(Match.TeamMechTime == null || Match.Mercs == null || Match.Supplies == null || Match.AllianceStats == null) {
                    ImGuiHelper.CenterAlignCursor("(?)");
                    ////ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - 2f * ImGuiHelpers.GlobalScale);
                    //ImGui.Text("");
                    ImGuiHelper.HelpMarker("Information missing due to match not fully recorded.", false);
                }

                ImGui.TableNextColumn();
                DrawTeamHeader(secondTeam);

                ImGui.TableNextColumn();
                DrawTeamTable(firstTeam, false);
                ImGui.TableNextColumn();
                if(Match.Mercs != null && Match.Supplies != null) {
                    var tableWidth = 55f * ImGuiHelpers.GlobalScale + ImGui.GetStyle().CellPadding.X * 4;
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - tableWidth / 2);
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 30f * ImGuiHelpers.GlobalScale);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 9f * ImGuiHelpers.GlobalScale);
                    DrawMidMercTable();
                }
                ImGui.TableNextColumn();
                DrawTeamTable(secondTeam, true);
            }
        }

        using(var tabBar = ImRaii.TabBar("TabBar")) {

            if(Match.AllianceStats != null) {
                using var tab = ImRaii.TabItem("Alliances");
                if(tab) {
                    if(CurrentTab != "Alliances") {
                        SetWindowSize(SizeConstraints!.Value.MinimumSize);
                        CurrentTab = "Alliances";
                    }
                    DrawAlliances();
                }
            }
            if(Match.PlayerScoreboards != null) {
                using var tab = ImRaii.TabItem("Scoreboard");
                if(tab) {
                    if(CurrentTab != "Scoreboard") {
                        SetWindowSize(new Vector2(975, 800));
                        CurrentTab = "Scoreboard";
                    }
                    ImGuiHelper.HelpMarker("Right-click table header to show and hide columns including extra metrics.", false, true);
                    if(_playerContributions != null) {
                        using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
                            ImGui.SameLine();
                        }
                        ImGuiComponents.ToggleButton("##showPercentages", ref ShowPercentages);
                        ImGui.SameLine();
                        ImGui.Text("Show team contributions");
                    }
                    if(_teamScoreboard != null) {
                        using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
                            ImGui.SameLine();
                        }
                        ImGui.Checkbox("###showTeamRows", ref ShowTeamRows);
                        ImGui.SameLine();
                        ImGui.Text("Show team totals");
                    }
                    using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
                        ImGui.SameLine();
                    }
                    _teamQuickFilter.Draw();
                    DrawPlayerStatsTable();
                }
            }
            if(_timeline != null) {
                using var tab = ImRaii.TabItem("Timeline");
                if(tab) {
                    if(CurrentTab != "Timeline") {
                        SetWindowSize(new Vector2(SizeConstraints!.Value.MinimumSize.X, 600));
                        CurrentTab = "Timeline";
                    }
                    DrawTimeline();
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
                        ImGuiHelper.WrappedTooltip("Average mechs deployed");
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
                        ImGuiHelper.WrappedTooltip("Average mechs deployed");
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
        if(Match.StructureHealth == null) {
            return;
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 36f * ImGuiHelpers.GlobalScale);
        //Plugin.Log.Debug($"{ImGui.GetStyle().CellPadding.X * 2}");
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X * 0, 0));
        using(var table = ImRaii.Table($"{team}--CoreTable", 1, ImGuiTableFlags.NoClip | ImGuiTableFlags.PadOuterX)) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 50f * ImGuiHelpers.GlobalScale);

            var drawIcon = (float size) => {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
                var image = Plugin.WindowManager.GetTextureHandle(TextureHelper.CoreIcons[team]);
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
        if(Match.StructureHealth == null) {
            return;
        }

        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using(var table = ImRaii.Table($"{team}{tower}--Table", 2, ImGuiTableFlags.NoClip | ImGuiTableFlags.None, new Vector2(60f, 30f) * ImGuiHelpers.GlobalScale)) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);

            var drawIcon = (RivalWingsStructure structure, float size) => {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
                var image = structure switch {
                    RivalWingsStructure.Core => TextureHelper.CoreIcons[team],
                    RivalWingsStructure.Tower1 => TextureHelper.Tower1Icons[team],
                    RivalWingsStructure.Tower2 => TextureHelper.Tower2Icons[team],
                    _ => TextureHelper.CoreIcons[team],
                };
                ImGui.Image(Plugin.WindowManager.GetTextureHandle(image), new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0.1f), new Vector2(0.9f));
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

    private void DrawMechTable(RivalWingsTeamName team, Dictionary<RivalWingsMech, double> mechTime, bool reverse, bool drawPercentage = false) {
        if(Match.MatchDuration == null) {
            return;
        }
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using(var table = ImRaii.Table($"{team}--MechTable", 2, ImGuiTableFlags.NoClip | ImGuiTableFlags.None, new Vector2(60f, 30f) * ImGuiHelpers.GlobalScale)) {
            ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);

            var drawIcon = (RivalWingsMech mech, float size) => {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - size / 2 * ImGuiHelpers.GlobalScale);
                var image = mech switch {
                    RivalWingsMech.Chaser => TextureHelper.ChaserIcons[team],
                    RivalWingsMech.Oppressor => TextureHelper.OppressorIcons[team],
                    RivalWingsMech.Justice => TextureHelper.JusticeIcons[team],
                    _ => TextureHelper.ChaserIcons[team],
                };
                ImGui.Image(Plugin.WindowManager.GetTextureHandle(image), new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), new Vector2(0.1f), new Vector2(0.9f));
            };
            var drawText = (RivalWingsMech mech) => {
                var uptime = mechTime[mech] / Match.MatchDuration.Value.TotalSeconds;
                string text = "";
                if(drawPercentage) {
                    text = (uptime * 100).ToString("N0");
                } else {
                    text = uptime.ToString("0.0");
                }
                ImGuiHelper.CenterAlignCursor(text);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
                ImGui.TextUnformatted(text);
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
    }

    private void DrawMidMercTable() {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
        using(var table = ImRaii.Table($"MidMercTable", 3, ImGuiTableFlags.NoClip | ImGuiTableFlags.None,
            new Vector2(55f * ImGuiHelpers.GlobalScale + ImGui.GetStyle().CellPadding.X * 3, (25f * 5 + 1) * ImGuiHelpers.GlobalScale))) {
            var firstTeam = (Plugin.Configuration.LeftPlayerTeam ? Match.LocalPlayerTeam ?? RivalWingsTeamName.Falcons : RivalWingsTeamName.Falcons);
            var secondTeam = (RivalWingsTeamName)((int)(firstTeam + 1) % 2);
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

            if(Match.Mercs != null) {
                ImGui.TableNextColumn();
                drawText(Match.Mercs[firstTeam].ToString());
                ImGui.TableNextColumn();
                drawImage(Plugin.WindowManager.GetTextureHandle(TextureHelper.GoblinMercIcon), 25f);
                ImGuiHelper.WrappedTooltip("Goblin Mercenary");
                ImGui.TableNextColumn();
                drawText(Match.Mercs[secondTeam].ToString());
            }

            if(Match.Supplies != null) {
                RivalWingsSupplies[] supplies = { RivalWingsSupplies.Gobtank, RivalWingsSupplies.Ceruleum, RivalWingsSupplies.Gobbiejuice, RivalWingsSupplies.Gobcrate };
                foreach(var supply in supplies) {
                    ImGui.TableNextColumn();
                    drawText(Match.Supplies[firstTeam][supply].ToString());
                    ImGui.TableNextColumn();
                    //ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 2.5f * ImGuiHelpers.GlobalScale);
                    DrawSuppliesIcon(supply, 25f);
                    ImGui.TableNextColumn();
                    drawText(Match.Supplies[secondTeam][supply].ToString());
                }
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
        ImGui.Image(Plugin.WindowManager.GetTextureHandle(TextureHelper.RWSuppliesTexture), new Vector2(size * ImGuiHelpers.GlobalScale, size * ImGuiHelpers.GlobalScale), uv0, uv1);
        ImGuiHelper.WrappedTooltip(MatchHelper.GetSuppliesName(supplies));
    }

    private void DrawPlayerStatsTable() {
        var tableFlags = ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.PadOuterX;
        if(Plugin.Configuration.StretchScoreboardColumns ?? false) {
            tableFlags -= ImGuiTableFlags.ScrollX;
        }

        using var table = ImRaii.Table($"postmatchplayers##{Match.Id}", 19, tableFlags, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
        //new Vector2(ImGui.GetContentRegionAvail().X, 550f * ImGuiHelpers.GlobalScale)
        if(!table) return;
        var widthStyle = Plugin.Configuration.StretchScoreboardColumns ?? false ? ImGuiTableColumnFlags.WidthStretch : ImGuiTableColumnFlags.WidthFixed;
        ImGui.TableSetupColumn("Alliance", widthStyle | ImGuiTableColumnFlags.NoHeaderLabel, ImGuiHelpers.GlobalScale * 10f, 3);
        ImGui.TableSetupColumn("Name", widthStyle, ImGuiHelpers.GlobalScale * 200f, 0);
        ImGui.TableSetupColumn("Home World", widthStyle, ImGuiHelpers.GlobalScale * 110f, 1);
        ImGui.TableSetupColumn("Job", widthStyle, ImGuiHelpers.GlobalScale * 50f, 2);
        ImGui.TableSetupColumn("Kills", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Kills".GetHashCode());
        ImGui.TableSetupColumn("Deaths", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Deaths".GetHashCode());
        ImGui.TableSetupColumn("Assists", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Assists".GetHashCode());
        ImGui.TableSetupColumn("Damage to PCs", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToPCs".GetHashCode());
        ImGui.TableSetupColumn("Damage to Other", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageToOther".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageTaken".GetHashCode());
        ImGui.TableSetupColumn("HP Restored", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"HPRestored".GetHashCode());
        ImGui.TableSetupColumn("Special", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 60f, (uint)"Special1".GetHashCode());
        ImGui.TableSetupColumn("Ceruleum", widthStyle, ImGuiHelpers.GlobalScale * 55f, (uint)"Ceruleum".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Kill/Assist", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerKA".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerLife".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageTakenPerLife".GetHashCode());
        ImGui.TableSetupColumn("HP Restored per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"HPRestoredPerLife".GetHashCode());
        ImGui.TableSetupColumn("KDA Ratio", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 50f, (uint)"KDA".GetHashCode());

        ImGui.TableSetupScrollFreeze(2, 1);

        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Alliance", 0, false);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Name", 0);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Home World", 0);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Job", 1);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Kills");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Deaths");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Assists");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage\nto PCs");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage\nto Other");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage\nDealt");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage\nTaken");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("HP\nRestored");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.RightAlignCursor2("(?)", -20f * ImGuiHelpers.GlobalScale);
            ImGui.TableHeader("");
            ImGuiHelper.HelpMarker("Not sure what this is. It's related to healing.", true);
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Ceru-\nleum");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage Dealt\nper Kill/Assist");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage Dealt\nper Life");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage Taken\nper Life");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("HP Restored\nper Life");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("KDA\nRatio");
        }

        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(sortSpecs.SpecsDirty) {
            SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
            sortSpecs.SpecsDirty = false;
        }

        if(ShowTeamRows && _teamScoreboard != null) {
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, Plugin.Configuration.Colors.TeamRowText);
            foreach(var row in _teamScoreboard.Where(x => _teamQuickFilter.FilterState[x.Key])) {
                var rowColor = Plugin.Configuration.GetRivalWingsTeamColor(row.Key);
                rowColor.W = Plugin.Configuration.TeamRowAlpha;
                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
                if(ImGui.TableNextColumn()) {
                    ImGui.TextUnformatted(MatchHelper.GetTeamName(row.Key));
                }
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.Kills}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.Deaths}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.Assists}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageToPCs}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageToOther}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealt}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageTaken}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.HPRestored}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.Special1}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.Ceruleum}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealtPerKA}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealtPerLife}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageTakenPerLife}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.HPRestoredPerLife}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{string.Format("{0:0.00}", row.Value.KDA)}", -11f);
                }
            }
        }

        foreach(var row in _scoreboard!) {
            var player = Match.Players!.Where(x => x.Name.Equals(row.Key)).First();
            var playerAlias = (PlayerAlias)row.Key;
            ImGui.TableNextColumn();
            //bool isPlayer = row.Key.Player != null;
            //bool isPlayerTeam = row.Key.Team == _dataModel.LocalPlayerTeam?.TeamName;
            var rowColor = Plugin.Configuration.GetRivalWingsTeamColor(player.Team);
            rowColor.W = Plugin.Configuration.PlayerRowAlpha;
            var textColor = Match.LocalPlayer is not null && Match.LocalPlayer.Equals(playerAlias) ? Plugin.Configuration.Colors.CCLocalPlayer : Plugin.Configuration.Colors.PlayerRowText;
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            string alliance = GetAllianceLetter(player.Alliance);
            ImGui.TextColored(textColor, $"{alliance}");
            if(ImGui.TableNextColumn()) {
                ImGui.TextColored(textColor, $"{playerAlias.Name}");
            }
            if(ImGui.TableNextColumn()) {
                ImGui.TextColored(textColor, $"{player.Name.HomeWorld}");
            }
            if(ImGui.TableNextColumn()) {
                var jobString = $"{player.Job}";
                ImGuiHelper.CenterAlignCursor(jobString);
                ImGui.TextColored(textColor, jobString);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].Kills) : row.Value.Kills)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].Deaths) : row.Value.Deaths)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].Assists) : row.Value.Assists)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].DamageToPCs) : row.Value.DamageToPCs)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].DamageToOther) : row.Value.DamageToOther)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].DamageDealt) : row.Value.DamageDealt)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].DamageTaken) : row.Value.DamageTaken)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].HPRestored) : row.Value.HPRestored)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].Special1) : row.Value.Special1)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[player.Name].Ceruleum) : row.Value.Ceruleum)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealtPerKA}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealtPerLife}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{row.Value.DamageTakenPerLife}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{row.Value.HPRestoredPerLife}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:0.00}", row.Value.KDA)}", -11f, textColor);
            }
        }
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        if(_scoreboard == null || _unfilteredScoreboard == null) return;
        Func<KeyValuePair<string, RivalWingsScoreboard>, object> comparator = (r) => 0;
        Func<KeyValuePair<RivalWingsTeamName, RivalWingsScoreboard>, object> teamComparator = (r) => 0;

        //0 = name
        //1 = homeworld
        //2 = job
        //3 = alliance
        if(columnId == 0) {
            comparator = (r) => Match.Players?.First(x => x.Name.Equals(r.Key)).Name.Name ?? "";
            teamComparator = (r) => r.Key;
        } else if(columnId == 1) {
            comparator = (r) => Match.Players?.First(x => x.Name.Equals(r.Key)).Name.HomeWorld ?? "";
        } else if(columnId == 2) {
            comparator = (r) => Match.Players?.First(x => x.Name.Equals(r.Key)).Job ?? 0;
        } else if(columnId == 3) {
            comparator = (r) => Match.Players?.First(x => x.Name.Equals(r.Key)).TeamAlliance ?? 0;
        } else {
            bool propFound = false;
            if(ShowPercentages && _playerContributions != null) {
                var props = typeof(RWScoreboardDouble).GetProperties();
                foreach(var prop in props) {
                    var propId = prop.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        //Plugin.Log.Debug($"sorting by {prop.Name}");
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
                        //Plugin.Log.Debug($"sorting by {prop.Name}");
                        comparator = (r) => prop.GetValue(r.Value) ?? 0;
                        teamComparator = (r) => prop.GetValue(r.Value) ?? 0;
                        break;
                    }
                }
            }
        }
        _scoreboard = direction == ImGuiSortDirection.Ascending ? _scoreboard.OrderBy(comparator).ToDictionary()
            : _scoreboard.OrderByDescending(comparator).ToDictionary();
        _unfilteredScoreboard = direction == ImGuiSortDirection.Ascending ? _unfilteredScoreboard.OrderBy(comparator).ToDictionary()
            : _unfilteredScoreboard.OrderByDescending(comparator).ToDictionary();
        if(!Plugin.Configuration.AnchorTeamNames) {
            _teamScoreboard = direction == ImGuiSortDirection.Ascending ? _teamScoreboard?.OrderBy(teamComparator).ToDictionary()
                : _teamScoreboard?.OrderByDescending(teamComparator).ToDictionary();
        }
    }

    private Task ApplyTeamFilter() {
        if(_scoreboard == null || _unfilteredScoreboard == null || Match.Players == null) {
            return Task.CompletedTask;
        }

        _scoreboard = _unfilteredScoreboard.Where(x => {
            var player = Match.Players.Where(y => y.Name.Equals(x.Key)).First();
            return _teamQuickFilter.FilterState[player.Team];
        }).ToDictionary();
        return Task.CompletedTask;
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
                if(i >= Match.AllianceStats!.Count) {
                    break;
                }
                DrawAllianceStatTable((RivalWingsTeamName)Match.LocalPlayerTeam, i);
            }
        }

    }

    private void DrawAllianceStatTable(RivalWingsTeamName team, int alliance) {
        if(Match.AllianceStats == null || alliance >= Match.AllianceStats.Count) {
            return;
        }
        var allianceStats = Match.AllianceStats?[alliance];
        if(allianceStats == null) {
            return;
        }

        //using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));

        var drawText = (string text, Vector4 color) => {
            ImGuiHelper.CenterAlignCursor(text);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f * ImGuiHelpers.GlobalScale);
            ImGui.TextColored(color, text);
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

                var teamAlliance = ((int)team * 6) + alliance;
                var color = teamAlliance == Match.LocalPlayerTeamMember!.TeamAlliance ? Plugin.Configuration.Colors.CCLocalPlayer : ImGuiColors.DalamudWhite;
                using(_ = Plugin.WindowManager.LargeFont.Push()) {
                    drawText(GetAllianceLetter(alliance), color);
                }
                if(Match.Players != null) {
                    string tooltipText = "";
                    Match.Players.Where(x => x.TeamAlliance == teamAlliance).ToList().ForEach(x => tooltipText += x.Name.Name + "\n");
                    if(tooltipText.Length > 0) {
                        tooltipText = tooltipText[..^1];
                        ImGuiHelper.WrappedTooltip(tooltipText);
                    }
                }
                ImGui.TableNextColumn();
                if(allianceStats.SoaringStacks > 0) {
                    var size = 40f;
                    var icon = TextureHelper.GetSoaringIcon((uint)allianceStats.SoaringStacks);
                    if(icon != null) {
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ImGuiHelpers.GlobalScale);
                        ImGui.Image(Plugin.WindowManager.GetTextureHandle((uint)icon), new Vector2(size * ImGuiHelpers.GlobalScale * 0.75f, size * ImGuiHelpers.GlobalScale), new Vector2(0f), new Vector2(1));
                    }
                }
                //ImGui.Image(Plugin.WindowManager.SoaringIcons[allianceStats.SoaringStacks].ImGuiHandle, new Vector2(25f * ImGuiHelpers.GlobalScale, 25f * ImGuiHelpers.GlobalScale));
                //ImGui.TableNextColumn();
            }

            //draw mech stats
            if(Match.LocalPlayerTeam != null && _allianceMechTimes != null && alliance < _allianceMechTimes.Count) {
                ImGui.TableNextColumn();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 49f * ImGuiHelpers.GlobalScale);
                DrawMechTable((RivalWingsTeamName)Match.LocalPlayerTeam, _allianceMechTimes[alliance].MechTime, false, true);
                string tooltipText = "Pilots:\n\n";
                _allianceMechTimes[alliance].Pilots.ForEach(x => tooltipText += x.Name + "\n");
                if(tooltipText.Length > 0) {
                    tooltipText = tooltipText[..^1];
                    ImGuiHelper.WrappedTooltip(tooltipText);
                }
            }
        }
    }

    private void DrawTimeline() {
        //filters...

        using var child = ImRaii.Child("timelineChild");
        using var table = ImRaii.Table("timelineTable", 2);
        ImGui.TableSetupColumn("time", ImGuiTableColumnFlags.WidthFixed, 50f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("text", ImGuiTableColumnFlags.WidthStretch);

        foreach(var matchEvent in _consolidatedEvents) {
            ImGui.TableNextColumn();
            var timeDiff = Match.MatchStartTime - matchEvent.Timestamp;
            ImGuiHelper.DrawNumericCell($"{ImGuiHelper.GetTimeSpanString(timeDiff ?? TimeSpan.Zero)} : ");
            ImGui.TableNextColumn();
            var eventType = matchEvent.GetType();
            switch(eventType) {
                case Type _ when eventType == typeof(MidClaimEvent):
                    DrawEvent(matchEvent as MidClaimEvent);
                    break;
                case Type _ when eventType == typeof(MercClaimEvent):
                    DrawEvent(matchEvent as MercClaimEvent);
                    break;
                case Type _ when eventType == typeof(MatchEndEvent):
                    DrawEvent(matchEvent as MatchEndEvent);
                    break;
                case Type _ when eventType == typeof(StructureHealthEvent):
                    DrawEvent(matchEvent as StructureHealthEvent);
                    break;
                case Type _ when eventType == typeof(AllianceSoaringEvent):
                    DrawEvent(matchEvent as AllianceSoaringEvent);
                    break;
                case Type _ when eventType == typeof(MechCountEvent):
                    DrawEvent(matchEvent as MechCountEvent);
                    break;
                default:
                    break;
            }
        }
    }

    private void DrawEvent(MidClaimEvent matchEvent) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        ImGui.Text("The ");
        ImGui.SameLine();
        var color = Plugin.Configuration.GetRivalWingsTeamColor(matchEvent.Team);
        ImGui.TextColored(color, MatchHelper.GetTeamName(matchEvent.Team));
        ImGui.SameLine();
        ImGui.Text(" have secured a ");
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudYellow, MatchHelper.GetSuppliesName(matchEvent.Kind));
        ImGui.SameLine();
        ImGui.Text(" shipment.");
    }

    private void DrawEvent(MercClaimEvent matchEvent) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        ImGui.Text("The ");
        ImGui.SameLine();
        var color = Plugin.Configuration.GetRivalWingsTeamColor(matchEvent.Team);
        ImGui.TextColored(color, MatchHelper.GetTeamName(matchEvent.Team));
        ImGui.SameLine();
        ImGui.Text(" have won a contract with a ");
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudYellow, "goblin mercenary.");
    }

    private void DrawEvent(MatchEndEvent matchEvent) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        ImGui.Text("The ");
        ImGui.SameLine();
        var color = Plugin.Configuration.GetRivalWingsTeamColor(matchEvent.Team);
        ImGui.TextColored(color, MatchHelper.GetTeamName(matchEvent.Team));
        ImGui.SameLine();
        ImGui.Text(" are victorious!");
    }

    private void DrawEvent(StructureHealthEvent matchEvent) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        if(matchEvent.Health <= 0) {
            ImGui.Text("The ");
            ImGui.SameLine();
            var color = Plugin.Configuration.GetRivalWingsTeamColor(matchEvent.Team);
            ImGui.TextColored(color, MatchHelper.GetTeamName(matchEvent.Team) + "' ");
            //insert icon
            ImGui.SameLine();
            ImGui.TextColored(color, MatchHelper.GetStructureName(matchEvent.Structure));
            ImGui.SameLine();
            ImGui.Text(" was destroyed.");
        }
    }

    private void DrawEvent(AllianceSoaringEvent matchEvent) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        if(matchEvent.Count == 20) {
            var color = Plugin.Configuration.GetRivalWingsTeamColor(Match.LocalPlayerTeam);
            ImGui.TextColored(color, $"Alliance {GetAllianceLetter(matchEvent.Alliance ?? 777)}");
            ImGui.SameLine();
            ImGui.Text(" is ");
            ImGui.SameLine();
            //insert icon
            ImGui.TextColored(ImGuiColors.DalamudOrange, "Flying High!");
        }
    }

    private void DrawEvent(MechCountEvent matchEvent) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        ImGui.Text("The ");
        ImGui.SameLine();
        var color = Plugin.Configuration.GetRivalWingsTeamColor(matchEvent.Team);
        ImGui.TextColored(color, MatchHelper.GetTeamName(matchEvent.Team));
        ImGui.SameLine();
        ImGui.Text($" have deployed their first warmachina ({matchEvent.Mech}).");
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
        string csv = "";

        //header
        csv += "Id,Start Time,Arena,Duration,Winner\n";
        csv += Match.Id + "," + Match.DutyStartTime + ","
            + (Match.Arena != null ? MatchHelper.GetArenaName(Match.Arena) : "") + ","
            + Match.MatchDuration + ","
            + Match.MatchWinner + ","
            + "\n";

        //core stats
        if(Match.StructureHealth != null) {
            csv += "\n\n\n";
            csv += "Team,Core,Tower1,Tower2\n";
            foreach(var team in Match.StructureHealth) {
                csv += team.Key + "," + team.Value[RivalWingsStructure.Core] + "," + team.Value[RivalWingsStructure.Tower1] + "," + team.Value[RivalWingsStructure.Tower2] + ","
                + "\n";
            }
        }

        //team mech times
        if(Match.TeamMechTime != null) {
            csv += "\n\n\n";
            csv += "Team,Chaser,Oppressor,Justice\n";
            foreach(var team in Match.TeamMechTime) {
                csv += team.Key + "," + team.Value[RivalWingsMech.Chaser] + "," + team.Value[RivalWingsMech.Oppressor] + "," + team.Value[RivalWingsMech.Justice] + ","
                + "\n";
            }
        }

        //player mech times
        if(Match.PlayerMechTime != null) {
            csv += "\n\n\n";
            csv += "Player,HomeWorld,Chaser,Oppressor,Justice\n";
            foreach(var player in Match.PlayerMechTime) {
                var alias = (PlayerAlias)player.Key;
                csv += alias.Name + "," + alias.HomeWorld + "," + player.Value[RivalWingsMech.Chaser] + "," + player.Value[RivalWingsMech.Oppressor] + "," + player.Value[RivalWingsMech.Justice] + ","
                + "\n";
            }
        }

        //alliance stats
        if(Match.AllianceStats != null) {
            csv += "\n\n\n";
            csv += "Alliance,Soaring,Ceruleum Generated, Ceruleum Consumed\n";
            foreach(var alliance in Match.AllianceStats) {
                csv += GetAllianceLetter(alliance.Key) + "," + alliance.Value.SoaringStacks + "," + alliance.Value.CeruleumGenerated + "," + alliance.Value.CeruleumConsumed + ","
                + "\n";
            }
        }

        //player stats
        if(Match.Players != null && Match.PlayerScoreboards != null) {
            csv += "\n\n\n";
            csv += "Alliance,Name,Home World,Job,Kills,Deaths,Assists,Damage Dealt,Damage to PCs,Damage To Other,Damage Taken, HP Restored,Special,Ceruleum\n";
            foreach(var player in Match.Players) {
                var scoreboard = Match.PlayerScoreboards[player.Name];
                csv += player.Alliance + "," + player.Name.Name + "," + player.Name.HomeWorld + "," + player.Job + "," + scoreboard.Kills + "," + scoreboard.Deaths + "," + scoreboard.Assists + ","
                    + scoreboard.DamageDealt + "," + scoreboard.DamageToPCs + "," + scoreboard.DamageToOther + "," + scoreboard.DamageTaken + "," + scoreboard.HPRestored + ","
                    + scoreboard.Special1 + "," + scoreboard.Ceruleum + ","
                    + "\n";
            }
        }
        return csv;
    }
}
