using Dalamud.Interface.Colors;
using ImGuiNET;
using ImPlotNET;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PvpStats.Windows.Summary;
internal class CrystallineConflictRankGraph {

    private Plugin _plugin;
    private List<(DateTime, int)> RankData = new();

    public CrystallineConflictRankGraph(Plugin plugin) {
        _plugin = plugin;
    }

    public void Refresh(List<CrystallineConflictMatch> matches) {
        List<(DateTime, int)> rankData = new();
        //DateTime firstRank = DateTime.MinValue;
        //DateTime lastRank = DateTime.MaxValue;
        foreach(var match in matches.OrderBy(x => x.DutyStartTime)) {
            if(match.PostMatch != null && match.PostMatch.RankBefore != null && match.PostMatch.RankAfter != null && match.MatchType == CrystallineConflictMatchType.Ranked) {
                rankData.Add((match.DutyStartTime, match.PostMatch.RankBefore.TotalCredit));
                rankData.Add(((DateTime)match.MatchEndTime, match.PostMatch.RankAfter.TotalCredit));
            }
        }
        _plugin.Log.Debug("...");
        foreach(var x in rankData) {
            _plugin.Log.Debug($"x: {x.Item1.Ticks} y: {x.Item2}");
        }
        _plugin.Log.Debug($"min cred: {PlayerRank.MinRank.TotalCredit}");

        RankData = rankData;
    }

    public unsafe void Draw() {

        //if(ImPlot.BeginPlot("test plot", ImGui.GetContentRegionAvail(), ImPlotFlags.NoTitle)) {
        //    float[] xs = new float[10], ys = new float[10];
        //    for(int i = 0; i < xs.Length; i++) {
        //        xs[i] = i;
        //        ys[i] = i;
        //    }

        //    //ImPlot.SetNextAxesToFit();
        //    ImPlot.SetupAxes("time", "", ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.AutoFit);
        //    ImPlot.SetNextMarkerStyle(ImPlotMarker.Asterisk);
        //    ImPlot.PlotLine("credit", ref xs[0], ref ys[0], xs.Length, ImPlotLineFlags.Segments);

        //    ImPlot.EndPlot();
        //}

        //var colors = new[] { ImGuiColors.DPSRed, ImGuiColors.TankBlue, ImGuiColors.ParsedGreen };
        //var cmap = ImPlot.AddColormap("Ranks", ref colors, 6);

        if(ImPlot.BeginPlot("rank graph", ImGui.GetContentRegionAvail(), ImPlotFlags.NoTitle)) {
            ImDrawListPtr drawList = ImPlot.GetPlotDrawList();
            float[] xs = RankData.Select(x => (float)((DateTimeOffset)x.Item1).ToUnixTimeSeconds()).ToArray();
            float[] ys = RankData.Select(x => (float)x.Item2).ToArray();
            //ImPlot.SetNextAxesToFit();
            ImPlot.SetupAxes("UTC Time", "");
            ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Time);
            //ImPlot.SetupAxisLimits(ImAxis.X1, xs[0], xs[xs.Length - 1]);
            ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, xs[0], xs[xs.Length - 1]);
            //ImPlot.SetupAxisZoomConstraints(ImAxis.X1, xs[0], xs[xs.Length - 1]);
            //ImPlot.SetupAxisLimits(ImAxis.Y1, PlayerRank.MinRank.TotalCredit, double.MaxValue);
            ImPlot.SetupAxisLimitsConstraints(ImAxis.Y1, PlayerRank.MinRank.TotalCredit, 50000);

            ImPlot.SetNextMarkerStyle(ImPlotMarker.Circle);
            ImPlot.PushStyleColor(ImPlotCol.Line, ImGui.GetColorU32(ImGuiColors.DalamudRed));
            ImPlot.PlotLine("Crystal Credit", ref xs[0], ref ys[0], xs.Length, ImPlotLineFlags.Segments);
            ImPlot.PopStyleColor();

            //ImPlot.PlotLine("", ref xs[0], ref ys[0], xs.Length, ImPlotLineFlags.Segments);
            //for(int i = 0; i < xs.Length; i += 2) {
            //    bool isGain = ys[i + 1] >= ys[i];
            //    var color = isGain ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
            //    drawList.AddLine(ImPlot.PlotToPixels(xs[i], ys[i]), ImPlot.PlotToPixels(xs[i + 1], ys[i + 1]), ImGui.GetColorU32(color));
            //}

            //add rank shading

            ImPlot.PushPlotClipRect();
            //bronze
            var bronzep1 = ImPlot.PlotToPixels(xs[0], -4800);
            var bronzep2 = ImPlot.PlotToPixels(xs[xs.Length - 1], -5700);
            drawList.AddRectFilled(bronzep1, bronzep2, ImGui.GetColorU32(new Vector4(0.54f, 0.47f, 0.33f, 0.2f)));

