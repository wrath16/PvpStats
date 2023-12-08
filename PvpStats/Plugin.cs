using Dalamud.Configuration;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using PvpStats.Helpers;
using PvpStats.Managers;
using PvpStats.Settings;
using PvpStats.Types.Player;
using PvpStats.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "PvpStats";

    private const string DatabaseName = "data.db";

    private const string CommandName = "/pvpstats";
    private const string DebugCommandName = "/pvpstatsdebug";
    private const string ConfigCommandName = "/pvpstatsconfig";

    //Dalamud services
    internal DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    internal IDataManager DataManager { get; init; }
    internal IClientState ClientState { get; init; }
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

    internal MatchManager MatchManager { get; init; }
    internal LocalizationManager LocalizationManager { get; init; }
    internal StorageManager StorageManager { get; init; }

    public Configuration Configuration { get; init; }
    internal GameFunctions Functions { get; init; }

    //UI
    internal WindowSystem WindowSystem = new("Pvp Stats");
    private MainWindow MainWindow;
    private DebugWindow DebugWindow;

    private bool _matchInProgress;
    private DateTime _lastHeaderUpdateTime;

    internal bool DataReadWrite { get; set; }
    //coordinates all data sequence-sensitive operations
    internal SemaphoreSlim DataLock { get; init; } = new SemaphoreSlim(1, 1);

    internal readonly Dictionary<Job, IDalamudTextureWrap> JobIcons = new();

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] IDataManager dataManager,
        [RequiredVersion("1.0")] IClientState clientState,
        [RequiredVersion("1.0")] ICondition condition,
        [RequiredVersion("1.0")] IDutyState dutyState,
        [RequiredVersion("1.0")] IPartyList partyList,
        [RequiredVersion("1.0")] IChatGui chatGui,
        [RequiredVersion("1.0")] IGameGui gameGui,
        [RequiredVersion("1.0")] IFramework framework,
        [RequiredVersion("1.0")] IPluginLog log,
        [RequiredVersion("1.0")] IAddonLifecycle addonLifecycle,
        [RequiredVersion("1.0")] IObjectTable objectTable,
        [RequiredVersion("1.0")] ITextureProvider textureProvider) {
        try {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            DataManager = dataManager;
            ClientState = clientState;
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

            AtkNodeHelper.Log = Log;

            foreach (var icon in PlayerJobHelper.JobIcons) {
                JobIcons.Add(icon.Key, TextureProvider.GetIcon(icon.Value));
            }

            //JobIcons = new Dictionary<Job, IDalamudTextureWrap>() {
            //    { Job.PLD, TextureProvider.GetIcon(62119)! },
            //    { Job.WAR, TextureProvider.GetIcon(62121)! },
            //    { Job.DRK, TextureProvider.GetIcon(62132)! },
            //    { Job.GNB, TextureProvider.GetIcon(62137)! },
            //    { Job.MNK, TextureProvider.GetIcon(62120)! },
            //    { Job.DRG, TextureProvider.GetIcon(62122)! },
            //    { Job.NIN, TextureProvider.GetIcon(62130)! },
            //    { Job.SAM, TextureProvider.GetIcon(62134)! },
            //    { Job.RPR, TextureProvider.GetIcon(62139)! },
            //    { Job.WHM, TextureProvider.GetIcon(62124)! },
            //    { Job.SCH, TextureProvider.GetIcon(62128)! },
            //    { Job.AST, TextureProvider.GetIcon(62133)! },
            //    { Job.SGE, TextureProvider.GetIcon(62140)! },
            //    { Job.BRD, TextureProvider.GetIcon(62123)! },
            //    { Job.MCH, TextureProvider.GetIcon(62131)! },
            //    { Job.DNC, TextureProvider.GetIcon(62138)! },
            //    { Job.BLM, TextureProvider.GetIcon(62125)! },
            //    { Job.SMN, TextureProvider.GetIcon(62127)! },
            //    { Job.RDM, TextureProvider.GetIcon(62135)! },
            //};

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(this);

            MatchManager = new MatchManager(this);
            LocalizationManager = new LocalizationManager(this);
            StorageManager = new StorageManager(this, $"{PluginInterface.GetPluginConfigDirectory()}\\{DatabaseName}");

            MainWindow = new MainWindow(this);
            WindowSystem.AddWindow(MainWindow);
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
                HelpMessage = "Opens stats window."
            });

#if DEBUG
            DebugWindow = new DebugWindow(this);
            WindowSystem.AddWindow(DebugWindow);
            CommandManager.AddHandler(DebugCommandName, new CommandInfo(OnDebugCommand) {
                HelpMessage = "Opens debug window."
            });
            DataReadWrite = true;
