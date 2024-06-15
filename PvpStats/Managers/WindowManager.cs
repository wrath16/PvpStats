using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Windowing;
using LiteDB;
using PvpStats.Services.DataCache;
using PvpStats.Types.Match;
using PvpStats.Windows;
using PvpStats.Windows.Detail;
using PvpStats.Windows.Tracker;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PvpStats.Managers;
internal class WindowManager : IDisposable {

    private WindowSystem WindowSystem;
    private Plugin _plugin;
    internal CCTrackerWindow CCTrackerWindow { get; private set; }
    internal FLTrackerWindow FLTrackerWindow { get; private set; }
    internal RWTrackerWindow RWTrackerWindow { get; private set; }
    internal ConfigWindow ConfigWindow { get; private set; }
    internal SplashWindow SplashWindow { get; private set; }
#if DEBUG
    internal DebugWindow? DebugWindow { get; private set; }
#endif

    //fallback icon for missing icons/textures
    //internal IDalamudTextureWrap Icon0 { get; private set; }

    //internal readonly Dictionary<Job, IDalamudTextureWrap?> JobIcons = new();
    //internal IDalamudTextureWrap CCBannerImage { get; private set; }
    //internal IDalamudTextureWrap FLBannerImage { get; private set; }
    //internal IDalamudTextureWrap RWBannerImage { get; private set; }
    //internal IDalamudTextureWrap RWSuppliesTexture { get; private set; }
    //internal IDalamudTextureWrap RWTeamIconTexture { get; private set; }
    //internal IDalamudTextureWrap GoblinMercIcon { get; private set; }
    //internal IDalamudTextureWrap TrainIcon { get; private set; }
    //internal readonly Dictionary<RivalWingsTeamName, IDalamudTextureWrap> CoreIcons = [];
    //internal readonly Dictionary<RivalWingsTeamName, IDalamudTextureWrap> Tower1Icons = [];
    //internal readonly Dictionary<RivalWingsTeamName, IDalamudTextureWrap> Tower2Icons = [];
    //internal readonly Dictionary<RivalWingsTeamName, IDalamudTextureWrap> ChaserIcons = [];
    //internal readonly Dictionary<RivalWingsTeamName, IDalamudTextureWrap> OppressorIcons = [];
    //internal readonly Dictionary<RivalWingsTeamName, IDalamudTextureWrap> JusticeIcons = [];
    //internal readonly Dictionary<int, IDalamudTextureWrap> SoaringIcons = [];

    //internal readonly Dictionary<FrontlineTeamName, IDalamudTextureWrap> FrontlineTeamIcons = [];
    //internal readonly Dictionary<int, IDalamudTextureWrap> BattleHighIcons = [];

    internal IFontHandle LargeFont { get; private set; }

