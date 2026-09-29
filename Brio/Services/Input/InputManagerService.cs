using Brio.Config;
using Brio.Game.GPose;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace Brio.Input;

public class InputManagerService : MediatorSubscriberBase
{
    private readonly IKeyState _keyState;
    private readonly ConfigurationService _configurationService;
    private readonly GPoseService _gPoseService;
    private readonly Dictionary<VirtualKey, bool> _lastFrameKeyStates = [];
    private readonly HashSet<VirtualKey> _keysUpLastFrame = [];

    public static InputManagerService Instance { get; private set; } = null!;

    public InputManagerService(IKeyState keyState, Mediator mediator, ConfigurationService configurationService, GPoseService gPoseService) : base(mediator)
    {
        _keyState = keyState;
        _configurationService = configurationService;
        _gPoseService = gPoseService;

        mediator.Subscribe<FrameworkUpdateMessage>(this, (state) => OnFrameworkUpdate());

        Instance = this;
    }

    private void OnFrameworkUpdate()
    {
        if(_gPoseService.IsGPosing is false || _configurationService.Configuration.InputManager.Enable is false)
            return;

        _keysUpLastFrame.Clear();
        foreach(var key in _keyState.GetValidVirtualKeys())
        {
            var isDown = _keyState[key];
         
            if(_lastFrameKeyStates.TryGetValue(key, out var wasDown) && wasDown && !isDown)
                _keysUpLastFrame.Add(key);

            _lastFrameKeyStates[key] = isDown;
        }
    }

    public bool IsKeyDown(VirtualKey key)
    {
        return _keyState[key];
    }

    public bool WasKeyReleased(VirtualKey key)
    {
        if(_keysUpLastFrame.Contains(key))
        {
            _keysUpLastFrame.Remove(key); // we do this so that we can 'eat' it for KeyBindings like "frezze actor" as it can toggle more then once a frame making it look as if it did nothing
            return true;
        }

        return false;
    }

    public static bool ActionKeysPressedLastFrame(InputAction action)
    {
        if(Instance._configurationService.Configuration.InputManager.Enable is false)
            return false;

        if(Instance._configurationService.Configuration.InputManager.KeyBindings.TryGetValue(action, out KeyConfig value))
        {
            if(value.Key == VirtualKey.NO_KEY)
                return false;

            if(value.RequireCtrl || action is InputAction.Brio_Ctrl)
            {
                if(Instance.IsKeyDown(VirtualKey.CONTROL) && Instance.WasKeyReleased(value.Key))
                {
                    Brio.Log.Debug($"ActionKeysPressedLastFrame: {action} with key {value.Key} and Ctrl pressed");
                    return true;
                }
            }
            else if(value.RequireShift || action is InputAction.Brio_Shift)
            {
                if(Instance.IsKeyDown(VirtualKey.SHIFT) && Instance.WasKeyReleased(value.Key))
                {
                    Brio.Log.Debug($"ActionKeysPressedLastFrame: {action} with key {value.Key} and Shift pressed");
                    return true;
                }
            }
            else if(value.RequireAlt || action is InputAction.Brio_Alt)
            {
                if(Instance.IsKeyDown(VirtualKey.MENU) && Instance.WasKeyReleased(value.Key))
                {
                    Brio.Log.Debug($"ActionKeysPressedLastFrame: {action} with key {value.Key} and Alt pressed");
                    return true;
                }
            }
            else
            {
                if(Instance.WasKeyReleased(value.Key))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool ActionKeysPressed(InputAction action)
    {
        if(Instance._configurationService.Configuration.InputManager.KeyBindings.TryGetValue(action, out KeyConfig value))
        {
            if(value.Key == VirtualKey.NO_KEY)
                return false;

            if(value.RequireCtrl || action is InputAction.Brio_Ctrl)
            {
                if(Instance.IsKeyDown(VirtualKey.CONTROL) && Instance.IsKeyDown(value.Key))
                {
                    return true;
                }
            }
            else if(value.RequireShift || action is InputAction.Brio_Shift)
            {
                if(Instance.IsKeyDown(VirtualKey.SHIFT) && Instance.IsKeyDown(value.Key))
                {
                    return true;
                }
            }
            else if(value.RequireAlt || action is InputAction.Brio_Alt)
            {
                if(Instance.IsKeyDown(VirtualKey.MENU) && Instance.IsKeyDown(value.Key))
                {
                    return true;
                }
            }
            else
            {
                if(Instance.IsKeyDown(value.Key))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static IEnumerable<VirtualKey> GetValidKeys()
    {
        return Instance._keyState.GetValidVirtualKeys();
    }

    public override void Dispose()
    {
        base.Dispose();

        GC.SuppressFinalize(this);
    }
}
