using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using ImGuiNET;
using System.Collections.Generic;

namespace Brio.Input;

internal class InputService
{
    private readonly IKeyState _keyState;

    public static InputService Instance { get; private set; } = null!;

    public InputService(IKeyState keyState)
    {
        Instance = this;
        _keyState = keyState;
    }

    public static IEnumerable<VirtualKey> GetValidKeys()
    {
        return Instance._keyState.GetValidVirtualKeys();
    }

    public static bool IsKeyDown(ImGuiKey key)
    {
        return IsKeyDown(ImGuiHelpers.ImGuiKeyToVirtualKey(key));
    }

    public static bool IsKeyDown(VirtualKey key)
    {
        if(!Instance._keyState.IsVirtualKeyValid(key))
            return false;

        return Instance._keyState[key];
    }
}