    internal WindowManager(Plugin plugin) {
        _plugin = plugin;
        WindowSystem = new("PvP Stats");

        //foreach(var icon in PlayerJobHelper.JobIcons) {
        //    JobIcons.Add(icon.Key, _plugin.TextureProvider.GetFromGameIcon(icon.Value).GetWrapOrEmpty());
        //}
        ////var rwTextureFile = _plugin.DataManager.GetFile("ui/uld/PVPSimulationHeader2_hr1.tex") as TexFile;
        ////RWSuppliesTexture = _plugin.TextureProvider.GetTexture(rwTextureFile);
        //RWSuppliesTexture = _plugin.TextureProvider.GetFromGame("ui/uld/PVPSimulationHeader2_hr1.tex").GetWrapOrEmpty();
        //RWTeamIconTexture = _plugin.TextureProvider.GetFromGame("ui/uld/PVPSimulationResult_hr1.tex").GetWrapOrEmpty();
        //GoblinMercIcon = _plugin.TextureProvider.GetFromGameIcon(60976).GetWrapOrEmpty();
        //TrainIcon = _plugin.TextureProvider.GetFromGameIcon(60980).GetWrapOrEmpty();
        //CoreIcons.Add(RivalWingsTeamName.Falcons, _plugin.TextureProvider.GetFromGameIcon(60947).GetWrapOrEmpty());
        //CoreIcons.Add(RivalWingsTeamName.Ravens, _plugin.TextureProvider.GetFromGameIcon(60948).GetWrapOrEmpty());
        //Tower1Icons.Add(RivalWingsTeamName.Falcons, _plugin.TextureProvider.GetFromGameIcon(60945).GetWrapOrEmpty());
        //Tower1Icons.Add(RivalWingsTeamName.Ravens, _plugin.TextureProvider.GetFromGameIcon(60946).GetWrapOrEmpty());
        //Tower2Icons.Add(RivalWingsTeamName.Falcons, _plugin.TextureProvider.GetFromGameIcon(60956).GetWrapOrEmpty());
        //Tower2Icons.Add(RivalWingsTeamName.Ravens, _plugin.TextureProvider.GetFromGameIcon(60957).GetWrapOrEmpty());
        //ChaserIcons.Add(RivalWingsTeamName.Unknown, _plugin.TextureProvider.GetFromGameIcon(60666).GetWrapOrEmpty());
        //ChaserIcons.Add(RivalWingsTeamName.Falcons, _plugin.TextureProvider.GetFromGameIcon(60939).GetWrapOrEmpty());
        //ChaserIcons.Add(RivalWingsTeamName.Ravens, _plugin.TextureProvider.GetFromGameIcon(60942).GetWrapOrEmpty());
        //OppressorIcons.Add(RivalWingsTeamName.Unknown, _plugin.TextureProvider.GetFromGameIcon(60667).GetWrapOrEmpty());
        //OppressorIcons.Add(RivalWingsTeamName.Falcons, _plugin.TextureProvider.GetFromGameIcon(60940).GetWrapOrEmpty());
        //OppressorIcons.Add(RivalWingsTeamName.Ravens, _plugin.TextureProvider.GetFromGameIcon(60943).GetWrapOrEmpty());
        //JusticeIcons.Add(RivalWingsTeamName.Unknown, _plugin.TextureProvider.GetFromGameIcon(60668).GetWrapOrEmpty());
        //JusticeIcons.Add(RivalWingsTeamName.Falcons, _plugin.TextureProvider.GetFromGameIcon(60941).GetWrapOrEmpty());
        //JusticeIcons.Add(RivalWingsTeamName.Ravens, _plugin.TextureProvider.GetFromGameIcon(60944).GetWrapOrEmpty());
        //for(int i = 1; i <= 19; i++) {
        //    SoaringIcons.Add(i, _plugin.TextureProvider.GetFromGameIcon(19181 + (uint)i - 1).GetWrapOrEmpty());
        //}
        //SoaringIcons.Add(20, _plugin.TextureProvider.GetFromGameIcon(14845).GetWrapOrEmpty());

        //BattleHighIcons.Add(1, _plugin.TextureProvider.GetFromGameIcon(61483).GetWrapOrEmpty());
        //BattleHighIcons.Add(2, _plugin.TextureProvider.GetFromGameIcon(61484).GetWrapOrEmpty());
        //BattleHighIcons.Add(3, _plugin.TextureProvider.GetFromGameIcon(61485).GetWrapOrEmpty());
        //BattleHighIcons.Add(4, _plugin.TextureProvider.GetFromGameIcon(61486).GetWrapOrEmpty());
        //BattleHighIcons.Add(5, _plugin.TextureProvider.GetFromGameIcon(61487).GetWrapOrEmpty());

        //FrontlineTeamIcons.Add(FrontlineTeamName.Maelstrom, _plugin.TextureProvider.GetFromGameIcon(61526).GetWrapOrEmpty());
        //FrontlineTeamIcons.Add(FrontlineTeamName.Adders, _plugin.TextureProvider.GetFromGameIcon(61527).GetWrapOrEmpty());
        //FrontlineTeamIcons.Add(FrontlineTeamName.Flames, _plugin.TextureProvider.GetFromGameIcon(61528).GetWrapOrEmpty());

        //CCBannerImage = _plugin.TextureProvider.GetFromFile(Path.Combine(_plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "cc_logo_full.png")).GetWrapOrEmpty();
        //RWBannerImage = _plugin.TextureProvider.GetFromFile(Path.Combine(_plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "rw_logo.png")).GetWrapOrEmpty();
        //FLBannerImage = _plugin.TextureProvider.GetFromFile(Path.Combine(_plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "fl_logo.png")).GetWrapOrEmpty();

        CCTrackerWindow = new(plugin);
        FLTrackerWindow = new(plugin);
        RWTrackerWindow = new(plugin);
        ConfigWindow = new(plugin);
        SplashWindow = new(plugin);
        WindowSystem.AddWindow(CCTrackerWindow);
        WindowSystem.AddWindow(FLTrackerWindow);
        WindowSystem.AddWindow(RWTrackerWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(SplashWindow);

#if DEBUG
        DebugWindow = new(plugin);
        WindowSystem.AddWindow(DebugWindow);
#endif

        LargeFont = _plugin.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 24f));
        _plugin.PluginInterface.UiBuilder.Draw += DrawUI;
        _plugin.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigWindow;
        _plugin.PluginInterface.UiBuilder.OpenMainUi += OpenSplashWindow;
        _plugin.ClientState.Login += OnLogin;
    }
    private void DrawUI() {
        WindowSystem.Draw();
    }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();
        _plugin.PluginInterface.UiBuilder.Draw -= DrawUI;
        _plugin.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigWindow;
        //CCBannerImage.Dispose();
        //RWBannerImage.Dispose();
        //FLBannerImage.Dispose();
        //GoblinMercIcon?.Dispose();
        //RWSuppliesTexture?.Dispose();

        _plugin.ClientState.Login -= OnLogin;
    }

    private void OnLogin() {
        Task.Delay(3000).ContinueWith((t) => {
            _plugin.DataQueue.QueueDataOperation(async () => {
                await _plugin.PlayerLinksService.BuildAutoLinksCache();
                await RefreshAll();
            });
        });
    }

    internal void AddWindow(Window window) {
        WindowSystem.AddWindow(window);
    }

    internal void RemoveWindow(Window window) {
        WindowSystem.RemoveWindow(window);
    }

    internal void OpenCCWindow() {
        CCTrackerWindow.IsOpen = true;
    }

    internal void OpenFLWindow() {
        FLTrackerWindow.IsOpen = true;
    }

    internal void OpenRWWindow() {
        RWTrackerWindow.IsOpen = true;
    }

    internal void OpenConfigWindow() {
        ConfigWindow.IsOpen = true;
    }

    internal void OpenSplashWindow() {
        SplashWindow.IsOpen = true;
    }

