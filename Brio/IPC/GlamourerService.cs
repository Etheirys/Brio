using Brio.Config;
using Brio.Game.Actor;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Brio.IPC;

internal class GlamourerService : IDisposable
{
    public bool IsGlamourerAvailable { get; private set; } = false;

    private const int GlamourerApiMajor = 1;
    private const int GlamourerApiMinor = 1;

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ConfigurationService _configurationService;
    private readonly GPoseService _gPoseService;
    private readonly IFramework _framework;
    private readonly ActorRedrawService _redrawService;
    private readonly PenumbraService _penumbraService; 

    private readonly Glamourer.Api.Helpers.EventSubscriber _glamourerInitializedSubscriber;

    private readonly Glamourer.Api.IpcSubscribers.ApiVersion _glamourerApiVersions;
    private readonly Glamourer.Api.IpcSubscribers.RevertState _glamourerRevertCharacter;

    private readonly uint UnLockCode = 0x6D617265; // From MareSynchronos's IpcCallerGlamourer.cs

    public GlamourerService(IDalamudPluginInterface pluginInterface, ConfigurationService configurationService, GPoseService gPoseService, IFramework framework, PenumbraService penumbraService,  ActorRedrawService redrawService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;
        _gPoseService = gPoseService;
        _framework = framework;
        _redrawService = redrawService;
        _penumbraService = penumbraService;

        _glamourerInitializedSubscriber = Glamourer.Api.IpcSubscribers.Initialized.Subscriber(pluginInterface, RefreshGlamourerStatus);

        _glamourerApiVersions = new Glamourer.Api.IpcSubscribers.ApiVersion(pluginInterface);
        _glamourerRevertCharacter = new Glamourer.Api.IpcSubscribers.RevertState(pluginInterface);

        RefreshGlamourerStatus();

        _configurationService.OnConfigurationChanged += RefreshGlamourerStatus;
        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
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

        bool ConnectToGlamourer()
        {
            try
            {
                bool glamourerInstalled = _pluginInterface.InstalledPlugins.Any(x => x.Name == "Glamourer" && x.IsLoaded == true);
                if(!glamourerInstalled)
                {
                    Brio.Log.Debug("Glamourer not present");
                    return false;
                }

                var (major, minor) = _glamourerApiVersions.Invoke();
                if(major != GlamourerApiMajor || minor < GlamourerApiMinor)
                {
                    Brio.Log.Warning($"Glamourer API mismatch, found v{major}.{minor}");
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
    }

    public Task RevertCharacter(ICharacter? character)
    {
        if(IsGlamourerAvailable == false && character is null)
            return Task.CompletedTask;

        Brio.Log.Warning("Starting glamourer revert...");

        var success = _glamourerRevertCharacter.Invoke(character!.ObjectIndex, UnLockCode);
        
        if(success == Glamourer.Api.Enums.GlamourerApiEc.InvalidKey)
        {
            Brio.Log.Fatal("Glamourer revert failed! Please report this to the Brio Devs!");
            return Task.CompletedTask;
        }

        return _framework.RunOnTick(async () =>
        {
            await _redrawService.WaitForDrawing(character!);
            Brio.Log.Debug("Glamourer revert complete");
        }, delayTicks: 5);

    }

    private void OnGPoseStateChange(bool newState)
    {
        RefreshGlamourerStatus();
    }

    public void Dispose()
    {
        _configurationService.OnConfigurationChanged -= RefreshGlamourerStatus;
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;

        _glamourerInitializedSubscriber.Dispose();
    }
}
