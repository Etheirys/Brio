using System.Collections.Generic;
using Brio.Config;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

namespace Brio.Input;
public class InputManagerService
{
    private readonly IKeyState _keyState;
    private readonly ConfigurationService _configurationService;

    public static InputManagerService Instance { get; private set; } = null!;

    public InputManagerService(IKeyState keyState, ConfigurationService configurationService)
    {
        _keyState = keyState;
        _configurationService = configurationService;
        Instance = this;
    }
    private bool IsKeyDown(VirtualKey key)
    {
        return _keyState[key];
    }
    public static bool ActionKeysPressed(InputAction action)
    {
        if(Instance._configurationService.Configuration.InputManager.KeyBindings.TryGetValue(action, out KeyConfig? value))
        {
            if(value.key == VirtualKey.NO_KEY)
                return false;

            if(value.requireCtrl)
            {
                if(Instance.IsKeyDown(VirtualKey.CONTROL) && Instance.IsKeyDown(value.key))
                {
                    return true;
                }
            }
            else if(value.requireShift)
            {
                if(Instance.IsKeyDown(VirtualKey.SHIFT) && Instance.IsKeyDown(value.key))
                {
                    return true;
                }
            }
            else if(value.requireAlt)
            {
                if(Instance.IsKeyDown(VirtualKey.MENU) && Instance.IsKeyDown(value.key))
                {
                    return true;
                }
            }
            else
            {
                if(Instance.IsKeyDown(value.key))
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
}