            //silver
            var silverp1 = ImPlot.PlotToPixels(xs[0], -3900);
            var silverp2 = ImPlot.PlotToPixels(xs[xs.Length - 1], -4800);
            drawList.AddRectFilled(silverp1, silverp2, ImGui.GetColorU32(new Vector4(0.90f, 0.91f, 0.98f, 0.2f)));
            //gold
            var goldp1 = ImPlot.PlotToPixels(xs[0], -2700);
            var goldp2 = ImPlot.PlotToPixels(xs[xs.Length - 1], -3900);
            drawList.AddRectFilled(goldp1, goldp2, ImGui.GetColorU32(new Vector4(0.80f, 0.62f, 0.20f, 0.2f)));
            //plat
            var platp1 = ImPlot.PlotToPixels(xs[0], -1500);
            var platp2 = ImPlot.PlotToPixels(xs[xs.Length - 1], -2700);
            drawList.AddRectFilled(platp1, platp2, ImGui.GetColorU32(new Vector4(0.90f, 0.91f, 0.98f, 0.2f)));
            //diamond
            var diap1 = ImPlot.PlotToPixels(xs[0], 0);
            var diap2 = ImPlot.PlotToPixels(xs[xs.Length - 1], -1500);
            drawList.AddRectFilled(diap1, diap2, ImGui.GetColorU32(new Vector4(0.15f, 0.96f, 0.8f, 0.2f)));
            ////crystal
            //var cryp1 = ImPlot.PlotToPixels(xs[0], 0);
            //var cryp2 = ImPlot.PlotToPixels(xs[xs.Length - 1], -1500);
            //drawList.AddRectFilled(diap1, diap2, ImGui.GetColorU32(new Vector4(0.15f, 0.96f, 0.8f, 0.1f)), 0);
            ImPlot.PopPlotClipRect();
            ImPlot.EndPlot();
        }
    }

    private static void PlotCandlestick(string label_id, double[] xs, double[] opens, double[] closes, double[] lows, double[] highs, int count, bool tooltip, float width_percent, Vector4 bullCol, Vector4 bearCol) {

        // get ImGui window DrawList
        ImDrawListPtr draw_list = ImPlot.GetPlotDrawList();
        // calc real value width
        double half_width = count > 1 ? (xs[1] - xs[0]) * width_percent : width_percent;

        // custom tool
        //if (ImPlot.IsPlotHovered() && tooltip) {
        //    ImPlotPoint mouse = ImPlot.GetPlotMousePos();
        //    mouse.x = ImPlot.RoundTime(ImPlotTime.FromDouble(mouse.x), ImPlotTimeUnit_Day).ToDouble();
        //    float tool_l = ImPlot::PlotToPixels(mouse.x - half_width * 1.5, mouse.y).x;
        //    float tool_r = ImPlot::PlotToPixels(mouse.x + half_width * 1.5, mouse.y).x;
        //    float tool_t = ImPlot::GetPlotPos().y;
        //    float tool_b = tool_t + ImPlot::GetPlotSize().y;
        //ImPlot::PushPlotClipRect();
        //    draw_list->AddRectFilled(ImVec2(tool_l, tool_t), ImVec2(tool_r, tool_b), IM_COL32(128,128,128,64));
        //    ImPlot::PopPlotClipRect();
        //    // find mouse location index
        //    int idx = BinarySearch(xs, 0, count - 1, mouse.x);
        //    // render tool tip (won't be affected by plot clip rect)
        //    if (idx != -1) {
        //        ImGui::BeginTooltip();
        //        char buff[32];
        //ImPlot::FormatDate(ImPlotTime::FromDouble(xs[idx]),buff,32,ImPlotDateFmt_DayMoYr,ImPlot::GetStyle().UseISO8601);
        //        ImGui::Text("Day:   %s",  buff);
        //        ImGui::Text("Open:  $%.2f", opens[idx]);
        //        ImGui::Text("Close: $%.2f", closes[idx]);
        //        ImGui::Text("Low:   $%.2f", lows[idx]);
        //        ImGui::Text("High:  $%.2f", highs[idx]);
        //        ImGui::EndTooltip();
        //    }
        //}

        // begin plot item

        //if(ImPlot::BeginItem(label_id)) {
        //// override legend icon color
        //ImPlot.GetCurrentItem()->Color = IM_COL32(64, 64, 64, 255);
        //// fit data if requested
        //if(ImPlot.FitThisFrame()) {
        //    for(int i = 0; i < count; ++i) {
        //        ImPlot::FitPoint(ImPlotPoint(xs[i], lows[i]));
        //        ImPlot::FitPoint(ImPlotPoint(xs[i], highs[i]));
        //    }
        //}
        // render data
        for(int i = 0; i < count; ++i) {
            Vector2 open_pos = ImPlot.PlotToPixels(xs[i] - half_width, opens[i]);
            Vector2 close_pos = ImPlot.PlotToPixels(xs[i] + half_width, closes[i]);
            Vector2 low_pos = ImPlot.PlotToPixels(xs[i], lows[i]);
            Vector2 high_pos = ImPlot.PlotToPixels(xs[i], highs[i]);
            uint color = ImGui.GetColorU32(opens[i] > closes[i] ? bearCol : bullCol);
            draw_list.AddLine(low_pos, high_pos, color);
            draw_list.AddRectFilled(open_pos, close_pos, color);
        }

        //// end plot item
        //ImPlot::EndItem();
        //}
    }
}
