using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PvpStats.Windows.Summary;
internal class FrontlineMeta : Refreshable<FrontlineMatch> {

    public override string Name => "FL Meta";

    private readonly Plugin Plugin;

    private static int MaxPlayers = 24;

    public FLScoreboardDouble Top1Contribs { get; private set; } = new();
    public FLScoreboardDouble Top1ContribFactor { get; private set; } = new();
    public FLScoreboardDouble Top4Contribs { get; private set; } = new();
    public FLScoreboardDouble Top4ContribFactor { get; private set; } = new();
    public FLScoreboardDouble GiniCoefficients { get; private set; } = new();

    private long[] _killBuckets = [];
    private long[] _deathBuckets = [];
    private long[] _assistBuckets = [];
    private long[] _damageToPCsBuckets = [];
    private long[] _damageToOtherBuckets = [];
    private long[] _damageTakenBuckets = [];
    private long[] _HPRestoredBuckets = [];

    public FrontlineMeta(Plugin plugin) {
        Plugin = plugin;
        Reset();
    }

    protected override void Reset() {
        _killBuckets = new long[MaxPlayers];
        _deathBuckets = new long[MaxPlayers];
        _assistBuckets = new long[MaxPlayers];
        _damageToPCsBuckets = new long[MaxPlayers];
        _damageToOtherBuckets = new long[MaxPlayers];
        _damageTakenBuckets = new long[MaxPlayers];
        _HPRestoredBuckets = new long[MaxPlayers];
    }

    protected override void ProcessMatch(FrontlineMatch match, bool remove = false) {
        if(match.PlayerScoreboards != null) {

            foreach(var team in match.Teams.Keys) {
                var teamScoreboards = match.PlayerScoreboards.Where(x => match.Players.Where(y => y.Name.Equals(x.Key)).First().Team == team);

                var killBucket = teamScoreboards.Select(x => x.Value.Kills).OrderByDescending(x => x).ToArray();
                var deathBucket = teamScoreboards.Select(x => x.Value.Deaths).OrderByDescending(x => x).ToArray();
                var assistBucket = teamScoreboards.Select(x => x.Value.Assists).OrderByDescending(x => x).ToArray();
                var pcDamageBucket = teamScoreboards.Select(x => x.Value.DamageToPCs).OrderByDescending(x => x).ToArray();
                var otherDamageBucket = teamScoreboards.Select(x => x.Value.DamageToOther).OrderByDescending(x => x).ToArray();
                var damageTakenBucket = teamScoreboards.Select(x => x.Value.DamageTaken).OrderByDescending(x => x).ToArray();
                var hpRestoredBucket = teamScoreboards.Select(x => x.Value.HPRestored).OrderByDescending(x => x).ToArray();
                IncrementBucket(ref _killBuckets, killBucket, remove);
                IncrementBucket(ref _deathBuckets, deathBucket, remove);
                IncrementBucket(ref _assistBuckets, assistBucket, remove);
                IncrementBucket(ref _damageToPCsBuckets, pcDamageBucket, remove);
                IncrementBucket(ref _damageToOtherBuckets, otherDamageBucket, remove);
                IncrementBucket(ref _damageTakenBuckets, damageTakenBucket, remove);
                IncrementBucket(ref _HPRestoredBuckets, hpRestoredBucket, remove);
            }
        }
    }

