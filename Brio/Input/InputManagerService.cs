using Brio.Config;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace Brio.Input;

public class InputManagerService : IDisposable
{
    private readonly IKeyState _keyState;
    private readonly ConfigurationService _configurationService;
    private readonly GPoseService _gPoseService;
    private readonly IFramework _framework;

    private readonly Dictionary<VirtualKey, bool> _lastFrameKeyStates = [];
    private readonly HashSet<VirtualKey> _keyUpTriggered = [];

    public static InputManagerService Instance { get; private set; } = null!;

    public InputManagerService(IKeyState keyState, IFramework framework, ConfigurationService configurationService, GPoseService gPoseService)
    {
        _keyState = keyState;
        _configurationService = configurationService;
        _gPoseService = gPoseService;
        _framework = framework;

        _framework.Update += OnFrameworkUpdate;

        Instance = this;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if(_gPoseService.IsGPosing is false || _configurationService.Configuration.InputManager.Enable is false)
            return;

        foreach(var key in _keyState.GetValidVirtualKeys())
        {
            _lastFrameKeyStates[key] = _keyState[key];
            if(_keyState[key])
                _keyUpTriggered.Remove(key);
        }
    }

    private bool IsKeyDown(VirtualKey key)
    {
        return _keyState[key];
    }

    public bool IsKeyUp(VirtualKey key)
    {
        if(_configurationService.Configuration.InputManager.Enable is false)
            return false;

        bool released = (!_keyState[key] && _lastFrameKeyStates.TryGetValue(key, out var wasDown) && wasDown);
        if(released && !_keyUpTriggered.Contains(key))
        {
            _keyUpTriggered.Add(key);
            return true;
        }
        return false;
    }

    public static bool ActionKeysPressedLastFrame(InputAction action)
    {
        if(Instance._configurationService.Configuration.InputManager.KeyBindings.TryGetValue(action, out KeyConfig? value))
        {
            if(value.Key == VirtualKey.NO_KEY)
                return false;

            if(value.RequireCtrl || action is InputAction.Brio_Ctrl)
            {
                if(Instance.IsKeyDown(VirtualKey.CONTROL) && Instance.IsKeyUp(value.Key))
                {
                    return true;
                }
            }
            else if(value.requireShift || action is InputAction.Brio_Shift)
            {
                if(Instance.IsKeyDown(VirtualKey.SHIFT) && Instance.IsKeyUp(value.Key))
                {
                    return true;
                }
            }
            else if(value.requireAlt || action is InputAction.Brio_Alt)
            {
                if(Instance.IsKeyDown(VirtualKey.MENU) && Instance.IsKeyUp(value.Key))
                {
                    return true;
                }
            }
            else
            {
                if(Instance.IsKeyUp(value.Key))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool ActionKeysPressed(InputAction action)
    {
        if(Instance._configurationService.Configuration.InputManager.KeyBindings.TryGetValue(action, out KeyConfig? value))
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
            else if(value.requireShift || action is InputAction.Brio_Shift)
            {
                if(Instance.IsKeyDown(VirtualKey.SHIFT) && Instance.IsKeyDown(value.Key))
                {
                    return true;
                }
            }
            else if(value.requireAlt || action is InputAction.Brio_Alt)
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

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;

        GC.SuppressFinalize(this);
    }
}