#endif

            CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnConfigCommand) {
                HelpMessage = "Open settings window."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Framework.Update += OnFrameworkUpdate;
            ChatGui.ChatMessage += OnChatMessage;
            ClientState.TerritoryChanged += OnTerritoryChanged;

            Log.Debug("starting up");
            //AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "PvPMKSIntroduction", OnPvPIntro);
            //AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "PvPMKSIntroduction", OnPvPIntroUpdate);
            //AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "PvPMKSIntroduction", OnPvPIntroPreSetup);
            //AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MKSRecord", OnPvPResults);
            //AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "MKSRecord", OnPvPResults);
            //AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "PvpProfileColosseum", OnPvPIntro);
        }
        catch (Exception e) {
            //remove handlers and release database if we fail to start
            Dispose();
            //it really shouldn't ever be null
            Log!.Error($"Failed to initialize plugin constructor: {e.Message}");
            //re-throw to prevent constructor from initializing
            throw;
        }

    }

    //Custom config loader. Unused
    public IPluginConfiguration? GetPluginConfig() {
        //string pluginName = PluginInterface.InternalName;
        FileInfo configFile = PluginInterface.ConfigFile;
        if (!configFile.Exists) {
            return null;
        }
        return JsonConvert.DeserializeObject<IPluginConfiguration>(File.ReadAllText(configFile.FullName), new JsonSerializerSettings {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Objects
        });
    }

    public void Dispose() {
#if DEBUG
        Log.Debug("disposing plugin");
#endif

        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(ConfigCommandName);

        Framework.Update -= OnFrameworkUpdate;
        ChatGui.ChatMessage -= OnChatMessage;

        //AddonLifecycle.UnregisterListener(OnPvPIntroPreSetup);
        //AddonLifecycle.UnregisterListener(OnPvPIntroUpdate);
        //AddonLifecycle.UnregisterListener(OnPvPResults);

        MatchManager.Dispose();
        StorageManager.Dispose();
    }

    private void OnCommand(string command, string args) {
        MainWindow.IsOpen = true;
    }

    private void OnConfigCommand(string command, string args) {
        DrawConfigUI();
    }

#if DEBUG
    private void OnDebugCommand(string command, string args) {
        DebugWindow.IsOpen = true;
    }
#endif

    private void DrawUI() {
        WindowSystem.Draw();
    }

    private void DrawConfigUI() {
    }

    private void OnFrameworkUpdate(IFramework framework) {
    }

    private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
    }

    private void OnDutyStarted(object? sender, ushort param1) {
        if (_matchInProgress) {
            Log.Debug("Match has started.");
        }

    }

    private void OnDutyCompleted(object? sender, ushort param1) {
        if (_matchInProgress) {
            _matchInProgress = false;
            Log.Debug("Match ended.");
        }
    }

    private void OnTerritoryChanged(ushort territoryId) {
        var dutyId = GetCurrentDutyId();
        var duty = DataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow(dutyId);
        //Log.Verbose($"Territory changed: {territoryId}, Current duty: {GetCurrentDutyId()}");
        //bool isCrystallineConflict = false;

        switch (dutyId) {
            case 835:
            case 836:
            case 837:
            case 856:
            case 857:
            case 858:
            case 912:
            case 918:
                //isCrystallineConflict = true;
                _matchInProgress = true;
                Log.Debug($"Match has started on {DataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow(dutyId).Name}");
                break;
            default:
                _matchInProgress = false;
                break;
        }

        //if(isCrystallineConflict) {
        //    Log.Debug($"Match has started on {duty}");
        //}

    }

    public string GetCurrentPlayer() {
        string? currentPlayerName = ClientState.LocalPlayer?.Name?.ToString();
        string? currentPlayerWorld = ClientState.LocalPlayer?.HomeWorld?.GameData?.Name?.ToString();
        if (currentPlayerName == null || currentPlayerWorld == null) {
            //throw exception?
            throw new InvalidOperationException("Cannot retrieve current player");
            //return "";
        }

        return $"{currentPlayerName} {currentPlayerWorld}";
    }

    internal unsafe ushort GetCurrentDutyId() {
        return GameMain.Instance()->CurrentContentFinderConditionId;
    }

    internal Task Refresh() {
        return Task.Run(async () => {
            try {
                await DataLock.WaitAsync();
                Task mainWindowTask = MainWindow.Refresh();
                Task.WaitAll([mainWindowTask]);
            }
            finally {
                DataLock.Release();
            }
        });
    }

    private void testMethod() {
        //TextureProvider.GetTextureFromGame
        //DataManager.G
        //PlayerAlias player = new() {
        //    Name = "Tomboy Titties Adamantoise"
        //};

        //ClientState.LocalPlayer.CurrentWorld.GameData.DataCenter.

        //TerritoryType t = new();
        ////t.

        //ContentFinderCondition c = new();
        ////c.
    }
}