    protected override void PostRefresh(List<FrontlineMatch> matches, List<FrontlineMatch> additions, List<FrontlineMatch> removals) {
        (Top1Contribs.Kills, Top1ContribFactor.Kills, Top4Contribs.Kills, Top4ContribFactor.Kills) = GetContribs(_killBuckets);
        (Top1Contribs.Deaths, Top1ContribFactor.Deaths, Top4Contribs.Deaths, Top4ContribFactor.Deaths) = GetContribs(_deathBuckets);
        (Top1Contribs.Assists, Top1ContribFactor.Assists, Top4Contribs.Assists, Top4ContribFactor.Assists) = GetContribs(_assistBuckets);
        (Top1Contribs.DamageToPCs, Top1ContribFactor.DamageToPCs, Top4Contribs.DamageToPCs, Top4ContribFactor.DamageToPCs) = GetContribs(_damageToPCsBuckets);
        (Top1Contribs.DamageToOther, Top1ContribFactor.DamageToOther, Top4Contribs.DamageToOther, Top4ContribFactor.DamageToOther) = GetContribs(_damageToOtherBuckets);
        (Top1Contribs.DamageTaken, Top1ContribFactor.DamageTaken, Top4Contribs.DamageTaken, Top4ContribFactor.DamageTaken) = GetContribs(_damageTakenBuckets);
        (Top1Contribs.HPRestored, Top1ContribFactor.HPRestored, Top4Contribs.HPRestored, Top4ContribFactor.HPRestored) = GetContribs(_HPRestoredBuckets);

        GiniCoefficients.Kills = CalculateGini(_killBuckets);
        GiniCoefficients.Deaths = CalculateGini(_deathBuckets);
        GiniCoefficients.Assists = CalculateGini(_assistBuckets);
        GiniCoefficients.DamageToPCs = CalculateGini(_damageToPCsBuckets);
        GiniCoefficients.DamageToOther = CalculateGini(_damageToOtherBuckets);
        GiniCoefficients.DamageTaken = CalculateGini(_damageTakenBuckets);
        GiniCoefficients.HPRestored = CalculateGini(_HPRestoredBuckets);
    }

    private static void IncrementBucket(ref long[] buckets, long[] bucket, bool remove = false) {
        double total = 0;
        foreach(var value in bucket) {
            total += value;
        }

        for(int i = 0; i < bucket.Length && i < MaxPlayers; i++) {
            var contrib = (long)((bucket[i] / total) * 10000L); //5-point precision
            if(remove) {
                Interlocked.Add(ref buckets[i], -contrib);
            } else {
                Interlocked.Add(ref buckets[i], contrib);
            }
        }
    }

    public static double ThreadsafeDoubleAdd(ref double value, double amount) {
        double initialValue, computedValue;
        do {
            initialValue = value;
            computedValue = initialValue + amount;
        }
        while(Interlocked.CompareExchange(ref value, computedValue, initialValue) != initialValue);

        return computedValue;
    }

    private static (double Top1Contribution, double Top1Factor, double Top4Contribution, double Top4Factor) GetContribs(long[] buckets) {
        long totalValue = 0;
        long top1Value = 0;
        long top4Value = 0;
        for(int i = 0; i < buckets.Length; i++) {
            var value = buckets[i];
            if(i == 0) {
                top1Value += value;
            }
            if(i < 4) {
                top4Value += value;
            }
            totalValue += value;
        }
        var top1Contrib = (double)top1Value / totalValue;
        var top4Contrib = (double)top4Value / totalValue;
        var top1ContribFactor = top1Contrib / (1d / MaxPlayers);
        var top4ContribFactor = top4Contrib / (4d / MaxPlayers);
        return (top1Contrib, top1ContribFactor, top4Contrib, top4ContribFactor);
    }

    public static double CalculateGini(long[] buckets) {
        if(buckets == null || buckets.Length == 0)
            return 0;

        int n = buckets.Length;
        var sorted = buckets.OrderByDescending(v => v).ToArray();
        long cumulativeTotal = 0;
        long cumulativeSum = 0;

        for(int i = 0; i < n; i++) {
            cumulativeSum += sorted[i];
            cumulativeTotal += cumulativeSum;
        }

        double mean = sorted.Average();
        if(mean == 0) return 0;

        double gini = (2.0d * cumulativeTotal) / (n * cumulativeSum) - (n + 1.0) / n;
        return gini;
    }

    public void Draw() {
        ImGui.NewLine();
        DrawMetaStatsTable();

    }