#if DEBUG
    internal void OpenDebugWindow() {
        if(DebugWindow is not null) {
            DebugWindow.IsOpen = true;
        }
    }
#endif

    //internal void OpenMatchDetailsWindow(CrystallineConflictMatch match) {
    //    var windowName = $"Match Details: {match.Id}";
    //    var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
    //    if(window is not null) {
    //        window.BringToFront();
    //        window.IsOpen = true;
    //    } else {
    //        _plugin.Log.Debug($"Opening item detail for...{match.DutyStartTime}");
    //        var itemDetail = new CrystallineConflictMatchDetail(_plugin, match);
    //        itemDetail.IsOpen = true;
    //        _plugin.WindowManager.AddWindow(itemDetail);
    //    }
    //}

    internal void OpenMatchDetailsWindow(PvpMatch match) {
        var windowName = $"Match Details: {match.Id}";
        var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
        if(window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        } else {
            _plugin.Log.Debug($"Opening item detail for...{match.DutyStartTime}");
            if(match.GetType() == typeof(CrystallineConflictMatch)) {
                var itemDetail = new CrystallineConflictMatchDetail(_plugin, (match as CrystallineConflictMatch)!);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            } else if(match.GetType() == typeof(FrontlineMatch)) {
                var itemDetail = new FrontlineMatchDetail(_plugin, (match as FrontlineMatch)!);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            } else if(match.GetType() == typeof(RivalWingsMatch)) {
                var itemDetail = new RivalWingsMatchDetail(_plugin, (match as RivalWingsMatch)!);
                itemDetail.IsOpen = true;
                _plugin.WindowManager.AddWindow(itemDetail);
            }

            //var itemDetail = new CrystallineConflictMatchDetail(_plugin, match);
            //itemDetail.IsOpen = true;
            //_plugin.WindowManager.AddWindow(itemDetail);
        }
    }

    internal void CloseAllMatchWindows() {
        var windows = WindowSystem.Windows.Where(w => w.WindowName.StartsWith("Match Details: ", StringComparison.OrdinalIgnoreCase));
        foreach(var window in windows) {
            window.IsOpen = false;
        }
    }

    internal void OpenFullEditWindow<T>(T match) where T : PvpMatch {
        var windowName = $"Full Edit: {match.GetHashCode()}";
        var window = WindowSystem.Windows.Where(w => w.WindowName == windowName).FirstOrDefault();
        if(window is not null) {
            window.BringToFront();
            window.IsOpen = true;
        } else {
            _plugin.Log.Debug($"Opening full edit details for...{match.DutyStartTime}");
            var matchType = typeof(T);
            MatchCacheService<T>? matchCache = null;
            switch(matchType) {
                case Type _ when matchType == typeof(CrystallineConflictMatch):
                    matchCache = _plugin.CCCache as MatchCacheService<T>;
                    break;
                case Type _ when matchType == typeof(FrontlineMatch):
                    matchCache = _plugin.FLCache as MatchCacheService<T>;
                    break;
                case Type _ when matchType == typeof(RivalWingsMatch):
                    matchCache = _plugin.RWCache as MatchCacheService<T>;
                    break;
                default:
                    break;
            }
            var itemDetail = new FullEditDetail<T>(_plugin, matchCache, match);
            itemDetail.IsOpen = true;
            _plugin.WindowManager.AddWindow(itemDetail);
        }
    }

    public async Task RefreshAll() {
        _plugin.Log.Debug("refreshing windows...");
        Task.WaitAll(ConfigWindow.Refresh(), RefreshCCWindow(), RefreshFLWindow(), RefreshRWWindow());
        await Task.CompletedTask;
    }

    public async Task RefreshConfigWindow() {
        await ConfigWindow.Refresh();
    }

    public async Task RefreshCCWindow() {
        await CCTrackerWindow.Refresh();
    }

    public async Task RefreshFLWindow() {
        await FLTrackerWindow.Refresh();
    }

    public async Task RefreshRWWindow() {
        await RWTrackerWindow.Refresh();
    }

    public nint GetTextureHandle(uint iconId) {
        return _plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty().ImGuiHandle;
    }

    public nint GetTextureHandle(string path) {
        return _plugin.TextureProvider.GetFromGame(path).GetWrapOrEmpty().ImGuiHandle;
    }
}
