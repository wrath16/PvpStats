using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using PvpStats.Managers;
using PvpStats.Managers.Game;
using PvpStats.Managers.Stats;
using PvpStats.Services;
using PvpStats.Services.DataCache;
using PvpStats.Settings;
using System;
using System.Threading.Tasks;

namespace PvpStats;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "PvpStats";

    internal const string DatabaseName = "data.db";

    private const string CCStatsCommandName = "/ccstats";
    private const string FLStatsCommandName = "/flstats";
    private const string RWStatsCommandName = "/rwstats";
    private const string DebugCommandName = "/pvpstatsdebug";
    private const string ConfigCommandName = "/pvpstatsconfig";

    //Dalamud services
    internal DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    internal IDataManager DataManager { get; init; }
    internal IClientState ClientState { get; init; }
    internal IGameNetwork GameNetwork { get; init; }
    internal ICondition Condition { get; init; }
    internal IDutyState DutyState { get; init; }
    //internal IPartyList PartyList { get; init; }
    internal IChatGui ChatGui { get; init; }
    internal IGameGui GameGui { get; init; }
    internal IFramework Framework { get; init; }
    internal IPluginLog Log { get; init; }
    internal IAddonEventManager AddonEventManager { get; init; }
    internal IAddonLifecycle AddonLifecycle { get; init; }
    internal IObjectTable ObjectTable { get; init; }
    internal ITextureProvider TextureProvider { get; init; }
    internal IGameInteropProvider InteropProvider { get; init; }
    internal ISigScanner SigScanner { get; init; }

    internal CrystallineConflictMatchManager? CCMatchManager { get; init; }
    internal FrontlineMatchManager? FLMatchManager { get; init; }
    internal RivalWingsMatchManager? RWMatchManager { get; init; }
    internal WindowManager WindowManager { get; init; }
    internal MigrationManager MigrationManager { get; init; }
    internal CrystallineConflictStatsManager CCStatsEngine { get; init; }
    internal FrontlineStatsManager FLStatsEngine { get; init; }
    internal RivalWingsStatsManager RWStatsEngine { get; init; }

    internal DataQueueService DataQueue { get; init; }
    internal LocalizationService Localization { get; init; }
    internal StorageService Storage { get; init; }
    internal CCMatchCacheService CCCache { get; init; }
    internal FLMatchCacheService FLCache { get; init; }
    internal RWMatchCacheService RWCache { get; init; }
    internal GameStateService GameState { get; init; }
    internal AtkNodeService AtkNodeService { get; init; }
    internal PlayerLinkService PlayerLinksService { get; init; }

    public Configuration Configuration { get; init; }
    internal MemoryService Functions { get; init; }

    internal bool DebugMode { get; set; }

    public Plugin(
        DalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IDataManager dataManager,
        IClientState clientState,
        IGameNetwork gameNetwork,
        ICondition condition,
        IDutyState dutyState,
        IPartyList partyList,
        IChatGui chatGui,
        IGameGui gameGui,
        IFramework framework,
        IPluginLog log,
        IAddonEventManager addonEventManager,
        IAddonLifecycle addonLifecycle,
        IObjectTable objectTable,
        ITextureProvider textureProvider,
        IGameInteropProvider interopProvider,
        ISigScanner sigScanner) {
        try {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            DataManager = dataManager;
            ClientState = clientState;
            GameNetwork = gameNetwork;
            Condition = condition;
            DutyState = dutyState;
            //PartyList = partyList;
            ChatGui = chatGui;
            GameGui = gameGui;
            Framework = framework;
            Log = log;
            AddonEventManager = addonEventManager;
            AddonLifecycle = addonLifecycle;
            ObjectTable = objectTable;
            TextureProvider = textureProvider;
            InteropProvider = interopProvider;
            SigScanner = sigScanner;

            try {
                Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            } catch(Exception e) {
                Configuration = new();
                Log.Error(e, "Error in configuration setup.");
            }
            Configuration.Initialize(this);

            DataQueue = new(this);
            Storage = new(this, $"{PluginInterface.GetPluginConfigDirectory()}\\{DatabaseName}");
            CCCache = new(this);
            FLCache = new(this);
            RWCache = new(this);
            Functions = new(this);
            GameState = new(this);
            AtkNodeService = new(this);
            PlayerLinksService = new(this);
            Localization = new(this);
            CCStatsEngine = new(this);
            FLStatsEngine = new(this);
            RWStatsEngine = new(this);
            WindowManager = new(this);
            MigrationManager = new(this);
            try {
                CCMatchManager = new(this);
            } catch(SignatureException e) {
                Log.Error(e, $"failed to initialize cc match manager");
            }
            try {
                FLMatchManager = new(this);
            } catch(SignatureException e) {
                Log.Error(e, $"failed to initialize fl match manager");
            }
            try {
                RWMatchManager = new(this);
            } catch(SignatureException e) {
                Log.Error(e, $"failed to initialize rw match manager");
            }

            CommandManager.AddHandler(CCStatsCommandName, new CommandInfo(OnCCCommand) {
                HelpMessage = "Opens Crystalline Conflict tracker."
            });
            CommandManager.AddHandler(FLStatsCommandName, new CommandInfo(OnFLCommand) {
                HelpMessage = "Opens Frontline tracker."
            });
            CommandManager.AddHandler(RWStatsCommandName, new CommandInfo(OnRWCommand) {
                HelpMessage = "Opens Rival Wings tracker."
            });
            CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnConfigCommand) {
                HelpMessage = "Opens config window."
            });