    private void DrawMetaStatsTable() {
        using(var table = ImRaii.Table($"MatchStatsTable", 8, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoClip | ImGuiTableFlags.NoSavedSettings)) {
            if(table) {
                float offset = -1f;
                ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 120f);
                ImGui.TableSetupColumn("Kills");
                ImGui.TableSetupColumn($"Deaths");
                ImGui.TableSetupColumn($"Assists");
                ImGui.TableSetupColumn("Damage to PCs");
                ImGui.TableSetupColumn("Damage to Other");
                ImGui.TableSetupColumn($"Damage Taken");
                ImGui.TableSetupColumn($"HP Restored");

                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Kills", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Deaths", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Assists", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Damage\nto PCs", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Damage\nto Other", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("Damage\nTaken", 2, true, true, offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawTableHeader("HP\nRestored", 2, true, true, offset);

                var minFactor = 1.5f;
                var maxFactor = 4f;
                var minContrib = minFactor / 24f;
                var maxContrib = maxFactor / 24f;
                var giniMin = 0.25f;
                var giniMax = 0.6f;

                //top 1 contribution
                ImGui.TableNextColumn();
                ImGui.Text("Top 1 Team Contrib.");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top1Contribs.Kills, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib, maxContrib, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top1Contribs.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib, maxContrib, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top1Contribs.Assists, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib, maxContrib, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top1Contribs.DamageToPCs, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib, maxContrib, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top1Contribs.DamageToOther, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib, maxContrib, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top1Contribs.DamageTaken, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib, maxContrib, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top1Contribs.HPRestored, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib, maxContrib, Plugin.Configuration.ColorScaleStats, "P1", offset);

                //top 1 factor
                ImGui.TableNextColumn();
                ImGui.Text("Factor");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top1ContribFactor.Kills:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top1ContribFactor.Deaths:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top1ContribFactor.Assists:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top1ContribFactor.DamageToPCs:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top1ContribFactor.DamageToOther:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top1ContribFactor.DamageTaken:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top1ContribFactor.HPRestored:#.0}x", offset);

                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextRow();

                //top 4 contribution
                ImGui.TableNextColumn();
                ImGui.Text("Top 4 Team Contrib.");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top4Contribs.Kills, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib * 4, maxContrib * 4, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top4Contribs.Deaths, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib * 4, maxContrib * 4, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top4Contribs.Assists, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib * 4, maxContrib * 4, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top4Contribs.DamageToPCs, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib * 4, maxContrib * 4, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top4Contribs.DamageToOther, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib * 4, maxContrib * 4, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top4Contribs.DamageTaken, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib * 4, maxContrib * 4, Plugin.Configuration.ColorScaleStats, "P1", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell((float)Top4Contribs.HPRestored, Plugin.Configuration.Colors.StatHigh, Plugin.Configuration.Colors.StatLow, minContrib * 4, maxContrib * 4, Plugin.Configuration.ColorScaleStats, "P1", offset);

                //top 4 factor
                ImGui.TableNextColumn();
                ImGui.Text("Factor");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top4ContribFactor.Kills:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top4ContribFactor.Deaths:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top4ContribFactor.Assists:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top4ContribFactor.DamageToPCs:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top4ContribFactor.DamageToOther:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top4ContribFactor.DamageTaken:#.0}x", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{Top4ContribFactor.HPRestored:#.0}x", offset);

                ImGui.TableNextRow();
                ImGui.TableNextRow();
                ImGui.TableNextRow();

                //gini coefficients
                ImGui.TableNextColumn();
                ImGui.Text("Gini coeff.");
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{GiniCoefficients.Kills:0.00}", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{GiniCoefficients.Deaths:0.00}", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{GiniCoefficients.Assists:0.00}", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{GiniCoefficients.DamageToPCs:0.00}", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{GiniCoefficients.DamageToOther:0.00}", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{GiniCoefficients.DamageTaken:0.00}", offset);
                ImGui.TableNextColumn();
                ImGuiHelper.DrawNumericCell($"{GiniCoefficients.HPRestored:0.00}", offset);
            }
        }
    }
}
