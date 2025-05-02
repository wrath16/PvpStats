using Dalamud.Game;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using ImPlotNET;
using Lumina.Excel.Sheets;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Event;
using PvpStats.Types.Event.CrystallineConflict;
using PvpStats.Types.Match;
using PvpStats.Types.Match.Timeline;
using PvpStats.Types.Player;
using PvpStats.Windows.Filter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;

namespace PvpStats.Windows.Detail;

internal class CrystallineConflictMatchDetail : MatchDetail<CrystallineConflictMatch> {

    //private Plugin _plugin;
    private CCTeamQuickFilter _teamQuickFilter;
    //private CrystallineConflictMatch _dataModel;
    private Dictionary<CrystallineConflictTeamName, CCScoreboardTally>? _teamScoreboard;
    private Dictionary<PlayerAlias, CCScoreboardDouble>? _playerContributions = [];
    private Dictionary<PlayerAlias, CCScoreboardTally>? _scoreboard;
    private Dictionary<PlayerAlias, CCScoreboardTally>? _unfilteredScoreboard;
    private CrystallineConflictTeamName? _localPlayerTeam;
    private List<CrystallineConflictPlayer> _players;

    private Vector2 _defaultScoreboardSize;
    private Vector2? _savedScoreboardSize;
    private Vector2 _defaultGraphSize;
    private Vector2? _savedGraphSize;
    private Vector2 _defaultTimelineSize;
    private Vector2? _savedTimelineSize;
    private bool _firstDraw = true;
    private ulong _scoreboardTicks = 0;
    private bool _easterEgg = false;

    private CrystallineConflictMatchTimeline? _timeline;
    private List<MatchEvent> _consolidatedEvents = new();
    private List<MatchEvent> _consolidatedEventsFiltered = new();
    private Dictionary<Type, bool> _eventFilters = new();
    private PlayerQuickSearchFilter _playerFilter = new();
    private List<(float Crystal, float Astra, float Umbra)> _consolidatedEventTeamPoints = new();
    private Dictionary<uint, string> _bNPCNames = new();
    private Dictionary<uint, string> _actionNames = new();
    private Dictionary<uint, uint> _actionIcons = new();
    private double[] _xAxisTicks = [];
    private string[] _xAxisLabels = [];
    private Dictionary<CrystallineConflictTeamName, (float[] Xs, float[] Ys)> _teamPoints = new() {
        {CrystallineConflictTeamName.Astra, new() },
        {CrystallineConflictTeamName.Umbra, new() },
    };
    private Dictionary<CrystallineConflictTeamName, (float[] Xs, float[] Ys)> _teamMidPoints = new() {
        {CrystallineConflictTeamName.Astra, new() },
        {CrystallineConflictTeamName.Umbra, new() },
    };
    private (float[] Xs, float[] Ys) _crystalPosition = new();

    internal CrystallineConflictMatchDetail(Plugin plugin, CrystallineConflictMatch match) : base(plugin, plugin.CCCache, match) {
        ForceMainWindow = true;
        PositionCondition = ImGuiCond.Appearing;
        CollapsedCondition = ImGuiCond.Appearing;
        Position = new Vector2(0, 0);
        Collapsed = false;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(650, 620),
            MaximumSize = new Vector2(5000, 5000)
        };
        Flags |= ImGuiWindowFlags.NoSavedSettings;

        switch(match.MatchType) {
            default:
                _defaultScoreboardSize = new Vector2(700, 680);
                _defaultGraphSize = new Vector2(975, 875);
                _defaultTimelineSize = new Vector2(700, 680);
                break;
            case CrystallineConflictMatchType.Ranked:
                _defaultScoreboardSize = new Vector2(700, 700);
                _defaultGraphSize = new Vector2(975, 895);
                _defaultTimelineSize = new Vector2(700, 700);
                break;
        }

        //if(!plugin.Configuration.ResizeableMatchWindow) {
        //    Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        //}
        //_plugin = plugin;
        //_dataModel = match;
        _teamQuickFilter = new(plugin, ApplyTeamFilter);

        //sort team players
        foreach(var team in Match.Teams) {
            team.Value.Players = [.. team.Value.Players.OrderBy(p => p.Job)];
        }

        _localPlayerTeam = Match.LocalPlayerTeam?.TeamName;
        _players = Match.Players;

        //setup post match data
        if(Match.PostMatch is not null) {
            _unfilteredScoreboard = match.GetPlayerScoreboards();
            _scoreboard = _unfilteredScoreboard;
            _teamScoreboard = match.GetTeamScoreboards();
            _playerContributions = match.GetPlayerContributions();
        }
        SortByColumn(0, ImGuiSortDirection.Ascending);