#if DEBUG
            CommandManager.AddHandler(DebugCommandName, new CommandInfo(OnDebugCommand) {
                HelpMessage = "Opens debug window."
            });
            DebugMode = true;
#endif
            DataQueue.QueueDataOperation(Initialize);
            //PluginInterface.UiBuilder.OpenConfigUi += WindowManager.OpenConfigWindow;
            //Log.Debug("PvP Stats has started.");
        } catch(Exception e) {
            //remove handlers and release database if we fail to start
            Log!.Error($"Failed to initialize plugin constructor: {e.Message}");
            Dispose();
            //re-throw to prevent constructor from initializing
            throw;
        }

    }

    public void Dispose() {
#if DEBUG
        Log.Debug("disposing plugin");
#endif

        CommandManager.RemoveHandler(CCStatsCommandName);
        CommandManager.RemoveHandler(FLStatsCommandName);
        CommandManager.RemoveHandler(ConfigCommandName);

        Functions?.Dispose();
        CCMatchManager?.Dispose();
        FLMatchManager?.Dispose();
        RWMatchManager?.Dispose();
        WindowManager?.Dispose();
        Storage?.Dispose();
        DataQueue?.Dispose();
        GameState?.Dispose();

        Configuration?.Save();
    }

    private void OnCCCommand(string command, string args) {
        WindowManager.OpenCCWindow();
    }

    private void OnFLCommand(string command, string args) {
        WindowManager.OpenFLWindow();
    }

    private void OnRWCommand(string command, string args) {
        WindowManager.OpenRWWindow();
    }

    private void OnConfigCommand(string command, string args) {
        WindowManager.OpenConfigWindow();
    }

#if DEBUG
    private void OnDebugCommand(string command, string args) {
        WindowManager.OpenDebugWindow();
    }
#endif

    private async Task Initialize() {
        if(Configuration.EnableDBCachingCC ?? true) {
            CCCache.EnableCaching();
        }
        if(Configuration.EnableDBCachingFL ?? true) {
            FLCache.EnableCaching();
        }
        if(Configuration.EnableDBCachingRW ?? true) {
            RWCache.EnableCaching();
        }
        await MigrationManager.BulkUpdateCCMatchTypes();
        await MigrationManager.BulkCCUpdateValidatePlayerCount();
        await WindowManager.RefreshAll();
        Log.Information("PvP Tracker initialized.");
    }
}
