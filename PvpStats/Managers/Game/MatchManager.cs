using Dalamud.Hooking;
using PvpStats.Types.Match;
using System;
using System.Collections.Generic;

namespace PvpStats.Managers.Game;
internal abstract class MatchManager<T> : IDisposable where T : PvpMatch {
    protected readonly Plugin Plugin;
    protected virtual T? CurrentMatch { get; set; }

    protected List<(string fieldName, Hook<Delegate>)> Hooks = new();

    internal MatchManager(Plugin plugin) {
        Plugin = plugin;
        plugin.InteropProvider.InitializeFromAttributes(this);
        plugin.ClientState.TerritoryChanged += OnTerritoryChanged;

        //get all hooks
        //Hooks = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.FieldType.IsAssignableTo(typeof(IDalamudHook))).Select(x => (x.Name,(Hook<Delegate>)x.GetValue(this)!)).ToList();

        //Plugin.Log.Debug($"name: {this.GetType().Name}");
        //x.FieldType.IsAssignableTo(typeof(Hook<Delegate>))
        //var hookFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.FieldType.GetGenericArguments().All(y => y.IsAssignableTo(typeof(Delegate)))).Count();
        //var hookFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.FieldType.IsAssignableTo(typeof(IDalamudHook))).Count();

        //var hookField = this.GetType().GetField("_flMatchEndHook", BindingFlags.NonPublic | BindingFlags.Instance);

        //Plugin.Log.Debug($"match end found?: {hookField?.Name}");
        //Plugin.Log.Debug($"match end type: {hookField?.FieldType}");

        //Plugin.Log.Debug($"hook fields: {hookFields}");
        //Hooks.ForEach(x => EnableHook(x.Item1, x.Item2));
    }

    public virtual void Dispose() {
        Hooks.ForEach(x => DisposeHook(x.Item1, x.Item2));
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
    }

    private void EnableHook(string name, IDalamudHook hook) {
        Plugin.Log.Debug($"enabling {name} at address: 0x{hook.Address:X2}");
        var hook2 = hook as Hook<Delegate>;
        hook2.Enable();
    }

    private void DisposeHook(string name, IDalamudHook hook) {
        //Plugin.Log.Debug($"disabling {name} at address: 0x{hook.Address:X2}");
        hook.Dispose();
    }

    protected void OnTerritoryChanged(ushort territoryId) {
        var dutyId = Plugin.GameState.GetCurrentDutyId();
        //Plugin.Log.Debug($"Territory changed: {territoryId}, Current duty: {dutyId}");
        if(IsMatchInProgress()) {
            Plugin.DataQueue.QueueDataOperation(() => {
                Plugin.Functions._opcodeMatchCount++;
                CurrentMatch = null;
                //Plugin.WindowManager.Refresh();
            });
        }
    }

    protected virtual void OnDutyCompleted(object? sender, ushort p1) {
        Plugin.Log.Debug("Duty has completed.");
    }

    public bool IsMatchInProgress() {
        return CurrentMatch != null;
    }
}
