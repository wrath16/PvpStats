using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using PvpStats.Managers;
using PvpStats.Managers.Game;
using PvpStats.Managers.Stats;
using PvpStats.Services;
using PvpStats.Settings;
using System;

namespace PvpStats;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "PvpStats";

    internal const string DatabaseName = "data.db";

    private const string CCStatsCommandName = "/ccstats";
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
    internal IPartyList PartyList { get; init; }
    internal IChatGui ChatGui { get; init; }
    internal IGameGui GameGui { get; init; }
    internal IFramework Framework { get; init; }
    internal IPluginLog Log { get; init; }
    internal IAddonLifecycle AddonLifecycle { get; init; }
    internal IObjectTable ObjectTable { get; init; }
    internal ITextureProvider TextureProvider { get; init; }
    internal IGameInteropProvider InteropProvider { get; init; }
    internal ISigScanner SigScanner { get; init; }

    internal MatchManager? MatchManager { get; init; }
    internal WindowManager WindowManager { get; init; }
    internal MigrationManager MigrationManager { get; init; }
    internal CrystallineConflictStatsManager CCStatsEngine { get; init; }

    internal DataQueueService DataQueue { get; init; }
    internal LocalizationService Localization { get; init; }
    internal StorageService Storage { get; init; }
    internal DataCacheService DataCache { get; init; }
    internal GameStateService GameState { get; init; }
    internal AtkNodeService AtkNodeService { get; init; }
    internal PlayerLinkService PlayerLinksService { get; init; }

    public Configuration Configuration { get; init; }
    internal MemoryService Functions { get; init; }

    internal bool DebugMode { get; set; }

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] IDataManager dataManager,
        [RequiredVersion("1.0")] IClientState clientState,
        [RequiredVersion("1.0")] IGameNetwork gameNetwork,
        [RequiredVersion("1.0")] ICondition condition,
        [RequiredVersion("1.0")] IDutyState dutyState,
        [RequiredVersion("1.0")] IPartyList partyList,
        [RequiredVersion("1.0")] IChatGui chatGui,
        [RequiredVersion("1.0")] IGameGui gameGui,
        [RequiredVersion("1.0")] IFramework framework,
        [RequiredVersion("1.0")] IPluginLog log,
        [RequiredVersion("1.0")] IAddonLifecycle addonLifecycle,
        [RequiredVersion("1.0")] IObjectTable objectTable,
        [RequiredVersion("1.0")] ITextureProvider textureProvider,
        [RequiredVersion("1.0")] IGameInteropProvider interopProvider,
        [RequiredVersion("1.0")] ISigScanner sigScanner) {
        try {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            DataManager = dataManager;
            ClientState = clientState;
            GameNetwork = gameNetwork;
            Condition = condition;
            DutyState = dutyState;
            PartyList = partyList;
            ChatGui = chatGui;
            GameGui = gameGui;
            Framework = framework;
            Log = log;
            AddonLifecycle = addonLifecycle;
            ObjectTable = objectTable;
            TextureProvider = textureProvider;
            InteropProvider = interopProvider;
            SigScanner = sigScanner;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(this);

            DataQueue = new(this);
            Storage = new(this, $"{PluginInterface.GetPluginConfigDirectory()}\\{DatabaseName}");
            DataCache = new(this);
            Functions = new(this);
            GameState = new(this);
            AtkNodeService = new(this);
            PlayerLinksService = new(this);
            Localization = new(this);
            CCStatsEngine = new(this);
            WindowManager = new(this);
            MigrationManager = new(this);
            try {
                MatchManager = new(this);
            } catch(SignatureException e) {
                Log.Error($"failed to initialize match manager: {e.Message}");
            }

            CommandManager.AddHandler(CCStatsCommandName, new CommandInfo(OnCommand) {
                HelpMessage = "Opens Crystalline Conflict tracker."
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
            //PluginInterface.UiBuilder.OpenConfigUi += WindowManager.OpenConfigWindow;

            Log.Debug("PvP Stats has started.");
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
        CommandManager.RemoveHandler(ConfigCommandName);

        MatchManager?.Dispose();
        WindowManager?.Dispose();
        Storage?.Dispose();
        DataQueue?.Dispose();
        GameState?.Dispose();
        Configuration?.Save();
    }

    private void OnCommand(string command, string args) {
        WindowManager.OpenMainWindow();
    }

    private void OnConfigCommand(string command, string args) {
        WindowManager.OpenConfigWindow();
    }

#if DEBUG
    private void OnDebugCommand(string command, string args) {
        WindowManager.OpenDebugWindow();
    }
#endif

    private void Initialize() {

    }
}