        _timeline = Plugin.CCCache.GetTimeline(Match);
        if(_timeline != null) {
            //setup timeline
            _consolidatedEvents = [.. _timeline.Kills, .. _timeline.MapEvents];
            if(Match.MatchDuration > TimeSpan.FromMinutes(5) && Match.MatchStartTime != null) {
                _consolidatedEvents.Add(new GenericMatchEvent((DateTime)Match.MatchStartTime + TimeSpan.FromMinutes(5), CrystallineConflictMatchEvent.OvertimeCommenced));
            }
            if(Match.MatchEndTime != null) {
                _consolidatedEvents.Add(new GenericMatchEvent((DateTime)Match.MatchEndTime, CrystallineConflictMatchEvent.MatchEnded));
            }
            _consolidatedEvents.Add(new GenericMatchEvent((DateTime)Match.MatchStartTime + TimeSpan.FromSeconds(20), CrystallineConflictMatchEvent.CrystalUnchained));
            foreach(var team in _timeline.TeamMidProgress ?? []) {
                var breachMid = team.Value.FirstOrDefault(x => x.Points >= 100);
                if(breachMid != null) {
                    _consolidatedEvents.Add(new ProgressEvent(breachMid.Timestamp, breachMid.Points) {
                        Team = team.Key,
                        Type = 1
                    });
                }
            }
            //limit break events
            List<CombinedActionEvent> limitBreakCasts = new();
            foreach(var mEvent in _timeline.LimitBreakCasts ?? []) {
                if(mEvent.ActionId == (uint)LimitBreak.SkyShatter2
                    || mEvent.ActionId == (uint)LimitBreak.TerminalTrigger2
                    || mEvent.ActionId == (uint)LimitBreak.Mesotes2) {
                    continue;
                }
                var action = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(ClientLanguage.English).GetRow(mEvent.ActionId);
                if(!_actionNames.ContainsKey(action.RowId)) {
                    _actionNames.Add(action.RowId, action.Name.ToString());
                    _actionIcons.Add(action.RowId, action.Icon);
                }
                var effectEvent = _timeline.LimitBreakEffects?.FirstOrDefault(x => x.ActionId == mEvent.ActionId 
                && x.Actor.Equals(mEvent.Actor) 
                && x.Timestamp >= mEvent.Timestamp 
                && (x.Timestamp - mEvent.Timestamp) <= TimeSpan.FromSeconds(8));
                limitBreakCasts.Add(new(mEvent, effectEvent));
            }
            _consolidatedEvents = [.. _consolidatedEvents, ..limitBreakCasts];
            _consolidatedEvents.Sort();
            ApplyTimelineFilters();

            //setup team point events
            if(_timeline.CrystalPosition != null && _timeline.TeamProgress != null) {
                foreach(var mEvent in _consolidatedEvents) {
                    float? crystalPos, astraPoints, umbraPoints;
                    if(mEvent is GenericMatchEvent 
                        && (mEvent as GenericMatchEvent)?.Type == CrystallineConflictMatchEvent.MatchEnded) {
                        astraPoints = Match.Teams[CrystallineConflictTeamName.Astra].Progress;
                        umbraPoints = Match.Teams[CrystallineConflictTeamName.Umbra].Progress;
                        crystalPos = _timeline.CrystalPosition.LastOrDefault()?.Points / 10f;
                    } else {
                        crystalPos = _timeline.CrystalPosition.LastOrDefault(x => x.Timestamp <= mEvent.Timestamp)?.Points / 10f;
                        astraPoints = _timeline.TeamProgress[CrystallineConflictTeamName.Astra].LastOrDefault(x => x.Timestamp <= mEvent.Timestamp)?.Points / 10f;
                        umbraPoints = _timeline.TeamProgress[CrystallineConflictTeamName.Umbra].LastOrDefault(x => x.Timestamp <= mEvent.Timestamp)?.Points / 10f;
                    }
                    _consolidatedEventTeamPoints.Add((crystalPos ?? 0, astraPoints ?? 0, umbraPoints ?? 0));
                }
            }

            //setup battlenpc names
            foreach(var killEvent in _timeline.Kills?.Where(x => x.KillerNameId != null) ?? []) {
                var killerNameId = (uint)killEvent.KillerNameId!;
                if(_bNPCNames.TryAdd(killerNameId, "")) {
                    var row = Plugin.DataManager.GetExcelSheet<BNpcName>(ClientLanguage.English).GetRow(killerNameId);
                    string fullName = row.Singular.ToString();
                    if(row.Article == 0) {
                        if(row.StartsWithVowel == 0) {
                            fullName = "A " + fullName;
                        } else {
                            fullName = "An " + fullName;
                        }
                    }
                    _bNPCNames[killerNameId] = fullName;
                }
            }

            //setup graphs
            List<double> axisTicks = new();
            List<string> axisLabels = new();
            for(int i = 0; i <= 15; i++) {
                axisTicks.Add(i * 60);
                axisLabels.Add(ImGuiHelper.GetTimeSpanString(new TimeSpan(0, i, 0)));
            }
            _xAxisTicks = axisTicks.ToArray();
            _xAxisLabels = axisLabels.ToArray();

            //point graphs
            SetupProgressGraph(CrystallineConflictTeamName.Astra);
            SetupProgressGraph(CrystallineConflictTeamName.Umbra);
            SetupMidProgressGraph(CrystallineConflictTeamName.Astra);
            SetupMidProgressGraph(CrystallineConflictTeamName.Umbra);
            SetupCrystalPositionGraph();
        }

        CSV = BuildCSV();

