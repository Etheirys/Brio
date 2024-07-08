using Brio.Config;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Linq;

namespace Brio.IPC;

internal class MareService : IDisposable
{
    public bool IsMareAvailable { get; private set; } = false;

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ConfigurationService _configurationService;

    private readonly ICallGateSubscriber<string, IGameObject, bool> _mareApplyMcdf;

    public MareService(IDalamudPluginInterface pluginInterface, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;

        _mareApplyMcdf = pluginInterface.GetIpcSubscriber<string, IGameObject, bool>("MareSynchronos.LoadMcdf");

        RefreshMareStatus();

        _configurationService.OnConfigurationChanged += RefreshMareStatus;
    }

    public void RefreshMareStatus()
    {
        if(_configurationService.Configuration.IPC.AllowMareIntegration)
        {
            IsMareAvailable = ConnectToMare();
        }
        else
        {
            IsMareAvailable = false;
        }
    }

    public bool LoadMcdfAsync(string fileName, IGameObject target)
    {
        RefreshMareStatus();

        if(IsMareAvailable == false)
        {
            Brio.Log.Error($"Failed load MCDF file, Mare is not available");
            return false;
        }

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

    private bool ConnectToMare()
    {
        try
        {
            bool mareInstalled = _pluginInterface.InstalledPlugins.Any(x => x.Name == "Mare Synchronos" && x.IsLoaded == true);

            if(!mareInstalled)
            {
                Brio.Log.Debug("Mare Synchronos not present");
                return false;
            }

            Brio.Log.Debug("Mare Synchronos integration initialized");

            return true;
        }
        catch(Exception ex)
        {
            Brio.Log.Debug(ex, "Mare Synchronos initialize error");
            return false;
        }
    }

    public void Dispose()
    {
        _configurationService.OnConfigurationChanged -= RefreshMareStatus;
    }
}
