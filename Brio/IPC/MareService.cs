using Brio.Config;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Brio.IPC;

internal class MareService : IDisposable
{
    public bool IsMareAvailable { get; private set; } = false;

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly ConfigurationService _configurationService;

    private readonly ICallGateSubscriber<string, GameObject, Task<bool>> _mareApplyMcdfAsync;

    public MareService(DalamudPluginInterface pluginInterface, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;

        _mareApplyMcdfAsync = pluginInterface.GetIpcSubscriber<string, GameObject, Task<bool>>("MareSynchronos.LoadMcdfAsync");

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

    public Task<bool> LoadMcdfAsync(string fileName, GameObject target)
    {
        RefreshMareStatus();

        if(IsMareAvailable == false)
        {
            Brio.Log.Error($"Failed load MCDF file, Mare is not available");
            return Task.FromResult(false);
        }

        try
        {
            return _mareApplyMcdfAsync.InvokeFunc(fileName, target);
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Failed to Invoke MareSynchronos.LoadMcdfAsync IPC");
            return Task.FromResult(false);
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
