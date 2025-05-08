using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using PvpStats.Utility;
using System.Numerics;

namespace PvpStats.Windows.Detail;
internal abstract class MatchDetail<T> : Window where T : PvpMatch {

    protected readonly Plugin Plugin;
    protected readonly MatchCacheService<T> Cache;
    protected readonly T Match;

    protected DataQueue RefreshQueue { get; private set; } = new();

    protected string CSV = "";
    protected bool ShowPercentages;
    protected bool ShowTeamRows = true;
    protected string CurrentTab = "";

    private bool _popupOpen = false;

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

    public override void Draw() {
        SizeCondition = ImGuiCond.Once;
    }

    protected void DrawFunctions() {
        //need to increment this for each function
        int functionCount = 3;
        //get width of strip
        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
            var buttonWidth = ImGui.GetStyle().FramePadding.X * 2 + ImGui.CalcTextSize(FontAwesomeIcon.Search.ToIconString()).X;
            var totalWidth = buttonWidth * functionCount + ImGui.GetStyle().ItemSpacing.X * (functionCount - 1);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - totalWidth) / 2f);
        }
        ImGuiHelper.CSVButton(CSV);

        ImGui.SameLine();
        using(_ = ImRaii.PushFont(UiBuilder.IconFont)) {
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
        using(var font = ImRaii.PushFont(UiBuilder.IconFont)) {
            var color = !Match.Tags.IsNullOrEmpty() ? Plugin.Configuration.Colors.Favorite : ImGuiColors.DalamudWhite;
            using(_ = ImRaii.PushColor(ImGuiCol.Text, color)) {
                if(ImGui.Button($"{FontAwesomeIcon.Tags.ToIconString()}##--Tags")) {
                    ImGui.OpenPopup($"{Match.Id}--TagsPopup");
                }
            }
        }
        ImGuiHelper.WrappedTooltip("Set tags");
        Plugin.WindowManager.SetTagsPopup(Match, Cache, ref _popupOpen);
    }

    protected void SetWindowSize(Vector2 size) {
        SizeCondition = ImGuiCond.Always;
        Size = size;
    }
}
