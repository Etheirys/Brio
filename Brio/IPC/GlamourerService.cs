using Brio.Config;
using Brio.Game.Actor;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Brio.IPC;

internal class GlamourerService : IDisposable
{
    public bool IsGlamourerAvailable { get; private set; } = false;

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly ConfigurationService _configurationService;
    private readonly GPoseService _gPoseService;
    private readonly IFramework _framework;
    private readonly ActorRedrawService _redrawService;

    private readonly ICallGateSubscriber<(int, int)> _glamourerApiVersions;
    private readonly ICallGateSubscriber<Character?, object?> _glamourerRevertCharacter;

    private const int GlamourerApiMajor = 0;
    private const int GlamourerApiMinor = 4;

    public GlamourerService(DalamudPluginInterface pluginInterface, ConfigurationService configurationService, GPoseService gPoseService, IFramework framework, ActorRedrawService redrawService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;
        _gPoseService = gPoseService;
        _framework = framework;
        _redrawService = redrawService;

        _glamourerApiVersions = pluginInterface.GetIpcSubscriber<(int, int)>("Glamourer.ApiVersions");
        _glamourerRevertCharacter = pluginInterface.GetIpcSubscriber<Character?, object?>("Glamourer.RevertCharacter");

        RefreshGlamourerStatus();

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
        _configurationService.OnConfigurationChanged += RefreshGlamourerStatus;
    }

    public void RefreshGlamourerStatus()
    {
        if(_configurationService.Configuration.IPC.AllowGlamourerIntegration)
        {
            IsGlamourerAvailable = ConnectToGlamourer();
        }
        else
        {
            IsGlamourerAvailable = false;
        }
    }

    public Task RevertCharacter(Character? character)
    {
        if(!IsGlamourerAvailable && character != null)
            return Task.CompletedTask;

        Brio.Log.Debug("Starting glamourer revert...");

        _glamourerRevertCharacter.InvokeAction(character);

        return _framework.RunOnTick(async () =>
        {
            await _redrawService.WaitForDrawing(character!);
            Brio.Log.Debug("Glamourer revert complete");
        }, delayTicks: 5);

    }

    private bool ConnectToGlamourer()
    {
        try
        {
            bool glamourerInstalled = _pluginInterface.InstalledPlugins.Any(x => x.Name == "Glamourer");
            if(!glamourerInstalled)
            {
                Brio.Log.Debug("Glamourer not present");
                return false;
            }

            var (major, minor) = _glamourerApiVersions.InvokeFunc();
            if(major != GlamourerApiMajor || minor < GlamourerApiMinor)
            {
                Brio.Log.Debug("Glamourer API mismatch");
                return false;
            }

            Brio.Log.Debug("Glamourer integration initialized");

            return true;
        }
        catch(Exception ex)
        {
            Brio.Log.Debug(ex, "Glamourer initialize error");
            return false;
        }
    }

    private void OnGPoseStateChange(bool newState)
    {
        RefreshGlamourerStatus();
    }

    public void Dispose()
    {
        _configurationService.OnConfigurationChanged -= RefreshGlamourerStatus;
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
