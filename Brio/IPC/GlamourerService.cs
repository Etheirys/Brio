using Brio.Config;
using Brio.Core;
using Brio.Game.Actor;
using Brio.Game.Core;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.Threading;
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
    private readonly IObjectTable _gameObjects;
    private readonly DalamudService _dalamudService;

    //
    //

    private readonly EventSubscriber _glamourerInitializedSubscriber;

    private readonly ApiVersion _glamourerApiVersion;

    private readonly GetState _glamourerGetState;
    private readonly ApplyState _glamourerApplyState;
    private readonly RevertState _glamourerRevertCharacter;
    private readonly RevertStateName _glamourerRevertByName;
    private readonly GetStateBase64? _glamourerGetAllCustomization;

    private readonly UnlockState _glamourerUnlock;
    private readonly UnlockStateName _glamourerUnlockByName;

    private readonly GetDesignList _glamourerGetDesignList;
    private readonly ApplyDesign _glamourerApplyDesign;

    //

    private readonly uint LockCode = 0x6D617265;

    public GlamourerService(IDalamudPluginInterface pluginInterface, IObjectTable gameObjects, ICommandManager commandManager, DalamudService dalamudService, ConfigurationService configurationService, IFramework framework, ActorRedrawService redrawService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;
        _framework = framework;
        _redrawService = redrawService;
        _commandManager = commandManager;
        _dalamudService = dalamudService;
        _gameObjects = gameObjects;

        _glamourerInitializedSubscriber = Initialized.Subscriber(_pluginInterface, OnConfigurationChanged);

        _glamourerApiVersion = new ApiVersion(_pluginInterface);

        _glamourerGetState = new GetState(_pluginInterface);
        _glamourerApplyState = new ApplyState(_pluginInterface);
        _glamourerRevertCharacter = new RevertState(_pluginInterface);

        _glamourerRevertByName = new RevertStateName(_pluginInterface);
        _glamourerGetAllCustomization = new GetStateBase64(_pluginInterface);
        _glamourerUnlock = new UnlockState(_pluginInterface);
        _glamourerUnlockByName = new UnlockStateName(_pluginInterface);

        _glamourerGetDesignList = new GetDesignList(_pluginInterface);
        _glamourerApplyDesign = new ApplyDesign(_pluginInterface);

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


    public bool UnlockAndRevertCharacterByName(string name)
    {
        if(IsAvailable == false || name.IsNullOrEmpty())
            return false;

        Brio.Log.Debug("Starting glamourer UnlockAndRevertByName...");

        var success = _glamourerUnlockByName.Invoke(name, LockCode);

        if(success is not GlamourerApiEc.Success)
        {
            Brio.Log.Info($"Glamourer UnlockAndRevertCharacterByName was not Successful: {success}");
            return false;
        }

        return true;
    }

    public bool UnlockAndRevertCharacter(IGameObject? character)
    {
        if(IsAvailable == false || character is null)
            return false;

        Brio.Log.Debug("Starting glamourer UnlockAndRevert...");

        var success = _glamourerRevertCharacter.Invoke(character!.ObjectIndex, LockCode);

        if(success is not GlamourerApiEc.Success)
        {
            Brio.Log.Info($"Glamourer UnlockAndRevertCharacter was not Successful: {success}");
            return false;
        }

        return true;
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

    public async Task RevertByNameAsync(string name, Guid applicationId)
    {
        if((!IsAvailable) || _dalamudService.IsZoning) return;

        await _framework.RunOnFrameworkThread(() =>
        {
            RevertByName(name, applicationId);

        }).ConfigureAwait(false);
    }
  
    public void ApplyAllAsync(IGameObject? character, string? customization, Guid applicationId)
    {
        if(IsAvailable == false || string.IsNullOrEmpty(customization)) return;

        try
        {
            Brio.Log.Debug("[{appid}] Calling on IPC: GlamourerApplyAll", applicationId);
            _glamourerApplyState.Invoke(customization, character!.ObjectIndex, LockCode);
        }
        catch(Exception ex)
        {
            Brio.Log.Debug(ex, "[{appid}] Failed to apply Glamourer data", applicationId);
        }
    }

    public void RevertByName(string name, Guid applicationId)
    {
        if((IsAvailable) || _dalamudService.IsZoning) return;

        try
        {
            Brio.Log.Debug("[{appid}] Calling On IPC: GlamourerRevertByName", applicationId);
            _glamourerRevertByName.Invoke(name, LockCode);
            Brio.Log.Debug("[{appid}] Calling On IPC: GlamourerUnlockName", applicationId);
            _glamourerUnlockByName.Invoke(name, LockCode);
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, "Error during Glamourer RevertByName");
        }
    }
  
    public async Task<string> GetCharacterCustomizationAsync(IntPtr character)
    {
        if(IsAvailable == false) return string.Empty;

        try
        {
            return await _framework.RunOnFrameworkThread(() =>
            {
                var gameObj = _gameObjects.CreateObjectReference(character);
                if(gameObj is ICharacter c)
                {
                    return _glamourerGetAllCustomization!.Invoke(c.ObjectIndex).Item2 ?? string.Empty;
                }
                return string.Empty;
            }).ConfigureAwait(false);
        }
        catch
        {
            return string.Empty;
        }
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

        GC.SuppressFinalize(this);
    }
}
