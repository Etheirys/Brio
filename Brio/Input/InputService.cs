using Brio.Config;
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

    private readonly HashSet<KeyBindEvents> _eventsDown = new();
    private readonly Dictionary<KeyBindEvents, List<Action>> _listeners = new();

    public static InputService Instance { get; private set; } = null!;

    public InputService(IKeyState keyState, IFramework framework, ConfigurationService configService)
    {
        Instance = this;
        _keyState = keyState;
        _framework = framework;
        _configService = configService;

        _framework.Update += OnFrameworkUpdate;
    }

    public static IEnumerable<VirtualKey> GetValidKeys()
    {
        return Instance._keyState.GetValidVirtualKeys();
    }

    public static bool IsKeyBindDown(KeyBindEvents keyBind)
    {
        return Instance._eventsDown.Contains(keyBind);
    }

    public void AddListener(KeyBindEvents keyBind, Action callback)
    {
        if(!_listeners.ContainsKey(keyBind))
            _listeners.Add(keyBind, new());

        _listeners[keyBind].Add(callback);
    }

    public void RemoveListener(KeyBindEvents keyBind, Action callback)
    {
        if(!_listeners.ContainsKey(keyBind))
            return;

        _listeners[keyBind].Remove(callback);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        foreach (var evt in Enum.GetValues<KeyBindEvents>())
        {
            this.CheckEvent(evt);
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
        else if (isDown && !wasDown)
        {
            _eventsDown.Add(evt);
            
            // just released, invoke listeners
            foreach(Action callback in _listeners[evt])
            {
                callback.Invoke();
            }
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
