using Brio.Input;
using Dalamud.Game.ClientState.Keys;
using System.Collections.Generic;

namespace Brio.Config;

internal class InputConfiguration
{
    public Dictionary<KeyBindEvents, KeyBind> Bindings { get; set; } = new()
    {
        // Default bindings
        { KeyBindEvents.Interface_ToggleBrioWindow, new(VirtualKey.B, true) },
        { KeyBindEvents.Interface_IncrementSmallModifier, new(VirtualKey.NO_KEY) },
        { KeyBindEvents.Interface_IncrementLargeModifier, new(VirtualKey.NO_KEY) },

        { KeyBindEvents.Posing_DisableGizmo, new(VirtualKey.SHIFT) },
        { KeyBindEvents.Posing_DisableSkeleton, new(VirtualKey.CONTROL) },
        { KeyBindEvents.Posing_HideOverlay, new(VirtualKey.MENU) },
    };
}
