using Brio.Config;
using Brio.Game.Core;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;

namespace Brio.IPC;

public class MareService : BrioIPC
{
    public override string Name { get; } = "Mare Synchronos";

    public override bool IsAvailable
        => CheckStatus() == IPCStatus.Available;

    public override bool AllowIntegration
        => _configurationService.Configuration.IPC.AllowMareIntegration;

    public override int APIMajor => 1;
    public override int APIMinor => 0;

    public override (int Major, int Minor) GetAPIVersion()
        => (1, 0);

    public override IDalamudPluginInterface GetPluginInterface()
        => _pluginInterface;

    //
    //

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ConfigurationService _configurationService;
    private readonly ObjectMonitorService _objectMonitorService;

    private readonly ICallGateSubscriber<string, IGameObject, bool> _mareApplyMcdf;
    private readonly ICallGateSubscriber<List<nint>> _mareGetHandledAddresses;

    public MareService(IDalamudPluginInterface pluginInterface, ObjectMonitorService objectMonitorService, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;
        _objectMonitorService = objectMonitorService;

        _mareApplyMcdf = pluginInterface.GetIpcSubscriber<string, IGameObject, bool>("MareSynchronos.LoadMcdf");
        _mareGetHandledAddresses = pluginInterface.GetIpcSubscriber<List<nint>>("MareSynchronos.GetHandledAddresses");

        OnConfigurationChanged();

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;
    }

    public bool LoadMcdfAsync(string fileName, IGameObject target)
    {
        if(IsAvailable == false || target is null)
            return false;

        try
        {
            return _mareApplyMcdf.InvokeFunc(fileName, target);
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Failed to Invoke MareSynchronos.LoadMcdfAsync IPC");
            return false;
        }
    }

    public bool HandledByMare(IGameObject obj)
    {
        if(IsAvailable == false || obj is null)
            return false;


        Brio.Log.Error($"HandledByMareIPC");

        var pointers = _mareGetHandledAddresses.InvokeFunc();

        foreach(var address in pointers)
        {
            try
            {
                Brio.Log.Error($"HandledByMareIPC address: {address}");

                var mareObj = _objectMonitorService.ObjectTable.CreateObjectReference(address);
                if(mareObj is not null && mareObj.ObjectIndex == obj.ObjectIndex)
                {
                    return true;
                }
            }
            catch
            {
            }
        }
        return false;
    }

    private void OnConfigurationChanged()
        => CheckStatus();

    public override void Dispose()
    {
        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;
    }
}
