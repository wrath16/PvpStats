using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using System.Numerics;

namespace PvpStats.Windows.Detail;
internal abstract class MatchDetail<T> : Window where T : PvpMatch {

    protected readonly Plugin Plugin;
    protected readonly MatchCacheService<T> Cache;
    protected readonly T Match;

    protected string CSV = "";
    protected bool ShowPercentages;

    public MatchDetail(Plugin plugin, MatchCacheService<T> cache, T match) : base($"Match Details: {match.Id}") {
        Plugin = plugin;
        Cache = cache;
        Match = match;

        PositionCondition = ImGuiCond.Appearing;
        CollapsedCondition = ImGuiCond.Appearing;
        Position = new Vector2(0, 0);
        Collapsed = false;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(650, 400),
            MaximumSize = new Vector2(5000, 5000)
        };
        Flags |= ImGuiWindowFlags.NoSavedSettings;
        //if(!plugin.Configuration.ResizeableMatchWindow) {
        //    Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        //}
    }

    protected abstract string BuildCSV();

    public override void OnClose() {
        Plugin.WindowManager.RemoveWindow(this);
    }

    protected void DrawFunctions() {
        //need to increment this for each function
        int functionCount = 2;
        //get width of strip
        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
            string text = "";
            for(int i = 0; i < functionCount; i++) {
                text += $"{FontAwesomeIcon.Star.ToIconString()}";
            }
            //ImGuiHelpers.CenterCursorForText(text);
            ImGuiHelper.CenterAlignCursor(text);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ((ImGui.GetStyle().FramePadding.X - 3f) * 2.5f + 9f * (functionCount - 1)));
        }

        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
            var text = $"{FontAwesomeIcon.Star.ToIconString()}{FontAwesomeIcon.Copy.ToIconString()}";
            var color = Match.IsBookmarked ? Plugin.Configuration.Colors.Favorite : ImGuiColors.DalamudWhite;
            using(_ = ImRaii.PushColor(ImGuiCol.Text, color)) {
                if(ImGui.Button($"{FontAwesomeIcon.Star.ToIconString()}##--FavoriteMatch")) {
                    Match.IsBookmarked = !Match.IsBookmarked;
                    Plugin.DataQueue.QueueDataOperation(async () => {
                        await Cache.UpdateMatch(Match);
                    });
                }
            }
        }
        ImGuiHelper.WrappedTooltip("Favorite match");
        ImGui.SameLine();
        ImGuiHelper.CSVButton(CSV);
    }
}
