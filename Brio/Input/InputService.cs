using Brio.Config;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace Brio.Input;

internal class InputService
{
    private readonly IKeyState _keyState;
    private readonly IFramework _framework;
    private readonly ConfigurationService _configService;
    private readonly GPoseService _gPoseService;

    private readonly HashSet<KeyBindEvents> _eventsDown = new();
    private readonly Dictionary<KeyBindEvents, List<Action>> _listeners = new();

    public static InputService Instance { get; private set; } = null!;

    public InputService(IKeyState keyState, IFramework framework, ConfigurationService configService, GPoseService gPoseService)
    {
        Instance = this;
        _keyState = keyState;
        _framework = framework;
        _configService = configService;
        _gPoseService = gPoseService;

        _framework.Update += OnFrameworkUpdate;
    }

    public static IEnumerable<VirtualKey> GetValidKeys()
    {
        return Instance._keyState.GetValidVirtualKeys();
    }

    public static bool IsKeyBindDown(KeyBindEvents evt)
    {
        if(Instance._configService.Configuration.Input.EnableKeybinds)
            return false;

        return Instance._eventsDown.Contains(evt);
    }

    public bool HasListener(KeyBindEvents evt)
    {
        if(!_listeners.ContainsKey(evt))
            return false;

        return _listeners[evt].Count > 0;
    }

    public void AddListener(KeyBindEvents evt, Action callback)
    {
        if(!_listeners.ContainsKey(evt))
            _listeners.Add(evt, new());

        _listeners[evt].Add(callback);
    }

    public void RemoveListener(KeyBindEvents evt, Action callback)
    {
        if(!_listeners.ContainsKey(evt))
            return;

        _listeners[evt].Remove(callback);
    }

    public KeyBind? GetKeyBind(KeyBindEvents evt)
    {
        KeyBind? bind = null;
        _configService.Configuration.Input.Bindings.TryGetValue(evt, out bind);
        return bind;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if(_gPoseService.IsGPosing && _configService.Configuration.Input.EnableKeybinds)
        {
            foreach(var evt in Enum.GetValues<KeyBindEvents>())
            {
                this.CheckEvent(evt);
            }
        }
    }

    private void CheckEvent(KeyBindEvents evt)
    {
        KeyBind? bind;
        if(!_configService.Configuration.Input.Bindings.TryGetValue(evt, out bind) || bind == null)
            return;

        bool isDown = this.IsDown(bind);
        bool wasDown = _eventsDown.Contains(evt);

        if(!isDown && wasDown)
        {
            _eventsDown.Remove(evt);
        }
        else if(isDown && !wasDown)
        {
            _eventsDown.Add(evt);

            try
            {
                // just released, invoke listeners
                foreach(Action callback in _listeners[evt])
                {
                    callback?.Invoke();
                }
            }
            catch(Exception) { }
        }
    }

    private bool IsDown(KeyBind bind)
    {
        if(bind.Key == VirtualKey.NO_KEY)
            return false;

        bool down = _keyState[bind.Key];

        if(bind.Key != VirtualKey.CONTROL)
            down &= _keyState[VirtualKey.CONTROL] == bind.Control;

        if(bind.Key != VirtualKey.MENU)
            down &= _keyState[VirtualKey.MENU] == bind.Alt;

        if(bind.Key != VirtualKey.SHIFT)
            down &= _keyState[VirtualKey.SHIFT] == bind.Shift;

        return down;

    }
}
