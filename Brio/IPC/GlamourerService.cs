using Brio.Config;
using Brio.Game.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brio.IPC;

public class GlamourerService : BrioIPC
{
    public override string Name { get; } = "Glamourer";

    public override bool IsAvailable
        => CheckStatus() == IPCStatus.Available;

    public override bool AllowIntegration
        => _configurationService.Configuration.IPC.AllowGlamourerIntegration;

    public override int APIMajor => 1;
    public override int APIMinor => 4;

    public override (int Major, int Minor) GetAPIVersion()
        => _glamourerApiVersion.Invoke();

    public override IDalamudPluginInterface GetPluginInterface()
        => _pluginInterface;

    //

    private readonly ConfigurationService _configurationService;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ActorRedrawService _redrawService;
    private readonly IFramework _framework;
    private readonly ICommandManager _commandManager;

    //
    //

    private readonly Glamourer.Api.Helpers.EventSubscriber _glamourerInitializedSubscriber;

    private readonly Glamourer.Api.IpcSubscribers.RevertState _glamourerRevertCharacter;
    private readonly Glamourer.Api.IpcSubscribers.ApiVersion _glamourerApiVersion;
    private readonly Glamourer.Api.IpcSubscribers.GetState _glamourerGetState;

    private readonly Glamourer.Api.IpcSubscribers.GetDesignList _glamourerGetDesignList;
    private readonly Glamourer.Api.IpcSubscribers.ApplyDesign _glamourerApplyDesign;

    //

    public readonly uint BrioKey = 0x11625;
    private readonly uint _unlock = 0x6D617265; // From MareSynchronos's IpcCallerGlamourer.cs

    public GlamourerService(IDalamudPluginInterface pluginInterface, ConfigurationService configurationService, IFramework framework, ICommandManager  commandManager, ActorRedrawService redrawService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;
        _framework = framework;
        _redrawService = redrawService;
        _commandManager = commandManager;

        _glamourerInitializedSubscriber = Glamourer.Api.IpcSubscribers.Initialized.Subscriber(pluginInterface, OnConfigurationChanged);

        _glamourerApiVersion = new Glamourer.Api.IpcSubscribers.ApiVersion(pluginInterface);
        _glamourerRevertCharacter = new Glamourer.Api.IpcSubscribers.RevertState(pluginInterface);
        _glamourerGetState = new Glamourer.Api.IpcSubscribers.GetState(pluginInterface);

        _glamourerGetDesignList = new Glamourer.Api.IpcSubscribers.GetDesignList(pluginInterface);
        _glamourerApplyDesign = new Glamourer.Api.IpcSubscribers.ApplyDesign(pluginInterface);

        OnConfigurationChanged();

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;
    }

    public void OpenGlamourer()
    {
        _commandManager.ProcessCommand("/glamourer");
    }

    public bool CheckForLock(IGameObject? character)
    {
        if(IsAvailable == false || character is null)
            return false;

        var (key, _) = _glamourerGetState.Invoke(character!.ObjectIndex);

        Brio.Log.Verbose("Glamourer CheckForLock... " + key);

        return key == Glamourer.Api.Enums.GlamourerApiEc.InvalidKey;
    }

    public Task UnlockAndRevertCharacter(IGameObject? character)
    {
        if(IsAvailable == false || character is null)
            return Task.CompletedTask;

        Brio.Log.Debug("Starting glamourer UnlockAndRevert...");

        var success = _glamourerRevertCharacter.Invoke(character!.ObjectIndex, _unlock);

        if(success == Glamourer.Api.Enums.GlamourerApiEc.InvalidKey)
        {
            var success2 = _glamourerRevertCharacter.Invoke(character!.ObjectIndex, BrioKey);
            if(success2 == Glamourer.Api.Enums.GlamourerApiEc.InvalidKey)
            {
                Brio.Log.Fatal("Glamourer revert failed! Please report this to the Brio Devs!");
                return Task.CompletedTask;
            }
        }

        return _framework.RunOnTick(async () =>
        {
            await _redrawService.WaitForDrawing(character!);
            Brio.Log.Debug("Glamourer revert complete");
        }, delayTicks: 5);
    }

    public Task RevertCharacter(IGameObject? character)
    {
        if(IsAvailable == false || character is null)
            return Task.CompletedTask;

        Brio.Log.Debug("Starting glamourer Revert...");

        var success = _glamourerRevertCharacter.Invoke(character!.ObjectIndex);

        if(success == Glamourer.Api.Enums.GlamourerApiEc.InvalidKey)
        {
            Brio.Log.Info("Glamourer character was locked..");
            UnlockAndRevertCharacter(character);
            return Task.CompletedTask;
        }

        if(success == Glamourer.Api.Enums.GlamourerApiEc.Success)
        {
            Brio.Log.Debug("Glamourer revert Started");
            return _framework.RunOnTick(async () =>
            {
                await _redrawService.WaitForDrawing(character!);
                Brio.Log.Debug("Glamourer revert complete");
            }, delayTicks: 5);
        }

        return Task.CompletedTask;
    }

    public Dictionary<Guid, string>? GetDesignList()
    {
        if(IsAvailable == false)
            return null;

        return _glamourerGetDesignList.Invoke();
    }

    public bool ApplyDesign(Guid design, IGameObject? character)
    {
        if(IsAvailable == false || character is null)
            return false;

        _glamourerApplyDesign.Invoke(design, character!.ObjectIndex);

        return true;
    }

    private void OnConfigurationChanged()
        => CheckStatus();

    public override void Dispose()
    {
        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;

        _glamourerInitializedSubscriber.Dispose();
    }
}
