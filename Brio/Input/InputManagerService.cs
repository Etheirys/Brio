using Brio.Config;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace Brio.Input;
public class InputManagerService
{
    private readonly IKeyState _keyState;
    private readonly ConfigurationService _configurationService;
    private readonly GPoseService _gPoseService;
    private readonly VirtualCameraManager _virtualCameraManager;

    public static InputManagerService Instance { get; private set; } = null!;

    public InputManagerService(IKeyState keyState, ConfigurationService configurationService, VirtualCameraManager virtualCameraManager, GPoseService gPoseService)
    {
        _keyState = keyState;
        _configurationService = configurationService;
        _gPoseService = gPoseService;
        _virtualCameraManager = virtualCameraManager;

        Instance = this;
    }

    private bool IsKeyDown(VirtualKey key)
    {
        return _keyState[key];
    }

    //
    //

    public static bool ActionKeysPressed(InputAction action)
    {
        if(Instance._configurationService.Configuration.InputManager.KeyBindings.TryGetValue(action, out KeyConfig? value))
        {
            if(value.Key == VirtualKey.NO_KEY)
                return false;

            if(value.RequireCtrl)
            {
                if(Instance.IsKeyDown(VirtualKey.CONTROL) && Instance.IsKeyDown(value.Key))
                {
                    return true;
                }
            }
            else if(value.requireShift)
            {
                if(Instance.IsKeyDown(VirtualKey.SHIFT) && Instance.IsKeyDown(value.Key))
                {
                    return true;
                }
            }
            else if(value.requireAlt)
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

    //public void AddListener(KeyBindEvents evt, Action callback)
    //{
    //    if(!_listeners.ContainsKey(evt))
    //        _listeners.Add(evt, new());

    //    _listeners[evt].Add(callback);
    //}

    //public void RemoveListener(KeyBindEvents evt, Action callback)
    //{
    //    if(!_listeners.ContainsKey(evt))
    //        return;

    //    _listeners[evt].Remove(callback);
    //}

    //private void OnFrameworkUpdate(IFramework framework)
    //{
    //    if(_gPoseService.IsGPosing == false)
    //        return;

    //    bool disable = false;
    //    if(_virtualCameraManager.CurrentCamera is not null && _virtualCameraManager.CurrentCamera.FreeCamValues.IsMovementEnabled)
    //    {
    //        disable = true;
    //    }

    //    if(_configurationService.Configuration.InputManager.Enable && disable == false)
    //    {
    //        foreach(var evt in keyBindEvents)
    //        {
    //            this.CheckEvent(evt);
    //        }
    //    }
    //}
}