        var ms = DateTime.Now.Millisecond;
        _easterEgg = ms % 100 == 0;
    }

    public override void OnClose() {
        Plugin.WindowManager.RemoveWindow(this);
    }

    public override void PreDraw() {
        base.PreDraw();
    }

    public override void Draw() {
        base.Draw();
        if(Plugin.Configuration.ShowBackgroundImage) {
            var cursorPosBefore = ImGui.GetCursorPos();
            //ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - (250f + 3f) * ImGuiHelpers.GlobalScale);
            //ImGui.SetCursorPosY((ImGui.GetCursorPos().Y + 50f * ImGuiHelpers.GlobalScale));
            //ImGui.Image(Plugin.WindowManager.CCBannerImage.ImGuiHandle, new Vector2(500, 230) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, new Vector4(1, 1, 1, 0.1f));
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - (243 / 2 + 3f) * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY((ImGui.GetCursorPos().Y + 40f * ImGuiHelpers.GlobalScale));
            ImGui.Image(Plugin.TextureProvider.GetFromFile(Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "cc_logo_full.png")).GetWrapOrEmpty().ImGuiHandle,
                new Vector2(243, 275) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, new Vector4(1, 1, 1, 0.1f));

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
                    ImGui.Text($"{MatchHelper.GetArenaName((CrystallineConflictMap)Match.Arena)}");
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
                ImGui.Text($"{Match.MatchType}");
                ImGui.TableNextColumn();
                bool noWinner = Match.MatchWinner is null;
                bool isWin = Match.MatchWinner == Match.LocalPlayerTeam?.TeamName;
                var color = ImGuiColors.DalamudWhite;
                color = noWinner ? ImGuiColors.DalamudGrey : color;
                string resultText = "";
                if(Match.IsSpectated && Match.MatchWinner is not null) {
                    color = Match.MatchWinner == CrystallineConflictTeamName.Astra ? Plugin.Configuration.Colors.CCPlayerTeam : Plugin.Configuration.Colors.CCEnemyTeam;
                    resultText = MatchHelper.GetTeamName((CrystallineConflictTeamName)Match.MatchWinner) + " WINS";
                } else {
                    color = isWin ? Plugin.Configuration.Colors.Win : Plugin.Configuration.Colors.Loss;
                    color = noWinner ? Plugin.Configuration.Colors.Other : color;
                    resultText = isWin ? "WIN" : "LOSS";
                    resultText = noWinner ? "UNKNOWN" : resultText;
                }
                ImGuiHelpers.CenterCursorForText(resultText);
                ImGui.TextColored(color, resultText);
                ImGui.TableNextColumn();
                string durationText = "";
                if(Match.MatchStartTime != null && Match.MatchEndTime != null) {
                    var duration = Match.MatchEndTime - Match.MatchStartTime;
                    durationText = $"{duration.Value.Minutes}{duration.Value.ToString(@"\:ss")}";
                    ImGuiHelper.RightAlignCursor(durationText);
                    ImGui.Text(durationText);
                }
            }
        }

        if(Match.Teams.Count == 2) {
            var firstTeam = Match.Teams.ElementAt(0).Value;
            var secondTeam = Match.Teams.ElementAt(1).Value;
            if(Plugin.Configuration.LeftPlayerTeam && !Match.IsSpectated) {
                firstTeam = Match.Teams.Where(x => x.Key == Match.LocalPlayerTeam!.TeamName).FirstOrDefault().Value;
                secondTeam = Match.Teams.Where(x => x.Key != Match.LocalPlayerTeam!.TeamName).FirstOrDefault().Value;
            }

            using(var table = ImRaii.Table("teams", 2, ImGuiTableFlags.PadOuterX)) {
                if(table) {
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
                    string firstTeamProgress = string.Format("{0:P1}", firstTeam.Progress / 100f);
                    ImGuiHelper.CenterAlignCursor(firstTeamProgress);
                    ImGui.TextUnformatted(firstTeamProgress);
                    ImGui.TableNextColumn();
                    var secondTeamProgress = string.Format("{0:P1}", secondTeam.Progress / 100f);
                    ImGuiHelper.CenterAlignCursor(secondTeamProgress);
                    ImGui.TextUnformatted($"{secondTeamProgress}");
                }
            }

            using(var table = ImRaii.Table($"players##{Match.Id}", 6, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoClip)) {

                if(table) {
                    ////ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

                    ImGui.TableSetupColumn("rankteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
                    ImGui.TableSetupColumn("playerteam1", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("jobteam1", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 24f);
                    ImGui.TableSetupColumn("jobteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 24f);
                    ImGui.TableSetupColumn("playerteam2", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("rankteam2", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 75f);
                    ImGui.TableNextRow();

                    int maxSize = int.Max(firstTeam.Players.Count, secondTeam.Players.Count);

                    for(int i = 0; i < maxSize; i++) {
                        if(i < firstTeam.Players.Count) {
                            var player0 = firstTeam.Players[i];
                            ImGui.TableNextColumn();
                            using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                string rank0 = player0.Rank != null && player0.Rank!.Tier != ArenaTier.None ? player0.Rank!.ToString() : "";
                                ImGuiHelper.RightAlignCursor(rank0);
                                ImGui.AlignTextToFramePadding();
                                ImGui.Text(rank0);
                            }
                            ImGui.TableNextColumn();
                            var playerColor0 = Match.LocalPlayerTeam is not null && firstTeam.TeamName == Match.LocalPlayerTeam.TeamName ? Plugin.Configuration.Colors.CCPlayerTeam : Plugin.Configuration.Colors.CCEnemyTeam;
                            playerColor0 = Match.LocalPlayer is not null && Match.LocalPlayer.Equals(player0) ? Plugin.Configuration.Colors.CCLocalPlayer : playerColor0;
                            if(Match.IsSpectated) {
                                playerColor0 = firstTeam.TeamName == CrystallineConflictTeamName.Astra ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                            }
                            string playerName0 = player0.Alias.Name;
                            ImGuiHelper.RightAlignCursor(playerName0);
                            ImGui.AlignTextToFramePadding();
                            //easter egg
                            if(_easterEgg && player0.Alias.Equals("Sarah Montcroix Siren")) {
                                var cursorBefore = ImGui.GetCursorPos();
                                using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f))) {
                                    ImGui.TextColored(Vector4.Zero, player0.Alias.Name);
                                    ImGuiHelper.WrappedTooltip(player0.Alias.HomeWorld);
                                }
                                ImGui.SetCursorPos(cursorBefore);
                                ImGui.AlignTextToFramePadding();
                                ImGuiHelper.DrawRainbowTextByChar(playerName0);
                            } else {
                                ImGui.TextColored(playerColor0, playerName0);
                                ImGuiHelper.WrappedTooltip(player0.Alias.HomeWorld);
                            }

                            ImGui.TableNextColumn();
                            using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                if(player0.Job != null && TextureHelper.JobIcons.TryGetValue((Job)player0.Job, out var icon)) {
                                    ImGui.Image(Plugin.WindowManager.GetTextureHandle(icon), new Vector2(24 * ImGuiHelpers.GlobalScale));
                                }
                            }
                        } else {
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                        }

                        if(i < secondTeam.Players.Count) {
                            var player1 = secondTeam.Players[i];
                            ImGui.TableNextColumn();
                            using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                if(player1.Job != null && TextureHelper.JobIcons.TryGetValue((Job)player1.Job, out var icon)) {
                                    //ImGui.Image(Plugin.WindowManager.JobIcons[(Job)player1.Job]?.ImGuiHandle ?? Plugin.WindowManager.Icon0.ImGuiHandle, new Vector2(24 * ImGuiHelpers.GlobalScale, 24 * ImGuiHelpers.GlobalScale));
                                    ImGui.Image(Plugin.WindowManager.GetTextureHandle(icon), new Vector2(24 * ImGuiHelpers.GlobalScale));
                                }
                            }

                            ImGui.TableNextColumn();
                            var playerColor1 = Match.LocalPlayerTeam is not null && secondTeam.TeamName == Match.LocalPlayerTeam.TeamName ? Plugin.Configuration.Colors.CCPlayerTeam : Plugin.Configuration.Colors.CCEnemyTeam;
                            playerColor1 = Match.LocalPlayer is not null && Match.LocalPlayer.Equals(player1) ? Plugin.Configuration.Colors.CCLocalPlayer : playerColor1;
                            if(Match.IsSpectated) {
                                playerColor1 = secondTeam.TeamName == CrystallineConflictTeamName.Astra ? ImGuiColors.TankBlue : ImGuiColors.DPSRed;
                            }
                            string playerName1 = secondTeam.Players[i].Alias.Name;
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X * 2);
                            //easter egg
                            if(_easterEgg && player1.Alias.Equals("Sarah Montcroix Siren")) {
                                var cursorBefore = ImGui.GetCursorPos();
                                using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f))) {
                                    ImGui.TextColored(Vector4.Zero, player1.Alias.Name);
                                    ImGuiHelper.WrappedTooltip(player1.Alias.HomeWorld);
                                }
                                ImGui.SetCursorPos(cursorBefore);
                                ImGui.AlignTextToFramePadding();
                                ImGuiHelper.DrawRainbowTextByChar(playerName1);
                            } else {
                                ImGui.TextColored(playerColor1, playerName1);
                                ImGuiHelper.WrappedTooltip(secondTeam.Players[i].Alias.HomeWorld);
                            }

                            ImGui.TableNextColumn();
                            using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                string rank1 = player1.Rank != null && player1.Rank?.Tier != ArenaTier.None ? player1.Rank!.ToString() : "";
                                ImGui.AlignTextToFramePadding();
                                ImGui.Text(rank1);
                            }

                        } else {
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                        }
                    }
                }
            }
        }
        ImGui.NewLine();
        if(Match.PostMatch is null) {
            ImGui.Text("Post game statistics unavailable.");
        } else {
            if((Match.MatchType == CrystallineConflictMatchType.Ranked || Match.MatchType == CrystallineConflictMatchType.Unknown)
                && Match.PostMatch.RankBefore is not null && Match.PostMatch.RankAfter is not null) {
                ImGui.Text($"{Match.PostMatch.RankBefore.ToString()} → {Match.PostMatch.RankAfter.ToString()}");
            }
            ImGui.NewLine();

            if(_timeline != null) {
                using(var tabBar = ImRaii.TabBar("TabBar")) {
                    if(Match.PostMatch != null) {
                        using var tab = ImRaii.TabItem("Scoreboard");
                        if(tab) {
                            if(CurrentTab != "Scoreboard") {
                                if(_scoreboardTicks < 20) {
                                    Flags |= ImGuiWindowFlags.AlwaysAutoResize;
                                } else {
                                    Flags &= ~ImGuiWindowFlags.AlwaysAutoResize;
                                    SetWindowSize(_savedScoreboardSize ?? _defaultScoreboardSize);
                                }
                                CurrentTab = "Scoreboard";
                            } else if(_scoreboardTicks >= 20) {
                                _savedScoreboardSize ??= ImGui.GetWindowSize();
                                Flags &= ~ImGuiWindowFlags.AlwaysAutoResize;
                            }
                            _scoreboardTicks++;
                            DrawScoreboard();
                        } else {
                            Flags &= ~ImGuiWindowFlags.AlwaysAutoResize;
                        }
                    }
                    if(_timeline.Kills != null) {
                        using(var tab = ImRaii.TabItem("Timeline")) {
                            if(tab) {
                                if(CurrentTab != "Timeline") {
                                    //SetWindowSize(new Vector2(SizeConstraints!.Value.MinimumSize.X, 600));
                                    SetWindowSize(_savedTimelineSize ?? _defaultTimelineSize);
                                    CurrentTab = "Timeline";
                                }
                                _savedTimelineSize = ImGui.GetWindowSize();
                                DrawTimeline();
                            }
                        }
                    }
                    using(var tab = ImRaii.TabItem("Graphs")) {
                        if(tab) {
                            if(CurrentTab != "Graphs") {
                                SetWindowSize(_savedGraphSize ?? _defaultGraphSize);
                                CurrentTab = "Graphs";
                            }
                            _savedGraphSize = ImGui.GetWindowSize();
                            DrawGraphs();
                        }
                    }
                }
            } else {
                DrawScoreboard();
            }
        }
        _firstDraw = false;
    }

    private void DrawScoreboard() {
        ImGui.NewLine();
        //ImGui.AlignTextToFramePadding();
        ImGuiHelper.HelpMarker("Right-click table header to show and hide columns including extra metrics.", true, true);
        using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
            ImGui.SameLine();
        }
        ImGuiComponents.ToggleButton("##showPercentages", ref ShowPercentages);
        ImGui.SameLine();
        //ImGui.AlignTextToFramePadding();
        ImGui.Text("Show team contributions");
        using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
            ImGui.SameLine();
        }
        ImGui.Checkbox("###showTeamRows", ref ShowTeamRows);
        ImGui.SameLine();
        ImGui.Text("Show team totals");
        using(var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * 2.5f * ImGuiHelpers.GlobalScale)) {
            ImGui.SameLine();
        }
        _teamQuickFilter.Draw();

        var tableFlags = ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.PadOuterX;
        if(Plugin.Configuration.StretchScoreboardColumns ?? false) {
            tableFlags -= ImGuiTableFlags.ScrollX;
        }
        using var table = ImRaii.Table($"postmatchplayers##{Match.Id}", 15, tableFlags);
        if(!table) return;
        var widthStyle = Plugin.Configuration.StretchScoreboardColumns ?? false ? ImGuiTableColumnFlags.WidthStretch : ImGuiTableColumnFlags.WidthFixed;
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 100f, 0);
        ImGui.TableSetupColumn("Home World", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 110f, 1);
        ImGui.TableSetupColumn("Job", widthStyle, ImGuiHelpers.GlobalScale * 50f, 2);
        ImGui.TableSetupColumn("Kills", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Kills".GetHashCode());
        ImGui.TableSetupColumn("Deaths", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Deaths".GetHashCode());
        ImGui.TableSetupColumn("Assists", widthStyle, ImGuiHelpers.GlobalScale * 52f, (uint)"Assists".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageDealt".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"DamageTaken".GetHashCode());
        ImGui.TableSetupColumn("HP Restored", widthStyle, ImGuiHelpers.GlobalScale * 65f, (uint)"HPRestored".GetHashCode());
        ImGui.TableSetupColumn("Time on Crystal", widthStyle, ImGuiHelpers.GlobalScale * 60f, (uint)"TimeOnCrystal".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Kill/Assist", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerKA".GetHashCode());
        ImGui.TableSetupColumn("Damage Dealt per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageDealtPerLife".GetHashCode());
        ImGui.TableSetupColumn("Damage Taken per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"DamageTakenPerLife".GetHashCode());
        ImGui.TableSetupColumn("HP Restored per Life", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 100f, (uint)"HPRestoredPerLife".GetHashCode());
        ImGui.TableSetupColumn("KDA Ratio", widthStyle | ImGuiTableColumnFlags.DefaultHide, ImGuiHelpers.GlobalScale * 50f, (uint)"KDA".GetHashCode());

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
            ImGuiHelper.DrawTableHeader("Damage\nDealt");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Damage\nTaken");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("HP\nRestored");
        }
        if(ImGui.TableNextColumn()) {
            ImGuiHelper.DrawTableHeader("Time on\nCrystal");
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
                ImGui.TableNextColumn();
                Vector4 rowColor = ImGuiColors.DalamudWhite;
                if(row.Key == Match.LocalPlayerTeam?.TeamName || (Match.IsSpectated && row.Key == CrystallineConflictTeamName.Astra)) {
                    rowColor = Plugin.Configuration.Colors.CCPlayerTeam;
                } else {
                    rowColor = Plugin.Configuration.Colors.CCEnemyTeam;
                }
                rowColor.W = Plugin.Configuration.TeamRowAlpha;

                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
                ImGui.TextUnformatted(MatchHelper.GetTeamName(row.Key));
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
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageDealt}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.DamageTaken}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{row.Value.HPRestored}", -11f);
                }
                if(ImGui.TableNextColumn()) {
                    ImGuiHelper.DrawNumericCell($"{ImGuiHelper.GetTimeSpanString(row.Value.TimeOnCrystal)}", -11f);
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
            //might have performance implications
            var player = Match.Players!.Where(x => x.Alias.Equals(row.Key)).FirstOrDefault();
            ImGui.TableNextColumn();
            Vector4 rowColor = ImGuiColors.DalamudWhite;
            if(player?.Team == Match.LocalPlayerTeam?.TeamName || (Match.IsSpectated && player?.Team == CrystallineConflictTeamName.Astra)) {
                rowColor = Plugin.Configuration.Colors.CCPlayerTeam;
            } else {
                rowColor = Plugin.Configuration.Colors.CCEnemyTeam;
            }
            rowColor.W = Plugin.Configuration.PlayerRowAlpha;

            var textColor = Match.LocalPlayer is not null && Match.LocalPlayer.Equals(row.Key) ? Plugin.Configuration.Colors.CCLocalPlayer : Plugin.Configuration.Colors.PlayerRowText;
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
            ImGui.TextColored(textColor, $"{row.Key.Name}");
            if(ImGui.TableNextColumn()) {
                ImGui.TextColored(textColor, $"{row.Key.HomeWorld}");
            }
            if(ImGui.TableNextColumn()) {
                var jobString = $"{player?.Job}";
                ImGuiHelper.CenterAlignCursor(jobString);
                ImGui.TextColored(textColor, jobString);
            }

            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].Kills) : row.Value.Kills)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].Deaths) : row.Value.Deaths)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].Assists) : row.Value.Assists)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].DamageDealt) : row.Value.DamageDealt)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].DamageTaken) : row.Value.DamageTaken)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].HPRestored) : row.Value.HPRestored)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{(ShowPercentages ? string.Format("{0:P1}", _playerContributions?[row.Key].TimeOnCrystal) : ImGuiHelper.GetTimeSpanString(row.Value.TimeOnCrystal))}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:f0}", row.Value.DamageDealtPerKA)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:f0}", row.Value.DamageDealtPerLife)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:f0}", row.Value.DamageTakenPerLife)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:f0}", row.Value.HPRestoredPerLife)}", -11f, textColor);
            }
            if(ImGui.TableNextColumn()) {
                ImGuiHelper.DrawNumericCell($"{string.Format("{0:0.00}", row.Value.KDA)}", -11f, textColor);
            }
        }
    }

    private void DrawTimeline() {
        //filters...
        string quickSearch = _playerFilter.SearchText;
        ImGuiHelper.SetDynamicWidth(150f, 250f, 3f);
        if(ImGui.InputTextWithHint("###playerQuickSearch", "Search for players and actions...", ref quickSearch, 100)) {
            _playerFilter.SearchText = quickSearch;
            RefreshQueue.QueueDataOperation(() => {
                ApplyTimelineFilters();
            });
        }
        ImGuiHelper.HelpMarker("Comma separate multiple phrases.");


        using var child = ImRaii.Child("timelineChild", ImGui.GetContentRegionAvail(), true);
        if(Match.DutyStartTime >= Match.MatchStartTime) {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Timeline incomplete due to duty joined in progress.");
        }
        using(var table = ImRaii.Table("timelineTable", 5)) {
            if(table) {
                ImGui.TableSetupColumn("time", ImGuiTableColumnFlags.WidthFixed, 40f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("team1", ImGuiTableColumnFlags.WidthFixed, 30f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("crystal", ImGuiTableColumnFlags.WidthFixed, 30f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("team2", ImGuiTableColumnFlags.WidthFixed, 30f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("text", ImGuiTableColumnFlags.WidthStretch);

                for(var i = 0; i < _consolidatedEvents.Count; i++) {
                    var matchEvent = _consolidatedEvents[i];
                    if(!_consolidatedEventsFiltered.Contains(matchEvent)) continue;
                    var astraColor = _localPlayerTeam == CrystallineConflictTeamName.Umbra ? Plugin.Configuration.Colors.CCEnemyTeam : Plugin.Configuration.Colors.CCPlayerTeam;
                    var umbraColor = _localPlayerTeam == CrystallineConflictTeamName.Umbra ? Plugin.Configuration.Colors.CCPlayerTeam : Plugin.Configuration.Colors.CCEnemyTeam;
                    var crystalColor = ImGuiColors.DalamudWhite;
                    if(_consolidatedEventTeamPoints[i].Crystal > 0) {
                        crystalColor = astraColor;
                    } else if(_consolidatedEventTeamPoints[i].Crystal < 0) {
                        crystalColor = umbraColor;
                    }

                    ImGui.TableNextColumn();
                    var timeDiff = Match.MatchStartTime - matchEvent.Timestamp;
                    ImGui.AlignTextToFramePadding();
                    ImGuiHelper.DrawNumericCell($"{ImGuiHelper.GetTimeSpanString(timeDiff ?? TimeSpan.Zero)} : ");
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{_consolidatedEventTeamPoints[i].Astra:0.0}", 0f, astraColor);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{Math.Abs(_consolidatedEventTeamPoints[i].Crystal):0.0}", 0f, crystalColor);
                    ImGui.TableNextColumn();
                    ImGuiHelper.DrawNumericCell($"{_consolidatedEventTeamPoints[i].Umbra:0.0}", 0f, umbraColor);
                    ImGui.TableNextColumn();
                    var eventType = matchEvent.GetType();
                    switch(eventType) {
                        case Type _ when eventType == typeof(GenericMatchEvent):
                            DrawEvent(matchEvent as GenericMatchEvent);
                            break;
                        case Type _ when eventType == typeof(KnockoutEvent):
                            DrawEvent(matchEvent as KnockoutEvent);
                            break;
                        case Type _ when eventType == typeof(ProgressEvent):
                            DrawEvent(matchEvent as ProgressEvent);
                            break;
                        case Type _ when eventType == typeof(CombinedActionEvent):
                            DrawEvent(matchEvent as CombinedActionEvent);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }

    private void DrawEvent(GenericMatchEvent mEvent) {
            switch(mEvent.Type) {
            case CrystallineConflictMatchEvent.CrystalUnchained:
                ImGui.Text("The crystal has been unchained.");
                break;
            case CrystallineConflictMatchEvent.OvertimeCommenced:
                ImGui.Text("Overtime has started.");
                break;
            case CrystallineConflictMatchEvent.MatchEnded:
                var color = Plugin.Configuration.Colors.CCEnemyTeam;
                if(_localPlayerTeam == null && Match.MatchWinner == CrystallineConflictTeamName.Astra
                    || Match.MatchWinner == _localPlayerTeam) {
                    color = Plugin.Configuration.Colors.CCPlayerTeam;
                }
                if(Match.MatchWinner != null) {
                    using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y))) {
                        ImGui.TextColored(color, $"Team {Match.MatchWinner}");
                        ImGui.SameLine();
                        ImGui.Text(" is victorious!");
                    }
                } else {
                    ImGui.Text("The match has ended...");
                }
                break;
            case CrystallineConflictMatchEvent.SpecialEvent:
                switch(Match.Arena) {
                    case CrystallineConflictMap.VolcanicHeart:
                        ImGui.Text("Igneous matter spews forth!");
                        break;
                    case CrystallineConflictMap.CloudNine:
                        ImGui.Text("The winds whip and churn!");
                        break;
                    case CrystallineConflictMap.ClockworkCastleTown:
                        ImGui.Text("The pneumatic parade is underway!");
                        break;
                    case CrystallineConflictMap.RedSands:
                        ImGui.Text("The desert heat intensifies!");
                        break;
                    default:
                        ImGui.Text("A special map event has occurred.");
                        break;
                }

                break;
            default:
                break;
        }
    }

    private void DrawEvent(ProgressEvent mEvent) {
        var color = Plugin.Configuration.Colors.CCEnemyTeam;
        if(_localPlayerTeam == null && mEvent.Team == CrystallineConflictTeamName.Astra
            || mEvent.Team == _localPlayerTeam) {
            color = Plugin.Configuration.Colors.CCPlayerTeam;
        }
        switch(mEvent.Type) {
            case 1:
                using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y))) {
                    ImGui.TextColored(color, $"Team {mEvent.Team}");
                    ImGui.SameLine();
                    ImGui.Text(" has breached the checkpoint.");
                }
                break;
            default:
                break;
        }
    }

    private void DrawEvent(KnockoutEvent mEvent) {
        var victimPlayer = _players.FirstOrDefault(x => x.Alias.Equals(mEvent.Victim));
        var killerPlayer = _players.FirstOrDefault(x => x.Alias.Equals(mEvent.CreditedKiller));

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));

        if(mEvent.KillerNameId != null) {
            ImGui.Text($"{_bNPCNames[(uint)mEvent.KillerNameId]}");
            if(mEvent.CreditedKiller != null) {
                ImGui.SameLine();
                ImGui.Text(" (");
                ImGui.SameLine();
                DrawPlayer(mEvent.CreditedKiller);
                ImGui.SameLine();
                ImGui.Text(")");
            }
        } else {
            if(mEvent.CreditedKiller != null) {
                DrawPlayer(mEvent.CreditedKiller);
                ImGui.SameLine();
            } else {
                ImGui.Text($"A gentle breeze somehow ");
            }
        }
        ImGui.SameLine();
        ImGui.Text(" knocked out ");
        ImGui.SameLine();
        DrawPlayer(mEvent.Victim);
        ImGui.SameLine();
        ImGui.Text(".");
    }

    private void DrawEvent(CombinedActionEvent mEvent) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        DrawPlayer(mEvent.Actor);
        ImGui.SameLine();
        if(mEvent.EffectTime != null) {
            ImGui.Text($" used ");
        } else {
            ImGui.Text($" ghosted ");
        }
        ImGui.SameLine();
        ImGui.Image(Plugin.WindowManager.GetTextureHandle(_actionIcons[mEvent.ActionId]), new Vector2(24 * ImGuiHelpers.GlobalScale));
        ImGui.SameLine();
        ImGui.Text($" {_actionNames[mEvent.ActionId]}");
        ImGui.SameLine();
        if(mEvent.PlayerCastTarget != null && !mEvent.PlayerCastTarget.Equals(mEvent.Actor)) {
            ImGui.Text(" on ");
            ImGui.SameLine();
            DrawPlayer(mEvent.PlayerCastTarget);
        }
        ImGui.SameLine();
        if(mEvent.EffectTime != null) {
            ImGui.Text($".");
        } else {
            ImGui.Text($"...");
        }
    }

    private void DrawPlayer(PlayerAlias name) {
        var player = _players.FirstOrDefault(x => x.Alias.Equals(name));
        Vector4 color = Plugin.Configuration.Colors.CCEnemyTeam;
        if(_localPlayerTeam == null && player?.Team == CrystallineConflictTeamName.Astra
            || player?.Team == _localPlayerTeam) {
            color = Plugin.Configuration.Colors.CCPlayerTeam;
        }

        using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
            if(player?.Job != null && TextureHelper.JobIcons.TryGetValue((Job)player.Job, out var icon)) {
                ImGui.Image(Plugin.WindowManager.GetTextureHandle(icon), new Vector2(24 * ImGuiHelpers.GlobalScale));
                ImGui.SameLine();
            }
        }
        using(var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y))) {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(color, $" {name.Name}");
        }
    }

    private void DrawGraphs() {
        //filters

        using var child = ImRaii.Child("graphChild", ImGui.GetContentRegionAvail(), true);
        if(child) {
            if(_timeline?.CrystalPosition != null) {
                DrawCrystalProgressGraph();
            }
        }
    }

    private void DrawCrystalProgressGraph() {
        using var plot = ImRaii.Plot("Crystal Progress", new Vector2(ImGui.GetContentRegionAvail().X, 500f * ImGuiHelpers.GlobalScale), ImPlotFlags.None);

        if(!plot) {
            return;
        }

        double[] yTicks = [-100, -50, 0, 50, 100];

        float[] fiftyXs = [300, 300];
        float[] fiftyYs = [-100, 100];

        float[] xInitialLimits = [0, 900];
        if(Match.MatchDuration <= TimeSpan.FromMinutes(5)) {
            xInitialLimits[1] = 300;
        }

        ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Linear);
        //ImPlot.SetupAxesLimits(xInitialLimits[0], xInitialLimits[1], -100, 100, ImPlotCond.Once);
        ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, 0, 900);
        ImPlot.SetupAxisLimitsConstraints(ImAxis.Y1, -100, 100);

        ImPlot.SetupAxes("Match Time", "", ImPlotAxisFlags.None, ImPlotAxisFlags.None);
        ImPlot.SetupLegend(ImPlotLocation.NorthWest, ImPlotLegendFlags.Horizontal | ImPlotLegendFlags.Outside);

        ImPlot.SetupAxisTicks(ImAxis.X1, ref _xAxisTicks[0], _xAxisTicks.Length, _xAxisLabels);
        ImPlot.SetupAxisTicks(ImAxis.Y1, ref yTicks[0], yTicks.Length);

        var astraColor = _localPlayerTeam == CrystallineConflictTeamName.Umbra ? Plugin.Configuration.Colors.CCEnemyTeam : Plugin.Configuration.Colors.CCPlayerTeam;
        var umbraColor = _localPlayerTeam == CrystallineConflictTeamName.Umbra ? Plugin.Configuration.Colors.CCPlayerTeam : Plugin.Configuration.Colors.CCEnemyTeam;

        using(var style = ImRaii.PushColor(ImPlotCol.Line, ImGuiColors.DalamudOrange)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 1f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Overtime", ref fiftyXs[0],
                ref fiftyYs[0],
                fiftyXs.Length, ImPlotStairsFlags.None);
        }

        using(var style = ImRaii.PushColor(ImPlotCol.Line, ImGuiColors.DalamudWhite)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 1f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Crystal Position", ref _crystalPosition.Xs[0],
                ref _crystalPosition.Ys[0],
                _crystalPosition.Xs.Length, ImPlotStairsFlags.None);
        }

        using(var style = ImRaii.PushColor(ImPlotCol.Line, astraColor)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 3f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Astra Progress", ref _teamPoints[CrystallineConflictTeamName.Astra].Xs[0],
                ref _teamPoints[CrystallineConflictTeamName.Astra].Ys[0],
                _teamPoints[CrystallineConflictTeamName.Astra].Xs.Length, ImPlotStairsFlags.None);
        }

        using(var style = ImRaii.PushColor(ImPlotCol.Line, umbraColor)) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 3f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Umbra Progress", ref _teamPoints[CrystallineConflictTeamName.Umbra].Xs[0],
                ref _teamPoints[CrystallineConflictTeamName.Umbra].Ys[0],
                _teamPoints[CrystallineConflictTeamName.Umbra].Xs.Length, ImPlotStairsFlags.None);
        }

        using(var style = ImRaii.PushColor(ImPlotCol.Line, astraColor - new Vector4(0f, 0f, 0f, 0.6f))) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 2f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Astra Mid Progress", ref _teamMidPoints[CrystallineConflictTeamName.Astra].Xs[0],
                ref _teamMidPoints[CrystallineConflictTeamName.Astra].Ys[0],
                _teamMidPoints[CrystallineConflictTeamName.Astra].Xs.Length, ImPlotStairsFlags.None);
        }

        using(var style = ImRaii.PushColor(ImPlotCol.Line, umbraColor - new Vector4(0f, 0f, 0f, 0.6f))) {
            using var _ = ImRaii.PushStyle(ImPlotStyleVar.LineWeight, 2f * ImGuiHelpers.GlobalScale);
            ImPlot.PlotStairs("Umbra Mid Progress", ref _teamMidPoints[CrystallineConflictTeamName.Umbra].Xs[0],
                ref _teamMidPoints[CrystallineConflictTeamName.Umbra].Ys[0],
                _teamMidPoints[CrystallineConflictTeamName.Umbra].Xs.Length, ImPlotStairsFlags.None);
        }
    }

    private void SetupCrystalPositionGraph() {
        if(_timeline?.CrystalPosition == null) {
            return;
        }

        int direction = 1;
        if(_localPlayerTeam == CrystallineConflictTeamName.Umbra) {
            direction = -1;
        }

        var pointEvents = _timeline.CrystalPosition
            //.Where(x => x.Points != 0 || (x.Timestamp - Match.MatchStartTime).Value.TotalSeconds > 10)
            .Prepend(new((DateTime)Match.MatchStartTime!, 0))
            .Append(new((DateTime)Match.MatchEndTime!, _timeline.CrystalPosition.Last().Points));
        _crystalPosition = (pointEvents.Select(x => (float)(x.Timestamp - Match.MatchStartTime).Value.TotalSeconds).ToArray(), pointEvents.Select(x => direction * x.Points / 10f).ToArray());
    }

    private void SetupProgressGraph(CrystallineConflictTeamName team) {
        if(_timeline?.TeamProgress == null) {
            return;
        }
        int direction = 1;
        if(_localPlayerTeam == null && team == CrystallineConflictTeamName.Umbra 
            || _localPlayerTeam != null && team != _localPlayerTeam) {
            direction = -1;
        }

        var pointEvents = _timeline.TeamProgress[team]
            //.Where(x => x.Points != 0 || (x.Timestamp - Match.MatchStartTime).Value.TotalSeconds > 10)
            .Append(new((DateTime)Match.MatchEndTime!, _timeline.TeamProgress[team].Last().Points));
        _teamPoints[team] = (pointEvents.Select(x => (float)(x.Timestamp - Match.MatchStartTime).Value.TotalSeconds).ToArray(), pointEvents.Select(x => direction * x.Points / 10f).ToArray());
    }

    private void SetupMidProgressGraph(CrystallineConflictTeamName team) {
        if(_timeline?.TeamMidProgress == null) {
            return;
        }
        int direction = 1;
        if(_localPlayerTeam == null && team == CrystallineConflictTeamName.Umbra
            || _localPlayerTeam != null && team != _localPlayerTeam) {
            direction = -1;
        }

        var pointEvents = _timeline.TeamMidProgress[team]
            //.Where(x => x.Points != 0 || (x.Timestamp - Match.MatchStartTime).Value.TotalSeconds > 10)
            .Append(new((DateTime)Match.MatchEndTime!, _timeline.TeamMidProgress[team].Last().Points));
        _teamMidPoints[team] = (pointEvents.Select(x => (float)(x.Timestamp - Match.MatchStartTime).Value.TotalSeconds).ToArray(), pointEvents.Select(x => direction * (float)x.Points).ToArray());
    }

    private void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        if(_unfilteredScoreboard == null || _scoreboard == null) return;

        //Func<KeyValuePair<CrystallineConflictPostMatchRow, (CCScoreboard, CCScoreboardDouble)>, object> comparator = (r) => 0;
        Func<KeyValuePair<PlayerAlias, CCScoreboardTally>, object> comparator = (r) => 0;
        Func<KeyValuePair<CrystallineConflictTeamName, CCScoreboardTally>, object> teamComparator = (r) => 0;

        //0 = name
        //1 = home world
        //2 = job
        if(columnId == 0) {
            comparator = (r) => Match.Players.First(x => x.Alias.Equals(r.Key)).Alias.Name ?? "";
            teamComparator = (r) => r.Key;
        } else if(columnId == 1) {
            comparator = (r) => Match.Players.First(x => x.Alias.Equals(r.Key)).Alias.HomeWorld ?? "";
        } else if(columnId == 2) {
            comparator = (r) => Match.Players.First(x => x.Alias.Equals(r.Key)).Job ?? 0;
        } else {
            bool propFound = false;
            if(ShowPercentages && _playerContributions != null) {
                var props = typeof(CCScoreboardDouble).GetProperties();
                foreach(var prop in props) {
                    var propId = prop.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        //Plugin.Log.Debug($"sorting by {prop.Name}");
                        comparator = (r) => prop.GetValue(_playerContributions[r.Key]) ?? 0;
                        propFound = true;
                        break;
                    }
                }
            }
            if(!propFound) {
                var props = typeof(CCScoreboardTally).GetProperties();
                var fields = typeof(CCScoreboardTally).GetFields();
                List<MemberInfo> members = [.. props, .. fields];
                //iterate to two levels
                foreach(var member in members) {
                    var propId = member.Name.GetHashCode();
                    if((uint)propId == columnId) {
                        if(member is PropertyInfo) {
                            comparator = (r) => (member as PropertyInfo)!.GetValue(r.Value) ?? 0;
                            teamComparator = (r) => (member as PropertyInfo)!.GetValue(r.Value) ?? 0;
                        } else if(member is FieldInfo) {
                            comparator = (r) => (member as FieldInfo)!.GetValue(r.Value) ?? 0;
                            teamComparator = (r) => (member as FieldInfo)!.GetValue(r.Value) ?? 0;
                        }
                        break;
                    }
                }
            }
        }

        _scoreboard = direction == ImGuiSortDirection.Ascending ? _scoreboard.OrderBy(comparator).ToDictionary()
            : _scoreboard.OrderByDescending(comparator).ToDictionary();
        _unfilteredScoreboard = direction == ImGuiSortDirection.Ascending ? _unfilteredScoreboard.OrderBy(comparator).ToDictionary()
            : _unfilteredScoreboard.OrderByDescending(comparator).ToDictionary();

        if(_teamScoreboard != null && !Plugin.Configuration.AnchorTeamNames) {
            _teamScoreboard = direction == ImGuiSortDirection.Ascending ? _teamScoreboard.OrderBy(teamComparator).ToDictionary()
            : _teamScoreboard.OrderByDescending(teamComparator).ToDictionary();
        }
    }

    private Task ApplyTeamFilter() {
        if(_scoreboard == null || _unfilteredScoreboard == null) {
            return Task.CompletedTask;
        }
        _scoreboard = _unfilteredScoreboard.Where(x => {
            var player = Match.Players.Where(y => y.Alias.Equals(x.Key)).FirstOrDefault();
            return _teamQuickFilter.FilterState[(CrystallineConflictTeamName)player.Team];
        }).ToDictionary();
        return Task.CompletedTask;
    }

    private void ApplyTimelineFilters() {
        List<MatchEvent> filteredList = new();
        var playerNames = _playerFilter.SearchText.Trim().Split(",").ToList();
        foreach(var mEvent in _consolidatedEvents) {
            if(mEvent is KnockoutEvent) {
                var mEvent2 = mEvent as KnockoutEvent;
                foreach(var fragment in playerNames) {
                    bool match1 = mEvent2!.Victim.ToString().Contains(fragment, StringComparison.OrdinalIgnoreCase);
                    bool match2 = mEvent2.CreditedKiller?.ToString().Contains(fragment, StringComparison.OrdinalIgnoreCase) ?? false;
                    if(match1 || match2) {
                        filteredList.Add(mEvent);
                        break;
                    }
                }
            } else if(mEvent is CombinedActionEvent) {
                var mEvent2 = mEvent as CombinedActionEvent;
                foreach(var fragment in playerNames) {
                    bool match1 = mEvent2!.Actor.ToString().Contains(fragment, StringComparison.OrdinalIgnoreCase);
                    bool match2 = mEvent2.PlayerCastTarget?.ToString().Contains(fragment, StringComparison.OrdinalIgnoreCase) ?? false;
                    bool match3 = _actionNames[mEvent2.ActionId].Contains(fragment, StringComparison.OrdinalIgnoreCase);
                    if(match1 || match2 || match3) {
                        filteredList.Add(mEvent);
                        break;
                    }
                }
            } else {
                filteredList.Add(mEvent);
            }
        }
        filteredList.Sort();
        _consolidatedEventsFiltered = filteredList;
    }

    protected override string BuildCSV() {
        string csv = "";
        //header
        csv += "Id,Start Time,Arena,Queue,Winner,Duration,Astra Progress,Umbra Progress,\n";
        csv += Match.Id + "," + Match.DutyStartTime + ","
            + (Match.Arena != null ? MatchHelper.GetArenaName((CrystallineConflictMap)Match.Arena!) : "") + ","
            + Match.MatchType + "," + Match.MatchWinner + "," + Match.MatchDuration + ","
            + Match.Teams[CrystallineConflictTeamName.Astra].Progress + "," + Match.Teams[CrystallineConflictTeamName.Umbra].Progress + ","
            + "\n";

        //post match
        if(_scoreboard != null) {
            csv += "\n\n\n";
            csv += "Name,HomeWorld,Job,Team,Kills,Deaths,Assists,Damage Dealt,Damage Taken,HP Restored,Time on Crystal,\n";
            var players = Match.Players;
            foreach(var row in _scoreboard) {
                var player = players.Where(x => x.Alias.Equals(row.Key)).FirstOrDefault();
                csv += row.Key.Name + "," + row.Key.HomeWorld + "," + player?.Job + ",";
                csv += player?.Team + "," + row.Value.Kills + "," + row.Value.Deaths + "," + row.Value.Assists + "," + row.Value.DamageDealt + "," + row.Value.DamageTaken + "," + row.Value.HPRestored + "," + row.Value.TimeOnCrystal + ",";
                csv += "\n";
            }
        }
        return csv;
    }
}
